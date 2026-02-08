using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupplyCompanySystem.Domain.Entities
{
    public enum InvoiceStatus
    {
        Draft = 0,
        Completed = 1,
        Cancelled = 2
    }

    public class Invoice : BaseEntity, INotifyPropertyChanged
    {
        private int _customerId;
        private Customer _customer;
        private DateTime _invoiceDate;
        private DateTime _createdDate;
        private InvoiceStatus _status;
        private List<InvoiceItem> _items;
        private decimal _totalAmount;
        private decimal _finalAmount;
        private string _notes;
        private decimal _profitMarginPercentage;
        private decimal _invoiceDiscountPercentage;
        private DateTime? _completedDate;

        public int CustomerId
        {
            get => _customerId;
            set
            {
                if (_customerId != value)
                {
                    _customerId = value;
                    OnPropertyChanged();
                }
            }
        }

        public Customer Customer
        {
            get => _customer;
            set
            {
                if (_customer != value)
                {
                    _customer = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                if (_invoiceDate != value)
                {
                    _invoiceDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public InvoiceStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();

                    if (value == InvoiceStatus.Completed && _completedDate == null)
                    {
                        CompletedDate = DateTime.Now;
                    }
                    else if (value == InvoiceStatus.Draft)
                    {
                        CompletedDate = null;
                    }
                }
            }
        }

        public DateTime? CompletedDate
        {
            get => _completedDate;
            set
            {
                if (_completedDate != value)
                {
                    _completedDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<InvoiceItem> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinalAmount
        {
            get => _finalAmount;
            set
            {
                if (_finalAmount != value)
                {
                    _finalAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal ProfitMarginPercentage
        {
            get => _profitMarginPercentage;
            set
            {
                if (_profitMarginPercentage != value)
                {
                    _profitMarginPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal InvoiceDiscountPercentage
        {
            get => _invoiceDiscountPercentage;
            set
            {
                if (_invoiceDiscountPercentage != value)
                {
                    _invoiceDiscountPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal InvoiceDiscountAmount => (TotalAmount * InvoiceDiscountPercentage) / 100;
        public decimal TotalProfitAmount => Items?.Sum(item => item.ItemProfitAmount) ?? 0;

        public Invoice()
        {
            InvoiceDate = DateTime.Now;
            CreatedDate = DateTime.Now;
            Status = InvoiceStatus.Draft;
            Items = new List<InvoiceItem>();
            ProfitMarginPercentage = 0;
            InvoiceDiscountPercentage = 0;
            CompletedDate = null;
        }

        public Invoice(DateTime invoiceDate)
        {
            InvoiceDate = invoiceDate;
            CreatedDate = DateTime.Now;
            Status = InvoiceStatus.Draft;
            Items = new List<InvoiceItem>();
            ProfitMarginPercentage = 0;
            InvoiceDiscountPercentage = 0;
            CompletedDate = null;
        }

        // ✅ دالة مساعدة للتحديث الجزئي
        public void UpdateStatus(InvoiceStatus newStatus, DateTime? completedDate = null)
        {
            Status = newStatus;
            if (completedDate.HasValue)
                CompletedDate = completedDate.Value;
            else if (newStatus == InvoiceStatus.Completed)
                CompletedDate = DateTime.Now;
            else if (newStatus == InvoiceStatus.Draft)
                CompletedDate = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}