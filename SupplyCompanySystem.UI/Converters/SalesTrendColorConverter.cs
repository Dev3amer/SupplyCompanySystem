using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class SalesTrendColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string trend)
            {
                return trend switch
                {
                    "زيادة" => System.Windows.Media.Brushes.Green,
                    "انخفاض" => System.Windows.Media.Brushes.Red,
                    "ثابت" => System.Windows.Media.Brushes.Orange,
                    "جديد" => System.Windows.Media.Brushes.Blue,
                    "لا توجد مبيعات" => System.Windows.Media.Brushes.Gray,
                    _ => System.Windows.Media.Brushes.Gray
                };
            }
            return System.Windows.Media.Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}