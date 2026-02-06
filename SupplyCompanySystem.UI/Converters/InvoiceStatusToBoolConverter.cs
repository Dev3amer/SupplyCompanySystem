using SupplyCompanySystem.Domain.Entities;
using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class InvoiceStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Invoice invoice)
            {
                // يعود true فقط إذا كانت الفاتورة مكتملة
                return invoice.Status == InvoiceStatus.Completed;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}