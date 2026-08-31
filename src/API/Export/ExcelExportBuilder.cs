using ClosedXML.Excel;

namespace HotelPOS.Api.Export
{
    /// <summary>One worksheet's worth of export data: a name, column headers, and rows of cell
    /// values (string, int, decimal or double - anything else is written via ToString()).</summary>
    public sealed record ExcelSheet(string Name, IReadOnlyList<string> Headers, IEnumerable<IReadOnlyList<object?>> Rows);

    /// <summary>
    /// Builds a styled .xlsx workbook (bold white-on-#173F5F header row, columns auto-sized) from
    /// one or more sheets of already-computed report data. Consolidates the per-report ClosedXML
    /// boilerplate previously duplicated across every WPF report view's own Export_Click handler.
    /// </summary>
    public static class ExcelExportBuilder
    {
        public static byte[] Build(params ExcelSheet[] sheets)
        {
            using var wb = new XLWorkbook();

            foreach (var sheet in sheets)
            {
                var ws = wb.Worksheets.Add(sheet.Name);

                for (int c = 0; c < sheet.Headers.Count; c++)
                    ws.Cell(1, c + 1).Value = sheet.Headers[c];

                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                headerRow.Style.Font.FontColor = XLColor.White;

                int r = 2;
                foreach (var row in sheet.Rows)
                {
                    for (int c = 0; c < row.Count; c++)
                        SetCell(ws.Cell(r, c + 1), row[c]);
                    r++;
                }

                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetCell(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    break;
                case string s:
                    cell.Value = s.ForSpreadsheet();
                    break;
                case decimal dec:
                    cell.Value = (double)dec;
                    break;
                case double d:
                    cell.Value = d;
                    break;
                case int i:
                    cell.Value = i;
                    break;
                default:
                    cell.Value = value.ToString()!.ForSpreadsheet();
                    break;
            }
        }
    }
}
