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

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            if (IsClickOnDataGrid(e, ArchiveDataGrid))
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

            ClearDataGridSelection();
        }
        private void DataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            var dataGridCell = FindVisualParent<DataGridCell>(source);
            var dataGridRow = FindVisualParent<DataGridRow>(source);

            if (dataGridCell != null || dataGridRow != null)
            {
                return;
            }

            ClearDataGridSelection();
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

        private void ClearDataGridSelection()
        {
            try
            {
                if (ArchiveDataGrid != null)
                {
                    ArchiveDataGrid.UnselectAll();
                    ArchiveDataGrid.SelectedItem = null;
                }

                if (_viewModel != null)
                {
                    _viewModel.ClearInvoiceSelection();
                }
            }
            catch
            {
            }
        }

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
            }
        }
    }
}