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
    public enum ExportType
    {
        All,
        ActiveOnly,
        InactiveOnly
    }

    public partial class ProductsView : UserControl, IDisposable
    {
        private ProductViewModel _viewModel;

        public ProductsView()
        {
            InitializeComponent();

            _viewModel = ServiceProvider.GetService<ProductViewModel>();
            DataContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            // ✅ إضافة معالج حدث للنقر في أي مكان في الـ UserControl
            this.MouseDown += ProductsView_MouseDown;
            this.Loaded += ProductsView_Loaded;
        }

        private void ProductsView_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ إضافة معالج حدث للنقر في النافذة الرئيسية أيضاً
            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                mainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
            }
        }

        private void ProductsView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearProductSelection(e);
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearProductSelection(e);
        }

        /// <summary>
        /// ✅ مسح تحديد المنتج إذا تم النقر خارج الجدول
        /// </summary>
        private void ClearProductSelection(MouseButtonEventArgs e)
        {
            try
            {
                // ✅ التحقق مما إذا كان النقر داخل الجدول
                if (ProductsDataGrid.IsMouseOver || IsMouseOverFormControls(e))
                {
                    return; // لا تمسح التحديد إذا النقر داخل الجدول أو عناصر النموذج
                }

                // ✅ مسح التحديد إذا تم النقر خارج الجدول وعناصر النموذج
                if (_viewModel != null && _viewModel.SelectedProduct != null && !_viewModel.IsEditMode)
                {
                    // مسح التحديد فقط إذا لم نكن في وضع التحرير
                    _viewModel.SelectedProduct = null;
                    ProductsDataGrid.UnselectAll();
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
            if (e.PropertyName == nameof(_viewModel.SelectedProduct))
            {
                // تحديث حالة الأزرار
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #region Category Dialog Logic

        private void AddNewCategory_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryDialog = new Window
            {
                Title = "إضافة تصنيف جديد",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Background = System.Windows.Media.Brushes.White,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize
            };

            var textBox = new TextBox { Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 15) };
            var okButton = new Button { Content = "موافق", Width = 80, Background = System.Windows.Media.Brushes.Green, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 5, 0), Padding = new Thickness(10, 5, 10, 5) };
            var cancelButton = new Button { Content = "إلغاء", Width = 80, Padding = new Thickness(10, 5, 10, 5) };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    var newCategory = textBox.Text.Trim();

                    // ✅ إضافة للـ CategoriesForForm (الفورم)
                    if (!_viewModel.CategoriesForForm.Contains(newCategory))
                    {
                        _viewModel.CategoriesForForm.Add(newCategory);
                    }

                    // ✅ إضافة للـ Categories (الفلترة)
                    if (!_viewModel.Categories.Contains(newCategory))
                    {
                        _viewModel.Categories.Add(newCategory);
                    }

                    // ✅ تعيين التصنيف الجديد
                    _viewModel.Category = newCategory;
                    newCategoryDialog.Close();
                }
                else MessageBox.Show("الرجاء إدخال اسم التصنيف", "تنبيه");
            };

            cancelButton.Click += (s, args) => newCategoryDialog.Close();

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "اسم التصنيف الجديد:", Margin = new Thickness(0, 0, 0, 10), FontWeight = FontWeights.Bold });
            stack.Children.Add(textBox);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnStack.Children.Add(okButton);
            btnStack.Children.Add(cancelButton);
            stack.Children.Add(btnStack);

            newCategoryDialog.Content = stack;
            newCategoryDialog.ShowDialog();
        }

        #endregion

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

            var options = new[] { ("جميع المنتجات", ExportType.All), ("النشطة فقط", ExportType.ActiveOnly), ("المعطلة فقط", ExportType.InactiveOnly) };

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

        private void ExecuteExport(string format, Action<List<Product>, string> exportAction)
        {
            try
            {
                var type = AskExportType();
                if (type == null) return;

                var ext = format.ToLower() == "excel" ? "xlsx" : format.ToLower();
                var dialog = new SaveFileDialog
                {
                    FileName = $"منتجات_{DateTime.Now:yyyyMMdd_HHmm}",
                    Filter = $"{format} Files (*.{ext})|*.{ext}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var all = _viewModel.GetAllProductsIncludingInactive();
                    var data = type switch
                    {
                        ExportType.ActiveOnly => all.Where(p => p.IsActive).ToList(),
                        ExportType.InactiveOnly => all.Where(p => !p.IsActive).ToList(),
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

        private void ExportToExcel_Click(object sender, RoutedEventArgs e) => ExecuteExport("Excel", ExportProductsToExcel);
        private void ExportToPdf_Click(object sender, RoutedEventArgs e) => ExecuteExport("PDF", ExportProductsToPdf);
        private void ExportToCsv_Click(object sender, RoutedEventArgs e) => ExecuteExport("CSV", ExportProductsToCsv);

        private void ExportProductsToExcel(List<Product> products, string filePath)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("المنتجات");
            string[] headers = { "ID", "الاسم", "الكود", "السعر", "الوحدة", "التصنيف", "الحالة" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            int r = 2;
            foreach (var p in products)
            {
                ws.Cell(r, 1).Value = p.Id;
                ws.Cell(r, 2).Value = p.Name;
                ws.Cell(r, 3).Value = p.SKU;
                ws.Cell(r, 4).Value = p.Price;
                ws.Cell(r, 5).Value = p.Unit;
                ws.Cell(r, 6).Value = p.Category;
                ws.Cell(r, 7).Value = p.IsActive ? "نشط" : "معطل";
                r++;
            }
            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private void ExportProductsToPdf(List<Product> products, string filePath)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.ContentFromRightToLeft();
                    page.Header().Text("تقرير المنتجات").FontSize(20).SemiBold().AlignCenter();
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(3); c.RelativeColumn(); c.RelativeColumn(); });
                        table.Header(h =>
                        {
                            h.Cell().Background("#2C3E50").Padding(5).Text("ID").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("الاسم").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("السعر").FontColor("#FFF");
                            h.Cell().Background("#2C3E50").Padding(5).Text("الحالة").FontColor("#FFF");
                        });
                        foreach (var p in products)
                        {
                            table.Cell().BorderBottom(1).Padding(5).Text(p.Id.ToString());
                            table.Cell().BorderBottom(1).Padding(5).Text(p.Name);
                            table.Cell().BorderBottom(1).Padding(5).Text(p.Price.ToString("N2"));
                            table.Cell().BorderBottom(1).Padding(5).Text(p.IsActive ? "نشط" : "معطل");
                        }
                    });
                });
            }).GeneratePdf(filePath);
        }

        private void ExportProductsToCsv(List<Product> products, string filePath)
        {
            using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("ID,Name,SKU,Price,Unit,Status");
            foreach (var p in products)
                writer.WriteLine($"{p.Id},{p.Name},{p.SKU},{p.Price},{p.Unit},{(p.IsActive ? "Active" : "Inactive")}");
        }

        #endregion

        public void Dispose()
        {
            if (_viewModel != null) _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            // ✅ إزالة معالجات الأحداث
            this.MouseDown -= ProductsView_MouseDown;
            this.Loaded -= ProductsView_Loaded;

            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                mainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
            }
        }
    }
}