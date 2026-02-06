namespace SupplyCompanySystem.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public Customer()
        {
            CreatedDate = DateTime.Now;
            IsActive = true;
        }
    }
}