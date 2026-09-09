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
        void Update(Company company);
        Task<bool> SaveChangesAsync();

    }
}
