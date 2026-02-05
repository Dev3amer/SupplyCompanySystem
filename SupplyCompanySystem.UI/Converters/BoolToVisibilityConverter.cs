using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;

            if (value is bool boolValue)
                isVisible = boolValue;
            else if (value != null)
                isVisible = true;

            bool invert = parameter != null && parameter.ToString() == "Invert";
            if (invert)
                isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}