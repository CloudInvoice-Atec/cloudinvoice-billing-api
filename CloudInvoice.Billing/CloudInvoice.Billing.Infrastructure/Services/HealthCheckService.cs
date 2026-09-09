using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Services
{
    public class HealthCheckService(ApplicationDbContext context) : IHealthCheckService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<bool> CanConnectAsync()
        {
            // Usa o mecanismo nativo do EF Core para testar a ligação à BD
            return await _context.Database.CanConnectAsync();
        }
    }
}
