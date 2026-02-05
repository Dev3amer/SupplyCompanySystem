using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class DisplayMemberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // إذا كان هناك DisplayMemberPath
            if (parameter is string displayPath && !string.IsNullOrEmpty(displayPath))
            {
                try
                {
                    var property = value.GetType().GetProperty(displayPath);
                    if (property != null)
                    {
                        return property.GetValue(value)?.ToString() ?? string.Empty;
                    }
                }
                catch
                {
                    // في حالة الخطأ، نعود للـ ToString العادي
                }
            }

            // محاولة استخدام خاصية Name إذا كان موجوداً
            var nameProperty = value.GetType().GetProperty("Name");
            if (nameProperty != null)
            {
                return nameProperty.GetValue(value)?.ToString() ?? string.Empty;
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}