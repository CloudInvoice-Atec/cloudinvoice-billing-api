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

            if (!await context.Set<Company>().AnyAsync())
            {
                context.Set<Company>().Add(new Company
                {
                    Id = 1,
                    Name = "CloudInvoice Lda",
                    TaxNumber = "500100200",
                    PrimaryActivityCode = "62010",

                    Address = "Avenida Central, 45, 2º Andar",
                    PostalCode = "4000-015",
                    City = "Porto",
                    Country = "Portugal",
                    Email = "geral@cloudinvoice.demo",
                    Phone = "+351 220 000 000",
                    Website = "https://www.cloudinvoice.demo",

                    RegistryOffice = "Conservatória do Registo Comercial do Porto",
                    CommercialRegistrationNumber = "500-100-200",
                    ShareCapital = 50000.00m,

                    BankName = "Banco Exemplo, S.A.",
                    Iban = "PT50 0000 0000 0000 0000 000 12",
                    Swift = "EXEMPTPLXXX"
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
