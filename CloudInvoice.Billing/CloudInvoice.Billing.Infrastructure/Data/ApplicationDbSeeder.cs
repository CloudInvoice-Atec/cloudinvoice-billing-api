using CloudInvoice.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Infrastructure.Data
{
    public static class ApplicationDbSeeder
    {
        // ==============================================================================
        // DADOS AUXILIARES PARA SEEDING
        // ==============================================================================

        // Clientes
        private static readonly string[] CompanyPrefixes = { "Comércio", "Indústria", "Serviços", "Soluções", "Consultoria", "Distribuição", "Tecnologias", "Construções", "Transportes", "Design", "Engenharia", "Logística", "Importação", "Exportação", "Manutenção" };
        private static readonly string[] CompanyWords = { "Atlântico", "Central", "Nacional", "Ibérico", "Norte", "Sul", "Moderno", "Global", "Premium", "Rápido", "Digital", "Verde", "Urbano", "Costa", "Litoral" };
        private static readonly string[] LegalForms = { "Lda", "SA", "Unipessoal Lda" };
        private static readonly string[] Cities = { "Porto", "Lisboa", "Braga", "Coimbra", "Aveiro", "Faro", "Setúbal", "Leiria", "Viseu", "Guimarães" };

        // Produtos (Artigos reais do seed da Catalog.API para gerar faturas)
        private static readonly (Guid ProductId, string Description, decimal UnitPrice, decimal TaxRate)[] SeedProducts =
        {
            (new Guid("a0000001-0000-0000-0000-000000000001"), "Teclado Mecânico RGB", 45.90m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000002"), "Rato sem Fios", 15.50m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000003"), "Monitor 24 Polegadas", 129.99m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000004"), "Café em Grão 1kg", 8.50m, 6m),
            (new Guid("a0000001-0000-0000-0000-000000000005"), "Azeite Extra Virgem 1L", 6.20m, 6m),
            (new Guid("a0000001-0000-0000-0000-000000000006"), "T-shirt Algodão", 12.00m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000008"), "Consultoria Informática", 45.00m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000009"), "Instalação de Rede", 150.00m, 23m),
            (new Guid("a0000001-0000-0000-0000-000000000010"), "Fita Adesiva", 25.00m, 23m),
        };

        // ==============================================================================
        // MÉTODO PRINCIPAL DE ORQUESTRAÇÃO
        // ==============================================================================

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();

            // 1. Aplica migrações pendentes (se necessário, descomentar)
            // await context.Database.MigrateAsync();

            // 2. Executar os seeders por ordem estrita de dependências
            await SeedCompanyAsync(context);
            await SeedCustomersAsync(context, 60);
            await SeedInvoicesAsync(context, 100);
        }

        // ==============================================================================
        // MÉTODOS PRIVADOS DE SEEDING
        // ==============================================================================

        private static async Task SeedCompanyAsync(ApplicationDbContext context)
        {
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

        private static async Task SeedCustomersAsync(ApplicationDbContext context, int totalCustomers)
        {
            var existingCount = await context.Set<Customer>().CountAsync();

            if (existingCount >= totalCustomers)
            {
                return;
            }

            var random = new Random(99);
            var customers = new List<Customer>();
            var usedTaxIds = new HashSet<string>();

            for (var i = 0; i < totalCustomers - existingCount; i++)
            {
                var prefix = CompanyPrefixes[random.Next(CompanyPrefixes.Length)];
                var word = CompanyWords[random.Next(CompanyWords.Length)];
                var legalForm = LegalForms[random.Next(LegalForms.Length)];
                var city = Cities[random.Next(Cities.Length)];

                var name = $"{prefix} {word} {legalForm}";
                string taxId;

                do
                {
                    taxId = random.Next(500000000, 599999999).ToString();
                } while (!usedTaxIds.Add(taxId));

                customers.Add(new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    TradeName = word,
                    TaxId = taxId,
                    Email = $"geral@{word.ToLowerInvariant()}{i}.pt",
                    Phone = $"+3519{random.Next(10000000, 99999999)}",
                    Address = $"Rua {word}, {random.Next(1, 300)}",
                    City = city,
                    PostalCode = $"{random.Next(1000, 4999)}-{random.Next(100, 999)}",
                    Country = "Portugal",
                    IsActive = random.Next(0, 10) != 0,
                    DefaultDiscount = random.Next(0, 3) * 5.0m,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.Set<Customer>().AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        private static async Task SeedInvoicesAsync(ApplicationDbContext context, int totalInvoices)
        {
            var existingCount = await context.Set<Invoice>().CountAsync();

            if (existingCount >= totalInvoices)
            {
                return;
            }

            var customers = await context.Set<Customer>().ToListAsync();
            var company = await context.Set<Company>().FirstOrDefaultAsync(c => c.Id == 1);

            // Precisa de clientes e empresa já semeados antes de conseguir criar faturas
            if (!customers.Any() || company is null)
            {
                return;
            }

            var random = new Random(42); // seed fixa - resultados sempre iguais entre arranques
            var statuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Issued, InvoiceStatus.Canceled };
            var paymentStatuses = new[] { PaymentStatus.Unpaid, PaymentStatus.PartiallyPaid, PaymentStatus.Paid };

            var invoices = new List<Invoice>();

            for (var i = existingCount + 1; i <= totalInvoices; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var status = statuses[random.Next(statuses.Length)];

                // Faturas canceladas ou em rascunho não fazem sentido como "pagas"
                var paymentStatus = status == InvoiceStatus.Issued
                    ? paymentStatuses[random.Next(paymentStatuses.Length)]
                    : PaymentStatus.Unpaid;

                var issueDate = DateTime.UtcNow.AddDays(-random.Next(1, 180));
                var dueDate = issueDate.AddDays(30);

                var lineCount = random.Next(1, 4); // 1 a 3 linhas por fatura
                var chosenProducts = SeedProducts.OrderBy(_ => random.Next()).Take(lineCount).ToList();

                var lines = new List<InvoiceLine>();
                decimal totalBase = 0;
                decimal totalTax = 0;

                foreach (var product in chosenProducts)
                {
                    var quantity = random.Next(1, 5);
                    var lineBase = quantity * product.UnitPrice;
                    var lineTax = lineBase * (product.TaxRate / 100);

                    lines.Add(new InvoiceLine
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.ProductId,
                        Description = product.Description,
                        Quantity = quantity,
                        UnitPrice = product.UnitPrice,
                        TaxRate = product.TaxRate,
                        DiscountPercentage = 0
                    });

                    totalBase += lineBase;
                    totalTax += lineTax;
                }

                invoices.Add(new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"INV-{issueDate:yyyyMMdd}-{i:D4}",
                    Reference = $"SEED-{i:D3}",
                    UserId = "seed-system",
                    IssueDate = issueDate,
                    DueDate = dueDate,
                    Status = status,
                    PaymentStatus = paymentStatus,
                    Notes = "Fatura gerada pelo InvoiceSeeder para testes.",
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CustomerTaxNumber = customer.TaxId,
                    CustomerAddress = customer.Address ?? "Morada não definida",
                    CompanyName = company.Name,
                    CompanyTaxNumber = company.TaxNumber,
                    CompanyAddress = company.Address ?? "Morada não definida",
                    TotalBase = totalBase,
                    TotalTax = totalTax,
                    TotalAmount = totalBase + totalTax,
                    Lines = lines
                });
            }

            await context.Set<Invoice>().AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }
    }
}