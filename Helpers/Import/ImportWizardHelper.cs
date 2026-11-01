using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lógica compartida del wizard de importación (paso 1: subir archivo).
    /// Cada módulo llama a estos métodos desde su propia página Import.
    /// </summary>
    public static class ImportWizardHelper
    {
        public const long TamanoMaximoBytes = 20 * 1024 * 1024; // 20 MB
        public const int MaxFilas = 5000;

        /// <summary>Formatos aceptados al subir el archivo.</summary>
        public static readonly string[] ExtensionesPermitidas = { ".xlsx", ".csv", ".tsv", ".txt", ".json" };

        /// <summary>Valor para el atributo accept del input file.</summary>
        public static string ExtensionesAceptadas => string.Join(",", ExtensionesPermitidas);

        /// <summary>Texto para mostrar al usuario.</summary>
        public const string FormatosTexto = "Excel (.xlsx), CSV, TSV/TXT (texto separado) o JSON";

        /// <summary>
        /// Resultado de procesar un archivo subido.
        /// </summary>
        public class ResultadoLectura
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public List<string> Headers { get; set; } = new();
            public List<Dictionary<string, string>> Rows { get; set; } = new();
            public int TotalFilas => Rows.Count;
        }

        /// <summary>
        /// Valida y lee un archivo subido (Excel, CSV, TSV, TXT o JSON).
        /// </summary>
        public static ResultadoLectura ProcesarArchivo(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return new ResultadoLectura { Ok = false, Error = "Debes seleccionar un archivo" };
            }

            if (archivo.Length > TamanoMaximoBytes)
            {
                return new ResultadoLectura { Ok = false, Error = "El archivo no puede pesar más de 20 MB" };
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (extension == ".xls")
            {
                return new ResultadoLectura
                {
                    Ok = false,
                    Error = "El formato antiguo de Excel (.xls) no es compatible. Ábrelo en Excel y usa Guardar como → Libro de Excel (.xlsx) o CSV."
                };
            }

            if (!ExtensionesPermitidas.Contains(extension))
            {
                return new ResultadoLectura
                {
                    Ok = false,
                    Error = $"Formato no permitido. Usa: {FormatosTexto}."
                };
            }

            ImportFile archivoLeido;
            try
            {
                using var stream = archivo.OpenReadStream();
                archivoLeido = extension switch
                {
                    ".xlsx" => ExcelImporter.Read(stream),
                    ".json" => JsonImporter.Read(stream),
                    _ => CsvImporter.Read(stream) // .csv, .tsv, .txt
                };
            }
            catch (Exception)
            {
                return new ResultadoLectura
                {
                    Ok = false,
                    Error = "No se pudo leer el archivo. Verifica que no esté dañado ni protegido con contraseña y que el formato coincida con su extensión."
                };
            }

            if (!archivoLeido.Ok)
            {
                return new ResultadoLectura { Ok = false, Error = archivoLeido.Error ?? "No se pudo leer el archivo" };
            }

            if (archivoLeido.Rows.Count == 0)
            {
                return new ResultadoLectura { Ok = false, Error = "El archivo no contiene filas de datos" };
            }

            if (archivoLeido.Rows.Count > MaxFilas)
            {
                return new ResultadoLectura
                {
                    Ok = false,
                    Error = $"El archivo tiene {archivoLeido.Rows.Count} filas. El máximo es {MaxFilas:N0} filas por importación."
                };
            }

            return new ResultadoLectura
            {
                Ok = true,
                Headers = archivoLeido.Headers,
                Rows = archivoLeido.Rows
            };
        }

        /// <summary>
        /// Guarda headers y filas en la sesión (como JSON).
        /// </summary>
        public static void GuardarEnSesion(ISession session, ImportWizardConfig config,
            List<string> headers, List<Dictionary<string, string>> rows)
        {
            session.SetString(config.SessionHeadersKey, JsonSerializer.Serialize(headers));
            session.SetString(config.SessionRowsKey, JsonSerializer.Serialize(rows));
        }

        /// <summary>
        /// Recupera headers y filas de la sesión.
        /// Devuelve null si no hay datos (sesión expirada).
        /// </summary>
        public static (List<string> Headers, List<Dictionary<string, string>> Rows)? RecuperarDeSesion(
            ISession session, ImportWizardConfig config)
        {
            var jsonHeaders = session.GetString(config.SessionHeadersKey);
            var jsonRows = session.GetString(config.SessionRowsKey);

            if (string.IsNullOrEmpty(jsonHeaders) || string.IsNullOrEmpty(jsonRows))
                return null;

            try
            {
                var headers = JsonSerializer.Deserialize<List<string>>(jsonHeaders) ?? new();
                var rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();
                return (headers, rows);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Limpia los datos del wizard en la sesión.
        /// </summary>
        public static void LimpiarSesion(ISession session, ImportWizardConfig config)
        {
            session.Remove(config.SessionHeadersKey);
            session.Remove(config.SessionRowsKey);
        }
    }
}