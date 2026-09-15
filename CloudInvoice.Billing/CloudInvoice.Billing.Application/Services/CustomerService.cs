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
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;

        public CustomerService(ICustomerRepository customerRepository, IMapper mapper)
        {
            _customerRepository = customerRepository;
            _mapper = mapper;
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto request)
        {
            // Mapeamento alinhado com o novo DTO de onboarding rápido
            var customer = _mapper.Map<Customer>(request);
            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;

            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();

            return _mapper.Map<CustomerResponseDto>(customer);
        }

        public async Task<CustomerResponseDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null) return null;

            return _mapper.Map<CustomerResponseDto>(customer);
        }

        public async Task<IEnumerable<InvoiceSummaryDto>> GetCustomerInvoicesAsync(Guid customerId, int count)
        {
            // Validamos se o cliente existe primeiro
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
            {
                return Enumerable.Empty<InvoiceSummaryDto>();
            }

            // Filtramos as faturas do cliente, ordenamos pelas mais recentes e limitamos a quantidade (ex: Take(5))
            // Nota: Certifica-te de que a entidade Invoice tem propriedades como IssueDate, TotalAmount, etc.
            var invoices = customer.Invoices
                .OrderByDescending(i => i.IssueDate)
                .Take(count)
                .Select(i => _mapper.Map<InvoiceSummaryDto>(i))
                .ToList();

            return invoices;
        }


        public async Task<bool> UpdateCustomerAsync(Guid id, UpdateCustomerDto request)
        {
            // Procuramos o cliente existente pelo ID através do repositório
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return false; // Cliente não encontrado
            }

            _mapper.Map(request, customer);

            // Atualizamos no repositório e guardamos as alterações
            _customerRepository.Update(customer); // (Certifica-te que o teu repositório tem o método Update ou usa a tracking do EF Core)
            await _customerRepository.SaveChangesAsync();

            return true;
        }



        public async Task<bool> DeleteCustomerAsync(Guid id)
        {
            // Procuramos o cliente existente pelo ID através do repositório
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return false; // Cliente não encontrado
            }
            // Aqui, em vez de remover fisicamente, podemos apenas marcar como inativo
            customer.IsActive = false;
            _customerRepository.Update(customer); // (Certifica-te que o teu repositório tem o método Update ou usa a tracking do EF Core)
            await _customerRepository.SaveChangesAsync();
            return true;
        }





        public async Task<PagedResultDto<CustomerResponseDto>> GetPagedCustomersAsync(CustomerQueryParameters parameters)
        {
            // 1. Obter a query base (Idealmente o repositório deve devolver IQueryable para filtrar na DB)
            var query = await _customerRepository.GetAllAsync();
            var queryable = query.AsQueryable();

            // 2. Aplicar Filtro de Pesquisa (Nome, NIF ou Email)
            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var searchLower = parameters.Search.ToLower();
                queryable = queryable.Where(c =>
                    c.Name.ToLower().Contains(searchLower) ||
                    c.TaxId.Contains(searchLower) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchLower)));
            }

            // 3. Aplicar Filtro de Estado
            if (parameters.IsActive.HasValue)
            {
                queryable = queryable.Where(c => c.IsActive == parameters.IsActive.Value);
            }

            // 4. Calcular Totais
            var totalCount = queryable.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

            // 5. Aplicar Paginação (Skip e Take)
            var pagedCustomers = queryable
                .OrderByDescending(c => c.CreatedAt) // Ordenar pelos mais recentes
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            // 6. Mapear para DTOs e devolver o objeto paginado
            var items = _mapper.Map<List<CustomerResponseDto>>(pagedCustomers);

            return new PagedResultDto<CustomerResponseDto>
            {
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = parameters.Page,
                PageSize = parameters.PageSize,
                Items = items
            };
        }


        public async Task<IEnumerable<CustomerResponseDto>> GetAllActiveCustomersAsync()
        {
            // 1. Vai buscar todos os clientes ao repositório
            var customers = await _customerRepository.GetAllAsync();

            // 2. Filtra apenas os ativos e mapeia para o DTO de resposta
            return _mapper.Map<IEnumerable<CustomerResponseDto>>(
                customers.Where(c => c.IsActive));
        }
    }
}
