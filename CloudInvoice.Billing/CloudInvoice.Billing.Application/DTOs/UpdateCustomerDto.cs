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
        public string TaxId { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Limites e condições comerciais editáveis
        public decimal? CreditLimit { get; set; }
        public int? PaymentTermsDays { get; set; }
        public decimal DefaultDiscount { get; set; }

        // Contactos e Morada
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Pessoa de Contacto
        public string? ContactPersonName { get; set; }
        public string? ContactPersonRole { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? ContactPersonPhone { get; set; }
    }
}
