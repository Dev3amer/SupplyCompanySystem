using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SupplyCompanySystem.UI.Views
{
    public partial class CustomersView : UserControl, IDisposable
    {
        private CustomerViewModel _viewModel;

        public CustomersView()
        {
            InitializeComponent();

            _viewModel = ServiceProvider.GetService<CustomerViewModel>();
            DataContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            // ✅ إضافة معالج حدث للنقر في أي مكان في الـ UserControl
            this.MouseDown += CustomersView_MouseDown;
            this.Loaded += CustomersView_Loaded;
        }

        private void CustomersView_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ إضافة معالج حدث للنقر في النافذة الرئيسية أيضاً
            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                mainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
            }
        }

        private void CustomersView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearCustomerSelection(e);
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearCustomerSelection(e);
        }

        /// <summary>
        /// ✅ مسح تحديد العميل إذا تم النقر خارج الجدول
        /// </summary>
        private void ClearCustomerSelection(MouseButtonEventArgs e)
        {
            try
            {
                // ✅ التحقق مما إذا كان النقر داخل الجدول
                if (CustomersDataGrid.IsMouseOver || IsMouseOverFormControls(e))
                {
                    return; // لا تمسح التحديد إذا النقر داخل الجدول أو عناصر النموذج
                }

                // ✅ مسح التحديد إذا تم النقر خارج الجدول وعناصر النموذج
                if (_viewModel != null && _viewModel.SelectedCustomer != null && !_viewModel.IsEditMode)
                {
                    // مسح التحديد فقط إذا لم نكن في وضع التحرير
                    _viewModel.SelectedCustomer = null;
                    CustomersDataGrid.UnselectAll();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطأ في مسح التحديد: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ التحقق مما إذا كان النقر داخل عناصر تحكم النموذج
        /// </summary>
        private bool IsMouseOverFormControls(MouseButtonEventArgs e)
        {
            try
            {
                var mousePos = e.GetPosition(this);

                // ✅ التحقق من عناصر النموذج التي يجب ألا تمسح التحديد
                var formElements = new List<UIElement>
                {
                    AddBtn, EditBtn, DeleteBtn, RestoreBtn, SaveBtn, CancelBtn
                };

                // ✅ الحصول على إحداثيات كل عنصر
                foreach (var element in formElements)
                {
                    if (element != null && element.IsVisible)
                    {
                        var bounds = new Rect(
                            element.TranslatePoint(new Point(0, 0), this),
                            new Size(element.RenderSize.Width, element.RenderSize.Height));

                        if (bounds.Contains(mousePos))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // ✅ تحديث حالة الأزرار عند تغيير التحديد
            if (e.PropertyName == nameof(_viewModel.SelectedCustomer))
            {
                // تحديث حالة الأزرار
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #region Export Logic

        private ExportType? AskExportType()
        {
            ExportType? selectedType = null;
            var dialog = new Window
            {
                Title = "خيارات التصدير",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "ماذا تريد أن تصدّر؟", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), FontSize = 14 });

            var options = new[] { ("جميع العملاء", ExportType.All), ("النشطين فقط", ExportType.ActiveOnly), ("المعطلين فقط", ExportType.InactiveOnly) };

            foreach (var opt in options)
            {
                var btn = new Button { Content = opt.Item1, Margin = new Thickness(0, 5, 0, 5), Padding = new Thickness(10), Cursor = System.Windows.Input.Cursors.Hand };
                btn.Click += (s, e) => { selectedType = opt.Item2; dialog.Close(); };
                stack.Children.Add(btn);
            }

            var closeBtn = new Button { Content = "إلغاء", Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(5), Background = System.Windows.Media.Brushes.Gray, Foreground = System.Windows.Media.Brushes.White };
            closeBtn.Click += (s, e) => dialog.Close();
            stack.Children.Add(closeBtn);

            dialog.Content = stack;
            dialog.ShowDialog();
            return selectedType;
        }

        private void ExecuteExport(string format, Action<List<Customer>, string> exportAction)
        {
            try
            {
                var type = AskExportType();
                if (type is null) return;

                var ext = format.ToLower() == "excel" ? "xlsx" : format.ToLower();
                var dialog = new SaveFileDialog
                {
                    FileName = $"عملاء_{DateTime.Now:yyyyMMdd_HHmm}",
                    Filter = $"{format} Files (*.{ext})|*.{ext}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var all = _viewModel.GetAllCustomersIncludingInactive();
                    var data = type switch
                    {
                        ExportType.ActiveOnly => all.Where(c => c.IsActive).ToList(),
                        ExportType.InactiveOnly => all.Where(c => !c.IsActive).ToList(),
                        _ => all
                    };

                    exportAction(data, dialog.FileName);
                    MessageBox.Show("تم التصدير بنجاح", "نجاح");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التصدير: {ex.Message}");
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e) => ExecuteExport("Excel", ExportCustomersToExcel);
        private void ExportToPdf_Click(object sender, RoutedEventArgs e) => ExecuteExport("PDF", ExportCustomersToPdf);
        private void ExportToCsv_Click(object sender, RoutedEventArgs e) => ExecuteExport("CSV", ExportCustomersToCsv);

        private void ExportCustomersToExcel(List<Customer> customers, string filePath)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("العملاء");
            string[] headers = { "ID", "الاسم", "الهاتف", "العنوان", "التاريخ", "الحالة" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            int r = 2;
            foreach (var c in customers)
            {
                ws.Cell(r, 1).Value = c.Id;
                ws.Cell(r, 2).Value = c.Name;
                ws.Cell(r, 3).Value = c.PhoneNumber;
                ws.Cell(r, 4).Value = c.Address;
                ws.Cell(r, 5).Value = c.CreatedDate.ToString("yyyy-MM-dd");
                ws.Cell(r, 6).Value = c.IsActive ? "نشط" : "معطل";
                r++;
            }
            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private void ExportCustomersToPdf(List<Customer> customers, string filePath)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.ContentFromRightToLeft();
                    page.Header().Text("تقرير العملاء").FontSize(20).SemiBold().AlignCenter();
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(3); c.RelativeColumn(); c.RelativeColumn(); });
                        table.Header(h =>
                        {
                            h.Cell().Background("#2C3E50").Padding(5).Text("ID").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("الاسم").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("الهاتف").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("الحالة").FontColor("#FFF");
                        });
                        foreach (var c in customers)
                        {
                            table.Cell().BorderBottom(1).Padding(5).Text(c.Id.ToString());
                            table.Cell().BorderBottom(1).Padding(5).Text(c.Name);
                            table.Cell().BorderBottom(1).Padding(5).Text(c.PhoneNumber);
                            table.Cell().BorderBottom(1).Padding(5).Text(c.IsActive ? "نشط" : "معطل");
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }

        private void ExportCustomersToCsv(List<Customer> customers, string filePath)
        {
            using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("ID,Name,PhoneNumber,Address,CreatedDate,Status");
            foreach (var c in customers)
                writer.WriteLine($"{c.Id},{c.Name},{c.PhoneNumber},{c.Address},{c.CreatedDate:yyyy-MM-dd},{(c.IsActive ? "Active" : "Inactive")}");
        }

        #endregion

        public void Dispose()
        {
            if (_viewModel != null) _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            // ✅ إزالة معالجات الأحداث
            this.MouseDown -= CustomersView_MouseDown;
            this.Loaded -= CustomersView_Loaded;

            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                mainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
            }
        }
    }
}