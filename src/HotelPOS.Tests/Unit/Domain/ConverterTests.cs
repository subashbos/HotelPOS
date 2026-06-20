using System.Globalization;
using System.Windows;
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
            var result = converter.Convert(0, typeof(double), null!, CultureInfo.InvariantCulture);
            Assert.Equal(0.5, (double)result);
        }

        [Fact]
        public void StockToOpacityConverter_ReturnsFull_WhenAvailable()
        {
            var converter = new StockToOpacityConverter();
            var result = converter.Convert(5, typeof(double), null!, CultureInfo.InvariantCulture);
            Assert.Equal(1.0, (double)result);
        }

        [Theory]
        [InlineData(true, 0x00, 0xA8, 0x96)] // Teal for tracked
        [InlineData(false, 0xA0, 0xAD, 0xB8)] // Muted for untracked
        public void BoolToColorConverter_ReturnsCorrectBrush(bool track, byte r, byte g, byte b)
        {
            var converter = new BoolToColorConverter();
            var brush = (SolidColorBrush)converter.Convert(track, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(r, brush.Color.R);
            Assert.Equal(g, brush.Color.G);
            Assert.Equal(b, brush.Color.B);
        }

        [Theory]
        [InlineData(true, null, Visibility.Visible)]
        [InlineData(false, null, Visibility.Collapsed)]
        [InlineData(true, "Inverted", Visibility.Collapsed)]
        [InlineData(false, "Inverted", Visibility.Visible)]
        [InlineData(5, null, Visibility.Visible)]
        [InlineData(0, null, Visibility.Collapsed)]
        public void BoolToVisibilityConverter_ReturnsCorrectVisibility(object val, string? param, Visibility expected)
        {
            var converter = new BoolToVisibilityConverter();
            var result = (Visibility)converter.Convert(val, typeof(Visibility), param!, CultureInfo.InvariantCulture);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, 0xC0, 0x39, 0x2B)] // Red for out of stock
        [InlineData(3, 0xD3, 0x54, 0x00)] // Orange for low stock
        [InlineData(10, 0x00, 0xA8, 0x96)] // Teal for OK stock
        public void StockToColorConverter_ReturnsCorrectBrush(int stock, byte r, byte g, byte b)
        {
            var converter = new StockToColorConverter();
            var brush = (SolidColorBrush)converter.Convert(stock, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(r, brush.Color.R);
            Assert.Equal(g, brush.Color.G);
            Assert.Equal(b, brush.Color.B);
        }

        [Fact]
        public void StockToColorMultiConverter_ReturnsCorrectBrush()
        {
            var converter = new StockToColorMultiConverter();
            
            // Track false -> Muted
            var brush = (SolidColorBrush)converter.Convert(new object[] { 0, false }, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(0xA0, brush.Color.R);

            // Track true, stock <= 0 -> Red
            brush = (SolidColorBrush)converter.Convert(new object[] { 0, true }, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(0xC0, brush.Color.R);

            // Track true, stock 3 -> Orange
            brush = (SolidColorBrush)converter.Convert(new object[] { 3, true }, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(0xD3, brush.Color.R);

            // Track true, stock 10 -> Teal
            brush = (SolidColorBrush)converter.Convert(new object[] { 10, true }, typeof(Brush), null!, CultureInfo.InvariantCulture);
            Assert.Equal(0x00, brush.Color.R);
        }
    }
}

