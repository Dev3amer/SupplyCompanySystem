using SupplyCompanySystem.Domain.Entities;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SupplyCompanySystem.UI.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InvoiceStatus status)
            {
                return status switch
                {
                    InvoiceStatus.Draft => new SolidColorBrush(Colors.Orange),
                    InvoiceStatus.Completed => new SolidColorBrush(Colors.Green),
                    InvoiceStatus.Cancelled => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}