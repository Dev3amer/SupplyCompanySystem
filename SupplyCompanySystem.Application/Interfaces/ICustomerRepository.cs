using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Application.Interfaces
{
    public interface ICustomerRepository
    {
        List<Customer> GetAll();
        Customer GetById(int id);

        List<Customer> GetActiveCustomers();
        List<Customer> GetInactiveCustomers();

        void Add(Customer customer);
        void Update(Customer customer);
        void Delete(int id);
        void SaveChanges();
    }
}