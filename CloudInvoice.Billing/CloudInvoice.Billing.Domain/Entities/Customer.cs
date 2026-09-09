namespace CloudInvoice.Billing.Domain.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TradeName { get; set; }
        public string TaxId { get; set; } = string.Empty; 
        public bool IsActive { get; set; } = true;
        public decimal? CurrentDebt { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? TotalInvoiced { get; set; }
        public int? PaymentTermsDays { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public decimal DefaultDiscount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public string? ContactPersonName { get; set; }
        public string? ContactPersonRole { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? ContactPersonPhone { get; set; }


        public List<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
