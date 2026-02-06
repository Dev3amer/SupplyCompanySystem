using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNotNull = value != null;

            if (parameter is string paramString)
            {
                if (paramString.Contains("|"))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 2)
                    {
                        return isNotNull ? parts[0] : parts[1];
                    }
                }

                if (paramString.StartsWith("#") && paramString.Contains("|"))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 2)
                    {
                        return isNotNull ? parts[0] : parts[1];
                    }
                }

                return isNotNull ? paramString : null;
            }

            return isNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}