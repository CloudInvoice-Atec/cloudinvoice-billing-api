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
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto request)
        {
            // Mapeamento alinhado com o novo DTO de onboarding rápido
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                TaxId = request.TaxId,
                ContactPersonName = request.ContactPersonName,
                ContactPersonEmail = request.ContactPersonEmail,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
                // Restantes campos comerciais (morada, país, etc.) ficam vazios
                // e serão preenchidos posteriormente através da página de edição (Update).
            };

            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();

            return MapToDto(customer);
        }

        public async Task<CustomerResponseDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null) return null;

            return MapToDto(customer);
        }

        // Método auxiliar privado para evitar duplicação de código no mapeamento
        // Método auxiliar privado para evitar duplicação de código no mapeamento
        private static CustomerResponseDto MapToDto(Customer customer)
        {
            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                TradeName = customer.TradeName,
                TaxId = customer.TaxId,
                IsActive = customer.IsActive,
                CurrentDebt = customer.CurrentDebt,
                CreditLimit = customer.CreditLimit,
                TotalInvoiced = customer.TotalInvoiced,
                PaymentTermsDays = customer.PaymentTermsDays,
                Email = customer.Email,
                Phone = customer.Phone,
                Website = customer.Website,
                Address = customer.Address,
                City = customer.City,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                DefaultDiscount = customer.DefaultDiscount,
                CreatedAt = customer.CreatedAt,
                ContactPersonName = customer.ContactPersonName,
                ContactPersonRole = customer.ContactPersonRole,
                ContactPersonEmail = customer.ContactPersonEmail,
                ContactPersonPhone = customer.ContactPersonPhone
            };
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
                .Select(i => new InvoiceSummaryDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    IssueDate = i.IssueDate,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status
                })
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

            customer.Name = request.Name;
            customer.TradeName = request.TradeName;
            customer.TaxId = request.TaxId; 
            customer.IsActive = request.IsActive;

            customer.CreditLimit = request.CreditLimit;
            customer.PaymentTermsDays = request.PaymentTermsDays;
            customer.DefaultDiscount = request.DefaultDiscount;

            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.Address = request.Address;
            customer.City = request.City;
            customer.PostalCode = request.PostalCode;
            customer.Country = request.Country;

            customer.ContactPersonName = request.ContactPersonName;
            customer.ContactPersonRole = request.ContactPersonRole;
            customer.ContactPersonEmail = request.ContactPersonEmail;
            customer.ContactPersonPhone = request.ContactPersonPhone;

            // Atualizamos no repositório e guardamos as alterações
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
            var items = pagedCustomers.Select(c => MapToDto(c)).ToList();

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
            return customers
                .Where(c => c.IsActive)
                .Select(c => new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    TradeName = c.TradeName,
                    TaxId = c.TaxId,
                    IsActive = c.IsActive,
                    CurrentDebt = c.CurrentDebt,
                    CreditLimit = c.CreditLimit,
                    TotalInvoiced = c.TotalInvoiced,
                    PaymentTermsDays = c.PaymentTermsDays,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    City = c.City,
                    PostalCode = c.PostalCode,
                    Country = c.Country,
                    DefaultDiscount = c.DefaultDiscount,
                    CreatedAt = c.CreatedAt,
                    ContactPersonName = c.ContactPersonName,
                    ContactPersonRole = c.ContactPersonRole,
                    ContactPersonEmail = c.ContactPersonEmail,
                    ContactPersonPhone = c.ContactPersonPhone
                });
        }
    }
}
