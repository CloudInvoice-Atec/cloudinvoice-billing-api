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

            // 2. Fetch our Company details (Assumindo que a empresa principal tem o ID 1)
            var company = await _companyRepository.GetByIdAsync(1);
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

                // Atribuição direta dos Enums do Domínio (Type Safety)
                Status = InvoiceStatus.Issued,
                PaymentStatus = PaymentStatus.Unpaid,

                // Active Relationships
                CustomerId = customer.Id,

                // Data Immutability: Freezing the data at the time of purchase
                CustomerName = customer.Name,
                CustomerTaxNumber = customer.TaxId,
                CustomerAddress = customer.Address,

                // Ajustado para as propriedades reais da entidade Company (Name em vez de LegalName)
                CompanyName = company.Name,
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
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available.");
                }

                // Map data to the Domain Entity (InvoiceLine)
                var invoiceLine = new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = itemDto.ProductId,
                    Description = availability.ProductName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = availability.BasePrice,
                    TaxRate = availability.TaxRate
                };

                invoice.Lines.Add(invoiceLine);
            }

            // 5. Calculate Totals
            invoice.TotalBase = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
            invoice.TotalTax = invoice.Lines.Sum(l => l.TaxAmount);
            invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotal);

            // 6. Save using the Repository
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return MapToResponseDto(invoice);
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetUserInvoicesAsync(string userId)
        {
            var invoices = await _invoiceRepository.GetByUserIdAsync(userId);
            return invoices.Select(MapToResponseDto);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null) return null;

            return MapToResponseDto(invoice);
        }

        private static InvoiceResponseDto MapToResponseDto(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                Status = invoice.Status,
                PaymentStatus = invoice.PaymentStatus,
                CustomerName = invoice.CustomerName,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                TotalBase = invoice.TotalBase,
                TotalTax = invoice.TotalTax,
                TotalAmount = invoice.TotalAmount
            };
        }
    }
}
