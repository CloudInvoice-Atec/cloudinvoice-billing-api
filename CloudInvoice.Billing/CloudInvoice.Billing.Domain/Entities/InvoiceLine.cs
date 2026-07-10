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
        public Guid InvoiceId { get; set; } // Foreign Key
        public Guid ProductId { get; set; } // Reference to Catalog.API Product
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; } // e.g., 23 for 23%

        // Calculated properties
        public decimal TaxAmount => UnitPrice * (TaxRate / 100) * Quantity;
        public decimal LineTotal => (UnitPrice * Quantity) + TaxAmount;

        // Navigation Property
        public Invoice Invoice { get; set; } = null!;
    }
}
