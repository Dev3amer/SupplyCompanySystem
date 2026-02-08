using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.UI.Commands;
using SupplyCompanySystem.UI.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class ReportsViewModel : BaseViewModel, IDisposable
    {
        private readonly IReportRepository _reportRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private readonly ReportExcelExporter _excelExporter;
        private readonly ReportPdfExporter _pdfExporter;

        // فلترة التقارير
        private DateTime _fromDate;
        private DateTime _toDate;
        private int _selectedYear;
        private int _topLimit = 10;

        // أنواع التقارير
        private ReportType _selectedReportType;

        // بيانات التقارير
        private SalesSummaryReport _salesSummary;
        private ObservableCollection<ProductSalesReport> _topSellingProducts;
        private ObservableCollection<ProductSalesReport> _leastSellingProducts;
        private ObservableCollection<CustomerReport> _topPayingCustomers;
        private ObservableCollection<CustomerReport> _topInvoiceCustomers;
        private ObservableCollection<DailySalesReport> _dailySales;
        private ObservableCollection<MonthlySalesReport> _monthlySales;
        private ObservableCollection<InventoryReport> _inventoryReport;

        // قائمة أنواع التقارير
        private List<ReportType> _reportTypes;

        // إحصائيات سريعة
        private string _quickStatsText;
        private bool _isLoading;
        private bool _hasReportData;

        // مؤقت لتأخير إنشاء التقرير (Debouncing)
        private System.Threading.Timer _reportGenerationTimer;
        private readonly object _timerLock = new object();
        private const int GENERATION_DELAY_MS = 500;

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged(nameof(FromDate));
                    if (IsSalesReport || SelectedReportType == ReportType.Summary || SelectedReportType == ReportType.DailySales)
                    {
                        ScheduleReportGeneration();
                    }
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnPropertyChanged(nameof(ToDate));
                    if (IsSalesReport || SelectedReportType == ReportType.Summary || SelectedReportType == ReportType.DailySales)
                    {
                        ScheduleReportGeneration();
                    }
                }
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged(nameof(SelectedYear));
                    if (SelectedReportType == ReportType.MonthlySales)
                    {
                        ScheduleReportGeneration();
                    }
                }
            }
        }

        public int TopLimit
        {
            get => _topLimit;
            set
            {
                if (_topLimit != value)
                {
                    _topLimit = value;
                    OnPropertyChanged(nameof(TopLimit));
                    if (IsTopListReport)
                    {
                        ScheduleReportGeneration();
                    }
                }
            }
        }

        public ReportType SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (_selectedReportType != value)
                {
                    _selectedReportType = value;
                    OnPropertyChanged(nameof(SelectedReportType));
                    OnPropertyChanged(nameof(IsProductReport));
                    OnPropertyChanged(nameof(IsCustomerReport));
                    OnPropertyChanged(nameof(IsSalesReport));
                    OnPropertyChanged(nameof(IsMonthlySalesReport));
                    OnPropertyChanged(nameof(IsInventoryReport));
                    OnPropertyChanged(nameof(IsSummaryReport));
                    OnPropertyChanged(nameof(IsTopListReport));
                    OnPropertyChanged(nameof(IsDailySalesReport));

                    ScheduleReportGeneration();
                }
            }
        }

        public bool IsProductReport => SelectedReportType == ReportType.TopProducts ||
                                      SelectedReportType == ReportType.LeastProducts;

        public bool IsCustomerReport => SelectedReportType == ReportType.TopPayingCustomers ||
                                       SelectedReportType == ReportType.TopInvoiceCustomers;

        public bool IsSalesReport => SelectedReportType == ReportType.DailySales ||
                                    SelectedReportType == ReportType.Summary ||
                                    SelectedReportType == ReportType.TopPayingCustomers ||
                                    SelectedReportType == ReportType.TopInvoiceCustomers;

        public bool IsMonthlySalesReport => SelectedReportType == ReportType.MonthlySales;

        public bool IsInventoryReport => SelectedReportType == ReportType.Inventory;

        public bool IsSummaryReport => SelectedReportType == ReportType.Summary;

        public bool IsTopListReport => IsProductReport || IsCustomerReport || SelectedReportType == ReportType.Inventory;

        public bool IsDailySalesReport => SelectedReportType == ReportType.DailySales;

        public List<ReportType> ReportTypes
        {
            get => _reportTypes;
            set { _reportTypes = value; OnPropertyChanged(nameof(ReportTypes)); }
        }

        public SalesSummaryReport SalesSummary
        {
            get => _salesSummary;
            set { _salesSummary = value; OnPropertyChanged(nameof(SalesSummary)); }
        }

        public ObservableCollection<ProductSalesReport> TopSellingProducts
        {
            get => _topSellingProducts;
            set { _topSellingProducts = value; OnPropertyChanged(nameof(TopSellingProducts)); }
        }

        public ObservableCollection<ProductSalesReport> LeastSellingProducts
        {
            get => _leastSellingProducts;
            set { _leastSellingProducts = value; OnPropertyChanged(nameof(LeastSellingProducts)); }
        }

        public ObservableCollection<CustomerReport> TopPayingCustomers
        {
            get => _topPayingCustomers;
            set { _topPayingCustomers = value; OnPropertyChanged(nameof(TopPayingCustomers)); }
        }

        public ObservableCollection<CustomerReport> TopInvoiceCustomers
        {
            get => _topInvoiceCustomers;
            set { _topInvoiceCustomers = value; OnPropertyChanged(nameof(TopInvoiceCustomers)); }
        }

        public ObservableCollection<DailySalesReport> DailySales
        {
            get => _dailySales;
            set { _dailySales = value; OnPropertyChanged(nameof(DailySales)); }
        }

        public ObservableCollection<MonthlySalesReport> MonthlySales
        {
            get => _monthlySales;
            set { _monthlySales = value; OnPropertyChanged(nameof(MonthlySales)); }
        }

        public ObservableCollection<InventoryReport> InventoryReport
        {
            get => _inventoryReport;
            set { _inventoryReport = value; OnPropertyChanged(nameof(InventoryReport)); }
        }

        public string QuickStatsText
        {
            get => _quickStatsText;
            set { _quickStatsText = value; OnPropertyChanged(nameof(QuickStatsText)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public bool HasReportData
        {
            get => _hasReportData;
            set { _hasReportData = value; OnPropertyChanged(nameof(HasReportData)); }
        }

        public List<int> AvailableYears { get; }

        public List<int> AvailableLimits { get; } = new List<int> { 5, 10, 15, 20, 25 };

        public RelayCommand ExportExcelCommand { get; }
        public RelayCommand ExportPdfCommand { get; }
        public RelayCommand ResetFiltersCommand { get; }
        public RelayCommand ShowQuickStatsCommand { get; }

        public ReportsViewModel(IReportRepository reportRepository,
                              ICustomerRepository customerRepository,
                              IProductRepository productRepository)
        {
            _reportRepository = reportRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _excelExporter = new ReportExcelExporter();
            _pdfExporter = new ReportPdfExporter();

            FromDate = DateTime.Now.AddDays(-30);
            ToDate = DateTime.Now;
            SelectedYear = DateTime.Now.Year;

            AvailableYears = Enumerable.Range(2020, DateTime.Now.Year - 2020 + 2).ToList();

            ReportTypes = new List<ReportType>
            {
                ReportType.Summary,
                ReportType.TopProducts,
                ReportType.LeastProducts,
                ReportType.TopPayingCustomers,
                ReportType.TopInvoiceCustomers,
                ReportType.DailySales,
                ReportType.MonthlySales,
                ReportType.Inventory
            };

            SelectedReportType = ReportType.Summary;

            TopSellingProducts = new ObservableCollection<ProductSalesReport>();
            LeastSellingProducts = new ObservableCollection<ProductSalesReport>();
            TopPayingCustomers = new ObservableCollection<CustomerReport>();
            TopInvoiceCustomers = new ObservableCollection<CustomerReport>();
            DailySales = new ObservableCollection<DailySalesReport>();
            MonthlySales = new ObservableCollection<MonthlySalesReport>();
            InventoryReport = new ObservableCollection<InventoryReport>();

            ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => HasReportData && !IsLoading);
            ExportPdfCommand = new RelayCommand(_ => ExportToPdf(), _ => HasReportData && !IsLoading);
            ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
            ShowQuickStatsCommand = new RelayCommand(_ => ShowQuickStats());

            LoadInitialReport();
        }

        private void ScheduleReportGeneration()
        {
            lock (_timerLock)
            {
                _reportGenerationTimer?.Dispose();
                _reportGenerationTimer = new System.Threading.Timer(
                    _ => GenerateReportInternal(),
                    null,
                    GENERATION_DELAY_MS,
                    System.Threading.Timeout.Infinite
                );
            }
        }

        private void GenerateReportInternal()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    IsLoading = true;
                    HasReportData = false;

                    ClearAllReports();

                    switch (SelectedReportType)
                    {
                        case ReportType.Summary:
                            LoadSalesSummary();
                            break;

                        case ReportType.TopProducts:
                            LoadTopProducts();
                            break;

                        case ReportType.LeastProducts:
                            LoadLeastProducts();
                            break;

                        case ReportType.TopPayingCustomers:
                            LoadTopPayingCustomers();
                            break;

                        case ReportType.TopInvoiceCustomers:
                            LoadTopInvoiceCustomers();
                            break;

                        case ReportType.DailySales:
                            LoadDailySales();
                            break;

                        case ReportType.MonthlySales:
                            LoadMonthlySales();
                            break;

                        case ReportType.Inventory:
                            LoadInventoryReport();
                            break;
                    }

                    HasReportData = HasReportDataMethod();
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                    HasReportData = false;
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        private void LoadInitialReport()
        {
            GenerateReportInternal();
        }

        private void ClearAllReports()
        {
            SalesSummary = null;
            TopSellingProducts.Clear();
            LeastSellingProducts.Clear();
            TopPayingCustomers.Clear();
            TopInvoiceCustomers.Clear();
            DailySales.Clear();
            MonthlySales.Clear();
            InventoryReport.Clear();
        }

        private bool HasReportDataMethod()
        {
            return SelectedReportType switch
            {
                ReportType.Summary => SalesSummary != null,
                ReportType.TopProducts => TopSellingProducts.Any(),
                ReportType.LeastProducts => LeastSellingProducts.Any(),
                ReportType.TopPayingCustomers => TopPayingCustomers.Any(),
                ReportType.TopInvoiceCustomers => TopInvoiceCustomers.Any(),
                ReportType.DailySales => DailySales.Any(),
                ReportType.MonthlySales => MonthlySales.Any(),
                ReportType.Inventory => InventoryReport.Any(),
                _ => false
            };
        }

        private void LoadSalesSummary()
        {
            try
            {
                var summary = _reportRepository.GetSalesSummary(FromDate, ToDate);
                SalesSummary = summary;

                if (summary != null)
                {
                    QuickStatsText = $"📊 {FromDate:yyyy/MM/dd} - {ToDate:yyyy/MM/dd}: {summary.TotalInvoices} فاتورة، {summary.TotalSalesAmount:0.00} جنيهاً";
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل ملخص المبيعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadTopProducts()
        {
            try
            {
                var (products, totalSales, totalItems) = _reportRepository.GetTopSellingProducts(
                    FromDate, ToDate, null, TopLimit);

                TopSellingProducts.Clear();
                if (products != null && products.Any())
                {
                    foreach (var product in products)
                    {
                        TopSellingProducts.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل أكثر المنتجات مبيعاً: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadLeastProducts()
        {
            try
            {
                var (products, totalSales, totalItems) = _reportRepository.GetLeastSellingProducts(
                    FromDate, ToDate, null, TopLimit);

                LeastSellingProducts.Clear();
                if (products != null && products.Any())
                {
                    foreach (var product in products)
                    {
                        LeastSellingProducts.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل أقل المنتجات مبيعاً: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadTopPayingCustomers()
        {
            try
            {
                var (customers, totalAmount) = _reportRepository.GetTopPayingCustomers(
                    FromDate, ToDate, TopLimit);

                TopPayingCustomers.Clear();
                if (customers != null && customers.Any())
                {
                    foreach (var customer in customers)
                    {
                        TopPayingCustomers.Add(customer);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل أكثر العملاء إنفاقاً: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadTopInvoiceCustomers()
        {
            try
            {
                var (customers, totalAmount) = _reportRepository.GetTopInvoiceCustomers(
                    FromDate, ToDate, TopLimit);

                TopInvoiceCustomers.Clear();
                if (customers != null && customers.Any())
                {
                    foreach (var customer in customers)
                    {
                        TopInvoiceCustomers.Add(customer);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل أكثر العملاء طلباً للعروض: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadDailySales()
        {
            try
            {
                var dailySales = _reportRepository.GetDailySales(FromDate, ToDate);

                DailySales.Clear();
                if (dailySales != null && dailySales.Any())
                {
                    foreach (var day in dailySales)
                    {
                        DailySales.Add(day);
                    }
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"لا توجد بيانات للمبيعات اليومية في الفترة من {FromDate:yyyy/MM/dd} إلى {ToDate:yyyy/MM/dd}",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل المبيعات اليومية: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadMonthlySales()
        {
            try
            {
                var monthlySales = _reportRepository.GetMonthlySales(SelectedYear);

                MonthlySales.Clear();
                if (monthlySales != null && monthlySales.Any())
                {
                    foreach (var month in monthlySales)
                    {
                        MonthlySales.Add(month);
                    }
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"لا توجد بيانات للمبيعات الشهرية لسنة {SelectedYear}",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل المبيعات الشهرية: {ex.Message}\n\nتفاصيل: {ex.InnerException?.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadInventoryReport()
        {
            try
            {
                var inventory = _reportRepository.GetInventoryReport();

                InventoryReport.Clear();
                if (inventory != null && inventory.Any())
                {
                    foreach (var item in inventory.Take(TopLimit))
                    {
                        InventoryReport.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show($"خطأ في تحميل تقرير المخزون: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private void LoadQuickStats()
        {
            try
            {
                var lastMonth = DateTime.Now.AddMonths(-1);
                var summary = _reportRepository.GetSalesSummary(lastMonth, DateTime.Now);

                if (summary != null)
                {
                    QuickStatsText = $"📈 الشهر الماضي: {summary.TotalInvoices} فاتورة - {summary.TotalSalesAmount:0.00} جنيهاً - {summary.TotalProfitAmount:0.00} ربح";
                }
                else
                {
                    QuickStatsText = "لا توجد بيانات للإحصائيات السريعة";
                }
            }
            catch
            {
                QuickStatsText = "جاري تحميل الإحصائيات...";
            }
        }

        private void ShowQuickStats()
        {
            LoadQuickStats();
            MessageBox.Show(QuickStatsText, "إحصائيات سريعة",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #region التصدير

        private void ExportToExcel()
        {
            if (!HasReportData || IsLoading)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                switch (SelectedReportType)
                {
                    case ReportType.Summary:
                        _excelExporter.ExportSummaryToExcel(SalesSummary, FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.TopProducts:
                        _excelExporter.ExportProductsToExcel(TopSellingProducts.ToList(), FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.LeastProducts:
                        _excelExporter.ExportProductsToExcel(LeastSellingProducts.ToList(), FromDate, ToDate, SelectedReportType, true);
                        break;

                    case ReportType.TopPayingCustomers:
                        _excelExporter.ExportCustomersToExcel(TopPayingCustomers.ToList(), FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.TopInvoiceCustomers:
                        _excelExporter.ExportCustomersToExcel(TopInvoiceCustomers.ToList(), FromDate, ToDate, SelectedReportType, true);
                        break;

                    case ReportType.DailySales:
                        _excelExporter.ExportDailySalesToExcel(DailySales.ToList(), FromDate, ToDate);
                        break;

                    case ReportType.MonthlySales:
                        _excelExporter.ExportMonthlySalesToExcel(MonthlySales.ToList(), SelectedYear);
                        break;

                    case ReportType.Inventory:
                        _excelExporter.ExportInventoryToExcel(InventoryReport.ToList(), FromDate, ToDate);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التصدير إلى Excel: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf()
        {
            if (!HasReportData || IsLoading)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                switch (SelectedReportType)
                {
                    case ReportType.Summary:
                        _pdfExporter.ExportSummaryToPdf(SalesSummary, FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.TopProducts:
                        _pdfExporter.ExportProductsToPdf(TopSellingProducts.ToList(), FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.LeastProducts:
                        _pdfExporter.ExportProductsToPdf(LeastSellingProducts.ToList(), FromDate, ToDate, SelectedReportType, true);
                        break;

                    case ReportType.TopPayingCustomers:
                        _pdfExporter.ExportCustomersToPdf(TopPayingCustomers.ToList(), FromDate, ToDate, SelectedReportType);
                        break;

                    case ReportType.TopInvoiceCustomers:
                        _pdfExporter.ExportCustomersToPdf(TopInvoiceCustomers.ToList(), FromDate, ToDate, SelectedReportType, true);
                        break;

                    case ReportType.DailySales:
                        _pdfExporter.ExportDailySalesToPdf(DailySales.ToList(), FromDate, ToDate);
                        break;

                    case ReportType.MonthlySales:
                        _pdfExporter.ExportMonthlySalesToPdf(MonthlySales.ToList(), SelectedYear);
                        break;

                    case ReportType.Inventory:
                        _pdfExporter.ExportInventoryToPdf(InventoryReport.ToList(), FromDate, ToDate);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التصدير إلى PDF: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region دوال مساعدة

        private string GetReportTypeArabic()
        {
            return SelectedReportType switch
            {
                ReportType.Summary => "ملخص المبيعات",
                ReportType.TopProducts => "أكثر المنتجات مبيعاً",
                ReportType.LeastProducts => "أقل المنتجات مبيعاً",
                ReportType.TopPayingCustomers => "أكثر العملاء إنفاقاً",
                ReportType.TopInvoiceCustomers => "أكثر العملاء طلباً للعروض",
                ReportType.DailySales => "المبيعات اليومية",
                ReportType.MonthlySales => "المبيعات الشهرية",
                ReportType.Inventory => "تقرير المخزون",
                _ => "تقرير"
            };
        }

        private void ResetFilters()
        {
            FromDate = DateTime.Now.AddDays(-30);
            ToDate = DateTime.Now;
            SelectedYear = DateTime.Now.Year;
            TopLimit = 10;

            ScheduleReportGeneration();
        }

        public void Dispose()
        {
            lock (_timerLock)
            {
                _reportGenerationTimer?.Dispose();
                _reportGenerationTimer = null;
            }

            TopSellingProducts?.Clear();
            LeastSellingProducts?.Clear();
            TopPayingCustomers?.Clear();
            TopInvoiceCustomers?.Clear();
            DailySales?.Clear();
            MonthlySales?.Clear();
            InventoryReport?.Clear();
        }

        #endregion
    }

    public enum ReportType
    {
        Summary,
        TopProducts,
        LeastProducts,
        TopPayingCustomers,
        TopInvoiceCustomers,
        DailySales,
        MonthlySales,
        Inventory
    }
}
