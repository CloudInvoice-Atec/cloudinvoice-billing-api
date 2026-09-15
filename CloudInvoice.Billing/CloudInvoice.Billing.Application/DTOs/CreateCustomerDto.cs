using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{

    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string? ContactPersonName { get; set; }
        public string? ContactPersonEmail { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
