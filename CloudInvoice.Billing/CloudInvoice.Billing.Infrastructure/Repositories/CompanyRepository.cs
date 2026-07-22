using CloudInvoice.Billing.Domain.Entities;
using CloudInvoice.Billing.Domain.Interfaces;
using CloudInvoice.Billing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Repositories
{
    // The class implements the interface, fulfilling the contract
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection of the EF Core DbContext
        public CompanyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            // Vai à base de dados procurar uma empresa pelo seu ID único
            return await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Company company)
        {
            // Prepara a entidade para ser inserida na base de dados
            await _context.Companies.AddAsync(company);
        }

        public async Task SaveChangesAsync()
        {
            // Executa efetivamente as alterações (Insert, Update, Delete) no SQL Server
            await _context.SaveChangesAsync();
        }

        public async Task<Company?> GetDefaultCompanyAsync()
        {
            // Assuming ID 1 is always our default issuer company
            return await _context.Companies.FirstOrDefaultAsync(c => c.Id == 1);
        }

        public async Task UpdateAsync(Company company)
        {
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();
        }
    }
}
