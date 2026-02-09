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

        // Cache للتقارير
        private readonly Dictionary<string, object> _reportCache = new Dictionary<string, object>();
        private DateTime _lastCacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(2);

        // مؤقت لتأخير إنشاء التقرير (Debouncing)
        private System.Threading.Timer _reportGenerationTimer;
        private readonly object _timerLock = new object();
        private const int GENERATION_DELAY_MS = 300;

        // Cancelation token لإلغاء العمليات السابقة
        private System.Threading.CancellationTokenSource _currentCts;

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
                // Cancel any previous operation
                _currentCts?.Cancel();
                _currentCts = new System.Threading.CancellationTokenSource();

                _reportGenerationTimer?.Dispose();
                _reportGenerationTimer = new System.Threading.Timer(
                    _ => GenerateReportInternal(_currentCts.Token),
                    null,
                    GENERATION_DELAY_MS,
                    System.Threading.Timeout.Infinite
                );
            }
        }

        private async void GenerateReportInternal(System.Threading.CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    IsLoading = true;
                    HasReportData = false;

                    ClearAllReports();

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    switch (SelectedReportType)
                    {
                        case ReportType.Summary:
                            await LoadSalesSummaryAsync(cancellationToken);
                            break;

                        case ReportType.TopProducts:
                            await LoadTopProductsAsync(cancellationToken);
                            break;

                        case ReportType.LeastProducts:
                            await LoadLeastProductsAsync(cancellationToken);
                            break;

                        case ReportType.TopPayingCustomers:
                            await LoadTopPayingCustomersAsync(cancellationToken);
                            break;

                        case ReportType.TopInvoiceCustomers:
                            await LoadTopInvoiceCustomersAsync(cancellationToken);
                            break;

                        case ReportType.DailySales:
                            await LoadDailySalesAsync(cancellationToken);
                            break;

                        case ReportType.MonthlySales:
                            await LoadMonthlySalesAsync(cancellationToken);
                            break;

                        case ReportType.Inventory:
                            await LoadInventoryReportAsync(cancellationToken);
                            break;
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        HasReportData = HasReportDataMethod();
                    }
                }
                catch (OperationCanceledException)
                {
                    // تم إلغاء العملية عمداً
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
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        IsLoading = false;
                    }
                }
            });
        }

        private void LoadInitialReport()
        {
            ScheduleReportGeneration();
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

        #region Cache Methods
        private string GetCacheKey(ReportType reportType)
        {
            return $"{reportType}_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}_{TopLimit}_{SelectedYear}";
        }

        private bool TryGetFromCache<T>(string cacheKey, out T cachedData)
        {
            // تنظيف الـ Cache إذا انتهت صلاحيته
            if (DateTime.Now - _lastCacheTime > _cacheDuration)
            {
                _reportCache.Clear();
                _lastCacheTime = DateTime.Now;
                cachedData = default;
                return false;
            }

            if (_reportCache.TryGetValue(cacheKey, out object data))
            {
                cachedData = (T)data;
                return true;
            }

            cachedData = default;
            return false;
        }

        private void AddToCache(string cacheKey, object data)
        {
            _reportCache[cacheKey] = data;
            _lastCacheTime = DateTime.Now;
        }
        #endregion

        #region Async Load Methods

        private async Task LoadSalesSummaryAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.Summary);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<SalesSummaryReport>(cacheKey, out var cachedSummary))
                {
                    SalesSummary = cachedSummary;

                    if (SalesSummary != null)
                    {
                        QuickStatsText = $"📊 {FromDate:yyyy/MM/dd} - {ToDate:yyyy/MM/dd}: {SalesSummary.TotalInvoices} فاتورة، {SalesSummary.TotalSalesAmount:0.00} جنيهاً";
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var summary = await Task.Run(() =>
                    _reportRepository.GetSalesSummary(FromDate, ToDate), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                SalesSummary = summary;

                if (summary != null)
                {
                    QuickStatsText = $"📊 {FromDate:yyyy/MM/dd} - {ToDate:yyyy/MM/dd}: {summary.TotalInvoices} فاتورة، {summary.TotalSalesAmount:0.00} جنيهاً";

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, summary);
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

        private async Task LoadTopProductsAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.TopProducts);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<ProductSalesReport>>(cacheKey, out var cachedProducts))
                {
                    TopSellingProducts.Clear();
                    if (cachedProducts != null)
                    {
                        foreach (var product in cachedProducts)
                        {
                            TopSellingProducts.Add(product);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var result = await Task.Run(() =>
                    _reportRepository.GetTopSellingProducts(FromDate, ToDate, null, TopLimit),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                TopSellingProducts.Clear();
                if (result.Products != null && result.Products.Any())
                {
                    foreach (var product in result.Products)
                    {
                        TopSellingProducts.Add(product);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, result.Products.ToList());
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

        private async Task LoadLeastProductsAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.LeastProducts);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<ProductSalesReport>>(cacheKey, out var cachedProducts))
                {
                    LeastSellingProducts.Clear();
                    if (cachedProducts != null)
                    {
                        foreach (var product in cachedProducts)
                        {
                            LeastSellingProducts.Add(product);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var result = await Task.Run(() =>
                    _reportRepository.GetLeastSellingProducts(FromDate, ToDate, null, TopLimit),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                LeastSellingProducts.Clear();
                if (result.Products != null && result.Products.Any())
                {
                    foreach (var product in result.Products)
                    {
                        LeastSellingProducts.Add(product);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, result.Products.ToList());
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

        private async Task LoadTopPayingCustomersAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.TopPayingCustomers);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<CustomerReport>>(cacheKey, out var cachedCustomers))
                {
                    TopPayingCustomers.Clear();
                    if (cachedCustomers != null)
                    {
                        foreach (var customer in cachedCustomers)
                        {
                            TopPayingCustomers.Add(customer);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var result = await Task.Run(() =>
                    _reportRepository.GetTopPayingCustomers(FromDate, ToDate, TopLimit),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                TopPayingCustomers.Clear();
                if (result.Customers != null && result.Customers.Any())
                {
                    foreach (var customer in result.Customers)
                    {
                        TopPayingCustomers.Add(customer);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, result.Customers.ToList());
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

        private async Task LoadTopInvoiceCustomersAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.TopInvoiceCustomers);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<CustomerReport>>(cacheKey, out var cachedCustomers))
                {
                    TopInvoiceCustomers.Clear();
                    if (cachedCustomers != null)
                    {
                        foreach (var customer in cachedCustomers)
                        {
                            TopInvoiceCustomers.Add(customer);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var result = await Task.Run(() =>
                    _reportRepository.GetTopInvoiceCustomers(FromDate, ToDate, TopLimit),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                TopInvoiceCustomers.Clear();
                if (result.Customers != null && result.Customers.Any())
                {
                    foreach (var customer in result.Customers)
                    {
                        TopInvoiceCustomers.Add(customer);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, result.Customers.ToList());
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

        private async Task LoadDailySalesAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.DailySales);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<DailySalesReport>>(cacheKey, out var cachedSales))
                {
                    DailySales.Clear();
                    if (cachedSales != null)
                    {
                        foreach (var day in cachedSales)
                        {
                            DailySales.Add(day);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var dailySales = await Task.Run(() =>
                    _reportRepository.GetDailySales(FromDate, ToDate),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                DailySales.Clear();
                if (dailySales != null && dailySales.Any())
                {
                    foreach (var day in dailySales)
                    {
                        DailySales.Add(day);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, dailySales.ToList());
                }
                else
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
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

        private async Task LoadMonthlySalesAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.MonthlySales);

                // محاولة جبد البيانات من الـ Cache
                if (TryGetFromCache<List<MonthlySalesReport>>(cacheKey, out var cachedSales))
                {
                    MonthlySales.Clear();
                    if (cachedSales != null)
                    {
                        foreach (var month in cachedSales)
                        {
                            MonthlySales.Add(month);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var monthlySales = await Task.Run(() =>
                    _reportRepository.GetMonthlySales(SelectedYear),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                MonthlySales.Clear();
                if (monthlySales != null && monthlySales.Any())
                {
                    foreach (var month in monthlySales)
                    {
                        MonthlySales.Add(month);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, monthlySales.ToList());
                }
                else
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
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

        private async Task LoadInventoryReportAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(ReportType.Inventory);

                // محاولة جلب البيانات من الـ Cache
                if (TryGetFromCache<List<InventoryReport>>(cacheKey, out var cachedInventory))
                {
                    InventoryReport.Clear();
                    if (cachedInventory != null)
                    {
                        foreach (var item in cachedInventory.Take(TopLimit))
                        {
                            InventoryReport.Add(item);
                        }
                    }
                    return;
                }

                // جلب البيانات من قاعدة البيانات
                var inventory = await Task.Run(() =>
                    _reportRepository.GetInventoryReport(),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                InventoryReport.Clear();
                if (inventory != null && inventory.Any())
                {
                    foreach (var item in inventory.Take(TopLimit))
                    {
                        InventoryReport.Add(item);
                    }

                    // حفظ في الـ Cache
                    AddToCache(cacheKey, inventory.ToList());
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

        #endregion

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

        private async void ExportToExcel()
        {
            if (!HasReportData || IsLoading)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                await Task.Run(() =>
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
                });

                IsLoading = false;
                MessageBox.Show("تم تصدير التقرير بنجاح إلى Excel", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                IsLoading = false;
                MessageBox.Show($"خطأ أثناء التصدير إلى Excel: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportToPdf()
        {
            if (!HasReportData || IsLoading)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                await Task.Run(() =>
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
                });

                IsLoading = false;
                MessageBox.Show("تم تصدير التقرير بنجاح إلى PDF", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                IsLoading = false;
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

            // مسح الـ Cache
            _reportCache.Clear();

            ScheduleReportGeneration();
        }

        public void Dispose()
        {
            lock (_timerLock)
            {
                _reportGenerationTimer?.Dispose();
                _reportGenerationTimer = null;

                _currentCts?.Cancel();
                _currentCts?.Dispose();
                _currentCts = null;
            }

            TopSellingProducts?.Clear();
            LeastSellingProducts?.Clear();
            TopPayingCustomers?.Clear();
            TopInvoiceCustomers?.Clear();
            DailySales?.Clear();
            MonthlySales?.Clear();
            InventoryReport?.Clear();

            _reportCache?.Clear();
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