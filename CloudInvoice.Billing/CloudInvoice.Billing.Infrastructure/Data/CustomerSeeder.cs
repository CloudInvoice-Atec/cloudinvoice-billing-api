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
                    IsActive = true,
                    CurrentDebt = 0.0m,
                    CreditLimit = 5000.0m,
                    TotalInvoiced = 1250.0m,
                    PaymentTermsDays = 30,
                    Email = "contacto@exemplo.pt",
                    Phone = "+351910000000",
                    Website = "www.exemplo.pt",
                    Address = "Rua Principal, 123",
                    City = "Porto",
                    PostalCode = "4000-001",
                    Country = "Portugal",
                    DefaultDiscount = 5.0m,
                    CreatedAt = DateTime.UtcNow,
                    ContactPersonName = "Ana Silva",
                    ContactPersonRole = "Diretora Financeira",
                    ContactPersonEmail = "ana.silva@exemplo.pt",
                    ContactPersonPhone = "+351911111111"
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = "Comércio de Tecnologias SA",
                    TradeName = "TechShop",
                    TaxId = "500300400",
                    IsActive = true,
                    CurrentDebt = 450.0m,
                    CreditLimit = 10000.0m,
                    TotalInvoiced = 8500.0m,
                    PaymentTermsDays = 60,
                    Email = "suporte@techshop.pt",
                    Phone = "+351920000000",
                    Website = "www.techshop.pt",
                    Address = "Avenida Central, 456",
                    City = "Lisboa",
                    PostalCode = "1000-001",
                    Country = "Portugal",
                    DefaultDiscount = 10.0m,
                    CreatedAt = DateTime.UtcNow,
                    ContactPersonName = "Carlos Santos",
                    ContactPersonRole = "Gestor de Compras",
                    ContactPersonEmail = "carlos.santos@techshop.pt",
                    ContactPersonPhone = "+351922222222"
                }
            };

            await context.Set<Customer>().AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }
    }
}
