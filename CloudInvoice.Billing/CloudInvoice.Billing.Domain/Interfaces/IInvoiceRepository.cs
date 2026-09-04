using CloudInvoice.Billing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Domain.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<IEnumerable<Invoice>> GetByUserIdAsync(string userId);
        Task<(IEnumerable<Invoice> Invoices, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(Invoice invoice);
        Task SaveChangesAsync();
        Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count);
        Task<IEnumerable<Invoice>> GetInvoicesFromDateAsync(DateTime startDate);
    }
}
