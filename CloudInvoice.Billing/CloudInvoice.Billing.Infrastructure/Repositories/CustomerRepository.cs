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
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(Guid id)
        {
            // Incluímos as faturas para garantir que consultas relacionais funcionem perfeitamente
            return await _context.Set<Customer>()
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Customer customer)
        {
            await _context.Set<Customer>().AddAsync(customer);
        }

        // Método de atualização exigido pela camada de negócio
        public void Update(Customer customer)
        {
            _context.Set<Customer>().Update(customer);
        }

        // Otimização para devolver um booleano indicando se a gravação teve sucesso
        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync()) > 0;
        }



        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Set<Customer>().ToListAsync();
        }
    }
}
