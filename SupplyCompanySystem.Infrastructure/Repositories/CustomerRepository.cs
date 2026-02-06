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
            return _context.Customers.ToList();
        }

        public List<Customer> GetActiveCustomers()
        {
            return _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public List<Customer> GetInactiveCustomers()
        {
            return _context.Customers
                .Where(c => !c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
        }

        public Customer GetById(int id)
        {
            return _context.Customers.FirstOrDefault(c => c.Id == id);
        }

        public void Add(Customer customer)
        {
            _context.Customers.Add(customer);
            SaveChanges();
        }

        public void Update(Customer customer)
        {
            _context.Customers.Update(customer);
            SaveChanges();
        }

        public void Delete(int id)
        {
            var customer = GetById(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}