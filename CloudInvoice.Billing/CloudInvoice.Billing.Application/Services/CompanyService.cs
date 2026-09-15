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
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IMapper _mapper;

        public CompanyService(ICompanyRepository companyRepository, IMapper mapper)
        {
            _companyRepository = companyRepository;
            _mapper = mapper;
        }

        public async Task<CompanyResponseDto?> GetCompanyByIdAsync(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return null;

            return _mapper.Map<CompanyResponseDto>(company);
        }

        public async Task<bool> UpdateCompanyAsync(int id, UpdateCompanyDto request)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return false;

            _mapper.Map(request, company);

            _companyRepository.Update(company);
            await _companyRepository.SaveChangesAsync();

            return true;
        }

    }
}
