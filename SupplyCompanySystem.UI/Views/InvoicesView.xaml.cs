using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SupplyCompanySystem.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        private InvoiceViewModel _viewModel;
        private DispatcherTimer _customerSearchTimer;
        private DispatcherTimer _productSearchTimer;
        private static readonly Regex _numericRegex = new Regex(@"^-?\d*[.,]?\d*$");

        public InvoicesView()
        {
            InitializeComponent();

            _viewModel = ServiceProvider.GetService<InvoiceViewModel>();
            this.DataContext = _viewModel;

            _customerSearchTimer = new DispatcherTimer();
            _customerSearchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _customerSearchTimer.Tick += CustomerSearchTimer_Tick;

            _productSearchTimer = new DispatcherTimer();
            _productSearchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _productSearchTimer.Tick += ProductSearchTimer_Tick;

            InvoiceItemsDataGrid.CellEditEnding += InvoiceItemsDataGrid_CellEditEnding;
            InvoiceItemsDataGrid.MouseDoubleClick += InvoiceItemsDataGrid_MouseDoubleClick;
            InvoiceItemsDataGrid.PreviewKeyDown += InvoiceItemsDataGrid_PreviewKeyDown;
            InvoiceItemsDataGrid.PreparingCellForEdit += InvoiceItemsDataGrid_PreparingCellForEdit;

            this.Unloaded += InvoicesView_Unloaded;
        }

        #region أحداث التحكم الرقمية

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string newText = GetProposedText(textBox, e.Text);

            if (!IsValidNumericInput(newText))
            {
                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text;
            int dotCount = text.Count(c => c == '.');
            int commaCount = text.Count(c => c == ',');

            if (dotCount > 1 || commaCount > 1)
            {
                string cleanedText = CleanExtraSeparators(text);
                textBox.Text = cleanedText;
                textBox.CaretIndex = cleanedText.Length;
            }
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if ((e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                e.Key == Key.Decimal || e.Key == Key.OemPeriod || e.Key == Key.OemComma ||
                e.Key == Key.Delete || e.Key == Key.Back ||
                e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Escape)
            {
                e.Handled = false;
            }
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    string proposedText = GetProposedText(textBox, clipboardText);

                    if (!IsValidNumericInput(proposedText))
                    {
                        e.Handled = true;
                        MessageBox.Show("يمكن لصق الأرقام فقط", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                string text = textBox.Text.Trim();
                text = text.Replace(',', '.');

                if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                {
                    if (result == Math.Floor(result))
                    {
                        textBox.Text = result.ToString("0");
                    }
                    else
                    {
                        textBox.Text = result.ToString("0.##");
                    }
                }
            }
        }

        private string GetProposedText(TextBox textBox, string newText)
        {
            string currentText = textBox.Text ?? "";
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            string beforeSelection = currentText.Substring(0, selectionStart);
            string afterSelection = currentText.Substring(selectionStart + selectionLength);

            return beforeSelection + newText + afterSelection;
        }

        private bool IsValidNumericInput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (text.StartsWith("-"))
            {
                text = text.Substring(1);
            }

            return _numericRegex.IsMatch(text);
        }

        private string CleanExtraSeparators(string text)
        {
            bool hasDot = false;
            bool hasComma = false;
            char[] result = new char[text.Length];
            int resultIndex = 0;

            foreach (char c in text)
            {
                if (c == '.')
                {
                    if (!hasDot)
                    {
                        result[resultIndex++] = c;
                        hasDot = true;
                    }
                }
                else if (c == ',')
                {
                    if (!hasComma)
                    {
                        result[resultIndex++] = c;
                        hasComma = true;
                    }
                }
                else
                {
                    result[resultIndex++] = c;
                }
            }

            return new string(result, 0, resultIndex);
        }

        #endregion

        #region Customer ComboBox Events المحسنة

        private void CustomerComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // السماح بالكتابة الطبيعية
            // البحث سيعمل تلقائياً عبر Binding
            _customerSearchTimer.Stop();
            _customerSearchTimer.Start();
        }

        private void CustomerComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // عند الضغط على Backspace أو Delete، تحديث البحث فقط
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                _customerSearchTimer.Stop();
                _customerSearchTimer.Start();
                e.Handled = false;
                return;
            }

            // عند الضغط على Enter، إغلاق القائمة
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (comboBox.SelectedItem != null)
                {
                    comboBox.IsDropDownOpen = false;
                    e.Handled = true;
                }
                else if (comboBox.Items.Count > 0)
                {
                    // إذا لم يكن هناك عنصر محدد، حدد أول عنصر
                    comboBox.SelectedIndex = 0;
                    comboBox.IsDropDownOpen = false;
                    e.Handled = true;
                }
                return;
            }

            // عند الضغط على Escape، مسح النص
            if (e.Key == Key.Escape)
            {
                _viewModel.TempCustomerSearchText = string.Empty;
                _viewModel.SelectedCustomer = null;
                comboBox.Text = string.Empty;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }

            // عند الضغط على الأسهم للتنقل بين النتائج
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }

                // التأكد من أن هناك عناصر في القائمة
                if (comboBox.Items.Count > 0)
                {
                    // تحديد أول عنصر إذا لم يكن هناك عنصر محدد
                    if (comboBox.SelectedIndex == -1)
                    {
                        comboBox.SelectedIndex = 0;
                    }
                }
                e.Handled = true;
                return;
            }

            // عند الضغط على Tab، إغلاق القائمة والانتقال للحقل التالي
            if (e.Key == Key.Tab)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = false;
                return;
            }

            // السماح بجميع المفاتيح الأخرى
            e.Handled = false;
        }

        private void CustomerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // تحديث العرض عند فتح القائمة
            comboBox.Items.Refresh();
        }

        private void CustomerComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // إذا كان هناك نص ولكن لم يتم اختيار عميل
            if (!string.IsNullOrWhiteSpace(comboBox.Text) && comboBox.SelectedItem == null)
            {
                // البحث عن عميل مطابق للنص
                var exactMatch = comboBox.Items.Cast<Domain.Entities.Customer>()
                    .FirstOrDefault(c => c.Name.Equals(comboBox.Text, StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                {
                    _viewModel.SelectedCustomer = exactMatch;
                    comboBox.Text = exactMatch.Name;
                }
                else if (comboBox.Items.Count == 1)
                {
                    // إذا كان هناك عنصر واحد فقط، حدده تلقائياً
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            if (comboBox.SelectedItem != null)
            {
                // عند اختيار عميل، إغلاق القائمة وتحديث النص
                comboBox.IsDropDownOpen = false;

                var customer = comboBox.SelectedItem as Domain.Entities.Customer;
                if (customer != null)
                {
                    // تحديث النص في حقل البحث
                    _viewModel.TempCustomerSearchText = customer.Name;
                    comboBox.Text = customer.Name;
                }
            }
        }

        private void CustomerComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboBox.IsDropDownOpen = false;
            }), DispatcherPriority.Background);
        }

        private void CustomerComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // عند التركيز، فتح القائمة إذا كان النص فارغاً (للمرة الأولى فقط)
            if (string.IsNullOrWhiteSpace(comboBox.Text))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    comboBox.IsDropDownOpen = true;
                }), DispatcherPriority.Background);
            }
        }

        private void CustomerSearchTimer_Tick(object sender, EventArgs e)
        {
            _customerSearchTimer.Stop();

            if (CustomerComboBox != null)
            {
                _viewModel.CustomerSearchText = _viewModel.TempCustomerSearchText;

                if (!string.IsNullOrWhiteSpace(_viewModel.TempCustomerSearchText))
                {
                    if (!CustomerComboBox.IsDropDownOpen)
                    {
                        CustomerComboBox.IsDropDownOpen = true;
                    }
                    if (_viewModel.FilteredCustomersView != null)
                    {
                        _viewModel.FilteredCustomersView.Refresh();
                    }
                }
                else
                {
                    CustomerComboBox.IsDropDownOpen = false;
                }
            }
        }

        #endregion

        #region Product ComboBox Events المحسنة

        private void ProductComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _productSearchTimer.Stop();
            _productSearchTimer.Start();
        }

        private void ProductComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // عند الضغط على Backspace أو Delete، تحديث البحث فقط
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                _productSearchTimer.Stop();
                _productSearchTimer.Start();
                e.Handled = false;
                return;
            }

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (comboBox.SelectedItem != null)
                {
                    comboBox.IsDropDownOpen = false;
                    e.Handled = true;
                }
                else if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                    comboBox.IsDropDownOpen = false;
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == Key.Escape)
            {
                _viewModel.TempProductSearchText = string.Empty;
                _viewModel.SelectedProduct = null;
                comboBox.Text = string.Empty;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }

                if (comboBox.Items.Count > 0 && comboBox.SelectedIndex == -1)
                {
                    comboBox.SelectedIndex = 0;
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = false;
                return;
            }

            e.Handled = false;
        }

        private void ProductComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            comboBox.Items.Refresh();
        }

        private void ProductComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            if (!string.IsNullOrWhiteSpace(comboBox.Text) && comboBox.SelectedItem == null)
            {
                var exactMatch = comboBox.Items.Cast<Domain.Entities.Product>()
                    .FirstOrDefault(p => p.Name.Equals(comboBox.Text, StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                {
                    _viewModel.SelectedProduct = exactMatch;
                    comboBox.Text = exactMatch.Name;
                }
                else if (comboBox.Items.Count == 1)
                {
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            if (comboBox.SelectedItem != null)
            {
                comboBox.IsDropDownOpen = false;

                var product = comboBox.SelectedItem as Domain.Entities.Product;
                if (product != null)
                {
                    _viewModel.TempProductSearchText = product.Name;
                    comboBox.Text = product.Name;
                }
            }
        }

        private void ProductComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboBox.IsDropDownOpen = false;
            }), DispatcherPriority.Background);
        }

        private void ProductComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            // عند التركيز، فتح القائمة إذا كان النص فارغاً (للمرة الأولى فقط)
            if (string.IsNullOrWhiteSpace(comboBox.Text))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    comboBox.IsDropDownOpen = true;
                }), DispatcherPriority.Background);
            }
        }

        private void ProductSearchTimer_Tick(object sender, EventArgs e)
        {
            _productSearchTimer.Stop();

            if (ProductComboBox != null)
            {
                _viewModel.ProductSearchText = _viewModel.TempProductSearchText;

                if (!string.IsNullOrWhiteSpace(_viewModel.TempProductSearchText))
                {
                    if (!ProductComboBox.IsDropDownOpen)
                    {
                        ProductComboBox.IsDropDownOpen = true;
                    }
                    if (_viewModel.FilteredProductsView != null)
                    {
                        _viewModel.FilteredProductsView.Refresh();
                    }
                }
                else
                {
                    ProductComboBox.IsDropDownOpen = false;
                }
            }
        }

        #endregion

        #region أحداث تعديل DataGrid

        private void InvoiceItemsDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            var textBox = e.EditingElement as TextBox;
            if (textBox != null)
            {
                textBox.SelectAll();

                textBox.Language = System.Windows.Markup.XmlLanguage.GetLanguage("en-US");
            }
        }

        private void InvoiceItemsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && _viewModel != null)
            {
                var column = e.Column as DataGridTextColumn;
                if (column != null)
                {
                    var item = e.Row.Item as Domain.Entities.InvoiceItem;
                    if (item != null)
                    {
                        var textBox = e.EditingElement as TextBox;
                        if (textBox != null)
                        {
                            string newValue = textBox.Text;

                            string columnHeader = column.Header?.ToString() ?? "";

                            switch (columnHeader)
                            {
                                case "مكسب %":
                                    if (decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal profitMargin))
                                    {
                                        item.ItemProfitMarginPercentage = profitMargin;
                                    }
                                    else if (decimal.TryParse(newValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out profitMargin))
                                    {
                                        item.ItemProfitMarginPercentage = profitMargin;
                                    }
                                    break;

                                case "الكمية":
                                    if (decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity))
                                    {
                                        item.Quantity = quantity;
                                    }
                                    else if (decimal.TryParse(newValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out quantity))
                                    {
                                        item.Quantity = quantity;
                                    }
                                    break;

                                case "خصم %":
                                    if (decimal.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal discount))
                                    {
                                        item.DiscountPercentage = discount;
                                    }
                                    else if (decimal.TryParse(newValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out discount))
                                    {
                                        item.DiscountPercentage = discount;
                                    }
                                    break;
                            }

                            _viewModel.RecalculateTotals();
                        }
                    }
                }
            }
        }

        private void InvoiceItemsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InvoiceItemsDataGrid.SelectedItem != null)
            {
                var hitTest = VisualTreeHelper.HitTest(InvoiceItemsDataGrid, e.GetPosition(InvoiceItemsDataGrid));
                if (hitTest != null)
                {
                    var cell = FindVisualParent<DataGridCell>(hitTest.VisualHit);
                    if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
                    {
                        InvoiceItemsDataGrid.BeginEdit();
                    }
                }
            }
        }

        private void InvoiceItemsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && InvoiceItemsDataGrid.SelectedItem != null)
            {
                var cell = GetCurrentCell(InvoiceItemsDataGrid);
                if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
                {
                    InvoiceItemsDataGrid.BeginEdit();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F2 && InvoiceItemsDataGrid.SelectedItem != null)
            {
                var cell = GetCurrentCell(InvoiceItemsDataGrid);
                if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
                {
                    InvoiceItemsDataGrid.BeginEdit();
                    e.Handled = true;
                }
            }
        }

        // إصلاح دالة GetCurrentCell
        private DataGridCell GetCurrentCell(DataGrid dataGrid)
        {
            if (dataGrid.CurrentCell != null)
            {
                var cellInfo = dataGrid.CurrentCell;
                var row = dataGrid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item) as DataGridRow;
                if (row != null)
                {
                    var cellsPresenter = GetVisualChild<DataGridCellsPresenter>(row);
                    if (cellsPresenter != null)
                    {
                        // استخدام GetChildrenCount بدلاً من ItemContainerGenerator
                        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(cellsPresenter); i++)
                        {
                            var child = VisualTreeHelper.GetChild(cellsPresenter, i);
                            if (child is DataGridCell cell)
                            {
                                if (cell.Column != null && cell.Column.DisplayIndex == cellInfo.Column.DisplayIndex)
                                {
                                    return cell;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        // إصلاح دالة GetVisualChild
        private T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var descendant = GetVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        // إصلاح دالة FindVisualParent
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

        #endregion

        #region إلغاء التحديد عند النقر في أي مكان

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            var button = FindVisualParent<Button>(source);

            if (button != null)
            {
                return;
            }

            if (IsClickOnDataGrid(e, InvoiceItemsDataGrid))
            {
                return;
            }

            if (IsClickOnDataGrid(e, InvoicesDataGrid))
            {
                return;
            }

            if (IsClickOnComboBox(e, CustomerComboBox) || IsClickOnComboBox(e, ProductComboBox))
            {
                return;
            }

            ClearDataGridSelection(InvoiceItemsDataGrid, () => _viewModel.SelectedInvoiceItem = null);
            ClearDataGridSelection(InvoicesDataGrid, () => _viewModel.SelectedInvoiceFromList = null);
        }

        private bool IsClickOnComboBox(MouseButtonEventArgs e, ComboBox comboBox)
        {
            if (comboBox == null || !comboBox.IsVisible) return false;

            try
            {
                var hitTest = VisualTreeHelper.HitTest(comboBox, e.GetPosition(comboBox));
                return hitTest != null;
            }
            catch
            {
                return false;
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

        private void ClearDataGridSelection(DataGrid dataGrid, Action updateViewModel)
        {
            if (dataGrid == null) return;

            try
            {
                dataGrid.UnselectAll();
                dataGrid.SelectedItem = null;

                updateViewModel?.Invoke();
            }
            catch
            {
                // تجاهل أي أخطاء في عملية الإلغاء
            }
        }

        #endregion

        #region تنظيف الموارد

        private void InvoicesView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (InvoiceItemsDataGrid != null)
                {
                    InvoiceItemsDataGrid.CellEditEnding -= InvoiceItemsDataGrid_CellEditEnding;
                    InvoiceItemsDataGrid.MouseDoubleClick -= InvoiceItemsDataGrid_MouseDoubleClick;
                    InvoiceItemsDataGrid.PreviewKeyDown -= InvoiceItemsDataGrid_PreviewKeyDown;
                    InvoiceItemsDataGrid.PreparingCellForEdit -= InvoiceItemsDataGrid_PreparingCellForEdit;
                }

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

                this.Unloaded -= InvoicesView_Unloaded;
                this.PreviewMouseDown -= UserControl_PreviewMouseDown;

                if (_viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                this.DataContext = null;
                _viewModel = null;
            }
            catch
            {
                // تجاهل الأخطاء أثناء التنظيف
            }
        }

        #endregion
    }
}