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
            Unloaded += UserControl_Unloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // إعداد معالجة أحداث MouseWheel بشكل صحيح
            SetupMouseWheelHandling();

            // إلغاء أي عمليات سابقة عند تحميل الصفحة
            if (_viewModel != null)
            {
                _viewModel.Dispose();
            }
        }

        private void SetupMouseWheelHandling()
        {
            // إعداد معالجة أحداث PreviewMouseWheel لكل عنصر
            AddPreviewMouseWheelHandler(TopProductsGrid);
            AddPreviewMouseWheelHandler(LeastProductsGrid);
            AddPreviewMouseWheelHandler(TopPayingCustomersGrid);
            AddPreviewMouseWheelHandler(TopInvoiceCustomersGrid);
            AddPreviewMouseWheelHandler(DailySalesGrid);
            AddPreviewMouseWheelHandler(MonthlySalesGrid);
            AddPreviewMouseWheelHandler(InventoryGrid);

            // إضافة معالج للـ UserControl نفسه
            this.PreviewMouseWheel += ReportsView_PreviewMouseWheel;
        }

        private void AddPreviewMouseWheelHandler(DataGrid dataGrid)
        {
            if (dataGrid != null)
            {
                dataGrid.PreviewMouseWheel += DataGrid_PreviewMouseWheel;
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // الحل: منع تمرير عجلة الفأرة داخل DataGrid عندما يكون تحت DatePicker
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                // تحقق مما إذا كان الفأرة فوق DatePicker داخل DataGrid
                var position = e.GetPosition(dataGrid);
                var element = dataGrid.InputHitTest(position) as DependencyObject;

                // البحث عن DatePicker في العناصر الأب
                while (element != null && !(element is DatePicker))
                {
                    element = VisualTreeHelper.GetParent(element);
                }

                // إذا كان هناك DatePicker تحت الفأرة، لا نمنع الحدث
                if (element is DatePicker)
                {
                    // السماح لـ DatePicker بالتعامل مع الحدث (لتغيير التاريخ)
                    return;
                }
            }

            // لمنع تمرير عجلة الفأرة داخل DataGrid (التمرير يكون بالـ ScrollViewer الرئيسي فقط)
            e.Handled = true;

            // تمرير الحدث إلى العنصر الرئيسي
            var newEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };

            // إيجاد أول ScrollViewer أب
            var scrollViewer = FindVisualParent<ScrollViewer>(dataGrid);
            if (scrollViewer != null)
            {
                scrollViewer.RaiseEvent(newEvent);
            }
        }

        private void ReportsView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // إذا كان الحدث من عنصر يحتاج لتمرير داخلي، نسمح له
            var source = e.OriginalSource as DependencyObject;

            // إذا كان المصدر DatePicker أو ComboBox، نتركه يعالج الحدث
            if (IsElementOfType<DatePicker>(source) || IsElementOfType<ComboBox>(source))
            {
                return;
            }

            // إذا كان الحدث من DataGrid، تم التعامل معه في DataGrid_PreviewMouseWheel
            if (IsElementOfType<DataGrid>(source))
            {
                return;
            }
        }

        private bool IsElementOfType<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T)
                    return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
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
                // تنظيف معالجات الأحداث
                RemoveMouseWheelHandler(TopProductsGrid);
                RemoveMouseWheelHandler(LeastProductsGrid);
                RemoveMouseWheelHandler(TopPayingCustomersGrid);
                RemoveMouseWheelHandler(TopInvoiceCustomersGrid);
                RemoveMouseWheelHandler(DailySalesGrid);
                RemoveMouseWheelHandler(MonthlySalesGrid);
                RemoveMouseWheelHandler(InventoryGrid);

                this.PreviewMouseWheel -= ReportsView_PreviewMouseWheel;

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

        private void RemoveMouseWheelHandler(DataGrid dataGrid)
        {
            if (dataGrid != null)
            {
                dataGrid.PreviewMouseWheel -= DataGrid_PreviewMouseWheel;
            }
        }
    }
}