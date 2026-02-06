using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class CollectionCountToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0;
            }
            else if (value is System.Collections.ICollection collection)
            {
                return collection.Count > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}