using AutoMapper;
using CloudInvoice.Billing.Application.DTOs;
using CloudInvoice.Billing.Application.DTOs.Dashboard;
using CloudInvoice.Billing.Domain.Entities;

namespace CloudInvoice.Billing.Application.Mappings
{
    public class BillingMappingProfile : Profile
    {
        public BillingMappingProfile()
        {
            CreateMap<Company, CompanyResponseDto>();
            CreateMap<UpdateCompanyDto, Company>()
                .ForMember(destination => destination.Id, options => options.Ignore());

            CreateMap<CreateCustomerDto, Customer>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.CreatedAt, options => options.Ignore())
                .ForMember(destination => destination.Invoices, options => options.Ignore());
            CreateMap<UpdateCustomerDto, Customer>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.CurrentDebt, options => options.Ignore())
                .ForMember(destination => destination.TotalInvoiced, options => options.Ignore())
                .ForMember(destination => destination.CreatedAt, options => options.Ignore())
                .ForMember(destination => destination.Invoices, options => options.Ignore());
            CreateMap<Customer, CustomerResponseDto>();

            CreateMap<CreateInvoiceDto, Invoice>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.InvoiceNumber, options => options.Ignore())
                .ForMember(destination => destination.UserId, options => options.Ignore())
                .ForMember(destination => destination.Customer, options => options.Ignore())
                .ForMember(destination => destination.CustomerName, options => options.Ignore())
                .ForMember(destination => destination.CustomerTaxNumber, options => options.Ignore())
                .ForMember(destination => destination.CustomerAddress, options => options.Ignore())
                .ForMember(destination => destination.CompanyName, options => options.Ignore())
                .ForMember(destination => destination.CompanyTaxNumber, options => options.Ignore())
                .ForMember(destination => destination.CompanyAddress, options => options.Ignore())
                .ForMember(destination => destination.TotalBase, options => options.Ignore())
                .ForMember(destination => destination.TotalTax, options => options.Ignore())
                .ForMember(destination => destination.TotalAmount, options => options.Ignore())
                .ForMember(destination => destination.Lines, options => options.Ignore());
            CreateMap<CreateInvoiceLineDto, InvoiceLine>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.InvoiceId, options => options.Ignore())
                .ForMember(destination => destination.Invoice, options => options.Ignore())
                .ForMember(destination => destination.Description, options => options.Ignore())
                .ForMember(destination => destination.UnitPrice, options => options.MapFrom(source => source.BasePrice))
                .ForMember(destination => destination.TaxAmount, options => options.Ignore())
                .ForMember(destination => destination.LineTotal, options => options.Ignore());

            CreateMap<UpdateInvoiceDto, Invoice>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.InvoiceNumber, options => options.Ignore())
                .ForMember(destination => destination.UserId, options => options.Ignore())
                .ForMember(destination => destination.Customer, options => options.Ignore())
                .ForMember(destination => destination.CustomerName, options => options.Ignore())
                .ForMember(destination => destination.CustomerTaxNumber, options => options.Ignore())
                .ForMember(destination => destination.CustomerAddress, options => options.Ignore())
                .ForMember(destination => destination.CompanyName, options => options.Ignore())
                .ForMember(destination => destination.CompanyTaxNumber, options => options.Ignore())
                .ForMember(destination => destination.CompanyAddress, options => options.Ignore())
                .ForMember(destination => destination.TotalBase, options => options.Ignore())
                .ForMember(destination => destination.TotalTax, options => options.Ignore())
                .ForMember(destination => destination.TotalAmount, options => options.Ignore())
                .ForMember(destination => destination.Lines, options => options.Ignore());
            CreateMap<UpdateInvoiceLineDto, InvoiceLine>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.InvoiceId, options => options.Ignore())
                .ForMember(destination => destination.Invoice, options => options.Ignore())
                .ForMember(destination => destination.Description, options => options.Ignore())
                .ForMember(destination => destination.UnitPrice, options => options.MapFrom(source => source.BasePrice))
                .ForMember(destination => destination.TaxAmount, options => options.Ignore())
                .ForMember(destination => destination.LineTotal, options => options.Ignore());

            CreateMap<Invoice, InvoiceResponseDto>();
            CreateMap<InvoiceLine, InvoiceLineResponseDto>();
            CreateMap<Invoice, InvoiceSummaryDto>();
            CreateMap<Invoice, RecentInvoiceDto>()
                .ForMember(destination => destination.CustomerName,
                    options => options.MapFrom(source => source.Customer == null ? "Desconhecido" : source.Customer.Name))
                .ForMember(destination => destination.Status,
                    options => options.MapFrom(source => source.Status.ToString()));
        }
    }
}
