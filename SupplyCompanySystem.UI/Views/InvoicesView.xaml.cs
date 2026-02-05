using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SupplyCompanySystem.UI.Views
{
    /// <summary>
    /// Interaction logic for InvoicesView.xaml
    /// إدارة حالة الأزرار عبر XAML Triggers لضمان أفضل أداء للميموري
    /// </summary>
    public partial class InvoicesView : UserControl
    {
        public InvoicesView()
        {
            InitializeComponent();

            // حقن الـ ViewModel وتعيينه كـ DataContext
            this.DataContext = ServiceProvider.GetService<InvoiceViewModel>();

            // الاشتراك في حدث التفريغ لضمان تنظيف المراجع
            this.Unloaded += InvoicesView_Unloaded;
        }

        private void InvoicesView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // فك ارتباط الحدث نفسه
                this.Unloaded -= InvoicesView_Unloaded;

                // تنظيف الـ ViewModel إذا كان يدعم التخلص من الموارد
                if (this.DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // قطع المرجع للـ DataContext للسماح لـ GC بمسحه
                this.DataContext = null;
            }
            catch
            {
                // منع انهيار البرنامج في حالة حدوث خطأ أثناء الإغلاق
            }
        }
    }
}
