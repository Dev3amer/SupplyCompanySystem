using SupplyCompanySystem.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class ReportTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReportType reportType && parameter is string reportTypeParam)
            {
                if (reportTypeParam == "LeastProducts")
                    return reportType == ReportType.LeastProducts ? Visibility.Visible : Visibility.Collapsed;
                else if (reportTypeParam == "TopInvoiceCustomers")
                    return reportType == ReportType.TopInvoiceCustomers ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}