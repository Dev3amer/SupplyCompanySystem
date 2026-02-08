using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;

namespace SupplyCompanySystem.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Customer> GetAll()
        {
            // ✅ إصلاح: استخدام AsNoTracking
            return _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToList();
        }

        public Customer GetById(int id)
        {
            // ✅ إصلاح: استخدام AsNoTracking
            return _context.Customers
                .AsNoTracking()
                .FirstOrDefault(c => c.Id == id);
        }

        public List<Customer> GetActiveCustomers()
        {
            return _context.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public List<Customer> GetInactiveCustomers()
        {
            return _context.Customers
                .AsNoTracking()
                .Where(c => !c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public void Add(Customer customer)
        {
            try
            {
                // ✅ إصلاح: نضمن عدم وجود كائنات مرتبطة
                DetachAllEntities();

                var newCustomer = new Customer
                {
                    Name = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    Address = customer.Address,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Customers.Add(newCustomer);
                _context.SaveChanges();

                // ✅ تحديث Id العميل الأصلي
                customer.Id = newCustomer.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة العميل: {ex.Message}");
            }
        }

        public void Update(Customer customer)
        {
            try
            {
                // ✅ إصلاح: نستخدم طريقة آمنة للتحديث
                DetachAllEntities();

                var existingCustomer = _context.Customers
                    .FirstOrDefault(c => c.Id == customer.Id);

                if (existingCustomer == null)
                    throw new InvalidOperationException($"العميل رقم {customer.Id} غير موجود");

                existingCustomer.Name = customer.Name;
                existingCustomer.PhoneNumber = customer.PhoneNumber;
                existingCustomer.Address = customer.Address;
                existingCustomer.IsActive = customer.IsActive;

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث العميل: {ex.Message}");
            }
        }

        public void Delete(int id)
        {
            try
            {
                // ✅ إصلاح: نستخدم AsNoTracking ثم نرفق الكائن
                var customer = _context.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.Id == id);

                if (customer != null)
                {
                    _context.Attach(customer);
                    _context.Customers.Remove(customer);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف العميل: {ex.Message}");
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        // ✅ دالة مساعدة لفصل جميع الكائنات
        private void DetachAllEntities()
        {
            var changedEntriesCopy = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }
    }
}