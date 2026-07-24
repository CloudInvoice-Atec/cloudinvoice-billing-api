using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{
    public class UpdateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string? TradeName { get; set; }
        public string TaxNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public decimal DefaultDiscount { get; set; }
        public bool IsActive { get; set; }
    }
}
