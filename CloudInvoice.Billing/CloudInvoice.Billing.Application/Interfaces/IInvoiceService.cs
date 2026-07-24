using CloudInvoice.Billing.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Interfaces
{
    public interface IInvoiceService
    {
        // Notice how the Service ONLY talks using DTOs, never Domain Entities!
        Task<InvoiceResponseDto> CreateInvoiceAsync(string userId, CreateInvoiceDto request);
        Task<IEnumerable<InvoiceResponseDto>> GetUserInvoicesAsync(string userId);
        Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId);
    }
}
