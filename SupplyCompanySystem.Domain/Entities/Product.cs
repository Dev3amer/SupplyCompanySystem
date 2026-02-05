namespace SupplyCompanySystem.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }

        // ✅ جديد - للتعطيل بدل الحذف
        public bool IsActive { get; set; } = true;

        public Product()
        {
            CreatedDate = DateTime.Now;
            IsActive = true;
        }
    }
}