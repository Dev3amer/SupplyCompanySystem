using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;

namespace SupplyCompanySystem.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context)
        {
            _context = context;
        }
        public List<Invoice> GetAll()
        {
            return _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
                .ToList();
        }

        public Invoice GetById(int id)
        {
            return _context.Invoices
                .Include(i => i.Customer)
                .FirstOrDefault(i => i.Id == id);
        }

        public Invoice GetByIdWithItems(int id)
        {
            return _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
                .FirstOrDefault(i => i.Id == id);
        }

        // ⭐ دالة جديدة: الحصول على فواتير مكتملة مع Pagination
        // ⭐ دالة جديدة: الحصول على فواتير مكتملة مع Pagination
        public (List<Invoice> Invoices, int TotalCount) GetCompletedInvoicesPaged(
            int pageNumber = 1,
            int pageSize = 50,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null)
        {
            // ⭐ التحقق من أن رقم الصفحة إيجابي
            if (pageNumber < 1) pageNumber = 1;

            var query = BuildFilteredQuery(fromDate, toDate, customerId, minAmount);

            // حساب العدد الإجمالي
            int totalCount = query.Count();

            // ⭐ التحقق من أن رقم الصفحة لا يتجاوز عدد الصفحات المتاحة
            int maxPageNumber = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (maxPageNumber < 1) maxPageNumber = 1;

            if (pageNumber > maxPageNumber)
            {
                pageNumber = maxPageNumber;
            }

            // تطبيق Pagination فقط إذا كان هناك بيانات
            List<Invoice> invoices;

            if (totalCount == 0)
            {
                invoices = new List<Invoice>();
            }
            else
            {
                // ⭐ التأكد من أن SKIP ليس سالباً
                int skip = (pageNumber - 1) * pageSize;
                if (skip < 0) skip = 0;

                invoices = query
                    .OrderByDescending(i => i.CompletedDate ?? i.InvoiceDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();
            }

            return (invoices, totalCount);
        }

        // ⭐ دالة جديدة: الحصول على جميع الفواتير المفلترة (للتحدير)
        public List<Invoice> GetCompletedInvoicesFiltered(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null)
        {
            var query = BuildFilteredQuery(fromDate, toDate, customerId, minAmount);

            return query
                .OrderByDescending(i => i.CompletedDate ?? i.InvoiceDate)
                .ToList();
        }

        // ⭐ دالة مساعدة لبناء استعلام الفلترة
        private IQueryable<Invoice> BuildFilteredQuery(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
                .Where(i => i.Status == InvoiceStatus.Completed)
                .AsQueryable();

            // تطبيق الفلترة
            if (fromDate.HasValue)
                query = query.Where(i => i.InvoiceDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(i => i.InvoiceDate.Date <= toDate.Value.Date);

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (minAmount.HasValue && minAmount.Value > 0)
                query = query.Where(i => i.FinalAmount >= minAmount.Value);

            return query;
        }

        public bool ReturnToDraft(int invoiceId)
        {
            var invoice = GetByIdWithItems(invoiceId);
            if (invoice == null || invoice.Status != InvoiceStatus.Completed)
                return false;

            invoice.Status = InvoiceStatus.Draft;
            invoice.CompletedDate = null;
            _context.Invoices.Update(invoice);
            SaveChanges();

            return true;
        }

        public void Add(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            SaveChanges();
        }

        public void Update(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            SaveChanges();
        }

        public void Delete(int id)
        {
            var invoice = GetById(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}