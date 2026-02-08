using Microsoft.EntityFrameworkCore;
using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // ✅ إعداد NoTracking كإفتراضي
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // ✅ منع التتبع التلقائي للكائنات
            this.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // ✅ إضافة تسجيل حساس للبيانات للمساعدة في التصحيح
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Customer>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Customer>()
                .Property(c => c.PhoneNumber)
                .HasMaxLength(20);

            modelBuilder.Entity<Customer>()
                .Property(c => c.Address)
                .HasMaxLength(200);

            modelBuilder.Entity<Product>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Product>()
                .Property(p => p.SKU)
                .HasMaxLength(50);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.Unit)
                .HasMaxLength(30);

            modelBuilder.Entity<Product>()
                .Property(p => p.Category)
                .HasMaxLength(50);

            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Product>()
                .Property(p => p.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Invoice>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.FinalAmount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.ProfitMarginPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceDate)
                .IsRequired();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.CompletedDate)
                .IsRequired(false);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceDiscountPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Notes)
                .HasMaxLength(500)
                .IsRequired(false);

            modelBuilder.Entity<InvoiceItem>()
                .HasKey(ii => ii.Id);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Product)
                .WithMany()
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.Quantity)
                .HasPrecision(10, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.UnitPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.OriginalUnitPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.DiscountPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.ItemProfitMarginPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.LineTotal)
                .HasPrecision(12, 2);
        }
    }
}