using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Common.Validators;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class ProductViewModel : BaseViewModel, IDisposable
    {
        private readonly IProductRepository _repository;
        private ObservableCollection<Product> _products;
        private List<Product> _allProducts;
        private List<Product> _filteredProducts;
        private Product _selectedProduct;
        private string _searchText;
        private bool _isEditMode;
        private bool _showInactiveProducts = false;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));
                    _currentPage = 1;
                    RefreshPagination();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(CanGoToPreviousPage));
                    OnPropertyChanged(nameof(CanGoToNextPage));
                }
            }
        }

        // خصائص جديدة للتحكم في إمكانية التنقل بين الصفحات
        public bool CanGoToPreviousPage => _currentPage > 1;
        public bool CanGoToNextPage => _currentPage < _totalPages;

        // Form Fields
        private string _name;
        private string _sku;
        private string _price;
        private string _unit;
        private string _category;
        private string _description;

        // Categories
        private ObservableCollection<string> _categories;
        private ObservableCollection<string> _categoriesForForm;
        private string _selectedCategory;

        // Properties
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(nameof(Products)); }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                if (value != null)
                {
                    Name = value.Name;
                    SKU = value.SKU;
                    Price = value.Price.ToString();
                    Unit = value.Unit;
                    Category = value.Category;
                    Description = value.Description;
                }
                OnPropertyChanged(nameof(SelectedProduct));

                DeleteCommand?.NotifyCanExecuteChanged();
                RestoreCommand?.NotifyCanExecuteChanged();
                EditCommand?.NotifyCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterProducts(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(CanAdd)); // تحديث حالة زرار الإضافة
                SaveCommand?.NotifyCanExecuteChanged();
                CancelCommand?.NotifyCanExecuteChanged();
                AddCommand?.NotifyCanExecuteChanged();
            }
        }

        // خاصية جديدة للتحكم في إمكانية الضغط على زر الإضافة
        public bool CanAdd => !_isEditMode;

        public bool ShowInactiveProducts
        {
            get => _showInactiveProducts;
            set { _showInactiveProducts = value; OnPropertyChanged(nameof(ShowInactiveProducts)); FilterProducts(); }
        }

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string SKU { get => _sku; set { _sku = value; OnPropertyChanged(nameof(SKU)); } }
        public string Price { get => _price; set { _price = value; OnPropertyChanged(nameof(Price)); } }
        public string Unit { get => _unit; set { _unit = value; OnPropertyChanged(nameof(Unit)); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(nameof(Category)); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(nameof(Description)); } }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(nameof(Categories)); }
        }

        public ObservableCollection<string> CategoriesForForm
        {
            get => _categoriesForForm;
            set { _categoriesForForm = value; OnPropertyChanged(nameof(CategoriesForForm)); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); FilterByCategory(); }
        }

        public string PaginationText => $"الصفحة {_currentPage} من {_totalPages}";

        public string TotalProductsText => _showInactiveProducts
            ? $"المنتجات المعطّلة: {_allProducts?.Count(p => !p.IsActive) ?? 0}"
            : $"إجمالي المنتجات النشطة: {_allProducts?.Count(p => p.IsActive) ?? 0}";

        // Commands
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ClearSearchCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }

        // Constructor
        public ProductViewModel(IProductRepository repository)
        {
            _repository = repository;
            Products = new ObservableCollection<Product>();
            Categories = new ObservableCollection<string>();
            CategoriesForForm = new ObservableCollection<string>();

            AddCommand = new RelayCommand(_ => Add(), _ => CanAdd);
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedProduct != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedProduct != null && SelectedProduct.IsActive);
            RestoreCommand = new RelayCommand(_ => Restore(), _ => SelectedProduct != null && !SelectedProduct.IsActive);
            SaveCommand = new RelayCommand(_ => Save(), _ => IsEditMode);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsEditMode);
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());
            NextPageCommand = new RelayCommand(_ => NextPage(), _ => CanGoToNextPage);
            PreviousPageCommand = new RelayCommand(_ => PreviousPage(), _ => CanGoToPreviousPage);

            LoadProductsFromDatabase();
        }

        private void LoadProductsFromDatabase()
        {
            try
            {
                var products = _repository.GetAll();
                _allProducts = products ?? new List<Product>();
                _filteredProducts = new List<Product>(_allProducts.Where(p => p.IsActive));
                LoadCategories();
                RefreshPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }

        private void LoadCategories()
        {
            Categories.Clear();
            Categories.Add("الكل");
            CategoriesForForm.Clear();

            var uniqueCategories = _allProducts
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var category in uniqueCategories)
            {
                Categories.Add(category);
                CategoriesForForm.Add(category);
            }

            SelectedCategory = "الكل";
        }

        private void Add()
        {
            IsEditMode = true;
            ClearForm();
        }

        private void Edit()
        {
            if (SelectedProduct != null) IsEditMode = true;
        }

        private void Save()
        {
            try
            {
                // ✅ التحقق الأساسي باستخدام الـ Validator
                var validationResult = ProductValidator.ValidateAll(
                    Name, SKU, Price, Unit, Category, Description ?? "");

                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage, "خطأ في البيانات",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedProduct == null)
                {
                    // ✅ إضافة منتج جديد
                    var newProduct = new Product
                    {
                        Name = Name.Trim(),
                        SKU = SKU.Trim(),
                        Price = decimal.Parse(Price),
                        Unit = Unit.Trim(),
                        Category = Category.Trim(),
                        Description = Description?.Trim(),
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    _repository.Add(newProduct); // ✅ الـ Repository سيتحقق من التكرار
                    _allProducts.Add(newProduct);

                    // ✅ تحديث قائمة التصنيفات
                    if (!CategoriesForForm.Contains(Category.Trim()))
                    {
                        CategoriesForForm.Add(Category.Trim());
                        Categories.Add(Category.Trim());
                    }

                    MessageBox.Show("تم إضافة المنتج بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ✅ تحديث المنتج الموجود
                    var updatedProduct = new Product
                    {
                        Id = SelectedProduct.Id,
                        Name = Name.Trim(),
                        SKU = SKU.Trim(),
                        Price = decimal.Parse(Price),
                        Unit = Unit.Trim(),
                        Category = Category.Trim(),
                        Description = Description?.Trim(),
                        CreatedDate = SelectedProduct.CreatedDate,
                        IsActive = SelectedProduct.IsActive
                    };

                    _repository.Update(updatedProduct); // ✅ الـ Repository سيتحقق من التكرار

                    // ✅ تحديث القائمة المحلية
                    var existingProduct = _allProducts.FirstOrDefault(p => p.Id == SelectedProduct.Id);
                    if (existingProduct != null)
                    {
                        existingProduct.Name = Name.Trim();
                        existingProduct.SKU = SKU.Trim();
                        existingProduct.Price = decimal.Parse(Price);
                        existingProduct.Unit = Unit.Trim();
                        existingProduct.Category = Category.Trim();
                        existingProduct.Description = Description?.Trim();
                    }

                    // ✅ تحديث قائمة التصنيفات
                    if (!CategoriesForForm.Contains(Category.Trim()))
                    {
                        CategoriesForForm.Add(Category.Trim());
                        Categories.Add(Category.Trim());
                    }

                    MessageBox.Show("تم تحديث المنتج بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsEditMode = false;
                ClearForm();
                FilterProducts();
            }
            catch (InvalidOperationException ex)
            {
                // ✅ خطأ التكرار من الـ Repository
                MessageBox.Show(ex.Message, "خطأ في البيانات",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (FormatException)
            {
                MessageBox.Show("السعر يجب أن يكون رقم صحيح", "خطأ في البيانات",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show(
                "هل تريد تعطيل المنتج؟\n\nملاحظة: لن تتمكن من استخدام هذا المنتج في الطلبات الجديدة.",
                "تأكيد التعطيل",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                SelectedProduct.IsActive = false;
                _repository.Update(SelectedProduct);
                FilterProducts();
                ClearForm();
                MessageBox.Show("تم تعطيل المنتج بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Restore()
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show(
                "هل تريد استرجاع المنتج؟",
                "تأكيد الاسترجاع",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SelectedProduct.IsActive = true;
                _repository.Update(SelectedProduct);
                FilterProducts();
                ClearForm();
                MessageBox.Show("تم استرجاع المنتج بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterProducts()
        {
            var query = _allProducts.AsEnumerable();

            if (_showInactiveProducts)
                query = query.Where(p => !p.IsActive);
            else
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    p.SKU.ToLower().Contains(s));
            }

            if (!_showInactiveProducts && SelectedCategory != "الكل")
                query = query.Where(p => p.Category == SelectedCategory);

            _filteredProducts = query.ToList();
            _currentPage = 1;
            RefreshPagination();
        }

        private void RefreshPagination()
        {
            _totalPages = _filteredProducts.Count > 0 ?
                (_filteredProducts.Count + _pageSize - 1) / _pageSize : 1;

            // التأكد من أن الصفحة الحالية لا تتجاوز العدد الإجمالي للصفحات
            if (_currentPage > _totalPages && _totalPages > 0)
                _currentPage = _totalPages;
            else if (_currentPage < 1 && _totalPages > 0)
                _currentPage = 1;

            OnPropertyChanged(nameof(PaginationText));
            OnPropertyChanged(nameof(TotalProductsText));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));

            // إعادة تحميل حالة الأوامر
            NextPageCommand?.NotifyCanExecuteChanged();
            PreviousPageCommand?.NotifyCanExecuteChanged();

            DisplayCurrentPage();
        }

        private void DisplayCurrentPage()
        {
            Products.Clear();
            var items = _filteredProducts.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
            foreach (var item in items) Products.Add(item);
        }

        private void NextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                DisplayCurrentPage();
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                NextPageCommand?.NotifyCanExecuteChanged();
                PreviousPageCommand?.NotifyCanExecuteChanged();
            }
        }

        private void PreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                DisplayCurrentPage();
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                NextPageCommand?.NotifyCanExecuteChanged();
                PreviousPageCommand?.NotifyCanExecuteChanged();
            }
        }

        private void FilterByCategory() => FilterProducts();
        private void ClearSearch() { SearchText = string.Empty; }

        private void Cancel()
        {
            IsEditMode = false;
            ClearForm();
        }

        private void ClearForm()
        {
            Name = SKU = Price = Unit = Description = string.Empty;
            Category = null;
            SelectedProduct = null;
        }

        public void Dispose()
        {
            _allProducts?.Clear();
            _filteredProducts?.Clear();
            Products?.Clear();
        }

        public List<Product> GetAllProducts()
        {
            return _allProducts.Where(p => p.IsActive).ToList();
        }

        public List<Product> GetAllProductsIncludingInactive()
        {
            return _allProducts ?? new List<Product>();
        }
    }
}