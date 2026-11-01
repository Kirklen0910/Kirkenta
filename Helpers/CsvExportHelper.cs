using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Kirkenta.Helpers
{
    /// <summary>Exporta filas a CSV (UTF-8 con BOM, para que Excel respete los acentos).</summary>
    public static class CsvExportHelper
    {
        public static FileContentResult Crear(string nombreArchivo, IEnumerable<string?[]> filas)
        {
            var sb = new StringBuilder();
            // Excel en español separa listas con ";": sin esta línea abre todo en una sola columna
            sb.Append("sep=,\r\n");
            foreach (var fila in filas)
                sb.AppendLine(string.Join(",", fila.Select(Escapar)));

            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return new FileContentResult(bytes, "text/csv") { FileDownloadName = nombreArchivo };
        }

        private static string Escapar(string? valor)
        {
            var v = valor ?? "";
            if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }
    }
}
