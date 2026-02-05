using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class InvoiceArchiveViewModel : BaseViewModel
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;

        private ObservableCollection<Invoice> _completedInvoices;
        private ObservableCollection<Customer> _customers;
        private Invoice _selectedInvoice;

        // ✅ فلترة
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
                // ✅ تحديث حالة الأزرار الإضافية
                (ExportPdfCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (ExportPdfWithoutPricesCommand as RelayCommand)?.NotifyCanExecuteChanged();
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

        // ✅ الأزرار الأساسية (دائماً ظاهرة)
        public RelayCommand ExportExcelCommand { get; }      // تصدير Excel
        public RelayCommand PrintPdfCommand { get; }         // طباعة PDF لكل الفواتير

        // ✅ الأزرار الإضافية (تظهر فقط عند تحديد فاتورة)
        public RelayCommand ExportPdfCommand { get; }        // تصدير PDF للفاتورة المحددة
        public RelayCommand ExportPdfWithoutPricesCommand { get; } // تصدير PDF بدون أسعار للفاتورة المحددة

        // ✅ أزرار الفلترة
        public RelayCommand SearchCommand { get; }
        public RelayCommand ResetFilterCommand { get; }

        public InvoiceArchiveViewModel(IInvoiceRepository invoiceRepository, ICustomerRepository customerRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;

            CompletedInvoices = new ObservableCollection<Invoice>();
            Customers = new ObservableCollection<Customer>();

            _fromDate = new DateTime(DateTime.Now.Year, 1, 1);
            _toDate = DateTime.Now;

            // ✅ الأزرار الأساسية
            ExportExcelCommand = new RelayCommand(_ => ExportExcel());
            PrintPdfCommand = new RelayCommand(_ => PrintPdf());

            // ✅ الأزرار الإضافية (تتطلب تحديد فاتورة)
            ExportPdfCommand = new RelayCommand(_ => ExportPdf(), _ => SelectedInvoice != null);
            ExportPdfWithoutPricesCommand = new RelayCommand(_ => ExportPdfWithoutPrices(), _ => SelectedInvoice != null);

            // ✅ أزرار الفلترة
            SearchCommand = new RelayCommand(_ => Search());
            ResetFilterCommand = new RelayCommand(_ => ResetFilter());

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // تحميل الفواتير المكتملة فقط
                var allInvoices = _invoiceRepository.GetAll();
                var completedInvoices = allInvoices
                    .Where(i => i.Status == InvoiceStatus.Completed)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToList();

                CompletedInvoices = new ObservableCollection<Invoice>(completedInvoices);

                // تحميل العملاء النشطين فقط
                var customers = _customerRepository.GetAll();
                Customers.Clear();
                Customers.Add(new Customer { Name = "الكل", Id = 0 }); // خيار "الكل"
                foreach (var customer in customers.Where(c => c.IsActive))
                {
                    Customers.Add(customer);
                }

                SelectedCustomer = Customers.FirstOrDefault();
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
                var allInvoices = _invoiceRepository.GetAll()
                    .Where(i => i.Status == InvoiceStatus.Completed)
                    .ToList();

                // ✅ فلترة حسب التاريخ
                var filtered = allInvoices.Where(i =>
                    i.InvoiceDate.Date >= FromDate.Date &&
                    i.InvoiceDate.Date <= ToDate.Date
                ).ToList();

                // ✅ فلترة حسب العميل
                if (SelectedCustomer != null && SelectedCustomer.Id != 0) // ليس "الكل"
                {
                    filtered = filtered.Where(i => i.Customer?.Id == SelectedCustomer.Id).ToList();
                }

                // ✅ فلترة حسب المبلغ
                if (!string.IsNullOrWhiteSpace(MinAmount) && decimal.TryParse(MinAmount, out decimal minAmount))
                {
                    filtered = filtered.Where(i => i.FinalAmount >= minAmount).ToList();
                }

                CompletedInvoices = new ObservableCollection<Invoice>(
                    filtered.OrderByDescending(i => i.InvoiceDate)
                );

                MessageBox.Show($"تم العثور على {filtered.Count} فاتورة", "نتيجة البحث",
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
        }

        // ✅ دالة: تصدير Excel (للفواتير المفلترة)
        private void ExportExcel()
        {
            try
            {
                if (CompletedInvoices.Count == 0)
                {
                    MessageBox.Show("لا توجد فواتير للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(
                    $"سيتم تصدير {CompletedInvoices.Count} فاتورة إلى ملف Excel\n" +
                    "هذه الوظيفة قيد التطوير وسيتم تفعيلها قريباً.",
                    "تصدير إلى Excel",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: سيتم إضافة كود التصدير إلى Excel هنا لاحقاً
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ دالة: طباعة PDF (للفواتير المفلترة)
        private void PrintPdf()
        {
            try
            {
                if (CompletedInvoices.Count == 0)
                {
                    MessageBox.Show("لا توجد فواتير للطباعة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(
                    $"سيتم طباعة {CompletedInvoices.Count} فاتورة كملف PDF واحد\n" +
                    "هذه الوظيفة قيد التطوير وسيتم تفعيلها قريباً.",
                    "طباعة PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: سيتم إضافة كود إنشاء PDF لجميع الفواتير هنا لاحقاً
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ دالة: تصدير PDF للفاتورة المحددة
        private void ExportPdf()
        {
            if (SelectedInvoice == null) return;

            try
            {
                MessageBox.Show(
                    $"سيتم تصدير الفاتورة رقم {SelectedInvoice.Id} كملف PDF\n" +
                    "هذه الوظيفة قيد التطوير وسيتم تفعيلها قريباً.",
                    "تصدير PDF للفاتورة المحددة",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: سيتم إضافة كود إنشاء PDF للفاتورة المحددة هنا لاحقاً
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ دالة: تصدير PDF بدون أسعار للفاتورة المحددة
        private void ExportPdfWithoutPrices()
        {
            if (SelectedInvoice == null) return;

            try
            {
                MessageBox.Show(
                    $"سيتم تصدير الفاتورة رقم {SelectedInvoice.Id} كملف PDF بدون أسعار\n" +
                    "هذه الوظيفة قيد التطوير وسيتم تفعيلها قريباً.",
                    "تصدير PDF بدون أسعار",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: سيتم إضافة كود إنشاء PDF بدون أسعار للفاتورة المحددة هنا لاحقاً
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}