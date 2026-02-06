using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SupplyCompanySystem.UI.Views
{
    public partial class InvoiceArchiveView : UserControl
    {
        private InvoiceArchiveViewModel _viewModel;

        public InvoiceArchiveView()
        {
            InitializeComponent();

            var invoiceRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.IInvoiceRepository>();
            var customerRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.ICustomerRepository>();

            _viewModel = new InvoiceArchiveViewModel(invoiceRepository, customerRepository);
            DataContext = _viewModel;
        }

        /// <summary>
        /// حدث للنقر في أي مكان في UserControl لإلغاء التحديد
        /// </summary>
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // ✅ الحصول على مصدر النقر
            var source = e.OriginalSource as DependencyObject;

            // ✅ التحقق مما إذا كان النقر على DataGrid أو عناصر التحكم
            if (IsClickOnDataGrid(e, ArchiveDataGrid))
            {
                // إذا كان النقر على DataGrid، لا نلغي التحديد
                return;
            }

            // ✅ التحقق مما إذا كان النقر على زر
            var button = FindVisualParent<Button>(source);
            if (button != null)
            {
                return;
            }

            // ✅ التحقق مما إذا كان النقر على ComboBox
            var comboBox = FindVisualParent<ComboBox>(source);
            if (comboBox != null)
            {
                return;
            }

            // ✅ التحقق مما إذا كان النقر على TextBox
            var textBox = FindVisualParent<TextBox>(source);
            if (textBox != null)
            {
                return;
            }

            // ✅ التحقق مما إذا كان النقر على DatePicker
            var datePicker = FindVisualParent<DatePicker>(source);
            if (datePicker != null)
            {
                return;
            }

            // ✅ النقر في أي مكان آخر يلغي التحديد
            ClearDataGridSelection();
        }

        /// <summary>
        /// حدث للنقر على DataGrid نفسه (للتحكم في النقر على صفوف الجدول)
        /// </summary>
        private void DataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            // ✅ التحقق مما إذا كان النقر على خلية أو صف في DataGrid
            var dataGridCell = FindVisualParent<DataGridCell>(source);
            var dataGridRow = FindVisualParent<DataGridRow>(source);

            if (dataGridCell != null || dataGridRow != null)
            {
                // النقر على صف أو خلية - نترك التحديد يعمل بشكل طبيعي
                return;
            }

            // ✅ النقر على مساحة فارغة داخل DataGrid يلغي التحديد
            ClearDataGridSelection();
        }

        // دالة مساعدة للعثور على عنصر أب في الشجرة المرئية
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        /// <summary>
        /// التحقق مما إذا كان النقر على DataGrid معين
        /// </summary>
        private bool IsClickOnDataGrid(MouseButtonEventArgs e, DataGrid dataGrid)
        {
            if (dataGrid == null || !dataGrid.IsVisible) return false;

            try
            {
                var hitTest = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                return hitTest != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// إلغاء التحديد من DataGrid وتحديث ViewModel
        /// </summary>
        private void ClearDataGridSelection()
        {
            try
            {
                // إلغاء التحديد من DataGrid
                if (ArchiveDataGrid != null)
                {
                    ArchiveDataGrid.UnselectAll();
                    ArchiveDataGrid.SelectedItem = null;
                }

                // تحديث ViewModel
                if (_viewModel != null)
                {
                    _viewModel.ClearInvoiceSelection();
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }

        /// <summary>
        /// تنظيف الموارد عند إغلاق الـ UserControl
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    if (_viewModel is System.IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _viewModel = null;
                }

                this.DataContext = null;
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }
    }
}