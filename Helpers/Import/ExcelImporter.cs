using ClosedXML.Excel;

namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Lee un Excel (.xlsx) a una lista de diccionarios: cada item es una fila,
    /// cada clave es el nombre de la columna (header) y el valor es el texto.
    /// </summary>
    public static class ExcelImporter
    {
        public static ImportFile Read(Stream stream)
        {
            var result = new ImportFile();

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            var usedRange = ws.RangeUsed();
            if (usedRange == null)
            {
                result.Error = "El archivo está vacío";
                return result;
            }

            var firstRow = usedRange.FirstRow();
            var lastRow = usedRange.LastRow();
            int headerRowNum = firstRow.RowNumber();
            int lastRowNum = lastRow.RowNumber();

            // Detectar headers: primera fila con al menos 1 celda no vacía
            int headerRow = headerRowNum;
            for (int r = headerRowNum; r <= Math.Min(headerRowNum + 5, lastRowNum); r++)
            {
                if (ws.Row(r).CellsUsed().Any())
                {
                    headerRow = r;
                    break;
                }
            }

            // Leer headers
            var headers = new List<string>();
            var headerCells = ws.Row(headerRow).CellsUsed().ToList();
            int maxCol = usedRange.LastColumn().ColumnNumber();

            for (int c = 1; c <= maxCol; c++)
            {
                var cellValue = ws.Cell(headerRow, c).GetString().Trim();
                headers.Add(string.IsNullOrEmpty(cellValue) ? $"Columna{c}" : cellValue);
            }

            if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            {
                result.Error = "No se encontraron encabezados en el archivo";
                return result;
            }

            result.Headers = headers;

            // Leer filas
            for (int r = headerRow + 1; r <= lastRowNum; r++)
            {
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool tieneDatos = false;

                for (int c = 1; c <= maxCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    var valor = cell.GetString()?.Trim() ?? "";
                    rowDict[headers[c - 1]] = valor;
                    if (!string.IsNullOrEmpty(valor)) tieneDatos = true;
                }

                if (tieneDatos)
                {
                    result.Rows.Add(rowDict);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Estructura común para archivos importados (Excel o CSV).
    /// </summary>
    public class ImportFile
    {
        public string? Error { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();

        public bool Ok => string.IsNullOrEmpty(Error);
    }
}