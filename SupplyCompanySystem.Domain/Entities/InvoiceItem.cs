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
        private decimal _originalUnitPrice; // ✅ جديد: السعر الأصلي بدون مكسب
        private decimal _discountPercentage;  // خصم لكل بند
        private decimal _itemProfitMarginPercentage; // ✅ جديد: نسبة المكسب على المنتج الفردي
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

        // ✅ جديد: السعر الأصلي للمنتج (بدون مكسب)
        public decimal OriginalUnitPrice
        {
            get => _originalUnitPrice;
            set
            {
                if (_originalUnitPrice != value)
                {
                    _originalUnitPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        // نسبة الخصم لكل بند
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

        // ✅ جديد: نسبة المكسب على المنتج الفردي
        public decimal ItemProfitMarginPercentage
        {
            get => _itemProfitMarginPercentage;
            set
            {
                if (_itemProfitMarginPercentage != value)
                {
                    _itemProfitMarginPercentage = value;
                    OnPropertyChanged();
                    // عند تغيير نسبة المكسب، نقوم بتحديث السعر
                    ApplyProfitMarginToUnitPrice();
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

        // ✅ جديد: حساب الإجمالي مع الخصم
        public decimal DiscountAmount => (Quantity * UnitPrice * DiscountPercentage) / 100;

        // ✅ جديد: حساب المكسب على هذا المنتج
        public decimal ItemProfitAmount => (Quantity * OriginalUnitPrice * ItemProfitMarginPercentage) / 100;

        // ✅ جديد: السعر بعد المكسب
        public decimal PriceAfterProfit => OriginalUnitPrice + (OriginalUnitPrice * ItemProfitMarginPercentage / 100);

        public InvoiceItem()
        {
            DiscountPercentage = 0;
            ItemProfitMarginPercentage = 0;
        }

        public InvoiceItem(int productId, decimal quantity, decimal unitPrice)
        {
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
            OriginalUnitPrice = unitPrice; // ✅ تعيين السعر الأصلي
            DiscountPercentage = 0;
            ItemProfitMarginPercentage = 0;
            UpdateLineTotal();
        }

        // ✅ جديد: تطبيق نسبة المكسب على السعر
        private void ApplyProfitMarginToUnitPrice()
        {
            if (OriginalUnitPrice > 0)
            {
                UnitPrice = OriginalUnitPrice + (OriginalUnitPrice * ItemProfitMarginPercentage / 100);
                UpdateLineTotal();
            }
        }

        // ✅ تحديث الإجمالي بناءً على الكمية والسعر والخصم
        private void UpdateLineTotal()
        {
            decimal subtotal = Quantity * UnitPrice;
            decimal discountAmount = (subtotal * DiscountPercentage) / 100;
            LineTotal = subtotal - discountAmount;
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(ItemProfitAmount));
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}