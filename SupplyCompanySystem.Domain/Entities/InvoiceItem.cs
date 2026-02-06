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
        private decimal _originalUnitPrice; // ✅ السعر الأصلي بدون مكسب
        private decimal _discountPercentage;  // خصم لكل بند
        private decimal _itemProfitMarginPercentage; // ✅ نسبة المكسب على المنتج الفردي
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

        // ✅ السعر الأصلي للمنتج (بدون مكسب)
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

        // ✅ نسبة المكسب على المنتج الفردي
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

        // ✅ السعر بعد المكسب (يساوي UnitPrice لأنه بالفعل يحتوي على المكسب)
        public decimal PriceAfterProfit => UnitPrice;

        // ✅ حساب الخصم على السعر الأصلي
        public decimal DiscountAmount => (Quantity * OriginalUnitPrice * DiscountPercentage) / 100;

        // ✅ حساب المكسب على هذا المنتج (على السعر الأصلي)
        public decimal ItemProfitAmount => (Quantity * OriginalUnitPrice * ItemProfitMarginPercentage) / 100;

        // ✅ حساب المكسب الفعلي الكلي (الفرق بين السعر النهائي والأصلي)
        public decimal TotalItemProfit => (Quantity * UnitPrice) - (Quantity * OriginalUnitPrice);

        // ✅ حساب نسبة المكسب الفعلية الكلية لعرضها في الجدول
        public decimal EffectiveProfitMarginPercentage
        {
            get
            {
                if (OriginalUnitPrice <= 0)
                    return 0;

                // النسبة الفعلية = (السعر بعد المكسب - السعر الأصلي) / السعر الأصلي × 100
                return ((UnitPrice - OriginalUnitPrice) / OriginalUnitPrice) * 100;
            }
        }

        public InvoiceItem()
        {
            DiscountPercentage = 0;
            ItemProfitMarginPercentage = 0;
        }

        public InvoiceItem(int productId, decimal quantity, decimal unitPrice)
        {
            ProductId = productId;
            // ✅ تعيين OriginalUnitPrice أولاً قبل UnitPrice
            OriginalUnitPrice = unitPrice;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountPercentage = 0;
            ItemProfitMarginPercentage = 0;
            UpdateLineTotal();
        }

        // ✅ تطبيق نسبة المكسب على السعر
        private void ApplyProfitMarginToUnitPrice()
        {
            if (OriginalUnitPrice > 0)
            {
                UnitPrice = OriginalUnitPrice + (OriginalUnitPrice * ItemProfitMarginPercentage / 100);
                UpdateLineTotal();
            }
        }

        // ✅ تحديث الإجمالي بناءً على الكمية والسعر والخصم
        // الصيغة الصحيحة: (الكمية × السعر بعد المكسب) - الخصم على السعر الأصلي
        private void UpdateLineTotal()
        {
            decimal subtotalWithProfit = Quantity * UnitPrice;
            decimal discountAmount = (Quantity * OriginalUnitPrice * DiscountPercentage) / 100;
            LineTotal = subtotalWithProfit - discountAmount;
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(ItemProfitAmount));
            OnPropertyChanged(nameof(PriceAfterProfit));
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}