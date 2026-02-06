using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    /// <summary>
    /// Converter للتعامل مع إدخال وعرض الأرقام العشرية
    /// - يعرض الأرقام الصحيحة بدون منازل عشرية
    /// - يسمح بإدخال النقطة والفاصلة كفاصل عشري
    /// </summary>
    public class DecimalInputConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is decimal decimalValue)
            {
                // ✅ في وضع العرض: تنسيق الأرقام
                if (decimalValue == Math.Floor(decimalValue))
                {
                    // رقم صحيح: عرض بدون منازل عشرية
                    return decimalValue.ToString("0", culture);
                }
                else
                {
                    // رقم عشري: عرض بحد أقصى منزلتين عشريتين
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

            // ✅ استبدال الفاصلة بالنقطة للتحويل
            stringValue = stringValue.Replace(',', '.');

            // ✅ السماح بإدخال النقطة لوحدها
            if (stringValue == ".")
                stringValue = "0";
            else if (stringValue == "-.")
                stringValue = "0";

            // ✅ محاولة التحويل مع CultureInfo.InvariantCulture
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            // ✅ المحاولة مع الثقافة الحالية
            if (decimal.TryParse(stringValue, NumberStyles.Any, culture, out result))
            {
                return result;
            }

            return 0m;
        }
    }
}