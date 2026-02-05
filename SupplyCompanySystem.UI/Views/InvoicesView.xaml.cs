using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SupplyCompanySystem.UI.Views
{
    /// <summary>
    /// Interaction logic for InvoicesView.xaml
    /// ComboBox قابلة للبحث مثل Google Chrome search bar
    /// </summary>
    public partial class InvoicesView : UserControl
    {
        private InvoiceViewModel _viewModel;
        private DispatcherTimer _customerSearchTimer;
        private DispatcherTimer _productSearchTimer;

        public InvoicesView()
        {
            InitializeComponent();

            // حقن الـ ViewModel وتعيينه كـ DataContext
            _viewModel = ServiceProvider.GetService<InvoiceViewModel>();
            this.DataContext = _viewModel;

            // إنشاء Timers للبحث المؤجل
            _customerSearchTimer = new DispatcherTimer();
            _customerSearchTimer.Interval = TimeSpan.FromMilliseconds(300); // تأخير 300ms للبحث
            _customerSearchTimer.Tick += CustomerSearchTimer_Tick;

            _productSearchTimer = new DispatcherTimer();
            _productSearchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _productSearchTimer.Tick += ProductSearchTimer_Tick;

            // الاشتراك في حدث التفريغ لضمان تنظيف المراجع
            this.Unloaded += InvoicesView_Unloaded;
        }

        #region Customer ComboBox Events

        private void CustomerComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // فتح القائمة عند الضغط على أي مفتاح كتابة
            if (e.Key >= Key.A && e.Key <= Key.Z ||
                e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key == Key.Space || e.Key == Key.OemComma || e.Key == Key.OemPeriod)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
            // إغلاق القائمة عند الضغط على Enter
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            // تنظيف البحث عند الضغط على Escape
            else if (e.Key == Key.Escape)
            {
                comboBox.Text = string.Empty;
                _viewModel.SelectedCustomer = null;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            // فتح القائمة عند الضغط على الأسهم
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }
        }

        private void CustomerComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إعادة تشغيل Timer للبحث المؤجل
            _customerSearchTimer.Stop();
            _customerSearchTimer.Start();

            // التأكد من أن القائمة مفتوحة أثناء الكتابة
            if (!comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void CustomerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || string.IsNullOrWhiteSpace(comboBox.Text)) return;

            // تحديث الفلترة عند فتح القائمة
            _viewModel.CustomerSearchText = comboBox.Text;
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إذا تم اختيار عنصر، أغلق القائمة
            if (comboBox.SelectedItem != null)
            {
                comboBox.IsDropDownOpen = false;

                // تحديث نص البحث ليكون اسم العميل المختار
                var customer = comboBox.SelectedItem as Domain.Entities.Customer;
                if (customer != null)
                {
                    comboBox.Text = customer.Name;
                }
            }
        }

        private void CustomerComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إغلاق القائمة بعد فترة قصيرة من فقدان التركيز
            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboBox.IsDropDownOpen = false;
            }), DispatcherPriority.Background);
        }

        private void CustomerSearchTimer_Tick(object sender, EventArgs e)
        {
            _customerSearchTimer.Stop();

            // تحديث نص البحث في ViewModel
            if (CustomerComboBox != null)
            {
                _viewModel.CustomerSearchText = CustomerComboBox.Text;

                // التأكد من أن القائمة مفتوحة إذا كان هناك نص
                if (!string.IsNullOrWhiteSpace(CustomerComboBox.Text) && !CustomerComboBox.IsDropDownOpen)
                {
                    CustomerComboBox.IsDropDownOpen = true;
                }
            }
        }

        #endregion

        #region Product ComboBox Events

        private void ProductComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // فتح القائمة عند الضغط على أي مفتاح كتابة
            if (e.Key >= Key.A && e.Key <= Key.Z ||
                e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key == Key.Space || e.Key == Key.OemComma || e.Key == Key.OemPeriod)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
            // إغلاق القائمة عند الضغط على Enter
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            // تنظيف البحث عند الضغط على Escape
            else if (e.Key == Key.Escape)
            {
                comboBox.Text = string.Empty;
                _viewModel.SelectedProduct = null;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            // فتح القائمة عند الضغط على الأسهم
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }
        }

        private void ProductComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إعادة تشغيل Timer للبحث المؤجل
            _productSearchTimer.Stop();
            _productSearchTimer.Start();

            // التأكد من أن القائمة مفتوحة أثناء الكتابة
            if (!comboBox.IsDropDownOpen)
                comboBox.IsDropDownOpen = true;
        }

        private void ProductComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || string.IsNullOrWhiteSpace(comboBox.Text)) return;

            // تحديث الفلترة عند فتح القائمة
            _viewModel.ProductSearchText = comboBox.Text;
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إذا تم اختيار عنصر، أغلق القائمة
            if (comboBox.SelectedItem != null)
            {
                comboBox.IsDropDownOpen = false;

                // تحديث نص البحث ليكون اسم المنتج المختار
                var product = comboBox.SelectedItem as Domain.Entities.Product;
                if (product != null)
                {
                    comboBox.Text = product.Name;
                }
            }
        }

        private void ProductComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إغلاق القائمة بعد فترة قصيرة من فقدان التركيز
            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboBox.IsDropDownOpen = false;
            }), DispatcherPriority.Background);
        }

        private void ProductSearchTimer_Tick(object sender, EventArgs e)
        {
            _productSearchTimer.Stop();

            // تحديث نص البحث في ViewModel
            if (ProductComboBox != null)
            {
                _viewModel.ProductSearchText = ProductComboBox.Text;

                // التأكد من أن القائمة مفتوحة إذا كان هناك نص
                if (!string.IsNullOrWhiteSpace(ProductComboBox.Text) && !ProductComboBox.IsDropDownOpen)
                {
                    ProductComboBox.IsDropDownOpen = true;
                }
            }
        }

        #endregion

        #region إلغاء التحديد عند النقر في أي مكان

        /// <summary>
        /// حدث للنقر في أي مكان في UserControl لإلغاء التحديد
        /// </summary>
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // ✅ التحقق مما إذا كان النقر على زر
            var source = e.OriginalSource as DependencyObject;
            var button = FindVisualParent<Button>(source);

            if (button != null)
            {
                // إذا كان النقر على زر، نتركه يعمل ولا تلغي التحديد
                return;
            }

            // التحقق مما إذا كان النقر على DataGrid بنود الفاتورة
            if (IsClickOnDataGrid(e, InvoiceItemsDataGrid))
            {
                return;
            }

            // التحقق مما إذا كان النقر على DataGrid الفواتير
            if (IsClickOnDataGrid(e, InvoicesDataGrid))
            {
                return;
            }

            // النقر في أي مكان آخر يلغي التحديد
            ClearDataGridSelection(InvoiceItemsDataGrid, () => _viewModel.SelectedInvoiceItem = null);
            ClearDataGridSelection(InvoicesDataGrid, () => _viewModel.SelectedInvoiceFromList = null);
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
                // التحقق من أن النقر داخل حدود DataGrid
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
        private void ClearDataGridSelection(DataGrid dataGrid, Action updateViewModel)
        {
            if (dataGrid == null) return;

            try
            {
                // إلغاء التحديد من DataGrid
                dataGrid.UnselectAll();
                dataGrid.SelectedItem = null;

                // تحديث ViewModel
                updateViewModel?.Invoke();
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }

        #endregion

        #region تنظيف الموارد

        private void InvoicesView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تنظيف Timers
                if (_customerSearchTimer != null)
                {
                    _customerSearchTimer.Stop();
                    _customerSearchTimer.Tick -= CustomerSearchTimer_Tick;
                    _customerSearchTimer = null;
                }

                if (_productSearchTimer != null)
                {
                    _productSearchTimer.Stop();
                    _productSearchTimer.Tick -= ProductSearchTimer_Tick;
                    _productSearchTimer = null;
                }

                // فك ارتباط الحدث نفسه
                this.Unloaded -= InvoicesView_Unloaded;
                this.PreviewMouseDown -= UserControl_PreviewMouseDown;

                // تنظيف الـ ViewModel إذا كان يدعم التخلص من الموارد
                if (_viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // قطع المرجع للـ DataContext للسماح لـ GC بمسحه
                this.DataContext = null;
                _viewModel = null;
            }
            catch
            {
                // منع انهيار البرنامج في حالة حدوث خطأ أثناء الإغلاق
            }
        }

        #endregion
    }
}