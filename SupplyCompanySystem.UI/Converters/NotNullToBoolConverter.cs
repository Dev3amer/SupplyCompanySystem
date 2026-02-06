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
                // ✅ معالجة المعاملات مثل: "valueIfTrue|valueIfFalse"
                if (paramString.Contains("|"))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 2)
                    {
                        // ✅ إرجاع القيمة المناسبة بناءً على null أو not null
                        return isNotNull ? parts[0] : parts[1];
                    }
                }

                // ✅ إذا كان المعامل لون (يبدأ بـ #)
                if (paramString.StartsWith("#") && paramString.Contains("|"))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 2)
                    {
                        return isNotNull ? parts[0] : parts[1];
                    }
                }

                // ✅ إذا كان المعامل كلمة واحدة، نستخدمها لـ true ونرجع null لـ false
                return isNotNull ? paramString : null;
            }

            // ✅ الإرجاع الافتراضي: true/false
            return isNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}