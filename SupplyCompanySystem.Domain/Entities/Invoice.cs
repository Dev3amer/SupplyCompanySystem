using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupplyCompanySystem.Domain.Entities
{
    public enum InvoiceStatus
    {
        Draft,
        Completed,
        Cancelled
    }

    public class Invoice : BaseEntity, INotifyPropertyChanged
    {
        private int _customerId;
        private Customer _customer;
        private DateTime _invoiceDate;
        private InvoiceStatus _status;
        private List<InvoiceItem> _items;
        private decimal _totalAmount;
        private decimal _finalAmount;
        private string _notes;
        private decimal _profitMarginPercentage;

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

        public InvoiceStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
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

        // ✅ نسبة المكسب - سيتم حفظها في قاعدة البيانات
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

        public Invoice()
        {
            InvoiceDate = DateTime.Now;
            Status = InvoiceStatus.Draft;
            Items = new List<InvoiceItem>();
            ProfitMarginPercentage = 0;
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}