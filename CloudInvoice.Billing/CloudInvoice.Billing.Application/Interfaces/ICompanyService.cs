using CloudInvoice.Billing.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyDto request);
        Task<CompanyResponseDto?> GetCompanyByIdAsync(int id);
        Task<bool> UpdateCompanyAsync(int id, UpdateCompanyDto request);
    }
}
