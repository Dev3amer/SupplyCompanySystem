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

        public ObservableCollection<Invoice> CompletedInvoices
        {
            get => _completedInvoices;
            set
            {
                _completedInvoices = value;
                OnPropertyChanged(nameof(CompletedInvoices));
                // ⭐ تحديث CanExecute للأزرار عند تغيير القائمة
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
                // ⭐ تحديث CanExecute للأزرار عند تغيير التحديد
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
                var completedInvoices = _invoiceRepository.GetCompletedInvoices();
                CompletedInvoices = new ObservableCollection<Invoice>(completedInvoices);

                var customers = _customerRepository.GetAll();
                Customers.Clear();
                Customers.Add(new Customer { Name = "الكل", Id = 0 });
                foreach (var customer in customers.Where(c => c.IsActive))
                {
                    Customers.Add(customer);
                }

                SelectedCustomer = Customers.FirstOrDefault();

                // ⭐ تحديث حالة الأزرار بعد تحميل البيانات
                ExportExcelCommand?.NotifyCanExecuteChanged();
                PrintPdfCommand?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search()
        {
            try
            {
                var completedInvoices = _invoiceRepository.GetCompletedInvoices();

                var filtered = completedInvoices.Where(i =>
                    i.InvoiceDate.Date >= FromDate.Date &&
                    i.InvoiceDate.Date <= ToDate.Date
                ).ToList();

                if (SelectedCustomer != null && SelectedCustomer.Id != 0)
                {
                    filtered = filtered.Where(i => i.Customer?.Id == SelectedCustomer.Id).ToList();
                }

                if (!string.IsNullOrWhiteSpace(MinAmount) && decimal.TryParse(MinAmount, out decimal minAmount))
                {
                    filtered = filtered.Where(i => i.FinalAmount >= minAmount).ToList();
                }

                CompletedInvoices = new ObservableCollection<Invoice>(
                    filtered.OrderByDescending(i => i.CompletedDate ?? i.InvoiceDate)
                );

                // ✅ إلغاء التحديد بعد البحث
                SelectedInvoice = null;

                MessageBox.Show($"تم العثور على {filtered.Count} عرض أسعار", "نتيجة البحث",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetFilter()
        {
            FromDate = new DateTime(DateTime.Now.Year, 1, 1);
            ToDate = DateTime.Now;
            SelectedCustomer = Customers.FirstOrDefault();
            MinAmount = string.Empty;
            LoadData();
            SelectedInvoice = null;
        }

        // ⭐ دالة جديدة: إعادة الفاتورة المكتملة إلى المسودة
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

                        // إزالة الفاتورة من القائمة الحالية
                        CompletedInvoices.Remove(SelectedInvoice);
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

        private void ExportFilteredInvoicesToExcel()
        {
            try
            {
                if (CompletedInvoices.Count == 0)
                {
                    MessageBox.Show("لا توجد عروض أسعار للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"عروض_الأسعار_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير عروض الأسعار إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("عروض_الأسعار");

                        var titleCell = worksheet.Cell(1, 1);
                        titleCell.Value = "قائمة عروض الأسعار السابقة";
                        titleCell.Style.Font.Bold = true;
                        titleCell.Style.Font.FontSize = 14;
                        titleCell.Style.Font.FontColor = XLColor.White;
                        titleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
                        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Range(1, 1, 1, 10).Merge();

                        worksheet.Cell(2, 1).Value = $"الفترة من: {FromDate:yyyy/MM/dd} إلى: {ToDate:yyyy/MM/dd}";
                        worksheet.Cell(3, 1).Value = $"عدد عروض الأسعار: {CompletedInvoices.Count}";
                        worksheet.Cell(3, 1).Style.Font.Bold = true;

                        worksheet.Cell(2, 8).Value = $"تاريخ التصدير: {DateTime.Now:yyyy/MM/dd HH:mm}";

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
                            "الحالة"
                        };

                        int headerRow = 5;
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
                        foreach (var invoice in CompletedInvoices)
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

                            for (int i = 5; i <= 7; i++)
                            {
                                var cell = worksheet.Cell(dataRow, i);
                                cell.Style.NumberFormat.Format = "#,##0.00";
                                cell.Style.Font.Bold = true;
                                cell.Style.Font.FontColor = XLColor.FromArgb(39, 174, 96);
                            }

                            for (int i = 1; i <= 10; i++)
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

                        worksheet.Columns("A,A,C,D,H,I").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Columns("B").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        worksheet.Columns("E,F,G").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        int totalRow = dataRow + 1;
                        worksheet.Cell(totalRow, 3).Value = "الإجماليات:";
                        worksheet.Cell(totalRow, 3).Style.Font.Bold = true;
                        worksheet.Cell(totalRow, 3).Style.Font.FontSize = 12;

                        worksheet.Cell(totalRow, 5).Value = CompletedInvoices.Sum(i => i.TotalAmount);
                        worksheet.Cell(totalRow, 6).Value = CompletedInvoices.Sum(i => i.InvoiceDiscountAmount);
                        worksheet.Cell(totalRow, 7).Value = CompletedInvoices.Sum(i => i.FinalAmount);

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
                        $"تم تصدير {CompletedInvoices.Count} عرض أسعار بنجاح إلى ملف Excel\n" +
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

        private void PrintAllFilteredInvoices()
        {
            try
            {
                if (CompletedInvoices.Count == 0)
                {
                    MessageBox.Show("لا توجد عروض أسعار للطباعة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"عروض_الأسعار_{FromDate:yyyy-MM-dd}_إلى_{ToDate:yyyy-MM-dd}.pdf",
                    Title = "طباعة عروض الأسعار إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    List<Invoice> fullInvoices = new List<Invoice>();

                    ProgressWindow progressWindow = new ProgressWindow();
                    progressWindow.Title = "جاري تجميع عروض الأسعار";
                    progressWindow.Show();

                    try
                    {
                        int current = 0;
                        int total = CompletedInvoices.Count;

                        foreach (var invoice in CompletedInvoices)
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

        // ✅ دالة عامة لإلغاء التحديد يمكن استدعاؤها من View
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