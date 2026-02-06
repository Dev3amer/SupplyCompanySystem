using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Application.Interfaces
{
    public interface IInvoiceRepository
    {
        List<Invoice> GetAll();
        Invoice GetById(int id);
        Invoice GetByIdWithItems(int id);

        (List<Invoice> Invoices, int TotalCount) GetCompletedInvoicesPaged(
            int pageNumber = 1,
            int pageSize = 50,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null);

        List<Invoice> GetCompletedInvoicesFiltered(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? customerId = null,
            decimal? minAmount = null);

        bool ReturnToDraft(int invoiceId);

        void Add(Invoice invoice);
        void Update(Invoice invoice);
        void Delete(int id);
        void SaveChanges();
    }
}