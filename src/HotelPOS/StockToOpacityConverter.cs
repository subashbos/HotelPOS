using System.Globalization;
using System.Windows.Data;

namespace HotelPOS
{
    public class StockToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) // NOSONAR
        {
            if (value is int stock && stock <= 0)
            {
                return 0.5; // Dim out-of-stock items
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) // NOSONAR
        {
            throw new NotImplementedException();
        }
    }
}
