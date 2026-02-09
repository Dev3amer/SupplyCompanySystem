using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using SupplyCompanySystem.UI.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class InvoiceViewModel : BaseViewModel, IDisposable
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private DispatcherTimer _updateTimer;

        private ObservableCollection<Invoice> _invoices;
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Product> _products;
        private ObservableCollection<InvoiceItem> _invoiceItems;

        private CollectionViewSource _customersViewSource;
        private CollectionViewSource _productsViewSource;

        private string _customerSearchText;
        private string _productSearchText;
        private string _tempCustomerSearchText = string.Empty;
        private string _tempProductSearchText = string.Empty;

        private Invoice _currentInvoice;
        private Invoice _selectedInvoiceFromList;
        private InvoiceItem _selectedInvoiceItem;
        private Customer _selectedCustomer;
        private Product _selectedProduct;
        private string _productQuantity;
        private string _productDiscount;
        private string _productProfitMargin;
        private string _profitMarginPercentage;
        private string _invoiceDiscountPercentage;
        private bool _isEditMode;
        private bool _isSaved;

        private DateTime _selectedInvoiceDate;

        private bool _isSelectingCustomer = false;
        private bool _isSelectingProduct = false;

        private bool _isUpdatingFromUser = false;

        private decimal _totalBeforeDiscount = 0;
        private decimal _totalItemsDiscount = 0;
        private decimal _subTotalAfterItemsDiscount = 0;
        private decimal _invoiceDiscountAmount = 0;
        private decimal _finalAmountCalculated = 0;
        private decimal _totalProfitAmount = 0;
        private decimal _totalProductProfit = 0;
        private decimal _totalInvoiceProfit = 0;

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set { _invoices = value; OnPropertyChanged(nameof(Invoices)); }
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged(nameof(Customers));
                if (_customersViewSource != null)
                {
                    _customersViewSource.Source = _customers;
                    _customersViewSource.View?.Refresh();
                }
            }
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged(nameof(Products));
                if (_productsViewSource != null)
                {
                    _productsViewSource.Source = _products;
                    _productsViewSource.View?.Refresh();
                }
            }
        }

        public string CustomerSearchText
        {
            get => _customerSearchText;
            set
            {
                if (_customerSearchText != value)
                {
                    _customerSearchText = value;
                    OnPropertyChanged(nameof(CustomerSearchText));
                    _customersViewSource?.View?.Refresh();
                }
            }
        }

        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (_productSearchText != value)
                {
                    _productSearchText = value;
                    OnPropertyChanged(nameof(ProductSearchText));
                    _productsViewSource?.View?.Refresh();
                }
            }
        }

        private bool _isFirstCustomerFocus = true;
        private bool _isFirstProductFocus = true;
        public string TempCustomerSearchText
        {
            get => _tempCustomerSearchText;
            set
            {
                if (_tempCustomerSearchText != value)
                {
                    _tempCustomerSearchText = value;
                    OnPropertyChanged(nameof(TempCustomerSearchText));

                    // تحديث البحث الفعلي
                    CustomerSearchText = value;

                    // إذا كان أول تركيز، فتح القائمة فارغة
                    if (_isFirstCustomerFocus && string.IsNullOrWhiteSpace(value))
                    {
                        _isFirstCustomerFocus = false;
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                        {
                            // تأخير فتح القائمة لضمان تحديث العرض أولاً
                            if (FilteredCustomersView != null)
                            {
                                FilteredCustomersView.Refresh();
                            }
                        }), DispatcherPriority.Background);
                    }
                }
            }
        }

        public string TempProductSearchText
        {
            get => _tempProductSearchText;
            set
            {
                if (_tempProductSearchText != value)
                {
                    _tempProductSearchText = value;
                    OnPropertyChanged(nameof(TempProductSearchText));

                    // تحديث البحث الفعلي
                    ProductSearchText = value;

                    // إذا كان أول تركيز، فتح القائمة فارغة
                    if (_isFirstProductFocus && string.IsNullOrWhiteSpace(value))
                    {
                        _isFirstProductFocus = false;
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                        {
                            if (FilteredProductsView != null)
                            {
                                FilteredProductsView.Refresh();
                            }
                        }), DispatcherPriority.Background);
                    }
                }
            }
        }

        public string ProductProfitMarginFormatted
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_productProfitMargin))
                    return string.Empty;

                if (decimal.TryParse(_productProfitMargin, out decimal value))
                {
                    if (value == Math.Floor(value))
                        return value.ToString("0");
                    else
                        return value.ToString("0.00");
                }

                return _productProfitMargin;
            }
            set
            {
                if (_productProfitMargin != value)
                {
                    _productProfitMargin = value;
                    OnPropertyChanged(nameof(ProductProfitMargin));
                    OnPropertyChanged(nameof(ProductProfitMarginFormatted));
                    (AddItemCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public ICollectionView FilteredCustomersView => _customersViewSource?.View;

        public ICollectionView FilteredProductsView => _productsViewSource?.View;

        public ObservableCollection<InvoiceItem> InvoiceItems
        {
            get => _invoiceItems;
            set
            {
                if (_invoiceItems != null)
                {
                    _invoiceItems.CollectionChanged -= InvoiceItems_CollectionChanged;
                    foreach (var item in _invoiceItems)
                    {
                        if (item is INotifyPropertyChanged notifyItem)
                            notifyItem.PropertyChanged -= InvoiceItem_PropertyChanged;
                    }
                }

                _invoiceItems = value;

                if (_invoiceItems != null)
                {
                    _invoiceItems.CollectionChanged += InvoiceItems_CollectionChanged;
                    foreach (var item in _invoiceItems)
                    {
                        if (item is INotifyPropertyChanged notifyItem)
                            notifyItem.PropertyChanged += InvoiceItem_PropertyChanged;
                    }
                }

                OnPropertyChanged(nameof(InvoiceItems));
                SyncInvoiceItems();
                RecalculateTotals();
            }
        }

        public Invoice CurrentInvoice
        {
            get => _currentInvoice;
            set
            {
                if (_currentInvoice != null)
                    ((INotifyPropertyChanged)_currentInvoice).PropertyChanged -= CurrentInvoice_PropertyChanged;

                _currentInvoice = value;

                if (_currentInvoice != null)
                {
                    ((INotifyPropertyChanged)_currentInvoice).PropertyChanged += CurrentInvoice_PropertyChanged;
                    ProfitMarginPercentage = _currentInvoice.ProfitMarginPercentage != 0
                        ? _currentInvoice.ProfitMarginPercentage.ToString()
                        : string.Empty;

                    InvoiceDiscountPercentage = _currentInvoice.InvoiceDiscountPercentage != 0
                        ? _currentInvoice.InvoiceDiscountPercentage.ToString()
                        : string.Empty;

                    SelectedInvoiceDate = _currentInvoice.InvoiceDate;
                }

                OnPropertyChanged(nameof(CurrentInvoice));
                RecalculateTotals();
            }
        }

        public Invoice SelectedInvoiceFromList
        {
            get => _selectedInvoiceFromList;
            set { _selectedInvoiceFromList = value; OnPropertyChanged(nameof(SelectedInvoiceFromList)); }
        }

        public InvoiceItem SelectedInvoiceItem
        {
            get => _selectedInvoiceItem;
            set
            {
                _selectedInvoiceItem = value;
                OnPropertyChanged(nameof(SelectedInvoiceItem));
                (DeleteItemCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;

                    if (_currentInvoice != null && value != null)
                    {
                        // ✅ إصلاح: نستخدم CustomerId فقط، وليس كائن Customer كامل
                        _currentInvoice.CustomerId = value.Id;
                    }

                    OnPropertyChanged(nameof(SelectedCustomer));
                }
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged(nameof(SelectedProduct));
                }
            }
        }

        public DateTime SelectedInvoiceDate
        {
            get => _selectedInvoiceDate;
            set
            {
                if (_selectedInvoiceDate != value)
                {
                    _selectedInvoiceDate = value;
                    OnPropertyChanged(nameof(SelectedInvoiceDate));

                    if (_currentInvoice != null)
                    {
                        _currentInvoice.InvoiceDate = value;
                    }
                }
            }
        }

        public string ProductQuantity
        {
            get => _productQuantity;
            set
            {
                _productQuantity = value;
                OnPropertyChanged(nameof(ProductQuantity));
                (AddItemCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public string ProductDiscount
        {
            get => _productDiscount;
            set
            {
                if (_productDiscount != value)
                {
                    _productDiscount = value;
                    OnPropertyChanged(nameof(ProductDiscount));
                }
            }
        }

        public string ProductProfitMargin
        {
            get => _productProfitMargin;
            set
            {
                if (_productProfitMargin != value)
                {
                    _productProfitMargin = value;
                    OnPropertyChanged(nameof(ProductProfitMargin));
                }
            }
        }

        public string ProfitMarginPercentage
        {
            get => _profitMarginPercentage;
            set
            {
                if (_profitMarginPercentage != value)
                {
                    _profitMarginPercentage = value;
                    OnPropertyChanged(nameof(ProfitMarginPercentage));

                    if (_currentInvoice != null)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            _currentInvoice.ProfitMarginPercentage = 0;
                        }
                        else if (decimal.TryParse(value, out decimal decimalValue))
                        {
                            _currentInvoice.ProfitMarginPercentage = decimalValue;
                        }
                    }

                    _isUpdatingFromUser = true;
                    RecalculateAllItemsPrices();
                    RecalculateTotalsInternal();
                    _isUpdatingFromUser = false;
                }
            }
        }

        public string InvoiceDiscountPercentage
        {
            get => _invoiceDiscountPercentage;
            set
            {
                if (_invoiceDiscountPercentage != value)
                {
                    _invoiceDiscountPercentage = value;
                    OnPropertyChanged(nameof(InvoiceDiscountPercentage));

                    if (_currentInvoice != null)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            _currentInvoice.InvoiceDiscountPercentage = 0;
                        }
                        else if (decimal.TryParse(value, out decimal decimalValue))
                        {
                            _currentInvoice.InvoiceDiscountPercentage = decimalValue;
                        }
                    }

                    _isUpdatingFromUser = true;
                    RecalculateTotalsInternal();
                    _isUpdatingFromUser = false;
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(nameof(IsEditMode)); }
        }

        public bool IsSaved
        {
            get => _isSaved;
            set { _isSaved = value; OnPropertyChanged(nameof(IsSaved)); }
        }

        public decimal TotalBeforeDiscount
        {
            get => _totalBeforeDiscount;
            private set
            {
                if (_totalBeforeDiscount != value)
                {
                    _totalBeforeDiscount = value;
                    OnPropertyChanged(nameof(TotalBeforeDiscount));
                }
            }
        }

        public decimal TotalItemsDiscount
        {
            get => _totalItemsDiscount;
            private set
            {
                if (_totalItemsDiscount != value)
                {
                    _totalItemsDiscount = value;
                    OnPropertyChanged(nameof(TotalItemsDiscount));
                }
            }
        }

        public decimal SubTotalAfterItemsDiscount
        {
            get => _subTotalAfterItemsDiscount;
            private set
            {
                if (_subTotalAfterItemsDiscount != value)
                {
                    _subTotalAfterItemsDiscount = value;
                    OnPropertyChanged(nameof(SubTotalAfterItemsDiscount));
                }
            }
        }

        public decimal InvoiceDiscountAmount
        {
            get => _invoiceDiscountAmount;
            private set
            {
                if (_invoiceDiscountAmount != value)
                {
                    _invoiceDiscountAmount = value;
                    OnPropertyChanged(nameof(InvoiceDiscountAmount));
                }
            }
        }

        public decimal FinalAmountCalculated
        {
            get => _finalAmountCalculated;
            private set
            {
                if (_finalAmountCalculated != value)
                {
                    _finalAmountCalculated = value;
                    OnPropertyChanged(nameof(FinalAmountCalculated));
                }
            }
        }

        public decimal TotalProfitAmount
        {
            get => _totalProfitAmount;
            private set
            {
                if (_totalProfitAmount != value)
                {
                    _totalProfitAmount = value;
                    OnPropertyChanged(nameof(TotalProfitAmount));
                }
            }
        }

        public decimal TotalProductProfit
        {
            get => _totalProductProfit;
            private set
            {
                if (_totalProductProfit != value)
                {
                    _totalProductProfit = value;
                    OnPropertyChanged(nameof(TotalProductProfit));
                }
            }
        }

        public decimal TotalInvoiceProfit
        {
            get => _totalInvoiceProfit;
            private set
            {
                if (_totalInvoiceProfit != value)
                {
                    _totalInvoiceProfit = value;
                    OnPropertyChanged(nameof(TotalInvoiceProfit));
                }
            }
        }

        public decimal TotalDiscountAmount
        {
            get => TotalItemsDiscount + InvoiceDiscountAmount;
        }

        public RelayCommand NewInvoiceCommand { get; }
        public RelayCommand SaveInvoiceCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand AddItemCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand DeleteItemCommand { get; }
        public RelayCommand CompleteInvoiceCommand { get; }
        public RelayCommand EditInvoiceCommand { get; }
        public RelayCommand CancelInvoiceCommand { get; }
        public RelayCommand DeleteInvoiceCommand { get; }

        public RelayCommand ClearCustomerSearchCommand { get; }
        public RelayCommand ClearProductSearchCommand { get; }

        public InvoiceViewModel(IInvoiceRepository invoiceRepository, ICustomerRepository customerRepository, IProductRepository productRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;

            Invoices = new ObservableCollection<Invoice>();
            Customers = new ObservableCollection<Customer>();
            Products = new ObservableCollection<Product>();
            InvoiceItems = new ObservableCollection<InvoiceItem>();

            _customersViewSource = new CollectionViewSource { Source = Customers };
            _customersViewSource.Filter += CustomersViewSource_Filter;

            _productsViewSource = new CollectionViewSource { Source = Products };
            _productsViewSource.Filter += ProductsViewSource_Filter;

            _profitMarginPercentage = string.Empty;
            _invoiceDiscountPercentage = string.Empty;
            _productProfitMargin = string.Empty;
            _customerSearchText = string.Empty;
            _productSearchText = string.Empty;

            SelectedInvoiceDate = DateTime.Now;

            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddItemCommand = new RelayCommand(_ => AddItem(), _ => CanAddItem());
            RemoveItemCommand = new RelayCommand(_ => RemoveItem(), _ => InvoiceItems.Count > 0);
            DeleteItemCommand = new RelayCommand(_ => DeleteSelectedItem(), _ => SelectedInvoiceItem != null);
            CompleteInvoiceCommand = new RelayCommand(_ => CompleteInvoice(), _ => _currentInvoice != null && InvoiceItems.Count > 0 && _isSaved);
            EditInvoiceCommand = new RelayCommand(_ => EditInvoice());
            CancelInvoiceCommand = new RelayCommand(_ => CancelInvoice());
            DeleteInvoiceCommand = new RelayCommand(_ => DeleteInvoice());

            ClearCustomerSearchCommand = new RelayCommand(_ =>
            {
                CustomerSearchText = string.Empty;
                SelectedCustomer = null;
            });
            ClearProductSearchCommand = new RelayCommand(_ =>
            {
                ProductSearchText = string.Empty;
                SelectedProduct = null;
            });

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            LoadData();
        }

        private void CustomersViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Customer customer)
            {
                if (string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    e.Accepted = true;
                }
                else
                {
                    var searchText = CustomerSearchText.ToLower();

                    // بحث شامل في جميع الحقول مع تحسين الأداء
                    e.Accepted = customer.Name.ToLower().Contains(searchText) ||
                                 (customer.PhoneNumber != null && customer.PhoneNumber.ToLower().Contains(searchText)) ||
                                 (customer.Address != null && customer.Address.ToLower().Contains(searchText));
                }
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void ProductsViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is Product product)
            {
                if (string.IsNullOrWhiteSpace(ProductSearchText))
                {
                    e.Accepted = true;
                }
                else
                {
                    var searchText = ProductSearchText.ToLower();

                    // بحث شامل في جميع حقول المنتج
                    e.Accepted = product.Name.ToLower().Contains(searchText) ||
                                 (product.SKU != null && product.SKU.ToLower().Contains(searchText)) ||
                                 (product.Category != null && product.Category.ToLower().Contains(searchText));
                }
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isUpdatingFromUser)
            {
                RecalculateTotalsInternal();
            }
        }

        private void CurrentInvoice_PropertyChanged(object sender, PropertyChangedEventArgs e) { }

        private void InvoiceItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InvoiceItem item in e.NewItems)
                {
                    if (item is INotifyPropertyChanged notifyItem)
                        notifyItem.PropertyChanged += InvoiceItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (InvoiceItem item in e.OldItems)
                {
                    if (item is INotifyPropertyChanged notifyItem)
                        notifyItem.PropertyChanged -= InvoiceItem_PropertyChanged;
                }
            }

            SyncInvoiceItems();
            RecalculateTotalsInternal();
            (RemoveItemCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void InvoiceItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is InvoiceItem item)
            {
                if (e.PropertyName == nameof(InvoiceItem.Quantity) ||
                    e.PropertyName == nameof(InvoiceItem.UnitPrice) ||
                    e.PropertyName == nameof(InvoiceItem.DiscountPercentage) ||
                    e.PropertyName == nameof(InvoiceItem.ItemProfitMarginPercentage))
                {
                    decimal invoiceProfitMargin = 0;
                    if (!string.IsNullOrWhiteSpace(ProfitMarginPercentage) &&
                        decimal.TryParse(ProfitMarginPercentage, out decimal profitRate))
                    {
                        invoiceProfitMargin = profitRate;
                    }

                    UpdateInvoiceItemLineTotal(item, invoiceProfitMargin);

                    RecalculateTotalsInternal();
                }
            }
        }

        private void UpdateInvoiceItemLineTotal(InvoiceItem item, decimal invoiceProfitMargin)
        {
            if (item == null) return;

            decimal subtotalWithProductProfit = item.Quantity * item.UnitPrice;
            decimal discountAmount = (item.Quantity * item.OriginalUnitPrice * item.DiscountPercentage) / 100;
            decimal subtotalAfterDiscount = subtotalWithProductProfit - discountAmount;
            decimal invoiceProfitAmount = (subtotalAfterDiscount * invoiceProfitMargin) / 100;
            item.LineTotal = subtotalAfterDiscount + invoiceProfitAmount;
        }

        private decimal CalculatePriceWithProfitMargin(decimal basePrice, decimal profitMarginPercentage)
        {
            if (profitMarginPercentage == 0)
                return basePrice;

            return basePrice + (basePrice * profitMarginPercentage / 100);
        }

        private decimal CalculateCumulativePrice(decimal basePrice, decimal productProfitMargin, decimal invoiceProfitMargin)
        {
            return basePrice + (basePrice * productProfitMargin / 100);
        }

        private void RecalculateAllItemsPrices()
        {
            if (_currentInvoice?.Items == null || _currentInvoice.Items.Count == 0)
                return;

            decimal invoiceProfitMargin = 0;
            if (!string.IsNullOrWhiteSpace(ProfitMarginPercentage) &&
                decimal.TryParse(ProfitMarginPercentage, out decimal profitRate))
            {
                invoiceProfitMargin = profitRate;
            }

            foreach (var item in InvoiceItems)
            {
                if (item.Product != null)
                {
                    decimal basePrice = item.OriginalUnitPrice;
                    decimal itemProfitMargin = item.ItemProfitMarginPercentage;
                    decimal unitPriceWithProductProfit = basePrice + (basePrice * itemProfitMargin / 100);

                    item.UnitPrice = unitPriceWithProductProfit;
                    UpdateInvoiceItemLineTotal(item, invoiceProfitMargin);
                }
            }

            RecalculateTotalsInternal();
        }

        public void RecalculateTotals()
        {
            _isUpdatingFromUser = true;

            decimal invoiceProfitMargin = 0;
            if (!string.IsNullOrWhiteSpace(ProfitMarginPercentage) &&
                decimal.TryParse(ProfitMarginPercentage, out decimal profitRate))
            {
                invoiceProfitMargin = profitRate;
            }

            foreach (var item in InvoiceItems)
            {
                UpdateInvoiceItemLineTotal(item, invoiceProfitMargin);
            }

            RecalculateTotalsInternal();

            _isUpdatingFromUser = false;
        }

        private void RecalculateTotalsInternal()
        {
            if (_currentInvoice == null)
            {
                TotalBeforeDiscount = 0;
                TotalItemsDiscount = 0;
                SubTotalAfterItemsDiscount = 0;
                InvoiceDiscountAmount = 0;
                FinalAmountCalculated = 0;
                TotalProfitAmount = 0;
                TotalProductProfit = 0;
                TotalInvoiceProfit = 0;
                return;
            }

            decimal totalLineTotals = _currentInvoice.Items?.Sum(i => i.LineTotal) ?? 0;
            decimal totalOriginalPrice = _currentInvoice.Items?.Sum(i => i.Quantity * i.OriginalUnitPrice) ?? 0;
            decimal totalProductProfit = _currentInvoice.Items?.Sum(i =>
                (i.Quantity * i.OriginalUnitPrice * i.ItemProfitMarginPercentage) / 100) ?? 0;
            decimal totalItemsDiscount = _currentInvoice.Items?.Sum(i =>
                (i.Quantity * i.OriginalUnitPrice * i.DiscountPercentage) / 100) ?? 0;

            decimal subtotalBeforeInvoiceProfit = _currentInvoice.Items?.Sum(i =>
                (i.Quantity * i.UnitPrice) - ((i.Quantity * i.OriginalUnitPrice * i.DiscountPercentage) / 100)) ?? 0;

            decimal invoiceProfitMargin = 0;
            if (!string.IsNullOrWhiteSpace(ProfitMarginPercentage) &&
                decimal.TryParse(ProfitMarginPercentage, out decimal profitRate))
            {
                invoiceProfitMargin = profitRate;
            }
            decimal totalInvoiceProfit = (subtotalBeforeInvoiceProfit * invoiceProfitMargin) / 100;

            decimal totalBeforeInvoiceDiscount = subtotalBeforeInvoiceProfit + totalInvoiceProfit;
            decimal invoiceDiscountAmount = (totalBeforeInvoiceDiscount * _currentInvoice.InvoiceDiscountPercentage) / 100;
            decimal finalAmountCalculated = totalBeforeInvoiceDiscount - invoiceDiscountAmount;

            decimal totalProfitAmount = totalProductProfit + totalInvoiceProfit;

            TotalBeforeDiscount = totalOriginalPrice;
            TotalItemsDiscount = totalItemsDiscount;
            SubTotalAfterItemsDiscount = subtotalBeforeInvoiceProfit;
            InvoiceDiscountAmount = invoiceDiscountAmount;
            FinalAmountCalculated = finalAmountCalculated;
            TotalProfitAmount = totalProfitAmount;
            TotalProductProfit = totalProductProfit;
            TotalInvoiceProfit = totalInvoiceProfit;

            _currentInvoice.TotalAmount = totalBeforeInvoiceDiscount;
            _currentInvoice.FinalAmount = finalAmountCalculated;
        }

        private void SyncInvoiceItems()
        {
            if (_currentInvoice != null)
                _currentInvoice.Items = InvoiceItems.ToList();
        }

        private bool CanAddItem()
        {
            return SelectedProduct != null &&
                   !string.IsNullOrWhiteSpace(ProductQuantity) &&
                   decimal.TryParse(ProductQuantity, out decimal qty) &&
                   qty > 0;
        }

        private void LoadData()
        {
            try
            {
                LoadActiveCustomers();
                LoadActiveProducts();
                LoadIncompleteInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadActiveCustomers()
        {
            Customers.Clear();
            var activeCustomers = _customerRepository.GetAll().Where(c => c.IsActive).OrderBy(c => c.Name).ToList();

            if (!activeCustomers.Any())
            {
                MessageBox.Show("لا يوجد عملاء نشطين في النظام. الرجاء تفعيل عميل أولاً.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            foreach (var customer in activeCustomers)
            {
                Customers.Add(customer);
            }

            _customersViewSource?.View?.Refresh();
        }

        private void LoadActiveProducts()
        {
            Products.Clear();
            var allProducts = _productRepository.GetAll();
            var activeProducts = allProducts.Where(p => p.IsActive).OrderBy(p => p.Name);

            foreach (var product in activeProducts)
            {
                Products.Add(product);
            }

            _productsViewSource?.View?.Refresh();
        }

        private void LoadIncompleteInvoices()
        {
            Invoices.Clear();
            var allInvoices = _invoiceRepository.GetAll();
            var incompleteInvoices = allInvoices
                .Where(inv => inv.Status != InvoiceStatus.Completed && inv.Status != InvoiceStatus.Cancelled)
                .OrderByDescending(inv => inv.InvoiceDate);

            foreach (var invoice in incompleteInvoices)
            {
                Invoices.Add(invoice);
            }
        }

        private void NewInvoice()
        {
            DateTime invoiceDate = SelectedInvoiceDate;

            CurrentInvoice = new Invoice(invoiceDate)
            {
                Status = InvoiceStatus.Draft,
                FinalAmount = 0,
                TotalAmount = 0,
                Items = new List<InvoiceItem>(),
                ProfitMarginPercentage = 0,
                InvoiceDiscountPercentage = 0
            };

            InvoiceItems = new ObservableCollection<InvoiceItem>();
            SelectedCustomer = null;
            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            ProductProfitMargin = string.Empty;
            ProfitMarginPercentage = string.Empty;
            InvoiceDiscountPercentage = string.Empty;
            CustomerSearchText = string.Empty;
            ProductSearchText = string.Empty;
            SelectedInvoiceFromList = null;
            IsEditMode = true;
            IsSaved = false;

            RecalculateTotalsInternal();
        }

        private void SelectInvoice()
        {
            if (SelectedInvoiceFromList == null) return;

            if (SelectedInvoiceFromList.Status == InvoiceStatus.Completed || SelectedInvoiceFromList.Status == InvoiceStatus.Cancelled)
            {
                MessageBox.Show("لا يمكن تعديل عرض الأسعار هذا", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CurrentInvoice = SelectedInvoiceFromList;
            InvoiceItems = new ObservableCollection<InvoiceItem>(SelectedInvoiceFromList.Items ?? new List<InvoiceItem>());
            SelectedCustomer = SelectedInvoiceFromList.Customer;
            SelectedInvoiceDate = SelectedInvoiceFromList.InvoiceDate;
            ProfitMarginPercentage = SelectedInvoiceFromList.ProfitMarginPercentage != 0
                ? SelectedInvoiceFromList.ProfitMarginPercentage.ToString()
                : string.Empty;
            InvoiceDiscountPercentage = SelectedInvoiceFromList.InvoiceDiscountPercentage != 0
                ? SelectedInvoiceFromList.InvoiceDiscountPercentage.ToString()
                : string.Empty;
            CustomerSearchText = SelectedCustomer?.Name ?? string.Empty;
            ProductSearchText = string.Empty;
            IsEditMode = false;
            IsSaved = true;

            RecalculateTotalsInternal();
        }

        private void EditInvoice()
        {
            SelectInvoice();
            IsEditMode = true;
        }

        private void AddItem()
        {
            if (!decimal.TryParse(ProductQuantity, out decimal quantity) || quantity <= 0 || SelectedProduct == null) return;

            decimal itemDiscount = 0;
            if (!string.IsNullOrWhiteSpace(ProductDiscount))
                decimal.TryParse(ProductDiscount, out itemDiscount);

            decimal itemProfitMargin = 0;
            if (!string.IsNullOrWhiteSpace(ProductProfitMargin))
                decimal.TryParse(ProductProfitMargin, out itemProfitMargin);

            decimal originalPrice = SelectedProduct.Price;

            decimal finalUnitPrice = originalPrice + (originalPrice * itemProfitMargin / 100);

            decimal invoiceProfitMargin = 0;
            if (!string.IsNullOrWhiteSpace(ProfitMarginPercentage) &&
                decimal.TryParse(ProfitMarginPercentage, out decimal invoiceMargin))
            {
                invoiceProfitMargin = invoiceMargin;
            }

            var existing = InvoiceItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id);

            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.DiscountPercentage = itemDiscount;
                existing.ItemProfitMarginPercentage = itemProfitMargin;
                existing.OriginalUnitPrice = originalPrice;
                existing.UnitPrice = finalUnitPrice;

                UpdateInvoiceItemLineTotal(existing, invoiceProfitMargin);

                int idx = InvoiceItems.IndexOf(existing);
                InvoiceItems.Move(idx, 0);
            }
            else
            {
                var newItem = new InvoiceItem()
                {
                    ProductId = SelectedProduct.Id,
                    Quantity = quantity,
                    OriginalUnitPrice = originalPrice,
                    UnitPrice = finalUnitPrice,
                    DiscountPercentage = itemDiscount,
                    ItemProfitMarginPercentage = itemProfitMargin
                };

                UpdateInvoiceItemLineTotal(newItem, invoiceProfitMargin);

                InvoiceItems.Insert(0, newItem);
            }

            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            ProductProfitMargin = string.Empty;
            ProductSearchText = string.Empty;

            _isUpdatingFromUser = true;
            RecalculateTotalsInternal();
            _isUpdatingFromUser = false;

            if (InvoiceItems.Count > 0)
            {
                SelectedInvoiceItem = InvoiceItems.First();
            }
        }

        private void RemoveItem()
        {
            if (InvoiceItems.Count > 0)
            {
                InvoiceItems.RemoveAt(InvoiceItems.Count - 1);
                RecalculateTotalsInternal();
            }
        }

        private void DeleteSelectedItem()
        {
            if (SelectedInvoiceItem != null)
            {
                InvoiceItems.Remove(SelectedInvoiceItem);
                RecalculateTotalsInternal();
            }
        }

        private void SaveInvoice()
        {
            if (SelectedCustomer == null || InvoiceItems.Count == 0)
            {
                MessageBox.Show("الرجاء إكمال بيانات العميل والبنود", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentInvoice.CustomerId = SelectedCustomer.Id;

            if (string.IsNullOrWhiteSpace(ProfitMarginPercentage))
            {
                CurrentInvoice.ProfitMarginPercentage = 0;
            }
            else if (decimal.TryParse(ProfitMarginPercentage, out decimal profitMargin))
            {
                CurrentInvoice.ProfitMarginPercentage = profitMargin;
            }

            if (string.IsNullOrWhiteSpace(InvoiceDiscountPercentage))
            {
                CurrentInvoice.InvoiceDiscountPercentage = 0;
            }
            else if (decimal.TryParse(InvoiceDiscountPercentage, out decimal invoiceDiscount))
            {
                CurrentInvoice.InvoiceDiscountPercentage = invoiceDiscount;
            }

            SyncInvoiceItems();
            RecalculateTotalsInternal();

            try
            {
                if (CurrentInvoice.Id == 0)
                {
                    var invoiceToSave = new Invoice
                    {
                        CustomerId = CurrentInvoice.CustomerId,
                        InvoiceDate = CurrentInvoice.InvoiceDate,
                        CreatedDate = CurrentInvoice.CreatedDate,
                        Status = CurrentInvoice.Status,
                        TotalAmount = CurrentInvoice.TotalAmount,
                        FinalAmount = CurrentInvoice.FinalAmount,
                        Notes = CurrentInvoice.Notes,
                        ProfitMarginPercentage = CurrentInvoice.ProfitMarginPercentage,
                        InvoiceDiscountPercentage = CurrentInvoice.InvoiceDiscountPercentage,
                        Items = new List<InvoiceItem>()
                    };

                    foreach (var item in InvoiceItems)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            OriginalUnitPrice = item.OriginalUnitPrice,
                            DiscountPercentage = item.DiscountPercentage,
                            ItemProfitMarginPercentage = item.ItemProfitMarginPercentage,
                            LineTotal = item.LineTotal
                        };
                        invoiceToSave.Items.Add(invoiceItem);
                    }

                    _invoiceRepository.Add(invoiceToSave);
                    CurrentInvoice.Id = invoiceToSave.Id;
                    Invoices.Add(CurrentInvoice);
                }
                else
                {
                    var invoiceToUpdate = new Invoice
                    {
                        Id = CurrentInvoice.Id,
                        CustomerId = CurrentInvoice.CustomerId,
                        InvoiceDate = CurrentInvoice.InvoiceDate,
                        CreatedDate = CurrentInvoice.CreatedDate,
                        Status = CurrentInvoice.Status,
                        TotalAmount = CurrentInvoice.TotalAmount,
                        FinalAmount = CurrentInvoice.FinalAmount,
                        Notes = CurrentInvoice.Notes,
                        ProfitMarginPercentage = CurrentInvoice.ProfitMarginPercentage,
                        InvoiceDiscountPercentage = CurrentInvoice.InvoiceDiscountPercentage,
                        Items = new List<InvoiceItem>()
                    };

                    foreach (var item in InvoiceItems)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            Id = item.Id,
                            InvoiceId = item.InvoiceId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            OriginalUnitPrice = item.OriginalUnitPrice,
                            DiscountPercentage = item.DiscountPercentage,
                            ItemProfitMarginPercentage = item.ItemProfitMarginPercentage,
                            LineTotal = item.LineTotal
                        };
                        invoiceToUpdate.Items.Add(invoiceItem);
                    }

                    _invoiceRepository.Update(invoiceToUpdate);
                }

                IsEditMode = false;
                IsSaved = true;

                MessageBox.Show("تم حفظ عرض الأسعار بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ عرض الأسعار: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CompleteInvoice()
        {
            if (CurrentInvoice == null || CurrentInvoice.Id == 0) return;

            try
            {
                CurrentInvoice.Status = InvoiceStatus.Completed;

                // ✅ إصلاح: استخدام طريقة تحديث الحالة فقط
                bool success = _invoiceRepository.UpdateInvoiceStatusAndDate(
                    CurrentInvoice.Id,
                    InvoiceStatus.Completed,
                    DateTime.Now);

                if (!success)
                {
                    MessageBox.Show("فشل في إكمال عرض الأسعار", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = InvoicePdfGenerator.GenerateInvoiceFileName(CurrentInvoice);
                string pdfPath = InvoicePdfGenerator.GenerateInvoicePdf(CurrentInvoice, fileName);

                MessageBox.Show(
                    $"تم إكمال عرض الأسعار بنجاح!\n\nتم حفظ عرض الأسعار في:\n{pdfPath}",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ResetAllFields();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إكمال عرض الأسعار أو إنشاء PDF: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelInvoice()
        {
            if (CurrentInvoice == null || CurrentInvoice.Id == 0) return;

            // ✅ استخدام الطريقة الجديدة لتحديث الحالة فقط
            bool success = _invoiceRepository.UpdateInvoiceStatus(CurrentInvoice.Id, InvoiceStatus.Cancelled);

            if (success)
            {
                Cancel();
                LoadData();
            }
            else
            {
                MessageBox.Show("فشل في إلغاء الفاتورة", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteInvoice()
        {
            if (SelectedInvoiceFromList == null) return;
            if (SelectedInvoiceFromList.Status == InvoiceStatus.Completed)
            {
                MessageBox.Show("لا يمكن حذف عرض أسعار مكتمل");
                return;
            }

            var result = MessageBox.Show($"حذف عرض الأسعار رقم {SelectedInvoiceFromList.Id}؟", "تأكيد", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _invoiceRepository.Delete(SelectedInvoiceFromList.Id);
                Invoices.Remove(SelectedInvoiceFromList);
            }
        }

        private void Cancel()
        {
            IsEditMode = false;
            IsSaved = false;
            CurrentInvoice = null;
            InvoiceItems = new ObservableCollection<InvoiceItem>();
            SelectedCustomer = null;
            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            ProductProfitMargin = string.Empty;
            ProfitMarginPercentage = string.Empty;
            InvoiceDiscountPercentage = string.Empty;
            CustomerSearchText = string.Empty;
            ProductSearchText = string.Empty;
            SelectedInvoiceFromList = null;
            SelectedInvoiceDate = DateTime.Now;

            RecalculateTotalsInternal();
        }

        private void ResetAllFields()
        {
            IsEditMode = false;
            IsSaved = false;
            CurrentInvoice = null;
            InvoiceItems = new ObservableCollection<InvoiceItem>();
            SelectedCustomer = null;
            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            ProductProfitMargin = string.Empty;
            ProfitMarginPercentage = string.Empty;
            InvoiceDiscountPercentage = string.Empty;
            CustomerSearchText = string.Empty;
            ProductSearchText = string.Empty;
            SelectedInvoiceFromList = null;
            SelectedInvoiceDate = DateTime.Now;

            TotalBeforeDiscount = 0;
            TotalItemsDiscount = 0;
            SubTotalAfterItemsDiscount = 0;
            InvoiceDiscountAmount = 0;
            FinalAmountCalculated = 0;
            TotalProfitAmount = 0;
            TotalProductProfit = 0;
            TotalInvoiceProfit = 0;
        }

        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
                _updateTimer = null;
            }

            if (_customersViewSource != null)
            {
                _customersViewSource.Filter -= CustomersViewSource_Filter;
            }

            if (_productsViewSource != null)
            {
                _productsViewSource.Filter -= ProductsViewSource_Filter;
            }
        }

    }
}