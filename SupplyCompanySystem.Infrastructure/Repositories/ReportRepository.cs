using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;

namespace SupplyCompanySystem.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _context;

        public ReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public (List<ProductSalesReport> Products, decimal TotalSales, int TotalItems) GetTopSellingProducts(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string category = null,
            int limit = 10)
        {
            try
            {
                var query = _context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Include(ii => ii.Product)
                    .Where(ii => ii.Invoice.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(ii => ii.Invoice.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(ii => ii.Invoice.InvoiceDate.Date <= toDate.Value.Date);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(ii => ii.Product.Category == category);

                var groupedData = query
                    .GroupBy(ii => new
                    {
                        ii.ProductId,
                        ii.Product.Name,
                        ii.Product.Category,
                        ii.Product.SKU,
                        ii.OriginalUnitPrice
                    })
                    .Select(g => new ProductSalesReport
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        Category = g.Key.Category,
                        SKU = g.Key.SKU,
                        UnitPrice = g.Key.OriginalUnitPrice,
                        TotalQuantity = (int)g.Sum(ii => ii.Quantity),
                        TotalAmount = g.Sum(ii => ii.LineTotal)
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(limit)
                    .ToList();

                decimal totalSales = groupedData.Sum(p => p.TotalAmount);
                int totalItems = groupedData.Sum(p => p.TotalQuantity);

                if (totalSales > 0)
                {
                    foreach (var product in groupedData)
                    {
                        product.PercentageOfTotal = (product.TotalAmount / totalSales) * 100;
                    }
                }

                return (groupedData, totalSales, totalItems);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات الأكثر مبيعاً: {ex.Message}");
            }
        }

        public (List<ProductSalesReport> Products, decimal TotalSales, int TotalItems) GetLeastSellingProducts(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string category = null,
            int limit = 10)
        {
            try
            {
                var query = _context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Include(ii => ii.Product)
                    .Where(ii => ii.Invoice.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(ii => ii.Invoice.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(ii => ii.Invoice.InvoiceDate.Date <= toDate.Value.Date);

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(ii => ii.Product.Category == category);

                var groupedData = query
                    .GroupBy(ii => new
                    {
                        ii.ProductId,
                        ii.Product.Name,
                        ii.Product.Category,
                        ii.Product.SKU,
                        ii.OriginalUnitPrice
                    })
                    .Select(g => new ProductSalesReport
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        Category = g.Key.Category,
                        SKU = g.Key.SKU,
                        UnitPrice = g.Key.OriginalUnitPrice,
                        TotalQuantity = (int)g.Sum(ii => ii.Quantity),
                        TotalAmount = g.Sum(ii => ii.LineTotal)
                    })
                    .OrderBy(p => p.TotalQuantity)
                    .ThenBy(p => p.ProductName)
                    .Take(limit)
                    .ToList();

                decimal totalSales = groupedData.Sum(p => p.TotalAmount);
                int totalItems = groupedData.Sum(p => p.TotalQuantity);

                if (totalSales > 0)
                {
                    foreach (var product in groupedData)
                    {
                        product.PercentageOfTotal = (product.TotalAmount / totalSales) * 100;
                    }
                }

                return (groupedData, totalSales, totalItems);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المنتجات الأقل مبيعاً: {ex.Message}");
            }
        }

        public (List<CustomerReport> Customers, decimal TotalAmount) GetTopPayingCustomers(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 10)
        {
            try
            {
                var query = _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date <= toDate.Value.Date);

                var groupedData = query
                    .GroupBy(i => new
                    {
                        i.CustomerId,
                        i.Customer.Name,
                        i.Customer.PhoneNumber
                    })
                    .Select(g => new CustomerReport
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.Name,
                        PhoneNumber = g.Key.PhoneNumber,
                        InvoiceCount = g.Count(),
                        TotalAmount = g.Sum(i => i.FinalAmount),
                        AverageInvoiceAmount = g.Average(i => i.FinalAmount),
                        LastInvoiceDate = g.Max(i => i.InvoiceDate)
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .Take(limit)
                    .ToList();

                decimal totalAmount = groupedData.Sum(c => c.TotalAmount);

                return (groupedData, totalAmount);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العملاء الأكثر إنفاقاً: {ex.Message}");
            }
        }

        public (List<CustomerReport> Customers, decimal TotalAmount) GetTopInvoiceCustomers(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 10)
        {
            try
            {
                var query = _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date <= toDate.Value.Date);

                var groupedData = query
                    .GroupBy(i => new
                    {
                        i.CustomerId,
                        i.Customer.Name,
                        i.Customer.PhoneNumber
                    })
                    .Select(g => new CustomerReport
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.Name,
                        PhoneNumber = g.Key.PhoneNumber,
                        InvoiceCount = g.Count(),
                        TotalAmount = g.Sum(i => i.FinalAmount),
                        AverageInvoiceAmount = g.Average(i => i.FinalAmount),
                        LastInvoiceDate = g.Max(i => i.InvoiceDate)
                    })
                    .OrderByDescending(c => c.InvoiceCount)
                    .ThenByDescending(c => c.TotalAmount)
                    .Take(limit)
                    .ToList();

                decimal totalAmount = groupedData.Sum(c => c.TotalAmount);

                return (groupedData, totalAmount);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب العملاء الأكثر طلباً للعروض: {ex.Message}");
            }
        }

        public SalesSummaryReport GetSalesSummary(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                        .ThenInclude(ii => ii.Product)
                    .Where(i => i.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(i => i.InvoiceDate.Date <= toDate.Value.Date);

                var invoices = query.ToList();

                var mostSoldProduct = invoices
                    .SelectMany(i => i.Items)
                    .GroupBy(ii => new { ii.ProductId, ii.Product.Name })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        TotalQuantity = (int)g.Sum(ii => ii.Quantity)
                    })
                    .OrderByDescending(g => g.TotalQuantity)
                    .FirstOrDefault();

                var topCustomer = invoices
                    .GroupBy(i => new { i.CustomerId, i.Customer.Name })
                    .Select(g => new
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.Name,
                        TotalAmount = g.Sum(i => i.FinalAmount)
                    })
                    .OrderByDescending(g => g.TotalAmount)
                    .FirstOrDefault();

                decimal totalProfit = 0;
                foreach (var invoice in invoices)
                {
                    foreach (var item in invoice.Items)
                    {
                        decimal itemProfit = (item.UnitPrice - item.OriginalUnitPrice) * item.Quantity;
                        totalProfit += itemProfit;
                    }
                }

                decimal totalDiscount = 0;
                foreach (var invoice in invoices)
                {
                    var invoiceDiscountProp = invoice.GetType().GetProperty("InvoiceDiscountAmount");
                    if (invoiceDiscountProp != null)
                    {
                        var discountValue = invoiceDiscountProp.GetValue(invoice);
                        if (discountValue != null)
                        {
                            totalDiscount += Convert.ToDecimal(discountValue);
                        }
                    }

                    foreach (var item in invoice.Items)
                    {
                        var discountProp = item.GetType().GetProperty("DiscountPercentage");
                        if (discountProp != null)
                        {
                            var discountValue = discountProp.GetValue(item);
                            if (discountValue != null)
                            {
                                decimal discountPercent = Convert.ToDecimal(discountValue);
                                totalDiscount += (item.OriginalUnitPrice * item.Quantity * discountPercent / 100);
                            }
                        }
                    }
                }

                var summary = new SalesSummaryReport
                {
                    PeriodFrom = fromDate,
                    PeriodTo = toDate,
                    TotalInvoices = invoices.Count,
                    TotalCustomers = invoices.Select(i => i.CustomerId).Distinct().Count(),
                    TotalProductsSold = (int)invoices.Sum(i => i.Items.Sum(ii => ii.Quantity)),
                    TotalSalesAmount = invoices.Sum(i => i.FinalAmount),
                    TotalProfitAmount = totalProfit,
                    TotalDiscountAmount = totalDiscount,
                    AverageInvoiceAmount = invoices.Count > 0 ? invoices.Average(i => i.FinalAmount) : 0
                };

                if (mostSoldProduct != null)
                {
                    summary.MostSoldProductId = mostSoldProduct.ProductId;
                    summary.MostSoldProductName = mostSoldProduct.ProductName;
                    summary.MostSoldQuantity = mostSoldProduct.TotalQuantity;
                }

                if (topCustomer != null)
                {
                    summary.TopCustomerId = topCustomer.CustomerId;
                    summary.TopCustomerName = topCustomer.CustomerName;
                    summary.TopCustomerTotal = topCustomer.TotalAmount;
                }

                return summary;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب ملخص المبيعات: {ex.Message}");
            }
        }

        public List<DailySalesReport> GetDailySales(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var invoices = _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .Where(i => i.Status == InvoiceStatus.Completed &&
                               i.InvoiceDate.Date >= fromDate.Date &&
                               i.InvoiceDate.Date <= toDate.Date)
                    .AsNoTracking()
                    .ToList();

                var dailyGroups = invoices
                    .GroupBy(i => i.InvoiceDate.Date)
                    .Select(g => new DailySalesReport
                    {
                        Date = g.Key,
                        InvoiceCount = g.Count(),
                        CustomerCount = g.Select(i => i.CustomerId).Distinct().Count(),
                        TotalAmount = g.Sum(i => i.FinalAmount),
                        TotalProfit = 0,
                        TotalDiscount = 0
                    })
                    .OrderBy(r => r.Date)
                    .ToList();

                foreach (var report in dailyGroups)
                {
                    var dayInvoices = invoices.Where(i => i.InvoiceDate.Date == report.Date).ToList();

                    foreach (var invoice in dayInvoices)
                    {
                        foreach (var item in invoice.Items)
                        {
                            decimal itemProfit = (item.UnitPrice - item.OriginalUnitPrice) * item.Quantity;
                            report.TotalProfit += itemProfit;

                            try
                            {
                                var discountProp = item.GetType().GetProperty("DiscountPercentage");
                                if (discountProp != null)
                                {
                                    var discountValue = discountProp.GetValue(item);
                                    if (discountValue != null)
                                    {
                                        decimal discountPercent = Convert.ToDecimal(discountValue);
                                        decimal itemDiscount = (item.OriginalUnitPrice * item.Quantity * discountPercent / 100);
                                        report.TotalDiscount += itemDiscount;
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }

                        try
                        {
                            var invoiceDiscountProp = invoice.GetType().GetProperty("InvoiceDiscountAmount");
                            if (invoiceDiscountProp != null)
                            {
                                var discountValue = invoiceDiscountProp.GetValue(invoice);
                                if (discountValue != null)
                                {
                                    report.TotalDiscount += Convert.ToDecimal(discountValue);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                var allDates = Enumerable.Range(0, (toDate.Date - fromDate.Date).Days + 1)
                    .Select(offset => fromDate.Date.AddDays(offset))
                    .ToList();

                foreach (var date in allDates)
                {
                    if (!dailyGroups.Any(r => r.Date.Date == date))
                    {
                        dailyGroups.Add(new DailySalesReport
                        {
                            Date = date,
                            InvoiceCount = 0,
                            CustomerCount = 0,
                            TotalAmount = 0,
                            TotalProfit = 0,
                            TotalDiscount = 0
                        });
                    }
                }

                return dailyGroups.OrderBy(r => r.Date).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المبيعات اليومية: {ex.Message}");
            }
        }

        public List<MonthlySalesReport> GetMonthlySales(
            int year)
        {
            try
            {
                var invoices = _context.Invoices
                    .Include(i => i.Items)
                    .Where(i => i.Status == InvoiceStatus.Completed &&
                               i.InvoiceDate.Year == year)
                    .AsNoTracking()
                    .ToList();

                var monthlyReports = new List<MonthlySalesReport>();

                for (int month = 1; month <= 12; month++)
                {
                    var monthInvoices = invoices
                        .Where(i => i.InvoiceDate.Month == month)
                        .ToList();

                    decimal totalProfit = 0;
                    foreach (var invoice in monthInvoices)
                    {
                        foreach (var item in invoice.Items)
                        {
                            totalProfit += (item.UnitPrice - item.OriginalUnitPrice) * item.Quantity;
                        }
                    }

                    var report = new MonthlySalesReport
                    {
                        Year = year,
                        Month = month,
                        MonthName = GetArabicMonthName(month),
                        InvoiceCount = monthInvoices.Count,
                        TotalAmount = monthInvoices.Sum(i => i.FinalAmount),
                        TotalProfit = totalProfit,
                        AverageAmount = monthInvoices.Count > 0 ?
                            monthInvoices.Average(i => i.FinalAmount) : 0,
                        GrowthPercentage = 0
                    };

                    monthlyReports.Add(report);
                }

                for (int i = 1; i < monthlyReports.Count; i++)
                {
                    var current = monthlyReports[i];
                    var previous = monthlyReports[i - 1];

                    if (previous.TotalAmount > 0)
                    {
                        current.GrowthPercentage = (int)((current.TotalAmount - previous.TotalAmount) / previous.TotalAmount * 100);
                    }
                    else if (current.TotalAmount > 0)
                    {
                        current.GrowthPercentage = 100;
                    }
                }

                return monthlyReports.OrderBy(r => r.Month).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب المبيعات الشهرية: {ex.Message}");
            }
        }

        public List<InventoryReport> GetInventoryReport()
        {
            try
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                var oneMonthAgo = DateTime.Now.AddMonths(-1);

                var products = _context.Products
                    .Where(p => p.IsActive)
                    .AsNoTracking()
                    .ToList();

                var invoiceItems = _context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Include(ii => ii.Product)
                    .Where(ii => ii.Invoice.Status == InvoiceStatus.Completed)
                    .AsNoTracking()
                    .ToList();

                var reports = new List<InventoryReport>();

                foreach (var product in products)
                {
                    var productItems = invoiceItems
                        .Where(ii => ii.ProductId == product.Id)
                        .ToList();

                    var lastMonthItems = productItems
                        .Where(ii => ii.Invoice.InvoiceDate >= oneMonthAgo)
                        .ToList();

                    var lastThreeMonthsItems = productItems
                        .Where(ii => ii.Invoice.InvoiceDate >= threeMonthsAgo)
                        .ToList();

                    int totalSoldQuantity = (int)productItems.Sum(ii => ii.Quantity);
                    int lastMonthSoldQuantity = (int)lastMonthItems.Sum(ii => ii.Quantity);
                    int lastThreeMonthsSoldQuantity = (int)lastThreeMonthsItems.Sum(ii => ii.Quantity);

                    var report = new InventoryReport
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Category = product.Category,
                        SKU = product.SKU,
                        UnitPrice = product.Price,
                        TotalSoldQuantity = totalSoldQuantity,
                        TotalSoldAmount = productItems.Sum(ii => ii.LineTotal),
                        LastMonthSoldQuantity = lastMonthSoldQuantity,
                        LastThreeMonthsSoldQuantity = lastThreeMonthsSoldQuantity
                    };

                    if (lastThreeMonthsItems.Any())
                    {
                        report.AverageMonthlySales = lastThreeMonthsSoldQuantity / 3.0m;
                    }

                    report.SalesTrend = DetermineSalesTrend(lastMonthSoldQuantity, report.AverageMonthlySales);

                    reports.Add(report);
                }

                return reports.OrderByDescending(r => r.TotalSoldAmount).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في جلب تقرير المخزون: {ex.Message}");
            }
        }

        public List<Domain.Entities.Invoice> GetInvoicesForPeriod(DateTime fromDate, DateTime toDate)
        {
            return _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(ii => ii.Product)
                .Where(i => i.Status == InvoiceStatus.Completed &&
                           i.InvoiceDate.Date >= fromDate.Date &&
                           i.InvoiceDate.Date <= toDate.Date)
                .AsNoTracking()
                .ToList();
        }

        private string DetermineSalesTrend(int lastMonthSales, decimal averageMonthlySales)
        {
            if (averageMonthlySales == 0)
            {
                return lastMonthSales > 0 ? "جديد" : "لا توجد مبيعات";
            }

            decimal percentage = ((decimal)lastMonthSales - averageMonthlySales) / averageMonthlySales * 100;

            if (percentage > 20)
                return "زيادة";
            else if (percentage < -20)
                return "انخفاض";
            else
                return "ثابت";
        }

        private string GetArabicMonthName(int month)
        {
            return month switch
            {
                1 => "يناير",
                2 => "فبراير",
                3 => "مارس",
                4 => "أبريل",
                5 => "مايو",
                6 => "يونيو",
                7 => "يوليو",
                8 => "أغسطس",
                9 => "سبتمبر",
                10 => "أكتوبر",
                11 => "نوفمبر",
                12 => "ديسمبر",
                _ => "غير معروف"
            };
        }
    }
}