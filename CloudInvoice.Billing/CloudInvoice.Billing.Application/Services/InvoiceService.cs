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
            // 1. Fetch the Customer and Company
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null) throw new ArgumentException("Customer not found.");

            var company = await _companyRepository.GetByIdAsync(1);
            if (company == null) throw new InvalidOperationException("Company settings not configured.");

            // ==========================================================
            // PASSO A: VALIDAR PRODUTOS EM BULK E CALCULAR TOTAIS
            // ==========================================================
            // NOTA: Substitui "CatalogItemAvailability" pelo teu DTO real que descobriste há pouco!
            var catalogData = new Dictionary<Guid, AvailabilityResponseDto>();
            var produtosInativos = new List<string>();

            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                catalogData[itemDto.ProductId] = availability;

                // Regista o produto na lista negra em vez de explodir logo a app
                if (!availability.IsAvailable)
                {
                    produtosInativos.Add(availability.ProductDescription ?? "Produto Desconhecido");
                }

                // Matemática usando as regras corretas de precedência
                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                decimal bruto = itemDto.Quantity * unitPrice;
                decimal baseLiquida = bruto * (1 - (itemDto.DiscountPercentage / 100m));
                decimal valorIva = baseLiquida * (taxRate / 100m);

                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);
            }

            // REGRA 1: Produtos inativos em Bulk
            // Impede a gravação e devolve uma mensagem agregada para o ecrã Blazor
            if (produtosInativos.Any())
            {
                throw new InvalidOperationException($"Tem produtos inativos na fatura: {string.Join(", ", produtosInativos)}. Por favor, remova-os para poder guardar ou emitir a fatura.");
            }

            // ==========================================================
            // PASSO B: VALIDAR LIMITE DE CRÉDITO
            // ==========================================================
            if (customer.CreditLimit > 0)
            {
                // Tem de ter aquele método novo no repositório para somar as faturas não pagas
                decimal dividaAtual = await _customerRepository.GetTotalDebtByCustomerIdAsync(customer.Id);

                if ((dividaAtual + sumTotalAmount) > customer.CreditLimit)
                {
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido! Limite: {customer.CreditLimit:C2} | Em Dívida: {dividaAtual:C2} | Valor desta Fatura: {sumTotalAmount:C2}");
                }
            }

            // ==========================================================
            // PASSO C: CRIAR A ENTIDADE FATURA (DOMÍNIO)
            // ==========================================================
            var invoice = _mapper.Map<Invoice>(request);
            invoice.Id = Guid.NewGuid();
            invoice.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
            invoice.UserId = userId;

            // Imutabilidade: snapshots são preenchidos a partir dos dados atuais.
            invoice.CustomerId = customer.Id;
            invoice.CustomerName = customer.Name;
            invoice.CustomerTaxNumber = customer.TaxId;
            invoice.CustomerAddress = customer.Address ?? string.Empty;
            invoice.CompanyName = company.Name;
            invoice.CompanyTaxNumber = company.TaxNumber;
            invoice.CompanyAddress = company.Address ?? string.Empty;

            // ==========================================================
            // PASSO D: CRIAR AS LINHAS
            // ==========================================================
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

            // ==========================================================
            // PASSO E: GRAVAR NA BASE DE DADOS
            // ==========================================================
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return _mapper.Map<InvoiceResponseDto>(invoice);
        }


        public async Task<InvoiceResponseDto?> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto request)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id); // Lembrar do .Include(i => i.Lines)
            if (invoice == null) return null;
            if (invoice.Status != InvoiceStatus.Draft) throw new InvalidOperationException("Apenas rascunhos podem ser editados.");

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null) throw new ArgumentException("Cliente não encontrado.");

            var company = await _companyRepository.GetByIdAsync(1);

            // ==========================================================
            // PASSO A: VALIDAR PRODUTOS EM BULK E CALCULAR TOTAIS
            // ==========================================================
            var catalogData = new Dictionary<Guid, AvailabilityResponseDto>();
            var produtosInativos = new List<string>(); // Acumulador de erros

            decimal sumTotalBase = 0;
            decimal sumTotalTax = 0;
            decimal sumTotalAmount = 0;

            foreach (var itemDto in request.Items)
            {
                var availability = await _catalogIntegrationService.CheckAvailabilityAsync(itemDto.ProductId);
                catalogData[itemDto.ProductId] = availability;

                // Se estiver inativo, guardamos o nome para avisar o utilizador
                if (!availability.IsAvailable)
                {
                    produtosInativos.Add(availability.ProductDescription ?? "Desconhecido");
                }

                // Calculamos os totais em memória para usar na validação de crédito
                decimal unitPrice = itemDto.BasePrice > 0 ? itemDto.BasePrice : availability.BasePrice;
                decimal taxRate = itemDto.TaxRate > 0 ? itemDto.TaxRate : availability.TaxRate;

                decimal bruto = itemDto.Quantity * unitPrice;
                decimal baseLiquida = bruto * (1 - (itemDto.DiscountPercentage / 100m));
                decimal valorIva = baseLiquida * (taxRate / 100m);

                sumTotalBase += baseLiquida;
                sumTotalTax += valorIva;
                sumTotalAmount += (baseLiquida + valorIva);
            }

            // REGRA 1: Bloquear imediatamente se houver produtos inativos
            if (produtosInativos.Any())
            {
                throw new InvalidOperationException($"Tem produtos inativos na fatura: {string.Join(", ", produtosInativos)}. Por favor, remova-os para poder guardar ou emitir a fatura.");
            }

            // ==========================================================
            // PASSO B: VALIDAR LIMITE DE CRÉDITO
            // ==========================================================
            // Assumindo que Customer tem uma propriedade CreditLimit. Se for null ou 0, não tem limite.
            if (customer.CreditLimit > 0)
            {
                decimal dividaAtual = await _customerRepository.GetTotalDebtByCustomerIdAsync(customer.Id);

                // Se a dívida atual + o total DESTA fatura ultrapassar o limite
                if ((dividaAtual + sumTotalAmount) > customer.CreditLimit)
                {
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido! Limite: {customer.CreditLimit:C2} | Em Dívida: {dividaAtual:C2} | Valor desta Fatura: {sumTotalAmount:C2}");
                }
            }

            // ==========================================================
            // PASSO C: APLICAR ALTERAÇÕES NA BASE DE DADOS
            // (Como já validámos tudo, agora é seguro apagar as linhas antigas)
            // ==========================================================
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

            // Gravar o resultado final
            await _invoiceRepository.UpdateAsync(invoice); // Opcional dependendo da implementação do Repositório
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
