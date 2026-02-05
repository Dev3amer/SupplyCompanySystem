using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupplyCompanySystem.Domain.Entities
{
    public class InvoiceItem : BaseEntity, INotifyPropertyChanged
    {
        private int _invoiceId;
        private Invoice _invoice;
        private int _productId;
        private Product _product;
        private decimal _quantity;
        private decimal _unitPrice;
        private decimal _discountPercentage;  // ✅ جديد - خصم لكل بند
        private decimal _lineTotal;

        public int InvoiceId
        {
            get => _invoiceId;
            set
            {
                if (_invoiceId != value)
                {
                    _invoiceId = value;
                    OnPropertyChanged();
                }
            }
        }

        public Invoice Invoice
        {
            get => _invoice;
            set
            {
                if (_invoice != value)
                {
                    _invoice = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                if (_productId != value)
                {
                    _productId = value;
                    OnPropertyChanged();
                }
            }
        }

        public Product Product
        {
            get => _product;
            set
            {
                if (_product != value)
                {
                    _product = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    UpdateLineTotal();
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    UpdateLineTotal();
                }
            }
        }

        // ✅ جديد - نسبة الخصم لكل بند
        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set
            {
                if (_discountPercentage != value)
                {
                    _discountPercentage = value;
                    OnPropertyChanged();
                    UpdateLineTotal();
                }
            }
        }

        public decimal LineTotal
        {
            get => _lineTotal;
            set
            {
                if (_lineTotal != value)
                {
                    _lineTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        // ✅ جديد - حساب الإجمالي مع الخصم
        public decimal DiscountAmount => (Quantity * UnitPrice * DiscountPercentage) / 100;

        public InvoiceItem()
        {
            DiscountPercentage = 0;
        }

        public InvoiceItem(int productId, decimal quantity, decimal unitPrice)
        {
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountPercentage = 0;
            UpdateLineTotal();
        }

        // ✅ جديد - تحديث الإجمالي بناءً على الكمية والسعر والخصم
        private void UpdateLineTotal()
        {
            decimal subtotal = Quantity * UnitPrice;
            decimal discountAmount = (subtotal * DiscountPercentage) / 100;
            LineTotal = subtotal - discountAmount;
            OnPropertyChanged(nameof(DiscountAmount));
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}