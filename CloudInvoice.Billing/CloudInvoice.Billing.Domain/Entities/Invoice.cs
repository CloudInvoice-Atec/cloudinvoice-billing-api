using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // From JWT (sub)
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending or Issued

        // Active Relationship (Foreign Key to current Customer)
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Buyer Data Frozen at the time of purchase (Immutability)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTaxNumber { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;

        // Issuer Data Frozen at the time of purchase (Immutability)
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyTaxNumber { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;

        // Totals
        public decimal TotalBase { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }

        // Relationship 1:N - An invoice has many lines
        public List<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    }
}
