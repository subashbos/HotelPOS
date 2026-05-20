using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using HotelPOS;
using Xunit;

namespace HotelPOS.Tests
{
    public class ConverterTests
    {
        [Fact]
        public void IdToEnabledConverter_StockCheck_ReturnsFalse_WhenOut()
        {
            var converter = new IdToEnabledConverter();
            var result = converter.Convert(new object[] { 0, true }, typeof(bool), "StockCheck", CultureInfo.InvariantCulture);
            Assert.False((bool)result);
        }

        [Fact]
        public void IdToEnabledConverter_StockCheck_ReturnsTrue_WhenAvailable()
        {
            var converter = new IdToEnabledConverter();
            var result = converter.Convert(new object[] { 10, true }, typeof(bool), "StockCheck", CultureInfo.InvariantCulture);
            Assert.True((bool)result);
        }

        [Fact]
        public void IdToEnabledConverter_StockCheck_ReturnsTrue_WhenNotTracking()
        {
            var converter = new IdToEnabledConverter();
            var result = converter.Convert(new object[] { 0, false }, typeof(bool), "StockCheck", CultureInfo.InvariantCulture);
            Assert.True((bool)result);
        }

        [Fact]
        public void StockToOpacityConverter_ReturnsDim_WhenOut()
        {
            var converter = new StockToOpacityConverter();
            var result = converter.Convert(0, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.5, (double)result);
        }

        [Fact]
        public void StockToOpacityConverter_ReturnsFull_WhenAvailable()
        {
            var converter = new StockToOpacityConverter();
            var result = converter.Convert(5, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(1.0, (double)result);
        }
    }
}
