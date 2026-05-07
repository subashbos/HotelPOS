using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HotelPOS
{
    public class StockToColorMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int stock && values[1] is bool track)
            {
                if (!track) return new SolidColorBrush(Color.FromRgb(0xA0, 0xAD, 0xB8)); // Muted

                if (stock <= 0) return new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)); // Red
                if (stock < 5) return new SolidColorBrush(Color.FromRgb(0xD3, 0x54, 0x00)); // Orange
                return new SolidColorBrush(Color.FromRgb(0x00, 0xA8, 0x96)); // Teal
            }
            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
