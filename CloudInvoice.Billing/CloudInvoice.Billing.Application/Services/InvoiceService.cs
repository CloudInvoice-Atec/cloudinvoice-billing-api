using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Domain.Entities;
using CloudInvoice.Billing.Domain.Interfaces;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            ICustomerRepository customerRepository,
            ICompanyRepository companyRepository,
            ICatalogIntegrationService catalogIntegrationService,
            IMapper mapper)
        {
            _invoiceRepository = invoiceRepository;
            _customerRepository = customerRepository;
            _companyRepository = companyRepository;
            _catalogIntegrationService = catalogIntegrationService;
            _mapper = mapper;
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
            var invoice = _mapper.Map<Invoice>(request);
            invoice.Id = Guid.NewGuid();
            invoice.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
            invoice.UserId = userId;
            invoice.CustomerId = customer.Id;
            invoice.CustomerName = customer.Name;
            invoice.CustomerTaxNumber = customer.TaxId;
            invoice.CustomerAddress = customer.Address ?? string.Empty;
            invoice.CompanyName = company.Name;
            invoice.CompanyTaxNumber = company.TaxNumber;
            invoice.CompanyAddress = company.Address ?? string.Empty;
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
                var invoiceLine = _mapper.Map<InvoiceLine>(itemDto);
                invoiceLine.Id = Guid.NewGuid();
                invoiceLine.InvoiceId = invoice.Id;
                invoiceLine.Description = availability.ProductDescription;
                invoiceLine.UnitPrice = unitPrice;
                invoiceLine.TaxRate = taxRate;

                invoice.Lines.Add(invoiceLine);
            }

            // 5. Calculate Totals
            invoice.TotalBase = sumTotalBase;
            invoice.TotalTax = sumTotalTax;
            invoice.TotalAmount = sumTotalAmount;

            // 6. Save using the Repository
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return _mapper.Map<InvoiceResponseDto>(invoice);
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

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null) throw new ArgumentException("Customer not found.");

            var company = await _companyRepository.GetByIdAsync(1);
            if (company == null) throw new InvalidOperationException("Company settings not configured.");


            if (invoice.Lines.Any())
            {
                invoice.Lines.Clear();
                await _invoiceRepository.SaveChangesAsync();
            }

            _mapper.Map(request, invoice);

            invoice.CustomerId = customer.Id;
            invoice.CustomerName = customer.Name;
            invoice.CustomerTaxNumber = customer.TaxId;
            invoice.CustomerAddress = customer.Address ?? string.Empty;

            invoice.CompanyName = company.Name;
            invoice.CompanyTaxNumber = company.TaxNumber;
            invoice.CompanyAddress = company.Address ?? string.Empty;


            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                if (!availability.IsAvailable)
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available.");

                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                var invoiceLine = _mapper.Map<InvoiceLine>(itemDto);
                invoiceLine.Id = Guid.NewGuid();
                invoiceLine.InvoiceId = invoice.Id;
                invoiceLine.Description = availability.ProductDescription;
                invoiceLine.UnitPrice = unitPrice;
                invoiceLine.TaxRate = taxRate;

                // Matemática
                decimal bruto = invoiceLine.Quantity * invoiceLine.UnitPrice;
                decimal baseLiquida = bruto * (1 - (invoiceLine.DiscountPercentage / 100m));
                decimal valorIva = baseLiquida * (invoiceLine.TaxRate / 100m);

                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);

          
                invoice.Lines.Add(invoiceLine);

                await _invoiceRepository.AddLinesAsync(invoiceLine);

            }

            invoice.TotalBase = sumTotalBase;
            invoice.TotalTax = sumTotalTax;
            invoice.TotalAmount = sumTotalAmount;


            await _invoiceRepository.SaveChangesAsync();

            return _mapper.Map<InvoiceResponseDto>(invoice);
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
            return _mapper.Map<IEnumerable<InvoiceResponseDto>>(invoices);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null) return null;

            return _mapper.Map<InvoiceResponseDto>(invoice);
        }

        public async Task<PagedResultDto<InvoiceResponseDto>> GetAllInvoicesAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (invoices, totalCount) = await _invoiceRepository.GetPagedAsync(pageNumber, pageSize);

            var invoiceDtos = _mapper.Map<List<InvoiceResponseDto>>(invoices);

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



        public async Task<bool> CancelInvoiceAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            // 3. Remove a validação do dono da fatura. Apenas verifica se existe.
            if (invoice == null)
            {
                return false;
            }

            if (invoice.Status != InvoiceStatus.Issued)
            {
                return false;
            }

            invoice.Status = InvoiceStatus.Canceled;

            await _invoiceRepository.UpdateAsync(invoice);

            return true;
        }

        public async Task<bool> MarkAsPaidAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            // 1. Verifica se existe (removida a validação de propriedade)
            if (invoice == null)
            {
                return false;
            }

            // 2. Apenas faturas com estado "Issued" podem receber pagamentos
            if (invoice.Status != InvoiceStatus.Issued)
            {
                return false;
            }

            // 3. Apenas faturas que não estejam totalmente pagas devem ser atualizadas
            if (invoice.PaymentStatus == PaymentStatus.Paid)
            {
                return false;
            }

            // 4. Atualiza o estado do pagamento
            invoice.PaymentStatus = PaymentStatus.Paid;

            // 5. Guarda na base de dados
            await _invoiceRepository.UpdateAsync(invoice);

            return true;
        }
    }
}
