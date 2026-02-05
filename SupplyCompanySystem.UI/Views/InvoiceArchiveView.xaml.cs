using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows.Controls;

namespace SupplyCompanySystem.UI.Views
{
    public partial class InvoiceArchiveView : UserControl
    {
        public InvoiceArchiveView()
        {
            InitializeComponent();

            // الحصول على الريبو من ServiceProvider
            var invoiceRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.IInvoiceRepository>();
            var customerRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.ICustomerRepository>();

            // إنشاء الـ ViewModel وربطه بالـ DataContext
            var viewModel = new InvoiceArchiveViewModel(invoiceRepository, customerRepository);
            DataContext = viewModel;
        }
    }
}