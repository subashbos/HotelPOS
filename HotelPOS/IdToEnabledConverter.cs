using System.Globalization;
using System.Windows.Data;

namespace HotelPOS
{
    public class IdToEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return true;
            if (values[0] == null || values[1] == null) return true;

            // Enabled if Ids are NOT equal
            return !values[0].Equals(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
