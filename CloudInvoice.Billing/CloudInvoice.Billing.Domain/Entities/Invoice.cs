using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Domain.Entities
{
    // Esta etiqueta mágica obriga o .NET a transformar o enum em String quando comunica com a API!
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InvoiceStatus
    {
        Draft,
        Issued,
        Canceled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        Unpaid,
        PartiallyPaid,
        Paid
    }
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string UserId { get; set; } = string.Empty; // From JWT (sub)
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public string? Notes { get; set; }

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
