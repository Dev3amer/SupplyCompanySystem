using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SupplyCompanySystem.UI.Views
{
    public partial class ReportsView : UserControl
    {
        private ReportsViewModel _viewModel;

        public ReportsView()
        {
            InitializeComponent();

            var reportRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.IReportRepository>();
            var customerRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.ICustomerRepository>();
            var productRepository = ServiceProvider.GetService<SupplyCompanySystem.Application.Interfaces.IProductRepository>();

            _viewModel = new ReportsViewModel(reportRepository, customerRepository, productRepository);
            this.DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // إلغاء أي عمليات سابقة عند تحميل الصفحة
            if (_viewModel != null)
            {
                _viewModel.Dispose();
            }
        }

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // إلغاء تحديد أي DataGrid عند النقر في أي مكان خارجها
            var source = e.OriginalSource as DependencyObject;

            if (IsClickOnDataGrid(e, TopProductsGrid) ||
                IsClickOnDataGrid(e, LeastProductsGrid) ||
                IsClickOnDataGrid(e, TopPayingCustomersGrid) ||
                IsClickOnDataGrid(e, TopInvoiceCustomersGrid) ||
                IsClickOnDataGrid(e, DailySalesGrid) ||
                IsClickOnDataGrid(e, MonthlySalesGrid) ||
                IsClickOnDataGrid(e, InventoryGrid))
            {
                return;
            }

            var button = FindVisualParent<Button>(source);
            if (button != null)
            {
                return;
            }

            var comboBox = FindVisualParent<ComboBox>(source);
            if (comboBox != null)
            {
                return;
            }

            var textBox = FindVisualParent<TextBox>(source);
            if (textBox != null)
            {
                return;
            }

            var datePicker = FindVisualParent<DatePicker>(source);
            if (datePicker != null)
            {
                return;
            }

            ClearAllDataGridSelections();
        }

        private void ClearAllDataGridSelections()
        {
            ClearDataGridSelection(TopProductsGrid);
            ClearDataGridSelection(LeastProductsGrid);
            ClearDataGridSelection(TopPayingCustomersGrid);
            ClearDataGridSelection(TopInvoiceCustomersGrid);
            ClearDataGridSelection(DailySalesGrid);
            ClearDataGridSelection(MonthlySalesGrid);
            ClearDataGridSelection(InventoryGrid);
        }

        private void ClearDataGridSelection(DataGrid dataGrid)
        {
            if (dataGrid != null && dataGrid.IsVisible)
            {
                dataGrid.UnselectAll();
                dataGrid.SelectedItem = null;
            }
        }

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

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.Dispose();
                    _viewModel = null;
                }

                this.DataContext = null;
            }
            catch
            {
                // تجاهل أي أخطاء أثناء التنظيف
            }
        }
    }
}