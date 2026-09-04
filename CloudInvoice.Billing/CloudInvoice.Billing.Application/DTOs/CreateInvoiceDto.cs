using CloudInvoice.Billing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{
    // Quando finaliza compra, o que é que a nossa API precisa de receber é apenas os identificadores e as quantidades.
    // A nossa API é que vai ao Catálogo e à base de dados procurar os preços e os NIFs!
    public class CreateInvoiceDto
    {
        public Guid CustomerId { get; set; }
        public string? Reference { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public string? Notes { get; set; }


        public List<CreateInvoiceLineDto> Items { get; set; } = new List<CreateInvoiceLineDto>();
    }

    public class CreateInvoiceLineDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal BasePrice { get; set; } = 0;
        public decimal DiscountPercentage { get; set; } = 0;
        public decimal TaxRate { get; set; }

    }
}
