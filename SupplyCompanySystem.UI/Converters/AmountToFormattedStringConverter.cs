using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class AmountToFormattedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount >= 1000000)
                    return $"{(amount / 1000000):0.0} مليون";
                else if (amount >= 1000)
                    return $"{(amount / 1000):0.0} ألف";
                else
                    return $"{amount:0}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}