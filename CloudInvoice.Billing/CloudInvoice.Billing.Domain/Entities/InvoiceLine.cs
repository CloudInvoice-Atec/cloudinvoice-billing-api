using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Domain.Entities
{
    public class InvoiceLine
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; } 
        public Guid ProductId { get; set; } 
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } 
        public decimal DiscountPercentage { get; set; }


        public decimal TaxAmount => UnitPrice * (TaxRate / 100) * Quantity;
        public decimal LineTotal => (UnitPrice * Quantity) + TaxAmount;


        public Invoice Invoice { get; set; } = null!;
    }
}
