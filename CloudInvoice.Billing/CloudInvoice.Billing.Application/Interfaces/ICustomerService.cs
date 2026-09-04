using CloudInvoice.Billing.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto request);
        Task<CustomerResponseDto?> GetCustomerByIdAsync(Guid id);

        Task<IEnumerable<InvoiceSummaryDto>> GetCustomerInvoicesAsync(Guid customerId, int count);

        Task<bool> UpdateCustomerAsync(Guid id, UpdateCustomerDto request);

        Task<PagedResultDto<CustomerResponseDto>> GetPagedCustomersAsync(CustomerQueryParameters parameters);

        Task<IEnumerable<CustomerResponseDto>> GetAllActiveCustomersAsync();
    }
}
