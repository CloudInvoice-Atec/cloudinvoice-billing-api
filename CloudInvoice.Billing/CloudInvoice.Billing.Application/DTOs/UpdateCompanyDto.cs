using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.DTOs
{
    public class UpdateCompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string PrimaryActivityCode { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Logo { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? RegistryOffice { get; set; }
        public string? CommercialRegistrationNumber { get; set; }
        public decimal ShareCapital { get; set; }
        public string? BankName { get; set; }
        public string? Iban { get; set; }
        public string? Swift { get; set; }
    }
}
