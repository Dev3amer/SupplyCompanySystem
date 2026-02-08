using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;
using System.Diagnostics;

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
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(ii => ii.Product)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();
        }

        public Invoice GetById(int id)
        {
            return _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(ii => ii.Product)
                .FirstOrDefault(i => i.Id == id);
        }

        public Invoice GetByIdWithItems(int id)
        {
            return _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Items)
                    .ThenInclude(ii => ii.Product)
                .FirstOrDefault(i => i.Id == id);
        }

        public (List<Invoice> Invoices, int TotalCount) GetCompletedInvoicesPaged(
            int pageNumber = 1,
            int pageSize = 50,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null)
        {
            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Completed);

            if (fromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= toDate.Value);

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (minAmount.HasValue)
                query = query.Where(i => i.FinalAmount >= minAmount.Value);

            int totalCount = query.Count();

            var invoices = query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (invoices, totalCount);
        }

        public List<Invoice> GetCompletedInvoicesFiltered(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null)
        {
            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Completed);

            if (fromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= toDate.Value);

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (minAmount.HasValue)
                query = query.Where(i => i.FinalAmount >= minAmount.Value);

            return query.OrderByDescending(i => i.InvoiceDate).ToList();
        }

        public bool ReturnToDraft(int invoiceId)
        {
            try
            {
                DetachAllEntities();

                var invoice = _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == invoiceId);

                if (invoice == null) return false;

                invoice.Status = InvoiceStatus.Draft;
                invoice.CompletedDate = null;

                _context.Attach(invoice);
                _context.Entry(invoice).State = EntityState.Modified;
                _context.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ✅ طريقة جديدة: تحديث حالة الفاتورة فقط
        public bool UpdateInvoiceStatus(int invoiceId, InvoiceStatus status)
        {
            try
            {
                DetachAllEntities();

                var invoice = _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == invoiceId);

                if (invoice == null) return false;

                var invoiceToUpdate = new Invoice
                {
                    Id = invoiceId,
                    Status = status,
                    CompletedDate = status == InvoiceStatus.Completed ? DateTime.Now : null
                };

                _context.Attach(invoiceToUpdate);
                _context.Entry(invoiceToUpdate).Property(x => x.Status).IsModified = true;
                _context.Entry(invoiceToUpdate).Property(x => x.CompletedDate).IsModified = true;

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطأ في تحديث حالة الفاتورة: {ex.Message}");
                return false;
            }
        }

        // ✅ طريقة جديدة: تحديث حالة الفاتورة وتاريخ الإكمال
        public bool UpdateInvoiceStatusAndDate(int invoiceId, InvoiceStatus status, DateTime? completedDate = null)
        {
            try
            {
                DetachAllEntities();

                var invoice = _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == invoiceId);

                if (invoice == null) return false;

                var invoiceToUpdate = new Invoice
                {
                    Id = invoiceId,
                    Status = status,
                    CompletedDate = completedDate ?? (status == InvoiceStatus.Completed ? DateTime.Now : null)
                };

                _context.Attach(invoiceToUpdate);
                _context.Entry(invoiceToUpdate).Property(x => x.Status).IsModified = true;
                _context.Entry(invoiceToUpdate).Property(x => x.CompletedDate).IsModified = true;

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطأ في تحديث حالة الفاتورة: {ex.Message}");
                return false;
            }
        }

        public void Add(Invoice invoice)
        {
            try
            {
                DetachAllEntities();

                var newInvoice = new Invoice
                {
                    CustomerId = invoice.CustomerId,
                    InvoiceDate = invoice.InvoiceDate,
                    CreatedDate = DateTime.Now,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    FinalAmount = invoice.FinalAmount,
                    Notes = invoice.Notes,
                    ProfitMarginPercentage = invoice.ProfitMarginPercentage,
                    InvoiceDiscountPercentage = invoice.InvoiceDiscountPercentage
                };

                _context.Invoices.Add(newInvoice);
                _context.SaveChanges();

                if (invoice.Items != null && invoice.Items.Any())
                {
                    foreach (var item in invoice.Items)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            InvoiceId = newInvoice.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            OriginalUnitPrice = item.OriginalUnitPrice,
                            DiscountPercentage = item.DiscountPercentage,
                            ItemProfitMarginPercentage = item.ItemProfitMarginPercentage,
                            LineTotal = item.LineTotal
                        };
                        _context.InvoiceItems.Add(invoiceItem);
                    }
                    _context.SaveChanges();
                }

                invoice.Id = newInvoice.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة الفاتورة: {ex.Message}");
            }
        }

        public void Update(Invoice invoice)
        {
            try
            {
                DetachAllEntities();

                var existingInvoice = _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Items)
                    .FirstOrDefault(i => i.Id == invoice.Id);

                if (existingInvoice == null)
                    throw new InvalidOperationException($"الفاتورة رقم {invoice.Id} غير موجودة");

                var invoiceToUpdate = new Invoice
                {
                    Id = invoice.Id,
                    CustomerId = invoice.CustomerId,
                    InvoiceDate = invoice.InvoiceDate,
                    CreatedDate = existingInvoice.CreatedDate,
                    Status = invoice.Status,
                    TotalAmount = invoice.TotalAmount,
                    FinalAmount = invoice.FinalAmount,
                    Notes = invoice.Notes,
                    ProfitMarginPercentage = invoice.ProfitMarginPercentage,
                    InvoiceDiscountPercentage = invoice.InvoiceDiscountPercentage,
                    CompletedDate = invoice.Status == InvoiceStatus.Completed && existingInvoice.CompletedDate == null
                        ? DateTime.Now
                        : existingInvoice.CompletedDate
                };

                _context.Attach(invoiceToUpdate);
                _context.Entry(invoiceToUpdate).State = EntityState.Modified;

                var existingItems = _context.InvoiceItems
                    .Where(ii => ii.InvoiceId == invoice.Id)
                    .AsNoTracking()
                    .ToList();

                if (existingItems.Any())
                {
                    _context.Database.ExecuteSqlRaw("DELETE FROM InvoiceItems WHERE InvoiceId = {0}", invoice.Id);
                }

                if (invoice.Items != null && invoice.Items.Any())
                {
                    foreach (var item in invoice.Items)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            InvoiceId = invoice.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            OriginalUnitPrice = item.OriginalUnitPrice,
                            DiscountPercentage = item.DiscountPercentage,
                            ItemProfitMarginPercentage = item.ItemProfitMarginPercentage,
                            LineTotal = item.LineTotal
                        };
                        _context.InvoiceItems.Add(invoiceItem);
                    }
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث الفاتورة: {ex.Message}");
            }
        }

        public void Delete(int id)
        {
            try
            {
                DetachAllEntities();

                var invoice = _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == id);

                if (invoice != null)
                {
                    _context.Attach(invoice);
                    _context.Invoices.Remove(invoice);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف الفاتورة: {ex.Message}");
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        private void DetachAllEntities()
        {
            try
            {
                var entries = _context.ChangeTracker.Entries().ToList();
                foreach (var entry in entries)
                {
                    if (entry.Entity != null)
                    {
                        entry.State = EntityState.Detached;
                    }
                }

                _context.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطأ في فصل الكائنات: {ex.Message}");
            }
        }
    }
}