using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lee un Excel (.xlsx) a una lista de diccionarios: cada item es una fila,
    /// cada clave es el nombre de la columna (header) y el valor es el texto.
    /// - Usa la primera hoja que tenga datos.
    /// - Detecta la fila de encabezados aunque haya títulos arriba de la tabla.
    /// - Ignora columnas totalmente vacías y repara encabezados repetidos.
    /// </summary>
    public static class ExcelImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            using var workbook = new XLWorkbook(stream);

            IXLWorksheet? ws = null;
            IXLRange? usedRange = null;
            foreach (var hoja in workbook.Worksheets)
            {
                var rango = hoja.RangeUsed();
                if (rango != null)
                {
                    ws = hoja;
                    usedRange = rango;
                    break;
                }
            }

            if (ws == null || usedRange == null)
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            int primeraFila = usedRange.FirstRow().RowNumber();
            int ultimaFila = usedRange.LastRow().RowNumber();
            int primeraCol = usedRange.FirstColumn().ColumnNumber();
            int ultimaCol = usedRange.LastColumn().ColumnNumber();

            int headerRow = DetectarFilaEncabezados(ws, primeraFila, ultimaFila, primeraCol, ultimaCol);

            // Columnas que se usarán: con encabezado o con algún dato debajo
            var columnas = new List<int>();
            var nombresOriginales = new List<string>();
            for (int c = primeraCol; c <= ultimaCol; c++)
            {
                var nombre = TextoCelda(ws.Cell(headerRow, c));
                bool tieneDatos = false;

                if (nombre.Length == 0)
                {
                    for (int r = headerRow + 1; r <= ultimaFila && !tieneDatos; r++)
                        tieneDatos = !string.IsNullOrWhiteSpace(TextoCelda(ws.Cell(r, c)));
                    if (!tieneDatos) continue; // columna totalmente vacía
                }

                columnas.Add(c);
                nombresOriginales.Add(nombre.Length == 0 ? $"Columna{c - primeraCol + 1}" : nombre);
            }

            if (columnas.Count == 0)
            {
                result.Error = "No se encontraron encabezados en el archivo";
                return result;
            }

            var headers = ImportFile.HacerHeadersUnicos(nombresOriginales);
            result.Headers = headers;

            for (int r = headerRow + 1; r <= ultimaFila; r++)
            {
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool tieneDatos = false;

                for (int i = 0; i < columnas.Count; i++)
                {
                    var valor = TextoCelda(ws.Cell(r, columnas[i]));
                    rowDict[headers[i]] = valor;
                    if (valor.Length > 0) tieneDatos = true;
                }

                if (tieneDatos)
                    result.Rows.Add(rowDict);
            }

            return result;
        }

        /// <summary>
        /// Convierte una celda a texto SIN depender del idioma del servidor ni del formato visual:
        /// números en formato invariante (sin notación científica), porcentajes como 15 (no 0.15),
        /// ceros a la izquierda conservados (formato 00000), fechas ISO y booleanos true/false.
        /// </summary>
        public static string TextoCelda(IXLCell cell)
        {
            try
            {
                if (cell.IsEmpty()) return "";

                switch (cell.DataType)
                {
                    case XLDataType.Number:
                    {
                        var d = cell.GetDouble();
                        var formato = cell.Style.NumberFormat.Format ?? "";

                        if (formato.Contains('%'))
                            return ((decimal)d * 100m).ToString("0.############", CultureInfo.InvariantCulture);

                        // Formato "00000": el usuario quiere ver los ceros a la izquierda (códigos, teléfonos)
                        if (Regex.IsMatch(formato, @"^0{2,}$"))
                            return d.ToString(new string('0', formato.Length), CultureInfo.InvariantCulture);

                        return d.ToString("0.############", CultureInfo.InvariantCulture);
                    }
                    case XLDataType.DateTime:
                    {
                        var dt = cell.GetDateTime();
                        return dt.TimeOfDay == TimeSpan.Zero
                            ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    case XLDataType.Boolean:
                        return cell.GetBoolean() ? "true" : "false";
                    case XLDataType.TimeSpan:
                        return cell.GetTimeSpan().ToString();
                    default:
                        return (cell.GetString() ?? "").Trim();
                }
            }
            catch
            {
                // Celda con fórmula en error u otro caso raro: lo que se vea en pantalla
                try { return (cell.GetFormattedString() ?? "").Trim(); }
                catch { return ""; }
            }
        }

        /// <summary>
        /// Los archivos reales suelen traer un título ("Listado de clientes") sobre la tabla.
        /// El encabezado es la primera fila (de las primeras 10) que tiene casi tantas celdas
        /// llenas como la fila más llena.
        /// </summary>
        private static int DetectarFilaEncabezados(IXLWorksheet ws, int primeraFila, int ultimaFila, int primeraCol, int ultimaCol)
        {
            int limite = Math.Min(primeraFila + 9, ultimaFila);
            var conteos = new Dictionary<int, int>();

            for (int r = primeraFila; r <= limite; r++)
            {
                int llenas = 0;
                for (int c = primeraCol; c <= ultimaCol; c++)
                {
                    if (!string.IsNullOrWhiteSpace(TextoCelda(ws.Cell(r, c)))) llenas++;
                }
                conteos[r] = llenas;
            }

            int maximo = conteos.Values.DefaultIfEmpty(0).Max();
            if (maximo == 0) return primeraFila;

            int minimo = (int)Math.Ceiling(maximo * 0.6);
            foreach (var par in conteos)
            {
                if (par.Value > 0 && par.Value >= minimo) return par.Key;
            }

            return primeraFila;
        }
    }
}
