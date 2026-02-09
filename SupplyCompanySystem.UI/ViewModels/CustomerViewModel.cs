using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Common.Validators;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class CustomerViewModel : BaseViewModel, IDisposable
    {
        private readonly ICustomerRepository _repository;
        private ObservableCollection<Customer> _customers;
        private List<Customer> _allCustomers;
        private List<Customer> _filteredCustomers;
        private Customer _selectedCustomer;
        private string _searchText;
        private bool _isEditMode;
        private bool _showInactiveCustomers = false;

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
        private string _phoneNumber;
        private string _address;

        // ===== Properties =====
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set { _customers = value; OnPropertyChanged(nameof(Customers)); }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                if (value != null)
                {
                    Name = value.Name;
                    PhoneNumber = value.PhoneNumber;
                    Address = value.Address;
                }
                OnPropertyChanged(nameof(SelectedCustomer));

                DeleteCommand?.NotifyCanExecuteChanged();
                RestoreCommand?.NotifyCanExecuteChanged();
                EditCommand?.NotifyCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); FilterCustomers(); }
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

        public bool ShowInactiveCustomers
        {
            get => _showInactiveCustomers;
            set { _showInactiveCustomers = value; OnPropertyChanged(nameof(ShowInactiveCustomers)); FilterCustomers(); }
        }

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string PhoneNumber { get => _phoneNumber; set { _phoneNumber = value; OnPropertyChanged(nameof(PhoneNumber)); } }
        public string Address { get => _address; set { _address = value; OnPropertyChanged(nameof(Address)); } }

        public string PaginationText => $"الصفحة {_currentPage} من {_totalPages}";

        public string TotalCustomersText => _showInactiveCustomers
            ? $"العملاء المعطّلين: {_allCustomers?.Count(c => !c.IsActive) ?? 0}"
            : $"إجمالي العملاء النشطين: {_allCustomers?.Count(c => c.IsActive) ?? 0}";

        // ===== Commands =====
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ClearSearchCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }

        // ===== Constructor =====
        public CustomerViewModel(ICustomerRepository repository)
        {
            _repository = repository;
            Customers = new ObservableCollection<Customer>();

            AddCommand = new RelayCommand(_ => Add(), _ => CanAdd);
            EditCommand = new RelayCommand(_ => Edit(), _ => SelectedCustomer != null);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedCustomer != null && SelectedCustomer.IsActive);
            RestoreCommand = new RelayCommand(_ => Restore(), _ => SelectedCustomer != null && !SelectedCustomer.IsActive);
            SaveCommand = new RelayCommand(_ => Save(), _ => IsEditMode);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => IsEditMode);
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());
            NextPageCommand = new RelayCommand(_ => NextPage(), _ => CanGoToNextPage);
            PreviousPageCommand = new RelayCommand(_ => PreviousPage(), _ => CanGoToPreviousPage);

            LoadCustomersFromDatabase();
        }

        private void LoadCustomersFromDatabase()
        {
            try
            {
                var customers = _repository.GetAll();
                _allCustomers = customers ?? new List<Customer>();
                _filteredCustomers = new List<Customer>(_allCustomers.Where(c => c.IsActive));
                RefreshPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }

        private void Add()
        {
            IsEditMode = true;
            ClearForm();
        }

        private void Edit()
        {
            if (SelectedCustomer != null) IsEditMode = true;
        }

        private void Save()
        {
            var validationResult = CustomerValidator.ValidateAll(Name, PhoneNumber, Address);
            if (!validationResult.IsValid)
            {
                MessageBox.Show(validationResult.ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nameValidation = CustomerValidator.ValidateNameUniqueness(Name, _allCustomers, SelectedCustomer?.Id);
            if (!nameValidation.IsValid)
            {
                MessageBox.Show(nameValidation.ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var phoneValidation = CustomerValidator.ValidatePhoneUniqueness(PhoneNumber, _allCustomers, SelectedCustomer?.Id);
            if (!phoneValidation.IsValid)
            {
                MessageBox.Show(phoneValidation.ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (SelectedCustomer == null)
                {
                    var newCustomer = new Customer
                    {
                        Name = Name,
                        PhoneNumber = PhoneNumber,
                        Address = Address,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };
                    _repository.Add(newCustomer);
                    _allCustomers.Add(newCustomer);
                    MessageBox.Show("تم إضافة العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    SelectedCustomer.Name = Name;
                    SelectedCustomer.PhoneNumber = PhoneNumber;
                    SelectedCustomer.Address = Address;
                    _repository.Update(SelectedCustomer);
                    MessageBox.Show("تم تحديث العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsEditMode = false;
                ClearForm();
                FilterCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedCustomer == null) return;
            if (MessageBox.Show("هل تريد تعطيل العميل؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                SelectedCustomer.IsActive = false;
                _repository.Update(SelectedCustomer);
                FilterCustomers();
                ClearForm();
                MessageBox.Show("تم تعطيل العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Restore()
        {
            if (SelectedCustomer == null) return;
            if (MessageBox.Show("هل تريد استرجاع العميل؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SelectedCustomer.IsActive = true;
                _repository.Update(SelectedCustomer);
                FilterCustomers();
                ClearForm();
                MessageBox.Show("تم استرجاع العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FilterCustomers()
        {
            var query = _allCustomers.AsEnumerable();

            if (_showInactiveCustomers)
                query = query.Where(c => !c.IsActive);
            else
                query = query.Where(c => c.IsActive);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    c.PhoneNumber.ToLower().Contains(s) ||
                    c.Address.ToLower().Contains(s));
            }

            _filteredCustomers = query.ToList();
            _currentPage = 1;
            RefreshPagination();
        }

        private void RefreshPagination()
        {
            _totalPages = _filteredCustomers.Count > 0 ?
                (_filteredCustomers.Count + _pageSize - 1) / _pageSize : 1;

            // التأكد من أن الصفحة الحالية لا تتجاوز العدد الإجمالي للصفحات
            if (_currentPage > _totalPages && _totalPages > 0)
                _currentPage = _totalPages;
            else if (_currentPage < 1 && _totalPages > 0)
                _currentPage = 1;

            OnPropertyChanged(nameof(PaginationText));
            OnPropertyChanged(nameof(TotalCustomersText));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));

            // إعادة تحميل حالة الأوامر
            NextPageCommand?.NotifyCanExecuteChanged();
            PreviousPageCommand?.NotifyCanExecuteChanged();

            DisplayCurrentPage();
        }

        private void DisplayCurrentPage()
        {
            Customers.Clear();
            var items = _filteredCustomers.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
            foreach (var item in items) Customers.Add(item);
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

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void Cancel()
        {
            IsEditMode = false;
            ClearForm();
        }

        private void ClearForm()
        {
            Name = PhoneNumber = Address = string.Empty;
            SelectedCustomer = null;
        }

        public List<Customer> GetAllCustomers()
        {
            return _allCustomers.Where(c => c.IsActive).ToList();
        }

        public List<Customer> GetAllCustomersIncludingInactive()
        {
            return _allCustomers ?? new List<Customer>();
        }

        public void Dispose()
        {
            _allCustomers?.Clear();
            _filteredCustomers?.Clear();
            Customers?.Clear();
        }
    }
}