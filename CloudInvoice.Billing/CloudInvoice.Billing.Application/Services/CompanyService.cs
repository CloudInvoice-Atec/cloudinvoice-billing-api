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
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;

        public CompanyService(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public async Task<CompanyResponseDto?> GetCompanyByIdAsync(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return null;

            return MapToDto(company);
        }

        public async Task<bool> UpdateCompanyAsync(int id, UpdateCompanyDto request)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return false;

            company.Name = request.Name;
            company.TaxNumber = request.TaxNumber;
            company.PrimaryActivityCode = request.PrimaryActivityCode;
            company.Address = request.Address;
            company.PostalCode = request.PostalCode;
            company.City = request.City;
            company.Country = request.Country;
            company.Logo = request.Logo;
            company.Email = request.Email;
            company.Phone = request.Phone;
            company.Website = request.Website;
            company.RegistryOffice = request.RegistryOffice;
            company.CommercialRegistrationNumber = request.CommercialRegistrationNumber;
            company.ShareCapital = request.ShareCapital;
            company.BankName = request.BankName;
            company.Iban = request.Iban;
            company.Swift = request.Swift;

            _companyRepository.Update(company);
            await _companyRepository.SaveChangesAsync();

            return true;
        }

        private static CompanyResponseDto MapToDto(Company company)
        {
            return new CompanyResponseDto
            {
                Id = company.Id,
                Name = company.Name,
                TaxNumber = company.TaxNumber,
                PrimaryActivityCode = company.PrimaryActivityCode,
                Address = company.Address,
                PostalCode = company.PostalCode,
                City = company.City,
                Country = company.Country,
                Logo = company.Logo,
                Email = company.Email,
                Phone = company.Phone,
                Website = company.Website,
                RegistryOffice = company.RegistryOffice,
                CommercialRegistrationNumber = company.CommercialRegistrationNumber,
                ShareCapital = company.ShareCapital,
                BankName = company.BankName,
                Iban = company.Iban,
                Swift = company.Swift
            };
        }
    }
}
