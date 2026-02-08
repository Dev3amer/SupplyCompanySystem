using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Domain.Entities;
using SupplyCompanySystem.Infrastructure.Data;
using System.Diagnostics;

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
            return _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToList();
        }

        public Product GetById(int id)
        {
            return _context.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);
        }

        public bool IsNameUnique(string name, int? excludeId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.IsActive);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return !query.Any();
        }

        public bool IsSkuUnique(string sku, int? excludeId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase) && p.IsActive);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return !query.Any();
        }

        public Product GetByName(string name)
        {
            return _context.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public Product GetBySku(string sku)
        {
            return _context.Products
                .AsNoTracking()
                .FirstOrDefault(p => p.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase));
        }

        // ✅ إضافة طريقة لجلب الفئات المميزة من قاعدة البيانات
        public List<string> GetDistinctCategories()
        {
            try
            {
                DetachAllEntities();

                var categories = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.Category))
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return categories ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"خطأ في جلب الفئات: {ex.Message}");
                return new List<string>();
            }
        }

        public void Add(Product product)
        {
            try
            {
                DetachAllEntities();

                var newProduct = new Product
                {
                    Name = product.Name,
                    SKU = product.SKU,
                    Price = product.Price,
                    Unit = product.Unit,
                    Category = product.Category,
                    Description = product.Description,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Products.Add(newProduct);
                _context.SaveChanges();

                product.Id = newProduct.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إضافة المنتج: {ex.Message}");
            }
        }

        public void Update(Product product)
        {
            try
            {
                DetachAllEntities();

                var existingProduct = _context.Products
                    .FirstOrDefault(p => p.Id == product.Id);

                if (existingProduct == null)
                    throw new InvalidOperationException($"المنتج رقم {product.Id} غير موجود");

                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.Price = product.Price;
                existingProduct.Unit = product.Unit;
                existingProduct.Category = product.Category;
                existingProduct.Description = product.Description;
                existingProduct.IsActive = product.IsActive;

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث المنتج: {ex.Message}");
            }
        }

        public void Delete(int id)
        {
            try
            {
                DetachAllEntities();

                var product = _context.Products
                    .AsNoTracking()
                    .FirstOrDefault(p => p.Id == id);

                if (product != null)
                {
                    _context.Attach(product);
                    _context.Products.Remove(product);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في حذف المنتج: {ex.Message}");
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