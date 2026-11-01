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
        // Sin topes de tamaño ni de filas: las filas se guardan en disco, no en la sesión
        public const long TamanoMaximoBytes = long.MaxValue;
        public const int MaxFilas = int.MaxValue;

        /// <summary>Formatos aceptados al subir el archivo.</summary>
        public static readonly string[] ExtensionesPermitidas = { ".xlsx", ".xls", ".csv", ".tsv", ".txt", ".json" };

        /// <summary>Valor para el atributo accept del input file.</summary>
        public static string ExtensionesAceptadas => string.Join(",", ExtensionesPermitidas);

        /// <summary>Texto para mostrar al usuario.</summary>
        public const string FormatosTexto = "Excel (.xlsx o .xls), CSV, TSV/TXT (texto separado) o JSON";

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

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

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
                    ".xls" => LeerXls(stream),
                    ".json" => JsonImporter.Read(stream),
                    _ => CsvImporter.Read(stream) // .csv, .tsv, .txt
                };
                // «1.234» en una columna que usa coma decimal es mil doscientos treinta y cuatro, no 1.234
                Kirkenta.Helpers.Migracion.MigracionFormatos.NormalizarNumerosAmbiguos(archivoLeido.Headers, archivoLeido.Rows);
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

        /// <summary>Excel 97-2003: la primera hoja con datos.</summary>
        private static ImportFile LeerXls(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var r = new Kirkenta.Helpers.Migracion.ResultadoMigracionLectura();
            Kirkenta.Helpers.Migracion.MigracionBasesDatos.LeerXls(ms, r);
            var hoja = r.Tablas.FirstOrDefault();
            if (hoja == null) return new ImportFile { Error = r.Avisos.FirstOrDefault() ?? "El archivo no tiene hojas con datos" };
            return new ImportFile { Headers = hoja.Headers, Rows = hoja.Rows };
        }

        private const string MarcaArchivo = "@archivo:";

        private static string CarpetaTemporal()
        {
            var c = Path.Combine(Path.GetTempPath(), "kirkenta-importacion");
            Directory.CreateDirectory(c);
            return c;
        }

        /// <summary>
        /// Guarda headers y filas: los encabezados en la sesión y las filas en un temporal en disco
        /// (la sesión vive en memoria y no está hecha para archivos grandes).
        /// </summary>
        public static void GuardarEnSesion(ISession session, ImportWizardConfig config,
            List<string> headers, List<Dictionary<string, string>> rows)
        {
            BorrarTemporal(session.GetString(config.SessionRowsKey));
            try
            {
                foreach (var f in Directory.GetFiles(CarpetaTemporal(), "*.json"))
                    if (File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddHours(-12)) File.Delete(f);
            }
            catch { /* limpieza best-effort */ }
            var token = Guid.NewGuid().ToString("N");
            using (var fs = File.Create(Path.Combine(CarpetaTemporal(), token + ".json")))
                JsonSerializer.Serialize(fs, rows);
            session.SetString(config.SessionHeadersKey, JsonSerializer.Serialize(headers));
            session.SetString(config.SessionRowsKey, MarcaArchivo + token);
        }

        private static string? RutaTemporal(string? valor)
        {
            if (valor == null || !valor.StartsWith(MarcaArchivo)) return null;
            var token = valor.Substring(MarcaArchivo.Length);
            return Guid.TryParseExact(token, "N", out _) ? Path.Combine(CarpetaTemporal(), token + ".json") : null;
        }

        private static void BorrarTemporal(string? valor)
        {
            var ruta = RutaTemporal(valor);
            if (ruta != null) try { File.Delete(ruta); } catch { /* ignorar */ }
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
                List<Dictionary<string, string>> rows;
                if (jsonRows.StartsWith(MarcaArchivo))
                {
                    var ruta = RutaTemporal(jsonRows);
                    if (ruta == null || !File.Exists(ruta)) return null;
                    using var fs = File.OpenRead(ruta);
                    rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(fs) ?? new();
                }
                else rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonRows) ?? new();
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
            BorrarTemporal(session.GetString(config.SessionRowsKey));
            session.Remove(config.SessionHeadersKey);
            session.Remove(config.SessionRowsKey);
        }
    }
}