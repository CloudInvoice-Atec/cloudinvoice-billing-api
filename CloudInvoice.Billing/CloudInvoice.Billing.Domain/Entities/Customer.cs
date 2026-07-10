namespace CloudInvoice.Billing.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public List<Invoice> Invoices { get; set; } = new List<Invoice>();

    }
}
