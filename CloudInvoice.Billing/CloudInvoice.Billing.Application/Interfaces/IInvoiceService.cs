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
        Task<InvoiceResponseDto> CreateInvoiceAsync(string userId, CreateInvoiceDto request);
        Task<IEnumerable<InvoiceResponseDto>> GetUserInvoicesAsync(string userId);
        Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId);
        Task<PagedResultDto<InvoiceResponseDto>> GetAllInvoicesAsync(int pageNumber, int pageSize);
        Task<InvoiceResponseDto?> UpdateInvoiceAsync(Guid invoiceId, UpdateInvoiceDto request);
        Task<bool> DeleteInvoiceAsync(Guid invoiceId);
    }
}
