using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class DecimalInputConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is decimal decimalValue)
            {
                if (decimalValue == Math.Floor(decimalValue))
                {
                    return decimalValue.ToString("0", culture);
                }
                else
                {
                    return decimalValue.ToString("0.##", culture);
                }
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0m;

            string stringValue = value.ToString().Trim();

            stringValue = stringValue.Replace(',', '.');

            if (stringValue == ".")
                stringValue = "0";
            else if (stringValue == "-.")
                stringValue = "0";

            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            if (decimal.TryParse(stringValue, NumberStyles.Any, culture, out result))
            {
                return result;
            }

            return 0m;
        }
    }
}