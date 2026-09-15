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

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null) throw new ArgumentException("Customer not found.");

            var company = await _companyRepository.GetByIdAsync(1);
            if (company == null) throw new InvalidOperationException("Company settings not configured.");


            var catalogData = new Dictionary<Guid, AvailabilityResponseDto>();
            var produtosInativos = new List<string>();

            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                catalogData[itemDto.ProductId] = availability;


                if (!availability.IsAvailable)
                {
                    produtosInativos.Add(availability.ProductDescription ?? "Produto Desconhecido");
                }


                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                decimal bruto = itemDto.Quantity * unitPrice;
                decimal baseLiquida = bruto * (1 - (itemDto.DiscountPercentage / 100m));
                decimal valorIva = baseLiquida * (taxRate / 100m);

                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);
            }


            if (produtosInativos.Any())
            {
                throw new InvalidOperationException($"Tem produtos inativos na fatura: {string.Join(", ", produtosInativos)}. Por favor, remova-os para poder guardar ou emitir a fatura.");
            }


            if (customer.CreditLimit > 0)
            {

                decimal dividaAtual = await _customerRepository.GetTotalDebtByCustomerIdAsync(customer.Id);

                if ((dividaAtual + sumTotalAmount) > customer.CreditLimit)
                {
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido! Limite: {customer.CreditLimit:C2} | Em Dívida: {dividaAtual:C2} | Valor desta Fatura: {sumTotalAmount:C2}");
                }
            }


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


            foreach (var itemDto in request.Items)
            {
                var availability = catalogData[itemDto.ProductId];
                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                var invoiceLine = _mapper.Map<InvoiceLine>(itemDto);
                invoiceLine.Id = Guid.NewGuid();
                invoiceLine.InvoiceId = invoice.Id;
                invoiceLine.Description = availability.ProductDescription;
                invoiceLine.UnitPrice = unitPrice;
                invoiceLine.TaxRate = taxRate;

                invoice.Lines.Add(invoiceLine);
            }

            invoice.TotalBase = sumTotalBase;
            invoice.TotalTax = sumTotalTax;
            invoice.TotalAmount = sumTotalAmount;


            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return _mapper.Map<InvoiceResponseDto>(invoice);
        }


        public async Task<InvoiceResponseDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto request)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id); 
            if (invoice == null) return null;
            if (invoice.Status != InvoiceStatus.Draft) throw new InvalidOperationException("Apenas rascunhos podem ser editados.");

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null) throw new ArgumentException("Cliente não encontrado.");

            var company = await _companyRepository.GetByIdAsync(1);


            var catalogData = new Dictionary<Guid, AvailabilityResponseDto>();
            var produtosInativos = new List<string>(); 

            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                catalogData[itemDto.ProductId] = availability;


                if (!availability.IsAvailable)
                {
                    produtosInativos.Add(availability.ProductDescription ?? "Desconhecido");
                }


                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                decimal bruto = itemDto.Quantity * unitPrice;
                decimal baseLiquida = bruto * (1 - (itemDto.DiscountPercentage / 100m));
                decimal valorIva = baseLiquida * (taxRate / 100m);

                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);
            }


            if (produtosInativos.Any())
            {
                throw new InvalidOperationException($"Tem produtos inativos na fatura: {string.Join(", ", produtosInativos)}. Por favor, remova-os para poder guardar ou emitir a fatura.");
            }


            if (customer.CreditLimit > 0)
            {
                decimal dividaAtual = await _customerRepository.GetTotalDebtByCustomerIdAsync(customer.Id);


                if ((dividaAtual + sumTotalAmount) > customer.CreditLimit)
                {
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido! Limite: {customer.CreditLimit:C2} | Em Dívida: {dividaAtual:C2} | Valor desta Fatura: {sumTotalAmount:C2}");
                }
            }


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


            foreach (var itemDto in request.Items)
            {
                var availability = catalogData[itemDto.ProductId];
                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;

                var invoiceLine = _mapper.Map<InvoiceLine>(itemDto);
                invoiceLine.Id = Guid.NewGuid();
                invoiceLine.InvoiceId = invoice.Id;
                invoiceLine.Description = availability.ProductDescription;
                invoiceLine.UnitPrice = unitPrice;
                invoiceLine.TaxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                invoice.Lines.Add(invoiceLine);
            }

            invoice.TotalBase = sumTotalBase;
            invoice.TotalTax = sumTotalTax;
            invoice.TotalAmount = sumTotalAmount;


            await _invoiceRepository.UpdateAsync(invoice); 
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


            if (invoice == null)
            {
                return false;
            }


            if (invoice.Status != InvoiceStatus.Issued)
            {
                return false;
            }


            if (invoice.PaymentStatus == PaymentStatus.Paid)
            {
                return false;
            }


            invoice.PaymentStatus = PaymentStatus.Paid;


            await _invoiceRepository.UpdateAsync(invoice);

            return true;
        }
    }
}
