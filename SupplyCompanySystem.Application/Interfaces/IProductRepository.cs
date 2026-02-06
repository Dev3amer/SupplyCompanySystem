using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Application.Interfaces
{
    public interface IProductRepository
    {
        List<Product> GetAll();
        Product GetById(int id);

        bool IsNameUnique(string name, int? excludeId = null);
        bool IsSkuUnique(string sku, int? excludeId = null);
        Product GetByName(string name);
        Product GetBySku(string sku);

        void Add(Product product);
        void Update(Product product);
        void Delete(int id);
        void SaveChanges();
    }
}