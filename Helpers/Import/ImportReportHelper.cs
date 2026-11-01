using System.Text;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kirkenta.Helpers.Import
{
    /// <summary>Una fila que falló (o generó advertencia) en la importación, con sus datos originales.</summary>
    public class ImportFilaReporte
    {
        public int Fila { get; set; }
        public string Tipo { get; set; } = "Error";
        public string Mensaje { get; set; } = "";
        public Dictionary<string, string> Datos { get; set; } = new();
    }

    /// <summary>Resumen que se guarda en sesión para mostrar la pantalla de resultado y descargar el reporte.</summary>
    public class ImportResumenSesion
    {
        public string Resumen { get; set; } = "";
        public string UrlIndex { get; set; } = "";
        public int Errores { get; set; }
        public int Advertencias { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<ImportFilaReporte> Filas { get; set; } = new();
    }

    /// <summary>
    /// Cierre del wizard de importación:
    /// - Si todo salió bien: mensaje de éxito y regreso al listado (como antes).
    /// - Si hubo errores o advertencias: pantalla de resultado con descarga de un CSV
    ///   que trae las filas fallidas con sus datos originales + el motivo, para corregirlas
    ///   y volver a subirlas.
    /// También genera la plantilla CSV descargable de cada importador.
    /// </summary>
    public static class ImportReportHelper
    {
        public const string SessionKey = "ImportUltimoResultado";
        private const int MaxFilasReporte = 5000;

        public static IActionResult Finalizar(
            PageModel page,
            string urlIndex,
            ImportResult resultado,
            List<string> headersArchivo,
            List<Dictionary<string, string>> filasArchivo)
        {
            if (resultado.Errores.Count == 0 && resultado.Advertencias.Count == 0)
            {
                page.TempData["Success"] = $"Importación completada: {resultado.Resumen()}";
                return new RedirectResult(urlIndex);
            }

            var resumen = new ImportResumenSesion
            {
                Resumen = resultado.Resumen(),
                UrlIndex = urlIndex,
                Errores = resultado.Errores.Count,
                Advertencias = resultado.Advertencias.Count,
                Headers = headersArchivo
            };

            void Agregar(IEnumerable<ImportRowError> items, string tipo)
            {
                foreach (var e in items)
                {
                    if (resumen.Filas.Count >= MaxFilasReporte) break;
                    // Las filas se numeran como en Excel: la 1 es el encabezado, los datos empiezan en la 2
                    var idx = e.NumeroFila - 2;
                    var datos = idx >= 0 && idx < filasArchivo.Count
                        ? filasArchivo[idx]
                        : new Dictionary<string, string>();
                    resumen.Filas.Add(new ImportFilaReporte
                    {
                        Fila = e.NumeroFila,
                        Tipo = tipo,
                        Mensaje = e.Mensaje,
                        Datos = datos
                    });
                }
            }

            Agregar(resultado.Errores, "Error");
            Agregar(resultado.Advertencias, "Advertencia");
            resumen.Filas = resumen.Filas.OrderBy(f => f.Fila).ToList();

            page.HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(resumen));
            return new RedirectToPageResult("/Import/Resultado");
        }

        public static ImportResumenSesion? Recuperar(ISession session)
        {
            var json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<ImportResumenSesion>(json); }
            catch { return null; }
        }

        /// <summary>
        /// CSV con las filas fallidas: columna "Fila", "Tipo", "Motivo" y luego las columnas
        /// originales del archivo (para corregir y volver a importar).
        /// </summary>
        public static byte[] ErroresCsv(ImportResumenSesion r)
        {
            var sb = new StringBuilder();
            sb.Append("sep=,\r\n"); // Excel en español: separa en columnas al abrir con doble clic
            var cabecera = new List<string> { "Fila", "Tipo", "Motivo" };
            cabecera.AddRange(r.Headers);
            sb.AppendLine(string.Join(",", cabecera.Select(Escapar)));

            foreach (var f in r.Filas)
            {
                var celdas = new List<string> { f.Fila.ToString(), f.Tipo, f.Mensaje };
                foreach (var h in r.Headers)
                    celdas.Add(f.Datos.GetValueOrDefault(h) ?? "");
                sb.AppendLine(string.Join(",", celdas.Select(Escapar)));
            }

            return ConBom(sb.ToString());
        }

        /// <summary>CSV de plantilla: solo la fila de encabezados con los nombres de los campos.</summary>
        public static byte[] PlantillaCsv(IEnumerable<string> etiquetas)
        {
            var limpias = etiquetas
                .Select(e => Regex.Replace(e, @"\s*\(.*?\)", "").Trim()) // "Activa (Sí/No)" -> "Activa"
                .Where(e => e.Length > 0);
            return ConBom("sep=,\r\n" + string.Join(",", limpias.Select(Escapar)) + "\r\n");
        }

        /// <summary>
        /// Plantilla en Excel (.xlsx): una columna por campo, con encabezados reales.
        /// (El CSV con comas se abre en UNA sola celda en Excel con configuración regional en español,
        /// porque ahí el separador de listas es el punto y coma.)
        /// </summary>
        public static byte[] PlantillaXlsx(IEnumerable<string> etiquetas, string hoja = "Plantilla")
        {
            var limpias = etiquetas
                .Select(e => Regex.Replace(e, @"\s*\(.*?\)", "").Trim())
                .Where(e => e.Length > 0)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(hoja);
            for (int i = 0; i < limpias.Count; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = limpias[i];
                c.Style.Font.Bold = true;
                c.Style.Font.FontColor = XLColor.White;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                // Texto: así SKU, RTN y teléfonos con ceros a la izquierda no se pierden al escribirlos
                ws.Column(i + 1).Style.NumberFormat.Format = "@";
                ws.Column(i + 1).Width = Math.Max(14, limpias[i].Length + 4);
            }
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static byte[] ConBom(string texto)
        {
            var preamble = new UTF8Encoding(true).GetPreamble(); // Excel necesita el BOM para leer acentos
            var cuerpo = Encoding.UTF8.GetBytes(texto);
            return preamble.Concat(cuerpo).ToArray();
        }

        private static string Escapar(string? valor)
        {
            valor ??= "";
            if (valor.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            return valor;
        }
    }
}
