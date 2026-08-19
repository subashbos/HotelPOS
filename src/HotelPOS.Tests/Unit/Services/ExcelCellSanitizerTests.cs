using HotelPOS.Services;
using Xunit;

namespace HotelPOS.Tests
{
    public class ExcelCellSanitizerTests
    {
        [Theory]
        [InlineData("=cmd|'/C calc'!A1")]
        [InlineData("+1+1")]
        [InlineData("-1+1")]
        [InlineData("@SUM(A1:A2)")]
        [InlineData("\tHidden")]
        public void ForSpreadsheet_FormulaTriggerPrefix_GetsQuoted(string value)
        {
            var result = value.ForSpreadsheet();

            Assert.StartsWith("'", result);
            Assert.Equal("'" + value, result);
        }

        [Theory]
        [InlineData("Acme Corp")]
        [InlineData("Table 5")]
        [InlineData("Milk Packet 1L")]
        public void ForSpreadsheet_OrdinaryText_IsUnchanged(string value)
        {
            Assert.Equal(value, value.ForSpreadsheet());
        }

        [Fact]
        public void ForSpreadsheet_NullOrEmpty_ReturnsEmptyString()
        {
            string? nullValue = null;
            Assert.Equal(string.Empty, nullValue.ForSpreadsheet());
            Assert.Equal(string.Empty, "".ForSpreadsheet());
        }
    }
}
