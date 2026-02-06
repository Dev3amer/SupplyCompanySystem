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

        // ⭐ دالة جديدة: الحصول على فواتير مكتملة مع إمكانية التعديل
        public List<Invoice> GetCompletedInvoices()
        {
            return _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
                .Where(i => i.Status == InvoiceStatus.Completed)
                .OrderByDescending(i => i.CompletedDate)
                .ThenByDescending(i => i.InvoiceDate)
                .ToList();
        }

        // ⭐ دالة جديدة: إعادة فاتورة مكتملة إلى حالة المسودة
        public bool ReturnToDraft(int invoiceId)
        {
            var invoice = GetByIdWithItems(invoiceId);
            if (invoice == null || invoice.Status != InvoiceStatus.Completed)
                return false;

            invoice.Status = InvoiceStatus.Draft;
            invoice.CompletedDate = null; // مسح تاريخ الإكمال
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