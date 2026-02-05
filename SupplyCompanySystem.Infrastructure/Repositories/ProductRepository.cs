using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;

namespace SupplyCompanySystem.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Product> GetAll()
        {
            return _context.Products.ToList();
        }

        public Product GetById(int id)
        {
            return _context.Products.FirstOrDefault(p => p.Id == id);
        }

        // ✅ التحقق من تكرار الاسم
        public bool IsNameUnique(string name, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.Name.ToLower() == name.ToLower().Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return !query.Any();
        }

        // ✅ التحقق من تكرار SKU
        public bool IsSkuUnique(string sku, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.SKU.ToLower() == sku.ToLower().Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return !query.Any();
        }

        // ✅ الحصول على منتج بالاسم
        public Product GetByName(string name)
        {
            return _context.Products
                .FirstOrDefault(p => p.Name.ToLower() == name.ToLower().Trim());
        }

        // ✅ الحصول على منتج بالـ SKU
        public Product GetBySku(string sku)
        {
            return _context.Products
                .FirstOrDefault(p => p.SKU.ToLower() == sku.ToLower().Trim());
        }

        public void Add(Product product)
        {
            // ✅ التحقق من التكرار قبل الإضافة
            if (!IsNameUnique(product.Name))
            {
                throw new InvalidOperationException($"اسم المنتج '{product.Name}' موجود بالفعل في النظام");
            }

            if (!IsSkuUnique(product.SKU))
            {
                throw new InvalidOperationException($"الكود '{product.SKU}' موجود بالفعل في النظام");
            }

            _context.Products.Add(product);
            SaveChanges();
        }

        public void Update(Product product)
        {
            var existingProduct = GetById(product.Id);
            if (existingProduct != null)
            {
                // ✅ التحقق من التكرار قبل التحديث (إذا تغير الاسم)
                if (existingProduct.Name != product.Name && !IsNameUnique(product.Name, product.Id))
                {
                    throw new InvalidOperationException($"اسم المنتج '{product.Name}' موجود بالفعل في النظام");
                }

                // ✅ التحقق من التكرار قبل التحديث (إذا تغير الـ SKU)
                if (existingProduct.SKU != product.SKU && !IsSkuUnique(product.SKU, product.Id))
                {
                    throw new InvalidOperationException($"الكود '{product.SKU}' موجود بالفعل في النظام");
                }

                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.Price = product.Price;
                existingProduct.Unit = product.Unit;
                existingProduct.Category = product.Category;
                existingProduct.Description = product.Description;
                existingProduct.IsActive = product.IsActive;

                SaveChanges();
            }
        }

        public void Delete(int id)
        {
            var product = GetById(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}