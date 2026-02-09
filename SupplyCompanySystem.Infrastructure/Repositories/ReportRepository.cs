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

                // استعلام محسن باستخدام Select قبل GroupBy
                var groupedData = query
                    .Select(ii => new
                    {
                        ii.ProductId,
                        ii.Product.Name,
                        ii.Product.Category,
                        ii.Product.SKU,
                        ii.OriginalUnitPrice,
                        ii.Quantity,
                        ii.LineTotal
                    })
                    .GroupBy(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Category,
                        p.SKU,
                        p.OriginalUnitPrice
                    })
                    .Select(g => new ProductSalesReport
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        Category = g.Key.Category,
                        SKU = g.Key.SKU,
                        UnitPrice = g.Key.OriginalUnitPrice,
                        TotalQuantity = (int)g.Sum(p => p.Quantity),
                        TotalAmount = g.Sum(p => p.LineTotal)
                    })
                    .OrderByDescending(p => p.TotalQuantity)
                    .Take(limit)
                    .AsNoTracking()
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

                // استعلام محسن
                var groupedData = query
                    .Select(ii => new
                    {
                        ii.ProductId,
                        ii.Product.Name,
                        ii.Product.Category,
                        ii.Product.SKU,
                        ii.OriginalUnitPrice,
                        ii.Quantity,
                        ii.LineTotal
                    })
                    .GroupBy(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Category,
                        p.SKU,
                        p.OriginalUnitPrice
                    })
                    .Select(g => new ProductSalesReport
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        Category = g.Key.Category,
                        SKU = g.Key.SKU,
                        UnitPrice = g.Key.OriginalUnitPrice,
                        TotalQuantity = (int)g.Sum(p => p.Quantity),
                        TotalAmount = g.Sum(p => p.LineTotal)
                    })
                    .OrderBy(p => p.TotalQuantity)
                    .ThenBy(p => p.ProductName)
                    .Take(limit)
                    .AsNoTracking()
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

                // استعلام محسن مع Select أولاً
                var groupedData = query
                    .Select(i => new
                    {
                        i.CustomerId,
                        i.Customer.Name,
                        i.Customer.PhoneNumber,
                        i.FinalAmount,
                        i.InvoiceDate
                    })
                    .GroupBy(i => new
                    {
                        i.CustomerId,
                        i.Name,
                        i.PhoneNumber
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
                    .AsNoTracking()
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

                // استعلام محسن
                var groupedData = query
                    .Select(i => new
                    {
                        i.CustomerId,
                        i.Customer.Name,
                        i.Customer.PhoneNumber,
                        i.FinalAmount,
                        i.InvoiceDate
                    })
                    .GroupBy(i => new
                    {
                        i.CustomerId,
                        i.Name,
                        i.PhoneNumber
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
                    .AsNoTracking()
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

                // استخدام Select لتحسين الأداء
                var invoiceData = query
                    .Select(i => new
                    {
                        i.Id,
                        i.CustomerId,
                        i.FinalAmount,
                        i.InvoiceDate,
                        Items = i.Items.Select(ii => new
                        {
                            ii.ProductId,
                            ii.Product.Name,
                            ii.Quantity,
                            ii.UnitPrice,
                            ii.OriginalUnitPrice,
                            ii.LineTotal
                        }).ToList()
                    })
                    .ToList();

                if (!invoiceData.Any())
                {
                    return new SalesSummaryReport
                    {
                        PeriodFrom = fromDate,
                        PeriodTo = toDate
                    };
                }

                // حساب أكثر المنتجات مبيعاً
                var mostSoldProduct = invoiceData
                    .SelectMany(i => i.Items)
                    .GroupBy(ii => new { ii.ProductId, ii.Name })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        TotalQuantity = (int)g.Sum(ii => ii.Quantity)
                    })
                    .OrderByDescending(g => g.TotalQuantity)
                    .FirstOrDefault();

                // حساب العملاء الأكثر إنفاقاً
                var topCustomer = invoiceData
                    .GroupBy(i => i.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        TotalAmount = g.Sum(i => i.FinalAmount)
                    })
                    .OrderByDescending(g => g.TotalAmount)
                    .FirstOrDefault();

                // حساب إجمالي الربح
                decimal totalProfit = 0;
                foreach (var invoice in invoiceData)
                {
                    foreach (var item in invoice.Items)
                    {
                        decimal itemProfit = (item.UnitPrice - item.OriginalUnitPrice) * item.Quantity;
                        totalProfit += itemProfit;
                    }
                }

                // حساب إجمالي الخصم (افتراضي)
                decimal totalDiscount = 0;

                var summary = new SalesSummaryReport
                {
                    PeriodFrom = fromDate,
                    PeriodTo = toDate,
                    TotalInvoices = invoiceData.Count,
                    TotalCustomers = invoiceData.Select(i => i.CustomerId).Distinct().Count(),
                    TotalProductsSold = (int)invoiceData.Sum(i => i.Items.Sum(ii => ii.Quantity)),
                    TotalSalesAmount = invoiceData.Sum(i => i.FinalAmount),
                    TotalProfitAmount = totalProfit,
                    TotalDiscountAmount = totalDiscount,
                    AverageInvoiceAmount = invoiceData.Count > 0 ? invoiceData.Average(i => i.FinalAmount) : 0
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
                    // نحتاج لجلب اسم العميل
                    var customer = _context.Customers
                        .Where(c => c.Id == topCustomer.CustomerId)
                        .Select(c => c.Name)
                        .FirstOrDefault();

                    summary.TopCustomerName = customer ?? "غير معروف";
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
                // استخدام استعلام أكثر كفاءة
                var invoiceData = _context.Invoices
                    .Include(i => i.Items)
                    .Where(i => i.Status == InvoiceStatus.Completed &&
                               i.InvoiceDate.Date >= fromDate.Date &&
                               i.InvoiceDate.Date <= toDate.Date)
                    .Select(i => new
                    {
                        i.InvoiceDate.Date,
                        i.CustomerId,
                        i.FinalAmount,
                        i.Items
                    })
                    .AsNoTracking()
                    .ToList();

                var dailyData = invoiceData
                    .GroupBy(i => i.Date)
                    .Select(g => new DailySalesReport
                    {
                        Date = g.Key,
                        InvoiceCount = g.Count(),
                        CustomerCount = g.Select(i => i.CustomerId).Distinct().Count(),
                        TotalAmount = g.Sum(i => i.FinalAmount),
                        TotalProfit = 0, // سيتم حسابه لاحقاً
                        TotalDiscount = 0 // سيتم حسابه لاحقاً
                    })
                    .OrderBy(r => r.Date)
                    .ToList();

                // حساب الربح لكل يوم
                foreach (var report in dailyData)
                {
                    var dayInvoices = invoiceData
                        .Where(i => i.Date == report.Date)
                        .ToList();

                    foreach (var invoice in dayInvoices)
                    {
                        foreach (var item in invoice.Items)
                        {
                            decimal itemProfit = (item.UnitPrice - item.OriginalUnitPrice) * item.Quantity;
                            report.TotalProfit += itemProfit;
                        }
                    }
                }

                // إضافة الأيام الفارغة
                var allDates = Enumerable.Range(0, (toDate.Date - fromDate.Date).Days + 1)
                    .Select(offset => fromDate.Date.AddDays(offset))
                    .ToList();

                foreach (var date in allDates)
                {
                    if (!dailyData.Any(r => r.Date.Date == date))
                    {
                        dailyData.Add(new DailySalesReport
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

                return dailyData.OrderBy(r => r.Date).ToList();
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
                var invoiceData = _context.Invoices
                    .Include(i => i.Items)
                    .Where(i => i.Status == InvoiceStatus.Completed &&
                               i.InvoiceDate.Year == year)
                    .Select(i => new
                    {
                        i.InvoiceDate.Month,
                        i.FinalAmount,
                        Items = i.Items.Select(ii => new
                        {
                            ii.UnitPrice,
                            ii.OriginalUnitPrice,
                            ii.Quantity
                        }).ToList()
                    })
                    .AsNoTracking()
                    .ToList();

                var monthlyData = invoiceData
                    .GroupBy(i => i.Month)
                    .Select(g => new MonthlySalesReport
                    {
                        Year = year,
                        Month = g.Key,
                        MonthName = GetArabicMonthName(g.Key),
                        InvoiceCount = g.Count(),
                        TotalAmount = g.Sum(i => i.FinalAmount),
                        TotalProfit = g.Sum(i => i.Items.Sum(ii =>
                            (ii.UnitPrice - ii.OriginalUnitPrice) * ii.Quantity)),
                        AverageAmount = g.Average(i => i.FinalAmount),
                        GrowthPercentage = 0
                    })
                    .OrderBy(r => r.Month)
                    .ToList();

                // حساب النمو الشهري
                for (int i = 1; i < monthlyData.Count; i++)
                {
                    var current = monthlyData[i];
                    var previous = monthlyData[i - 1];

                    if (previous.TotalAmount > 0)
                    {
                        current.GrowthPercentage = (int)((current.TotalAmount - previous.TotalAmount) / previous.TotalAmount * 100);
                    }
                    else if (current.TotalAmount > 0)
                    {
                        current.GrowthPercentage = 100;
                    }
                }

                // إضافة الأشهر الفارغة
                var allMonths = Enumerable.Range(1, 12);
                foreach (var month in allMonths)
                {
                    if (!monthlyData.Any(r => r.Month == month))
                    {
                        monthlyData.Add(new MonthlySalesReport
                        {
                            Year = year,
                            Month = month,
                            MonthName = GetArabicMonthName(month),
                            InvoiceCount = 0,
                            TotalAmount = 0,
                            TotalProfit = 0,
                            AverageAmount = 0,
                            GrowthPercentage = 0
                        });
                    }
                }

                return monthlyData.OrderBy(r => r.Month).ToList();
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

                // استعلام محسن للمنتجات
                var products = _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Category,
                        p.SKU,
                        p.Price
                    })
                    .AsNoTracking()
                    .ToList();

                // استعلام محسن للمبيعات - استخدام نوع محدد
                var salesQuery = _context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Where(ii => ii.Invoice.Status == InvoiceStatus.Completed)
                    .Select(ii => new
                    {
                        ii.ProductId,
                        ii.Quantity,
                        ii.LineTotal,
                        ii.Invoice.InvoiceDate
                    })
                    .AsNoTracking()
                    .ToList();

                var salesData = salesQuery
                    .GroupBy(ii => ii.ProductId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var reports = new List<InventoryReport>();

                foreach (var product in products)
                {
                    if (!salesData.TryGetValue(product.Id, out var productSales))
                    {
                        // لا توجد مبيعات لهذا المنتج
                        reports.Add(new InventoryReport
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Category = product.Category,
                            SKU = product.SKU,
                            UnitPrice = product.Price,
                            TotalSoldQuantity = 0,
                            TotalSoldAmount = 0,
                            LastMonthSoldQuantity = 0,
                            LastThreeMonthsSoldQuantity = 0,
                            AverageMonthlySales = 0,
                            SalesTrend = "لا توجد مبيعات"
                        });
                        continue;
                    }

                    int totalSoldQuantity = 0;
                    decimal totalSoldAmount = 0;
                    int lastMonthSoldQuantity = 0;
                    int lastThreeMonthsSoldQuantity = 0;

                    foreach (var sale in productSales)
                    {
                        totalSoldQuantity += (int)sale.Quantity;
                        totalSoldAmount += sale.LineTotal;

                        if (sale.InvoiceDate >= oneMonthAgo)
                        {
                            lastMonthSoldQuantity += (int)sale.Quantity;
                        }

                        if (sale.InvoiceDate >= threeMonthsAgo)
                        {
                            lastThreeMonthsSoldQuantity += (int)sale.Quantity;
                        }
                    }

                    var report = new InventoryReport
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Category = product.Category,
                        SKU = product.SKU,
                        UnitPrice = product.Price,
                        TotalSoldQuantity = totalSoldQuantity,
                        TotalSoldAmount = totalSoldAmount,
                        LastMonthSoldQuantity = lastMonthSoldQuantity,
                        LastThreeMonthsSoldQuantity = lastThreeMonthsSoldQuantity
                    };

                    if (lastThreeMonthsSoldQuantity > 0)
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