using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HotelPOS
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool track && track)
            {
                return new SolidColorBrush(Color.FromRgb(0x00, 0xA8, 0x96)); // Teal for tracked
            }
            return new SolidColorBrush(Color.FromRgb(0xA0, 0xAD, 0xB8)); // Muted for untracked
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
         
            throw new NotImplementedException();
        }
    }
}
