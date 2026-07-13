using System.Globalization;
using System.Windows.Data;

namespace HotelPOS
{
    public class IdToEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) // NOSONAR
        {
            if (parameter?.ToString() == "StockCheck")
            {
                if (values.Length < 2) return true;
                if (values[0] is int stock && values[1] is bool track && track && stock <= 0) return false;
                return true;
            }

            if (values.Length < 2) return true;
            if (values[0] == null || values[1] == null) return true;

            // Enabled if Ids are NOT equal (used for active tab styling/enabling)
            return !values[0].Equals(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) // NOSONAR
        {
            throw new NotImplementedException();
        }
    }
}
