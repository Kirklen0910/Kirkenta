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
            sb.Append('﻿');

            // Excel en español usa ";" como separador de listas: sin esta línea abre todo en una sola columna
            sb.Append("sep=,\r\n");

            // Headers
            sb.Append(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));
            sb.Append("\r\n");

            // Data (TODAS las celdas se escapan: direcciones con coma, notas con saltos de línea, etc.)
            foreach (var item in items)
            {
                var valores = new List<string>(columns.Count);
                foreach (var col in columns)
                {
                    var value = col.Value(item);
                    valores.Add(EscapeCsv(FormatValue(value, col.Format)));
                }
                sb.Append(string.Join(",", valores));
                sb.Append("\r\n");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string FormatValue(object? value, string? format)
        {
            if (value == null) return "";

            return value switch
            {
                // Sin separador de miles: "1,234.50" dentro de un CSV separado por comas es ambiguo
                decimal dec => dec.ToString(SinMiles(format) ?? "0.00", CultureInfo.InvariantCulture),
                double dbl => dbl.ToString(SinMiles(format) ?? "0.00", CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),
                DateTime dt => dt.ToString(format ?? "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool b => b ? "1" : "0",
                _ => value.ToString() ?? ""
            };
        }

        private static string? SinMiles(string? format) =>
            format?.Replace("#,##0", "0").Replace(",", "");

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
