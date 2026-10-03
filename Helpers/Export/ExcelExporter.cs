using ClosedXML.Excel;

namespace Kirkenta.Helpers.Export
{
    public static class ExcelExporter
    {
        public static byte[] Export<T>(
            IEnumerable<T> items,
            List<ExportColumn<T>> columns,
            string sheetName = "Datos",
            string? titulo = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            int headerRow = 1;
            int dataStartRow = 2;

            if (!string.IsNullOrWhiteSpace(titulo))
            {
                var tituloCell = ws.Cell(1, 1);
                tituloCell.Value = titulo;
                tituloCell.Style.Font.Bold = true;
                tituloCell.Style.Font.FontSize = 14;
                tituloCell.Style.Font.FontColor = XLColor.FromHtml("#4f46e5");
                ws.Range(1, 1, 1, columns.Count).Merge();

                headerRow = 2;
                dataStartRow = 3;
            }

            for (int c = 0; c < columns.Count; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Column(c + 1).Width = columns[c].Width;
            }
            ws.Row(headerRow).Height = 22;

            int row = dataStartRow;
            foreach (var item in items)
            {
                for (int c = 0; c < columns.Count; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    var value = columns[c].Value(item);

                    if (value == null)
                    {
                        cell.Value = "";
                        continue;
                    }

                    switch (value)
                    {
                        case decimal dec:
                            cell.Value = dec;
                            cell.Style.NumberFormat.Format = columns[c].Format ?? "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case double dbl:
                            cell.Value = dbl;
                            cell.Style.NumberFormat.Format = columns[c].Format ?? "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case int i:
                            cell.Value = i;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = columns[c].Format ?? "dd/MM/yyyy HH:mm";
                            break;
                        case bool b:
                            cell.Value = b ? "Sí" : "No";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            break;
                        default:
                            cell.Value = value.ToString();
                            break;
                    }
                }
                row++;
            }

            if (row > dataStartRow)
            {
                var dataRange = ws.Range(headerRow, 1, row - 1, columns.Count);
                var borderColor = XLColor.FromHtml("#e5e7eb");

                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                dataRange.Style.Border.TopBorderColor = borderColor;
                dataRange.Style.Border.BottomBorderColor = borderColor;
                dataRange.Style.Border.LeftBorderColor = borderColor;
                dataRange.Style.Border.RightBorderColor = borderColor;
                dataRange.Style.Border.InsideBorderColor = borderColor;

                ws.Range(headerRow, 1, row - 1, columns.Count).SetAutoFilter();
                ws.SheetView.FreezeRows(headerRow);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public static byte[] Plantilla<T>(
            List<ExportColumn<T>> columns,
            string sheetName = "Plantilla",
            string? titulo = null)
        {
            return Export(new List<T>(), columns, sheetName, titulo);
        }
    }
}