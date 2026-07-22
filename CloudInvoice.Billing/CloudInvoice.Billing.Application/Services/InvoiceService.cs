using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Domain.Entities;
using CloudInvoice.Billing.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudInvoice.Billing.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        // Dependency Injection
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ICatalogIntegrationService _catalogIntegrationService;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            ICompanyRepository companyRepository,
            ICatalogIntegrationService catalogIntegrationService)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _companyRepository = companyRepository;
            _catalogIntegrationService = catalogIntegrationService;
        }

        public async Task<InvoiceResponseDto> CreateInvoiceAsync(string userId, CreateInvoiceDto request)
        {
            // 1. Fetch the Customer from our database
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException("Customer not found.");
            }

            // 2. Fetch our Company details (Assuming ID 1 is our default company)
            var company = await _companyRepository.GetDefaultCompanyAsync();
            if (company == null)
            {
                throw new InvalidOperationException("Company settings not configured.");
            }

            // 3. Initialize the Domain Entity (The actual Invoice)
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                UserId = userId,
                IssueDate = DateTime.UtcNow,
                Status = "Issued",

                // Active Relationships
                CustomerId = customer.Id,

                // Data Immutability: Freezing the data at the time of purchase
                CustomerName = customer.Name,
                CustomerTaxNumber = customer.TaxNumber,
                CustomerAddress = customer.Address,

                CompanyName = company.LegalName,
                CompanyTaxNumber = company.TaxNumber,
                CompanyAddress = company.Address
            };

            // 4. Process each item in the request DTO
            foreach (var itemDto in request.Items)
            {
                // Call the external Catalog.API via our integration service
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);

                if (!availability.IsAvailable)
                {
                    // In a real scenario, we might return a specific error or skip the item.
                    // For now, we throw an exception to stop the process.
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available.");
                }

                // Map data to the Domain Entity (InvoiceLine)
                var invoiceLine = new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = itemDto.ProductId,
                    Description = availability.ProductName, // Assuming the DTO returns the name
                    Quantity = itemDto.Quantity,
                    UnitPrice = availability.BasePrice,     // Assuming the DTO returns the price
                    TaxRate = availability.TaxRate          // Assuming the DTO returns the tax rate
                };

                invoice.Lines.Add(invoiceLine);
            }

            // 5. Calculate Totals (Summing up the calculated properties from the Domain Entity)
            invoice.TotalBase = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
            invoice.TotalTax = invoice.Lines.Sum(l => l.TaxAmount);
            invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotal);

            // 6. Save using the Repository
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            // 7. Map the resulting Entity back to a safe DTO to return to the Controller
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                Status = invoice.Status,
                CustomerName = invoice.CustomerName,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                TotalBase = invoice.TotalBase,
                TotalTax = invoice.TotalTax,
                TotalAmount = invoice.TotalAmount
            };
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetUserInvoicesAsync(string userId)
        {
            // 1. Fetch data from Repository (Returns Domain Entities)
            var invoices = await _invoiceRepository.GetByUserIdAsync(userId);

            // 2. Map Entities to DTOs before returning them to the Controller
            // We use LINQ Select to transform the list
            return invoices.Select(invoice => new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                Status = invoice.Status,
                CustomerName = invoice.CustomerName,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                TotalBase = invoice.TotalBase,
                TotalTax = invoice.TotalTax,
                TotalAmount = invoice.TotalAmount
            });
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            // 1. Fetch single invoice from Repository
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

            // 2. Handle not found scenario
            if (invoice == null)
            {
                return null;
            }

            // 3. Map Entity to DTO
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                Status = invoice.Status,
                CustomerName = invoice.CustomerName,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                TotalBase = invoice.TotalBase,
                TotalTax = invoice.TotalTax,
                TotalAmount = invoice.TotalAmount
            };
        }

    }
}
