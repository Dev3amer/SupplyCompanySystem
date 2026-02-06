using ClosedXML.Excel;
using Microsoft.Win32;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class InvoiceArchiveViewModel : BaseViewModel, System.IDisposable
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;

        private ObservableCollection<Invoice> _completedInvoices;
        private ObservableCollection<Customer> _customers;
        private Invoice _selectedInvoice;

        private DateTime _fromDate;
        private DateTime _toDate;
        private Customer _selectedCustomer;
        private string _minAmount;

        // ⭐ خصائص Pagination الجديدة
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private bool _isLoading = false;

        // ⭐ قائمة أحجام الصفحات المتاحة
        public List<int> AvailablePageSizes { get; } = new List<int> { 10, 25, 50, 100, 250 };

        public ObservableCollection<Invoice> CompletedInvoices
        {
            get => _completedInvoices;
            set
            {
                _completedInvoices = value;
                OnPropertyChanged(nameof(CompletedInvoices));
                ExportExcelCommand?.NotifyCanExecuteChanged();
                PrintPdfCommand?.NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged(nameof(Customers));
            }
        }

        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged(nameof(SelectedInvoice));
                ExportPdfCommand?.NotifyCanExecuteChanged();
                ExportPdfWithoutPricesCommand?.NotifyCanExecuteChanged();
                ReturnToDraftCommand?.NotifyCanExecuteChanged();
            }
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged(nameof(FromDate));
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                _toDate = value;
                OnPropertyChanged(nameof(ToDate));
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged(nameof(SelectedCustomer));
            }
        }

        public string MinAmount
        {
            get => _minAmount;
            set
            {
                _minAmount = value;
                OnPropertyChanged(nameof(MinAmount));
            }
        }

        // ⭐ خصائص Pagination
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value < 1)
                {
                    _currentPage = 1;
                }
                else if (TotalPages > 0 && value > TotalPages)
                {
                    _currentPage = TotalPages;
                }
                else
                {
                    _currentPage = value;
                }

                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PaginationText));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));

                if (!_isLoading)
                    LoadData();
            }
        }

        // ⭐ تحديث خاصية PageSize
        public int PageSize
        {
            get => _pageSize;
            set
            {
                _pageSize = value;
                OnPropertyChanged(nameof(PageSize));

                // ⭐ العودة للصفحة الأولى مع إعادة حساب Pagination
                _currentPage = 1;
                OnPropertyChanged(nameof(CurrentPage));
                LoadData();
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                _totalCount = value;
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(PaginationText));
                OnPropertyChanged(nameof(TotalCountText));
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                _totalPages = value;
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PaginationText));
                OnPropertyChanged(nameof(CanGoToLastPage));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // ⭐ خصائص التحكم في أزرار Pagination
        public bool CanGoToFirstPage => CurrentPage > 1;
        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        public bool CanGoToLastPage => CurrentPage < TotalPages;

        public string PaginationText =>
            $"الصفحة {CurrentPage} من {TotalPages}";

        public string TotalCountText =>
            $"إجمالي الفواتير: {TotalCount} فاتورة";

        // ⭐ أوامر Pagination الجديدة
        public RelayCommand FirstPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand LastPageCommand { get; }

        public RelayCommand ExportExcelCommand { get; }
        public RelayCommand PrintPdfCommand { get; }
        public RelayCommand ExportPdfCommand { get; }
        public RelayCommand ExportPdfWithoutPricesCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand ResetFilterCommand { get; }
        public RelayCommand ReturnToDraftCommand { get; }

        public InvoiceArchiveViewModel(IInvoiceRepository invoiceRepository, ICustomerRepository customerRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;

            CompletedInvoices = new ObservableCollection<Invoice>();
            Customers = new ObservableCollection<Customer>();

            _fromDate = new DateTime(DateTime.Now.Year, 1, 1);
            _toDate = DateTime.Now;

            // ⭐ تهيئة أوامر Pagination
            FirstPageCommand = new RelayCommand(_ => CurrentPage = 1,
                _ => CanGoToFirstPage);

            PreviousPageCommand = new RelayCommand(_ => CurrentPage--,
                _ => CanGoToPreviousPage);

            NextPageCommand = new RelayCommand(_ => CurrentPage++,
                _ => CanGoToNextPage);

            LastPageCommand = new RelayCommand(_ => CurrentPage = TotalPages,
                _ => CanGoToLastPage);

            // ⭐ تعديل CanExecute للأزرار
            ExportExcelCommand = new RelayCommand(_ => ExportFilteredInvoicesToExcel(),
                _ => CompletedInvoices != null && CompletedInvoices.Count > 0);

            PrintPdfCommand = new RelayCommand(_ => PrintAllFilteredInvoices(),
                _ => CompletedInvoices != null && CompletedInvoices.Count > 0);

            ExportPdfCommand = new RelayCommand(_ => ExportSelectedInvoicePdf(),
                _ => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Completed);

            ExportPdfWithoutPricesCommand = new RelayCommand(_ => ExportSelectedInvoicePdfWithoutPrices(),
                _ => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Completed);

            SearchCommand = new RelayCommand(_ => Search());
            ResetFilterCommand = new RelayCommand(_ => ResetFilter());
            ReturnToDraftCommand = new RelayCommand(_ => ReturnToDraft(),
                _ => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Completed);

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                IsLoading = true;

                // ⭐ حفظ رقم الصفحة الحالي قبل البحث
                int currentPageBeforeSearch = CurrentPage;

                // ⭐ تحويل بيانات الفلترة
                int? customerId = (SelectedCustomer?.Id > 0) ? SelectedCustomer.Id : null;
                decimal? minAmountValue = null;

                if (!string.IsNullOrWhiteSpace(MinAmount) && decimal.TryParse(MinAmount, out decimal minAmount))
                {
                    minAmountValue = minAmount;
                }

                // ⭐ استخدام الدالة الجديدة مع Pagination
                var result = _invoiceRepository.GetCompletedInvoicesPaged(
                    pageNumber: CurrentPage,
                    pageSize: PageSize,
                    fromDate: FromDate,
                    toDate: ToDate,
                    customerId: customerId,
                    minAmount: minAmountValue);

                CompletedInvoices = new ObservableCollection<Invoice>(result.Invoices);
                TotalCount = result.TotalCount;

                // ⭐ حساب عدد الصفحات مع التحقق من عدم القسمة على صفر
                TotalPages = TotalCount > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;

                // ⭐ إذا كانت الصفحة الحالية أكبر من عدد الصفحات بعد البحث
                // نعود للصفحة الأولى تلقائياً
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = 1;
                }

                // ⭐ تحديث قائمة العملاء إذا كانت فارغة
                if (Customers.Count == 0)
                {
                    var customers = _customerRepository.GetAll();
                    Customers.Clear();
                    Customers.Add(new Customer { Name = "الكل", Id = 0 });
                    foreach (var customer in customers.Where(c => c.IsActive))
                    {
                        Customers.Add(customer);
                    }

                    SelectedCustomer = Customers.FirstOrDefault();
                }

                // ⭐ تحديث حالة الأزرار
                ExportExcelCommand?.NotifyCanExecuteChanged();
                PrintPdfCommand?.NotifyCanExecuteChanged();
                FirstPageCommand?.NotifyCanExecuteChanged();
                PreviousPageCommand?.NotifyCanExecuteChanged();
                NextPageCommand?.NotifyCanExecuteChanged();
                LastPageCommand?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

                // ⭐ في حالة الخطأ، نعود للصفحة الأولى
                CurrentPage = 1;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Search()
        {
            try
            {
                // ⭐ العودة للصفحة الأولى عند البحث
                CurrentPage = 1;
                LoadData();

                if (TotalCount > 0)
                {
                    MessageBox.Show($"تم العثور على {TotalCount} عرض أسعار", "نتيجة البحث",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على عروض أسعار", "نتيجة البحث",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SelectedInvoice = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ResetFilter()
        {
            try
            {
                FromDate = new DateTime(DateTime.Now.Year, 1, 1);
                ToDate = DateTime.Now;
                SelectedCustomer = Customers.FirstOrDefault();
                MinAmount = string.Empty;

                // ⭐ العودة للصفحة الأولى
                CurrentPage = 1;
                LoadData();

                SelectedInvoice = null;

                MessageBox.Show("تم إعادة تعيين الفلترة بنجاح", "إعادة التعيين",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إعادة التعيين: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ⭐ دالة جديدة: استعراض خيارات التصدير
        private ExportOption AskExportOption()
        {
            var dialog = new Window
            {
                Title = "خيارات التصدير",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            ExportOption? selectedOption = null;

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = "ما الذي تريد تصديره؟",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14
            });

            var options = new[]
            {
                ("الصفحة الحالية فقط", ExportOption.CurrentPage),
                ("جميع الفواتير المفلترة", ExportOption.AllFiltered)
            };

            foreach (var opt in options)
            {
                var btn = new Button
                {
                    Content = opt.Item1,
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btn.Click += (s, e) => { selectedOption = opt.Item2; dialog.Close(); };
                stack.Children.Add(btn);
            }

            var closeBtn = new Button
            {
                Content = "إلغاء",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(5),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White
            };
            closeBtn.Click += (s, e) => dialog.Close();
            stack.Children.Add(closeBtn);

            dialog.Content = stack;
            dialog.ShowDialog();
            return selectedOption ?? ExportOption.Cancel;
        }

        private enum ExportOption
        {
            Cancel,
            CurrentPage,
            AllFiltered
        }

        private void ExportFilteredInvoicesToExcel()
        {
            try
            {
                var exportOption = AskExportOption();
                if (exportOption == ExportOption.Cancel) return;

                List<Invoice> invoicesToExport;
                string exportTypeText;

                if (exportOption == ExportOption.CurrentPage)
                {
                    invoicesToExport = CompletedInvoices.ToList();
                    exportTypeText = $"الصفحة {CurrentPage} (عدد {invoicesToExport.Count})";
                }
                else
                {
                    // ⭐ جلب جميع الفواتير المفلترة
                    int? customerId = (SelectedCustomer?.Id > 0) ? SelectedCustomer.Id : null;
                    decimal? minAmountValue = null;

                    if (!string.IsNullOrWhiteSpace(MinAmount) && decimal.TryParse(MinAmount, out decimal minAmount))
                    {
                        minAmountValue = minAmount;
                    }

                    invoicesToExport = _invoiceRepository.GetCompletedInvoicesFiltered(
                        fromDate: FromDate,
                        toDate: ToDate,
                        customerId: customerId,
                        minAmount: minAmountValue);

                    exportTypeText = $"جميع الفواتير المفلترة (عدد {invoicesToExport.Count})";
                }

                if (invoicesToExport.Count == 0)
                {
                    MessageBox.Show("لا توجد فواتير للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"عروض_الأسعار_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = $"تصدير {exportTypeText}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("عروض_الأسعار");

                        // ⭐ عنوان التصدير بناء على النوع
                        var titleCell = worksheet.Cell(1, 1);
                        titleCell.Value = exportOption == ExportOption.CurrentPage
                            ? $"قائمة عروض الأسعار - الصفحة {CurrentPage}"
                            : "قائمة عروض الأسعار - جميع الفواتير المفلترة";
                        titleCell.Style.Font.Bold = true;
                        titleCell.Style.Font.FontSize = 14;
                        titleCell.Style.Font.FontColor = XLColor.White;
                        titleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
                        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Range(1, 1, 1, 11).Merge();

                        // ⭐ معلومات التصدير
                        worksheet.Cell(2, 1).Value = $"الفترة من: {FromDate:yyyy/MM/dd} إلى: {ToDate:yyyy/MM/dd}";
                        worksheet.Cell(3, 1).Value = $"عدد الفواتير: {invoicesToExport.Count}";
                        worksheet.Cell(3, 1).Style.Font.Bold = true;

                        if (exportOption == ExportOption.CurrentPage)
                        {
                            worksheet.Cell(4, 1).Value = $"الصفحة: {CurrentPage} من {TotalPages}";
                        }

                        worksheet.Cell(2, 9).Value = $"تاريخ التصدير: {DateTime.Now:yyyy/MM/dd HH:mm}";

                        var headers = new string[]
                        {
                            "رقم مسلسل",
                            "اسم العميل",
                            "التاريخ",
                            "تاريخ الإكمال",
                            "الإجمالي قبل الخصم",
                            "قيمة الخصم",
                            "الإجمالي النهائي",
                            "عدد الأصناف",
                            "الكمية الإجمالية",
                            "الحالة",
                            "ملاحظات"
                        };

                        int headerRow = exportOption == ExportOption.CurrentPage ? 6 : 5;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(headerRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.Black;
                        }

                        int dataRow = headerRow + 1;
                        foreach (var invoice in invoicesToExport)
                        {
                            var fullInvoice = _invoiceRepository.GetByIdWithItems(invoice.Id);

                            int itemCount = fullInvoice?.Items?.Count ?? 0;
                            decimal totalQuantity = fullInvoice?.Items?.Sum(i => i.Quantity) ?? 0;
                            decimal discountAmount = invoice.InvoiceDiscountAmount;

                            worksheet.Cell(dataRow, 1).Value = invoice.Id;
                            worksheet.Cell(dataRow, 2).Value = invoice.Customer?.Name ?? "";
                            worksheet.Cell(dataRow, 3).Value = invoice.InvoiceDate.ToString("yyyy/MM/dd");
                            worksheet.Cell(dataRow, 4).Value = invoice.CompletedDate?.ToString("yyyy/MM/dd HH:mm") ?? "";
                            worksheet.Cell(dataRow, 5).Value = invoice.TotalAmount;
                            worksheet.Cell(dataRow, 6).Value = discountAmount;
                            worksheet.Cell(dataRow, 7).Value = invoice.FinalAmount;
                            worksheet.Cell(dataRow, 8).Value = itemCount;
                            worksheet.Cell(dataRow, 9).Value = totalQuantity;
                            worksheet.Cell(dataRow, 10).Value = GetInvoiceStatusArabic(invoice.Status);
                            worksheet.Cell(dataRow, 11).Value = invoice.Notes ?? "";

                            for (int i = 5; i <= 7; i++)
                            {
                                var cell = worksheet.Cell(dataRow, i);
                                cell.Style.NumberFormat.Format = "#,##0.00";
                                cell.Style.Font.Bold = true;
                                cell.Style.Font.FontColor = XLColor.FromArgb(39, 174, 96);
                            }

                            for (int i = 1; i <= 11; i++)
                            {
                                var cell = worksheet.Cell(dataRow, i);
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                cell.Style.Border.OutsideBorderColor = XLColor.Gray;
                            }

                            dataRow++;
                        }

                        worksheet.Columns("A:C").Width = 15;
                        worksheet.Columns("D").Width = 18;
                        worksheet.Columns("E:G").Width = 20;
                        worksheet.Columns("H:I").Width = 15;
                        worksheet.Column("J").Width = 12;
                        worksheet.Column("K").Width = 25;

                        worksheet.Columns("A,A,C,D,H,I,J").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Columns("B").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Columns("E,F,G").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        int totalRow = dataRow + 1;
                        worksheet.Cell(totalRow, 3).Value = "الإجماليات:";
                        worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
                        worksheet.Cell(totalRow, 3).Style.Font.FontSize = 12;

                        worksheet.Cell(totalRow, 5).Value = invoicesToExport.Sum(i => i.TotalAmount);
                        worksheet.Cell(totalRow, 6).Value = invoicesToExport.Sum(i => i.InvoiceDiscountAmount);
                        worksheet.Cell(totalRow, 7).Value = invoicesToExport.Sum(i => i.FinalAmount);

                        for (int i = 5; i <= 7; i++)
                        {
                            var cell = worksheet.Cell(totalRow, i);
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 12;
                            cell.Style.Font.FontColor = XLColor.FromArgb(231, 76, 60);
                            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(253, 227, 226);
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                            cell.Style.Border.OutsideBorderColor = XLColor.DarkRed;
                        }

                        workbook.SaveAs(filePath);
                    }

                    MessageBox.Show(
                        $"تم تصدير {invoicesToExport.Count} عرض أسعار بنجاح إلى ملف Excel\n" +
                        $"المسار: {filePath}",
                        "تصدير ناجح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير إلى Excel: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintAllFilteredInvoices()
        {
            try
            {
                var exportOption = AskExportOption();
                if (exportOption == ExportOption.Cancel) return;

                List<Invoice> invoicesToPrint;
                string exportTypeText;

                if (exportOption == ExportOption.CurrentPage)
                {
                    invoicesToPrint = CompletedInvoices.ToList();
                    exportTypeText = $"الصفحة {CurrentPage}";
                }
                else
                {
                    // ⭐ جلب جميع الفواتير المفلترة
                    int? customerId = (SelectedCustomer?.Id > 0) ? SelectedCustomer.Id : null;
                    decimal? minAmountValue = null;

                    if (!string.IsNullOrWhiteSpace(MinAmount) && decimal.TryParse(MinAmount, out decimal minAmount))
                    {
                        minAmountValue = minAmount;
                    }

                    invoicesToPrint = _invoiceRepository.GetCompletedInvoicesFiltered(
                        fromDate: FromDate,
                        toDate: ToDate,
                        customerId: customerId,
                        minAmount: minAmountValue);

                    exportTypeText = "جميع الفواتير المفلترة";
                }

                if (invoicesToPrint.Count == 0)
                {
                    MessageBox.Show("لا توجد فواتير للطباعة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"عروض_الأسعار_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = $"طباعة {exportTypeText}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    List<Invoice> fullInvoices = new List<Invoice>();

                    ProgressWindow progressWindow = new ProgressWindow();
                    progressWindow.Title = $"جاري تجميع {exportTypeText}";
                    progressWindow.Show();

                    try
                    {
                        int current = 0;
                        int total = invoicesToPrint.Count;

                        foreach (var invoice in invoicesToPrint)
                        {
                            current++;
                            progressWindow.UpdateProgress($"جاري تحميل عرض الأسعار {current} من {total}",
                                (int)((current * 100) / total));

                            var fullInvoice = _invoiceRepository.GetByIdWithItems(invoice.Id);
                            if (fullInvoice != null)
                            {
                                fullInvoices.Add(fullInvoice);
                            }
                        }

                        progressWindow.UpdateProgress("جاري إنشاء ملف PDF...", 100);

                        BulkInvoicePdfGenerator.GenerateBulkInvoicesPdf(fullInvoices, filePath, FromDate, ToDate);

                        progressWindow.Close();

                        MessageBox.Show(
                            $"تم طباعة {fullInvoices.Count} عرض أسعار بنجاح إلى ملف PDF واحد\n" +
                            $"المسار: {filePath}\n\n" +
                            "سيتم فتح الملف الآن للعرض.",
                            "طباعة ناجحة",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        progressWindow?.Close();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}\n\n{ex.InnerException?.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnToDraft()
        {
            if (SelectedInvoice == null) return;

            var result = MessageBox.Show(
                $"هل تريد إعادة عرض الأسعار رقم {SelectedInvoice.Id} إلى حالة المسودة؟\n\n" +
                "ملاحظة: سيتم إزالة الفاتورة من قائمة العروض المكتملة وإضافتها لقائمة المسودة.",
                "إعادة إلى المسودة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = _invoiceRepository.ReturnToDraft(SelectedInvoice.Id);

                    if (success)
                    {
                        MessageBox.Show(
                            $"تم إعادة عرض الأسعار رقم {SelectedInvoice.Id} إلى المسودة بنجاح.\n" +
                            "يمكنك الآن تعديله من صفحة عروض الأسعار الجديدة.",
                            "تمت العملية",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // ⭐ إعادة تحميل البيانات بعد الحذف
                        CompletedInvoices.Remove(SelectedInvoice);
                        TotalCount--;
                        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

                        if (CurrentPage > TotalPages && TotalPages > 0)
                            CurrentPage = TotalPages;

                        SelectedInvoice = null;
                    }
                    else
                    {
                        MessageBox.Show("تعذر إعادة الفاتورة إلى المسودة. يرجى المحاولة مرة أخرى.",
                            "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في إعادة الفاتورة إلى المسودة: {ex.Message}",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetInvoiceStatusArabic(InvoiceStatus status)
        {
            switch (status)
            {
                case InvoiceStatus.Completed:
                    return "مكتملة";
                case InvoiceStatus.Draft:
                    return "مسودة";
                case InvoiceStatus.Cancelled:
                    return "ملغية";
                default:
                    return status.ToString();
            }
        }

        private void ExportSelectedInvoicePdf()
        {
            if (SelectedInvoice == null) return;

            try
            {
                var fullInvoice = _invoiceRepository.GetByIdWithItems(SelectedInvoice.Id);
                if (fullInvoice == null)
                {
                    MessageBox.Show("تعذر تحميل بيانات عرض الأسعار", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = InvoicePdfGenerator.GenerateInvoiceFileName(fullInvoice);
                string pdfPath = InvoicePdfGenerator.GenerateInvoicePdf(fullInvoice, fileName);

                MessageBox.Show(
                    $"تم تصدير عرض الأسعار رقم {fullInvoice.Id} بنجاح!\n" +
                    $"الملف: {Path.GetFileName(pdfPath)}",
                    "تصدير PDF ناجح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير PDF: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSelectedInvoicePdfWithoutPrices()
        {
            if (SelectedInvoice == null) return;

            try
            {
                var fullInvoice = _invoiceRepository.GetByIdWithItems(SelectedInvoice.Id);
                if (fullInvoice == null)
                {
                    MessageBox.Show("تعذر تحميل بيانات عرض الأسعار", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = InvoicePdfGenerator.GenerateInvoiceFileName(fullInvoice) + "_بدون_أسعار";
                string pdfPath = InvoicePdfGenerator.GenerateInvoicePdfWithoutPrices(fullInvoice, fileName);

                MessageBox.Show(
                    $"تم تصدير عرض الأسعار رقم {fullInvoice.Id} بدون أسعار!\n" +
                    $"الملف: {Path.GetFileName(pdfPath)}",
                    "تصدير PDF بدون أسعار",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير PDF بدون أسعار: {ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearInvoiceSelection()
        {
            SelectedInvoice = null;
        }

        public void Dispose()
        {
            try
            {
                _completedInvoices?.Clear();
                _customers?.Clear();
                _selectedInvoice = null;
                _selectedCustomer = null;
            }
            catch
            {
            }
        }
    }
}