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

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
            {
                return Enumerable.Empty<InvoiceSummaryDto>();
            }


            var invoices = customer.Invoices
                .OrderByDescending(i => i.IssueDate)
                .Take(count)
                .Select(i => _mapper.Map<InvoiceSummaryDto>(i))
                .ToList();

            return invoices;
        }


        public async Task<bool> UpdateCustomerAsync(Guid id, UpdateCustomerDto request)
        {

            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return false; 
            }

            _mapper.Map(request, customer);


            _customerRepository.Update(customer); 
            await _customerRepository.SaveChangesAsync();

            return true;
        }



        public async Task<bool> DeleteCustomerAsync(Guid id)
        {

            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return false; 
            }

            customer.IsActive = false;
            _customerRepository.Update(customer); 
            await _customerRepository.SaveChangesAsync();
            return true;
        }





        public async Task<PagedResultDto<CustomerResponseDto>> GetPagedCustomersAsync(CustomerQueryParameters parameters)
        {

            var query = await _customerRepository.GetAllAsync();
            var queryable = query.AsQueryable();

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var searchLower = parameters.Search.ToLower();
                queryable = queryable.Where(c =>
                    c.Name.ToLower().Contains(searchLower) ||
                    c.TaxId.Contains(searchLower) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchLower)));
            }


            if (parameters.IsActive.HasValue)
            {
                queryable = queryable.Where(c => c.IsActive == parameters.IsActive.Value);
            }


            var totalCount = queryable.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);


            var pagedCustomers = queryable
                .OrderByDescending(c => c.CreatedAt) 
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();


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

            var customers = await _customerRepository.GetAllAsync();


            return _mapper.Map<IEnumerable<CustomerResponseDto>>(
                customers.Where(c => c.IsActive));
        }
    }
}
