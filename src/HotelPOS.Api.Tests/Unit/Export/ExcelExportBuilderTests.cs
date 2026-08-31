using System.IO;
using ClosedXML.Excel;
using HotelPOS.Api.Export;
using Xunit;

namespace HotelPOS.Tests.Unit.Export
{
    public class ExcelExportBuilderTests
    {
        private static XLWorkbook Load(byte[] bytes) => new(new MemoryStream(bytes));

        [Fact]
        public void Build_WritesHeadersAndTypedRowValues()
        {
            var sheet = new ExcelSheet(
                "Sales Report",
                new[] { "Item", "Qty", "Price" },
                new[] { (IReadOnlyList<object?>)new object?[] { "Coffee", 3, 45.5m } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));
            var ws = wb.Worksheet("Sales Report");

            Assert.Equal("Item", ws.Cell(1, 1).GetString());
            Assert.Equal("Qty", ws.Cell(1, 2).GetString());
            Assert.Equal("Price", ws.Cell(1, 3).GetString());

            Assert.Equal("Coffee", ws.Cell(2, 1).GetString());
            Assert.Equal(3, ws.Cell(2, 2).GetValue<int>());
            Assert.Equal(45.5, ws.Cell(2, 3).GetDouble());
        }

        [Fact]
        public void Build_StylesHeaderRow_BoldWhiteOnDarkBlue()
        {
            var sheet = new ExcelSheet("Report", new[] { "Col" }, new[] { (IReadOnlyList<object?>)new object?[] { "x" } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));
            var header = wb.Worksheet("Report").Row(1);

            Assert.True(header.Style.Font.Bold);
            Assert.Equal(XLColor.White, header.Style.Font.FontColor);
            Assert.Equal(XLColor.FromHtml("#173F5F"), header.Style.Fill.BackgroundColor);
        }

        [Fact]
        public void Build_MultipleSheets_PreservesNamesOrderAndData()
        {
            var first = new ExcelSheet("B2B Invoices", new[] { "A" }, new[] { (IReadOnlyList<object?>)new object?[] { "one" } });
            var second = new ExcelSheet("B2C Summary", new[] { "B" }, new[] { (IReadOnlyList<object?>)new object?[] { "two" } });

            using var wb = Load(ExcelExportBuilder.Build(first, second));

            Assert.Equal(2, wb.Worksheets.Count);
            Assert.Equal(new[] { "B2B Invoices", "B2C Summary" }, wb.Worksheets.Select(w => w.Name));
            Assert.Equal("one", wb.Worksheet("B2B Invoices").Cell(2, 1).GetString());
            Assert.Equal("two", wb.Worksheet("B2C Summary").Cell(2, 1).GetString());
        }

        [Fact]
        public void Build_NullCellValue_LeavesCellBlank()
        {
            var sheet = new ExcelSheet("Report", new[] { "Col" }, new[] { (IReadOnlyList<object?>)new object?[] { null } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));

            Assert.True(wb.Worksheet("Report").Cell(2, 1).IsEmpty());
        }

        [Theory]
        [InlineData("=SUM(A1:A9)")]
        [InlineData("+cmd|' /c calc'!A0")]
        [InlineData("-1+1")]
        [InlineData("@SUM(1,1)")]
        public void Build_StringStartingWithFormulaTrigger_IsStoredAsLiteralTextNotFormula(string maliciousValue)
        {
            var sheet = new ExcelSheet("Report", new[] { "Col" }, new[] { (IReadOnlyList<object?>)new object?[] { maliciousValue } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));
            var cell = wb.Worksheet("Report").Cell(2, 1);

            // The apostrophe prefix forces Excel's "text quote" interpretation, so the leading
            // trigger character never gets evaluated as a formula — ClosedXML strips the quote
            // marker back out when reading the cell as a string, same as Excel would display it.
            Assert.False(cell.HasFormula);
            Assert.Equal(XLDataType.Text, cell.DataType);
            Assert.Equal(maliciousValue, cell.GetString());
        }

        [Fact]
        public void Build_OrdinaryString_IsWrittenVerbatim()
        {
            var sheet = new ExcelSheet("Report", new[] { "Col" }, new[] { (IReadOnlyList<object?>)new object?[] { "Regular Customer" } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));

            Assert.Equal("Regular Customer", wb.Worksheet("Report").Cell(2, 1).GetString());
        }

        [Fact]
        public void Build_UnrecognizedValueType_FallsBackToSanitizedToString()
        {
            var sheet = new ExcelSheet("Report", new[] { "Col" }, new[] { (IReadOnlyList<object?>)new object?[] { 'x' } });

            using var wb = Load(ExcelExportBuilder.Build(sheet));

            Assert.Equal("x", wb.Worksheet("Report").Cell(2, 1).GetString());
        }
    }
}
