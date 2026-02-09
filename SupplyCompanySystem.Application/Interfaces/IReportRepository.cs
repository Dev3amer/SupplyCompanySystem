namespace SupplyCompanySystem.Application.Interfaces
{
    public interface IReportRepository
    {
        // تقارير المنتجات
        (List<ProductSalesReport> Products, decimal TotalSales, int TotalItems) GetTopSellingProducts(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string category = null, // ✅ تغيير من int? إلى string
            int limit = 10);

        (List<ProductSalesReport> Products, decimal TotalSales, int TotalItems) GetLeastSellingProducts(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string category = null, // ✅ تغيير من int? إلى string
            int limit = 10);

        // تقارير العملاء
        (List<CustomerReport> Customers, decimal TotalAmount) GetTopPayingCustomers(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 10);

        (List<CustomerReport> Customers, decimal TotalAmount) GetTopInvoiceCustomers(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 10);

        // تقارير مالية
        SalesSummaryReport GetSalesSummary(
            DateTime? fromDate = null,
            DateTime? toDate = null);

        List<DailySalesReport> GetDailySales(
            DateTime fromDate,
            DateTime toDate);

        List<MonthlySalesReport> GetMonthlySales(
            int year);

        // تقارير المخزون
        List<InventoryReport> GetInventoryReport();

        List<Domain.Entities.Invoice> GetInvoicesForPeriod(DateTime fromDate, DateTime toDate);
    }

    public class ProductSalesReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string SKU { get; set; }
        public decimal UnitPrice { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class CustomerReport
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
    }

    public class SalesSummaryReport
    {
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalSalesAmount { get; set; } // المبيعات الإجمالية قبل الخصم
        public decimal TotalProfitAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; } // إجمالي الخصومات
        public decimal TotalNetAmount { get; set; } // المبلغ الصافي بعد الخصم
        public decimal AverageInvoiceAmount { get; set; }
        public int MostSoldProductId { get; set; }
        public string MostSoldProductName { get; set; }
        public int MostSoldQuantity { get; set; }
        public int TopCustomerId { get; set; }
        public string TopCustomerName { get; set; }
        public decimal TopCustomerTotal { get; set; }

        // ⭐ خصائص جديدة للتحليل
        public decimal DiscountPercentage { get; set; } // نسبة الخصم من المبيعات
        public decimal ProfitMargin { get; set; } // هامش الربح %
        public decimal AverageDiscountPerInvoice { get; set; } // متوسط الخصم لكل فاتورة
        public int DiscountedInvoicesCount { get; set; } // عدد الفواتير المخصومة
    }

    public class DailySalesReport
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public int CustomerCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    public class MonthlySalesReport
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int Year { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal AverageAmount { get; set; }
        public int GrowthPercentage { get; set; }
    }

    public class InventoryReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string SKU { get; set; }
        public decimal UnitPrice { get; set; }
        public int TotalSoldQuantity { get; set; }
        public decimal TotalSoldAmount { get; set; }
        public int LastMonthSoldQuantity { get; set; }
        public int LastThreeMonthsSoldQuantity { get; set; }
        public decimal AverageMonthlySales { get; set; }
        public string SalesTrend { get; set; }
    }
}