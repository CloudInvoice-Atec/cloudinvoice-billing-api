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
        public List<CreateInvoiceLineDto> Items { get; set; } = new List<CreateInvoiceLineDto>();
    }

    public class CreateInvoiceLineDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
