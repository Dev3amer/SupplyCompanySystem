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

        #region Customer ComboBox Events

        private void CustomerComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            if (e.Key >= Key.A && e.Key <= Key.Z ||
                e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key == Key.Space || e.Key == Key.OemComma || e.Key == Key.OemPeriod)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                comboBox.Text = string.Empty;
                _viewModel.SelectedCustomer = null;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
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

            _customerSearchTimer.Stop();
            _customerSearchTimer.Start();

            if (!comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void CustomerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || string.IsNullOrWhiteSpace(comboBox.Text)) return;

            _viewModel.CustomerSearchText = comboBox.Text;
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            if (comboBox.SelectedItem != null)
            {
                comboBox.IsDropDownOpen = false;

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

            Dispatcher.BeginInvoke(new Action(() =>
            {
                comboBox.IsDropDownOpen = false;
            }), DispatcherPriority.Background);
        }

        private void CustomerSearchTimer_Tick(object sender, EventArgs e)
        {
            _customerSearchTimer.Stop();

            if (CustomerComboBox != null)
            {
                _viewModel.CustomerSearchText = CustomerComboBox.Text;

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

            if (e.Key >= Key.A && e.Key <= Key.Z ||
                e.Key >= Key.D0 && e.Key <= Key.D9 ||
                e.Key == Key.Space || e.Key == Key.OemComma || e.Key == Key.OemPeriod)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                comboBox.Text = string.Empty;
                _viewModel.SelectedProduct = null;
                comboBox.IsDropDownOpen = false;
                e.Handled = true;
            }
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

            _productSearchTimer.Stop();
            _productSearchTimer.Start();

            if (!comboBox.IsDropDownOpen)
                comboBox.IsDropDownOpen = true;
        }

        private void ProductComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || string.IsNullOrWhiteSpace(comboBox.Text)) return;

            _viewModel.ProductSearchText = comboBox.Text;
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

        private void ProductSearchTimer_Tick(object sender, EventArgs e)
        {
            _productSearchTimer.Stop();

            if (ProductComboBox != null)
            {
                _viewModel.ProductSearchText = ProductComboBox.Text;

                if (!string.IsNullOrWhiteSpace(ProductComboBox.Text) && !ProductComboBox.IsDropDownOpen)
                {
                    ProductComboBox.IsDropDownOpen = true;
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
        private DataGridCell GetCurrentCell(DataGrid dataGrid)
        {
            if (dataGrid.CurrentCell != null)
            {
                var cellInfo = dataGrid.CurrentCell;
                var row = dataGrid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item) as DataGridRow;
                if (row != null)
                {
                    var presenter = GetVisualChild<DataGridCellsPresenter>(row);
                    if (presenter != null)
                    {
                        var cell = presenter.ItemContainerGenerator.ContainerFromIndex(cellInfo.Column.DisplayIndex) as DataGridCell;
                        return cell;
                    }
                }
            }
            return null;
        }
        private T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
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

            ClearDataGridSelection(InvoiceItemsDataGrid, () => _viewModel.SelectedInvoiceItem = null);
            ClearDataGridSelection(InvoicesDataGrid, () => _viewModel.SelectedInvoiceFromList = null);
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
            }
        }

        #endregion
    }
}