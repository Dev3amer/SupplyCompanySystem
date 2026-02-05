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
