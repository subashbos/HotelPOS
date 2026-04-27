using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HotelPOS
{
    public class StockToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0) return new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)); // Red for out
                if (stock < 5) return new SolidColorBrush(Color.FromRgb(0xD3, 0x54, 0x00)); // Orange for low
            }
            return new SolidColorBrush(Color.FromRgb(0x00, 0xA8, 0x96)); // Teal for OK
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
