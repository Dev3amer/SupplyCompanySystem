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
        private decimal _originalUnitPrice;
        private decimal _discountPercentage;
        private decimal _itemProfitMarginPercentage;
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
                    OnPropertyChanged(nameof(QuantityFormatted));
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

        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set
            {
                if (_discountPercentage != value)
                {
                    _discountPercentage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DiscountPercentageFormatted));
                    UpdateLineTotal();
                }
            }
        }

        public decimal ItemProfitMarginPercentage
        {
            get => _itemProfitMarginPercentage;
            set
            {
                if (_itemProfitMarginPercentage != value)
                {
                    _itemProfitMarginPercentage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ItemProfitMarginPercentageFormatted));
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

        public string ItemProfitMarginPercentageFormatted
        {
            get
            {
                if (_itemProfitMarginPercentage == Math.Floor(_itemProfitMarginPercentage))
                    return _itemProfitMarginPercentage.ToString("0");
                else
                    return _itemProfitMarginPercentage.ToString("0.00");
            }
        }

        public string QuantityFormatted
        {
            get
            {
                if (_quantity == Math.Floor(_quantity))
                    return _quantity.ToString("0");
                else
                    return _quantity.ToString("0.00");
            }
        }

        public string DiscountPercentageFormatted
        {
            get
            {
                if (_discountPercentage == Math.Floor(_discountPercentage))
                    return _discountPercentage.ToString("0");
                else
                    return _discountPercentage.ToString("0.00");
            }
        }

        public decimal PriceAfterProfit => UnitPrice;
        public decimal DiscountAmount => (Quantity * OriginalUnitPrice * DiscountPercentage) / 100;
        public decimal ItemProfitAmount => (Quantity * OriginalUnitPrice * ItemProfitMarginPercentage) / 100;

        public decimal TotalItemProfit => (Quantity * UnitPrice) - (Quantity * OriginalUnitPrice);

        public decimal EffectiveProfitMarginPercentage
        {
            get
            {
                if (OriginalUnitPrice <= 0)
                    return 0;
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
            OriginalUnitPrice = unitPrice;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountPercentage = 0;
            ItemProfitMarginPercentage = 0;
            UpdateLineTotal();
        }

        private void ApplyProfitMarginToUnitPrice()
        {
            if (OriginalUnitPrice > 0)
            {
                UnitPrice = OriginalUnitPrice + (OriginalUnitPrice * ItemProfitMarginPercentage / 100);
                UpdateLineTotal();
            }
        }

        private void UpdateLineTotal()
        {
            decimal subtotalWithProfit = Quantity * UnitPrice;
            decimal discountAmount = (Quantity * OriginalUnitPrice * DiscountPercentage) / 100;
            LineTotal = subtotalWithProfit - discountAmount;
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(ItemProfitAmount));
            OnPropertyChanged(nameof(PriceAfterProfit));
        }

        public void UpdateLineTotalWithInvoiceProfit(decimal invoiceProfitMarginPercentage)
        {
            decimal subtotalWithProductProfit = Quantity * UnitPrice;
            decimal discountAmount = (Quantity * OriginalUnitPrice * DiscountPercentage) / 100;
            decimal subtotalAfterDiscount = subtotalWithProductProfit - discountAmount;
            decimal invoiceProfitAmount = (subtotalAfterDiscount * invoiceProfitMarginPercentage) / 100;
            LineTotal = subtotalAfterDiscount + invoiceProfitAmount;

            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(ItemProfitAmount));
            OnPropertyChanged(nameof(PriceAfterProfit));
            OnPropertyChanged(nameof(LineTotal));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}