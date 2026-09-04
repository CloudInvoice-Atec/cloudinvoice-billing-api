using CloudInvoice.Billing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{
    // Histórico de Faturas, devolver a informação já processada e limpa.
    public class InvoiceResponseDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }

        // Mapeados como Enums para que o .NET os serializa automaticamente como Strings na API
        public InvoiceStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? Notes { get; set; }

        // Emphasizing immutability: we return the frozen data
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTaxNumber { get; set; } = string.Empty;

        // Formatted totals for the frontend
        public decimal TotalBase { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public List<InvoiceLineResponseDto> Lines { get; set; } = new List<InvoiceLineResponseDto>();

    }

    public class InvoiceLineResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal LineTotal { get; set; }
    }
}
