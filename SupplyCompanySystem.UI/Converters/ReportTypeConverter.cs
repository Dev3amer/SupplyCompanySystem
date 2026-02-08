using SupplyCompanySystem.UI.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class ReportTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReportType reportType)
            {
                return reportType switch
                {
                    ReportType.Summary => "📊 ملخص المبيعات",
                    ReportType.TopProducts => "🏆 أكثر المنتجات مبيعاً",
                    ReportType.LeastProducts => "📉 أقل المنتجات مبيعاً",
                    ReportType.TopPayingCustomers => "💰 أكثر العملاء إنفاقاً",
                    ReportType.TopInvoiceCustomers => "📋 أكثر العملاء طلباً للعروض",
                    ReportType.DailySales => "📅 المبيعات اليومية",
                    ReportType.MonthlySales => "📈 المبيعات الشهرية",
                    ReportType.Inventory => "📦 تقرير المخزون",
                    _ => "غير معروف"
                };
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                return strValue switch
                {
                    "📊 ملخص المبيعات" => ReportType.Summary,
                    "🏆 أكثر المنتجات مبيعاً" => ReportType.TopProducts,
                    "📉 أقل المنتجات مبيعاً" => ReportType.LeastProducts,
                    "💰 أكثر العملاء إنفاقاً" => ReportType.TopPayingCustomers,
                    "📋 أكثر العملاء طلباً للعروض" => ReportType.TopInvoiceCustomers,
                    "📅 المبيعات اليومية" => ReportType.DailySales,
                    "📈 المبيعات الشهرية" => ReportType.MonthlySales,
                    "📦 تقرير المخزون" => ReportType.Inventory,
                    _ => ReportType.Summary
                };
            }
            return ReportType.Summary;
        }
    }
}