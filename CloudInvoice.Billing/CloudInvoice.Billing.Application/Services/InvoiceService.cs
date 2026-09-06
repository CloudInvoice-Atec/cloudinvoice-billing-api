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
                Reference = request.Reference,
                UserId = userId,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,

                // Atribuição direta dos Enums do Domínio (Type Safety)
                Status = request.Status,
                PaymentStatus = request.PaymentStatus,
                Notes = request.Notes,

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

            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                // Call the external Catalog.API via our integration service
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);

                if (!availability.IsAvailable)
                {
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available.");
                }

                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;
                decimal discountPercentage = itemDto.DiscountPercentage;

                // 1. Valor bruto da linha
                decimal bruto = itemDto.Quantity * itemDto.BasePrice;

                // 2. Base Líquida (já com o desconto abatido)
                // Se DiscountPercentage for 50, fica 50/100 = 0.5. Bruto * (1 - 0.5)
                decimal baseLiquida = bruto * (1 - (itemDto.DiscountPercentage / 100m));

                // 3. Valor do IVA sobre a base líquida
                decimal valorIva = baseLiquida * (itemDto.TaxRate / 100m);

                // 4. Acumular para os totais globais da fatura
                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);


                // Map data to the Domain Entity (InvoiceLine)
                var invoiceLine = new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = itemDto.ProductId,
                    Description = availability.ProductDescription,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    TaxRate = taxRate,
                    DiscountPercentage = discountPercentage
                };

                invoice.Lines.Add(invoiceLine);
            }

            // 5. Calculate Totals
            invoice.TotalBase = sumTotalBase;
            invoice.TotalTax = sumTotalTax;
            invoice.TotalAmount = sumTotalAmount;

            // 6. Save using the Repository
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return MapToResponseDto(invoice);
        }


        public async Task<InvoiceResponseDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto request)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
            {
                return null; 
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Cannot update a non-draft invoice.");
            }

            // 2. Atualizar os dados gerais da fatura com base no DTO
            invoice.Reference = request.Reference;
            invoice.DueDate = request.DueDate;
            invoice.Status = request.Status;
            invoice.PaymentStatus = request.PaymentStatus;
            invoice.Notes = request.Notes;

            // 3. Limpar as linhas antigas para substituir pelas novas enviadas no update
            invoice.Lines.Clear();

            // 4. Processar e validar cada nova linha de produto
            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                if (!availability.IsAvailable)
                {
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available.");
                }

                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;


                var invoiceLine = new InvoiceLine
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = itemDto.ProductId,
                    Description = availability.ProductDescription,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    TaxRate = taxRate
                };

                invoice.Lines.Add(invoiceLine);
            }

            // 5. Recalcular os Totais Globais
            invoice.TotalBase = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
            invoice.TotalTax = invoice.Lines.Sum(l => l.TaxAmount);
            invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotal);

            // 6. Persistir as alterações através do repositório
            await _invoiceRepository.UpdateAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            // 7. Devolver o DTO de resposta mapeado
            return MapToResponseDto(invoice);
        }

        public async Task<bool> DeleteInvoiceAsync(Guid invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                return false;
            }
            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Cannot delete a non-draft invoice.");
            }
            await _invoiceRepository.DeleteAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();
            return true; 
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

        public async Task<PagedResultDto<InvoiceResponseDto>> GetAllInvoicesAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (invoices, totalCount) = await _invoiceRepository.GetPagedAsync(pageNumber, pageSize);

            var invoiceDtos = invoices.Select(MapToResponseDto).ToList();

            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResultDto<InvoiceResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = invoiceDtos
            };
        }



        private static InvoiceResponseDto MapToResponseDto(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Reference = invoice.Reference,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                PaymentStatus = invoice.PaymentStatus,
                Notes = invoice.Notes,
                CustomerName = invoice.CustomerName,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                TotalBase = invoice.TotalBase,
                TotalTax = invoice.TotalTax,
                TotalAmount = invoice.TotalAmount,

                Lines = invoice.Lines.Select(line => new InvoiceLineResponseDto
                {
                    Id = line.Id,
                    ProductId = line.ProductId,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountPercentage = line.DiscountPercentage,
                    TaxRate = line.TaxRate,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal
                }).ToList()
            };
        }
    }
}
