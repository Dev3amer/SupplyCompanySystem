using SupplyCompanySystem.Domain.Entities;
using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class StatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InvoiceStatus status)
            {
                return status switch
                {
                    InvoiceStatus.Draft => "مسودة",
                    InvoiceStatus.Completed => "مكتملة",
                    InvoiceStatus.Cancelled => "ملغية",
                    _ => "غير معروفة"
                };
            }
            return "غير معروفة";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusStr)
            {
                return statusStr switch
                {
                    "مسودة" => InvoiceStatus.Draft,
                    "مكتملة" => InvoiceStatus.Completed,
                    "ملغية" => InvoiceStatus.Cancelled,
                    _ => InvoiceStatus.Draft
                };
            }
            return InvoiceStatus.Draft;
        }
    }
}