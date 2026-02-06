using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Application.Interfaces
{
    public interface IInvoiceRepository
    {
        List<Invoice> GetAll();
        Invoice GetById(int id);
        Invoice GetByIdWithItems(int id);

        // ⭐ دوال جديدة
        List<Invoice> GetCompletedInvoices();
        bool ReturnToDraft(int invoiceId); // ⭐ إضافة هذه الدالة

        void Add(Invoice invoice);
        void Update(Invoice invoice);
        void Delete(int id);
        void SaveChanges();
    }
}