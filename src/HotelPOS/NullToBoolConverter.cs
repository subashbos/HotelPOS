using System.Globalization;
using System.Windows.Data;

namespace HotelPOS
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) // NOSONAR
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) // NOSONAR
        {
            throw new NotImplementedException();
        }
    }
}
