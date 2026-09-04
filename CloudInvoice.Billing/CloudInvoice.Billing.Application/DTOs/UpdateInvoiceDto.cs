using CloudInvoice.Billing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{
    public class UpdateInvoiceDto
    {
        public string? Reference { get; set; }
        public DateTime DueDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? Notes { get; set; }

        public List<UpdateInvoiceLineDto> Items { get; set; } = new List<UpdateInvoiceLineDto>();
    }

    public class UpdateInvoiceLineDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal BasePrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TaxRate { get; set; }
    }
}
