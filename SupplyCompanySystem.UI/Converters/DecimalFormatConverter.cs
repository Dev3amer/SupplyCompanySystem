using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is decimal decimalValue)
            {
                // إذا كان الرقم صحيحاً (لا يوجد كسور)
                if (decimalValue == Math.Floor(decimalValue))
                {
                    // إرجاع الرقم بدون كسور
                    return decimalValue.ToString("0", culture);
                }
                else
                {
                    // إرجاع الرقم مع منزلتين عشريتين
                    return decimalValue.ToString("0.00", culture);
                }
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0m;

            string stringValue = value.ToString().Trim();

            // ✅ السماح بالنقطة والفاصلة كفاصل عشري
            // استبدال الفاصلة بنقطة للتحويل
            stringValue = stringValue.Replace(',', '.');

            // ✅ السماح بإدخال النقطة العشرية وحدها (مثل "10.")
            // إذا انتهى النص بنقطة، أضف صفر
            if (stringValue.EndsWith("."))
            {
                stringValue += "0";
            }

            // محاولة التحويل إلى decimal
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            return 0m;
        }
    }
}