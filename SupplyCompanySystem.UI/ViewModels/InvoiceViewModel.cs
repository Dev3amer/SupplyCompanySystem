using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using SupplyCompanySystem.UI.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
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

        // ✅ إزالة خصائص البحث المنفصلة
        private Invoice _currentInvoice;
        private Invoice _selectedInvoiceFromList;
        private InvoiceItem _selectedInvoiceItem;
        private Customer _selectedCustomer;
        private Product _selectedProduct;
        private string _productQuantity;
        private string _productDiscount;
        private string _profitMarginPercentage;
        private bool _isEditMode;
        private bool _isSaved;

        // ===== Properties =====
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set { _invoices = value; OnPropertyChanged(nameof(Invoices)); }
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set { _customers = value; OnPropertyChanged(nameof(Customers)); }
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(nameof(Products)); }
        }

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
            set { _selectedInvoiceItem = value; OnPropertyChanged(nameof(SelectedInvoiceItem)); }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                if (_currentInvoice != null && value != null)
                {
                    _currentInvoice.CustomerId = value.Id;
                    _currentInvoice.Customer = value;
                }
                OnPropertyChanged(nameof(SelectedCustomer));
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(nameof(SelectedProduct)); }
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
                _productDiscount = value;
                OnPropertyChanged(nameof(ProductDiscount));
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

                    RecalculateAllItemsPrices();
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

        // ===== Computed =====
        public decimal TotalBeforeDiscount => _currentInvoice?.Items.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        public decimal TotalDiscount => _currentInvoice?.Items.Sum(i => (i.Quantity * i.UnitPrice * i.DiscountPercentage) / 100) ?? 0;
        public decimal FinalAmountCalculated => TotalBeforeDiscount - TotalDiscount;

        // ===== Commands =====
        public RelayCommand NewInvoiceCommand { get; }
        public RelayCommand SaveInvoiceCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand AddItemCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand DeleteItemCommand { get; }
        public RelayCommand CompleteInvoiceCommand { get; }
        public RelayCommand SelectInvoiceCommand { get; }
        public RelayCommand EditInvoiceCommand { get; }
        public RelayCommand CancelInvoiceCommand { get; }
        public RelayCommand DeleteInvoiceCommand { get; }

        // ===== Constructor =====
        public InvoiceViewModel(IInvoiceRepository invoiceRepository, ICustomerRepository customerRepository, IProductRepository productRepository)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;

            Invoices = new ObservableCollection<Invoice>();
            Customers = new ObservableCollection<Customer>();
            Products = new ObservableCollection<Product>();
            InvoiceItems = new ObservableCollection<InvoiceItem>();

            _profitMarginPercentage = string.Empty;

            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddItemCommand = new RelayCommand(_ => AddItem(), _ => CanAddItem());
            RemoveItemCommand = new RelayCommand(_ => RemoveItem(), _ => InvoiceItems.Count > 0);
            DeleteItemCommand = new RelayCommand(_ => DeleteSelectedItem());
            CompleteInvoiceCommand = new RelayCommand(_ => CompleteInvoice(), _ => _currentInvoice != null && InvoiceItems.Count > 0 && _isSaved);
            SelectInvoiceCommand = new RelayCommand(_ => SelectInvoice());
            EditInvoiceCommand = new RelayCommand(_ => EditInvoice());
            CancelInvoiceCommand = new RelayCommand(_ => CancelInvoice());
            DeleteInvoiceCommand = new RelayCommand(_ => DeleteInvoice());

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            LoadData();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e) => RecalculateTotals();

        // ===== Events =====
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
            RecalculateTotals();
            (RemoveItemCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void InvoiceItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is InvoiceItem item)
            {
                if (e.PropertyName == nameof(InvoiceItem.Quantity) ||
                    e.PropertyName == nameof(InvoiceItem.UnitPrice) ||
                    e.PropertyName == nameof(InvoiceItem.DiscountPercentage))
                {
                    item.LineTotal = (item.Quantity * item.UnitPrice) - ((item.Quantity * item.UnitPrice * item.DiscountPercentage) / 100);
                    RecalculateTotals();
                }
            }
        }

        // ===== Helpers =====

        private decimal CalculatePriceWithProfitMargin(decimal basePrice)
        {
            if (string.IsNullOrWhiteSpace(ProfitMarginPercentage))
                return basePrice;

            if (decimal.TryParse(ProfitMarginPercentage, out decimal profitRate))
            {
                if (profitRate == 0)
                    return basePrice;

                return basePrice + (basePrice * profitRate / 100);
            }
            return basePrice;
        }

        private void RecalculateAllItemsPrices()
        {
            if (_currentInvoice?.Items == null || _currentInvoice.Items.Count == 0)
                return;

            foreach (var item in InvoiceItems)
            {
                if (item.Product != null)
                {
                    decimal basePrice = item.Product.Price;
                    item.UnitPrice = CalculatePriceWithProfitMargin(basePrice);
                }
            }
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            if (_currentInvoice == null) return;
            _currentInvoice.TotalAmount = TotalBeforeDiscount;
            _currentInvoice.FinalAmount = FinalAmountCalculated;

            OnPropertyChanged(nameof(TotalBeforeDiscount));
            OnPropertyChanged(nameof(TotalDiscount));
            OnPropertyChanged(nameof(FinalAmountCalculated));
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

        // ===== Commands Methods =====
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
            CurrentInvoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                Status = InvoiceStatus.Draft,
                FinalAmount = 0,
                TotalAmount = 0,
                Items = new List<InvoiceItem>(),
                ProfitMarginPercentage = 0
            };
            InvoiceItems = new ObservableCollection<InvoiceItem>();
            SelectedCustomer = null;
            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            ProfitMarginPercentage = string.Empty;
            SelectedInvoiceFromList = null;
            IsEditMode = true;
            IsSaved = false;
        }

        private void SelectInvoice()
        {
            if (SelectedInvoiceFromList == null) return;

            if (SelectedInvoiceFromList.Status == InvoiceStatus.Completed || SelectedInvoiceFromList.Status == InvoiceStatus.Cancelled)
            {
                MessageBox.Show("لا يمكن تعديل هذه الفاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CurrentInvoice = SelectedInvoiceFromList;
            InvoiceItems = new ObservableCollection<InvoiceItem>(SelectedInvoiceFromList.Items ?? new List<InvoiceItem>());
            SelectedCustomer = SelectedInvoiceFromList.Customer;
            ProfitMarginPercentage = SelectedInvoiceFromList.ProfitMarginPercentage != 0
                ? SelectedInvoiceFromList.ProfitMarginPercentage.ToString()
                : string.Empty;
            IsEditMode = false;
            IsSaved = true;
        }

        private void EditInvoice() => IsEditMode = true;

        private void AddItem()
        {
            if (!decimal.TryParse(ProductQuantity, out decimal quantity) || quantity <= 0 || SelectedProduct == null) return;

            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(ProductDiscount)) decimal.TryParse(ProductDiscount, out discount);

            decimal priceWithMargin = CalculatePriceWithProfitMargin(SelectedProduct.Price);

            var existing = InvoiceItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.DiscountPercentage = discount;
                existing.UnitPrice = priceWithMargin;
                existing.LineTotal = (existing.Quantity * existing.UnitPrice) - ((existing.Quantity * existing.UnitPrice * existing.DiscountPercentage) / 100);

                int idx = InvoiceItems.IndexOf(existing);
                InvoiceItems[idx] = existing;
            }
            else
            {
                var newItem = new InvoiceItem(SelectedProduct.Id, quantity, priceWithMargin)
                {
                    Product = SelectedProduct,
                    DiscountPercentage = discount
                };
                newItem.LineTotal = (newItem.Quantity * newItem.UnitPrice) - ((newItem.Quantity * newItem.UnitPrice * newItem.DiscountPercentage) / 100);
                InvoiceItems.Add(newItem);
            }

            SelectedProduct = null;
            ProductQuantity = string.Empty;
            ProductDiscount = string.Empty;
            RecalculateTotals();
        }

        private void RemoveItem()
        {
            if (InvoiceItems.Count > 0) InvoiceItems.RemoveAt(InvoiceItems.Count - 1);
        }

        private void DeleteSelectedItem()
        {
            if (SelectedInvoiceItem != null) InvoiceItems.Remove(SelectedInvoiceItem);
        }

        private void SaveInvoice()
        {
            if (SelectedCustomer == null || InvoiceItems.Count == 0)
            {
                MessageBox.Show("الرجاء إكمال بيانات العميل والبنود", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentInvoice.CustomerId = SelectedCustomer.Id;
            CurrentInvoice.Customer = SelectedCustomer;

            if (string.IsNullOrWhiteSpace(ProfitMarginPercentage))
            {
                CurrentInvoice.ProfitMarginPercentage = 0;
            }
            else if (decimal.TryParse(ProfitMarginPercentage, out decimal profitMargin))
            {
                CurrentInvoice.ProfitMarginPercentage = profitMargin;
            }

            SyncInvoiceItems();
            RecalculateTotals();

            if (CurrentInvoice.Id == 0)
            {
                _invoiceRepository.Add(CurrentInvoice);
                Invoices.Add(CurrentInvoice);
            }
            else _invoiceRepository.Update(CurrentInvoice);

            IsEditMode = false;
            IsSaved = true;
        }

        public void CompleteInvoice()
        {
            if (CurrentInvoice == null || CurrentInvoice.Id == 0) return;

            try
            {
                CurrentInvoice.Status = InvoiceStatus.Completed;
                _invoiceRepository.Update(CurrentInvoice);

                string fileName = InvoicePdfGenerator.GenerateInvoiceFileName(CurrentInvoice);
                string pdfPath = InvoicePdfGenerator.GenerateInvoicePdf(CurrentInvoice, fileName);

                MessageBox.Show(
                    $"تم إكمال الفاتورة بنجاح!\n\nتم حفظ الفاتورة في:\n{pdfPath}",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Cancel();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إكمال الفاتورة أو إنشاء PDF: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelInvoice()
        {
            if (CurrentInvoice == null || CurrentInvoice.Id == 0) return;
            CurrentInvoice.Status = InvoiceStatus.Cancelled;
            _invoiceRepository.Update(CurrentInvoice);
            Cancel();
            LoadData();
        }

        private void DeleteInvoice()
        {
            if (SelectedInvoiceFromList == null) return;
            if (SelectedInvoiceFromList.Status == InvoiceStatus.Completed)
            {
                MessageBox.Show("لا يمكن حذف فاتورة مكتملة");
                return;
            }

            var result = MessageBox.Show($"حذف الفاتورة رقم {SelectedInvoiceFromList.Id}؟", "تأكيد", MessageBoxButton.YesNo);
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
            ProfitMarginPercentage = string.Empty;
            SelectedInvoiceFromList = null;
        }

        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
                _updateTimer = null;
            }
        }
    }
}