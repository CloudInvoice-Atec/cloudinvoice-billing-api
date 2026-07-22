using CloudInvoice.Billing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Domain.Interfaces
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(int id);
        Task AddAsync(Company company);
        Task SaveChangesAsync();

        Task<Company?> GetDefaultCompanyAsync();

        // Let's also leave the Update method ready for your Admin UI later
        Task UpdateAsync(Company company);
    }
}
