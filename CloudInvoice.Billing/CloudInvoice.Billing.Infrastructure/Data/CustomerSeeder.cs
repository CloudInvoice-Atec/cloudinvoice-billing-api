using CloudInvoice.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Data
{
    public static class CustomerSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Garante que a base de dados está criada
            await context.Database.EnsureCreatedAsync();

            // Se já existir algum cliente, não faz nada para evitar duplicações
            if (await context.Set<Customer>().AnyAsync())
            {
                return;
            }

            // Insere clientes de teste iniciais
            var customers = new[]
            {
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Empresa Exemplo Lda",
                    TradeName = "Exemplo Store",
                    TaxId = "500100200",
                    Email = "contacto@exemplo.pt",
                    Phone = "+351910000000",
                    Address = "Rua Principal, 123",
                    City = "Porto",
                    PostalCode = "4000-001",
                    Country = "Portugal",
                    IsActive = true,
                    DefaultDiscount = 5.0m,
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Comércio de Tecnologias SA",
                    TradeName = "TechShop",
                    TaxId = "500300400",
                    Email = "suporte@techshop.pt",
                    Phone = "+351920000000",
                    Address = "Avenida Central, 456",
                    City = "Lisboa",
                    PostalCode = "1000-001",
                    Country = "Portugal",
                    IsActive = true,
                    DefaultDiscount = 10.0m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Set<Customer>().AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }
    }
}
