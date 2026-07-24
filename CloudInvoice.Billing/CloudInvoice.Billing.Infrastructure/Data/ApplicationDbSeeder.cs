using CloudInvoice.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Data
{
    public static class ApplicationDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();

            // 1. Aplica migrações pendentes
            await context.Database.MigrateAsync();

            // 2. Insere a empresa padrão caso a tabela esteja vazia
            if (!await context.Set<Company>().AnyAsync())
            {
                context.Set<Company>().Add(new Company
                {
                    Id = 1,
                    Name = "Minha Empresa Padrão, Lda",
                    TaxNumber = "999999999",
                    PrimaryActivityCode = "00000",
                    Address = "Rua Principal, 123",
                    City = "Porto",
                    Country = "Portugal"
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
