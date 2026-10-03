using System.Globalization;
using System.Text;

namespace Kirkenta.Helpers.Export
{
    public static class CsvExporter
    {
        public static byte[] Export<T>(IEnumerable<T> items, List<ExportColumn<T>> columns)
        {
            var sb = new StringBuilder();

            // BOM para que Excel detecte UTF-8 (y muestre bien los acentos)
            sb.Append('\uFEFF');

            // Headers
            sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));

            // Data
            foreach (var item in items)
            {
                var valores = new List<string>();
                foreach (var col in columns)
                {
                    var value = col.Value(item);
                    valores.Add(FormatValue(value, col.Format));
                }
                sb.AppendLine(string.Join(",", valores));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string FormatValue(object? value, string? format)
        {
            if (value == null) return "";

            return value switch
            {
                decimal dec => dec.ToString(format ?? "0.00", CultureInfo.InvariantCulture),
                double dbl => dbl.ToString(format ?? "0.00", CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString(format ?? "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "1" : "0",
                _ => value.ToString() ?? ""
            };
        }

        private static string EscapeCsv(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            // Si contiene coma, comilla o salto de línea, hay que entrecomillar
            if (texto.Contains(',') || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r'))
            {
                return "\"" + texto.Replace("\"", "\"\"") + "\"";
            }
            return texto;
        }
    }
}