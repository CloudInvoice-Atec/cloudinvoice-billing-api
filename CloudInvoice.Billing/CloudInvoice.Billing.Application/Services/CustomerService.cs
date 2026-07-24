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
                TaxNumber = request.TaxNumber,
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
        private static CustomerResponseDto MapToDto(Customer customer)
        {
            return new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                TradeName = customer.TradeName,
                TaxNumber = customer.TaxNumber,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                City = customer.City,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                DefaultDiscount = customer.DefaultDiscount,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt
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

            // Atualizamos as propriedades da entidade com os dados vindos do DTO
            customer.Name = request.Name;
            customer.TradeName = request.TradeName;
            customer.TaxNumber = request.TaxNumber;
            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.Address = request.Address;
            customer.City = request.City;
            customer.PostalCode = request.PostalCode;
            customer.Country = request.Country;
            customer.DefaultDiscount = request.DefaultDiscount;
            customer.IsActive = request.IsActive;

            // Atualizamos no repositório e guardamos as alterações
            _customerRepository.Update(customer); // (Certifica-te que o teu repositório tem o método Update ou usa a tracking do EF Core)
            await _customerRepository.SaveChangesAsync();

            return true;
        }
    }
}
