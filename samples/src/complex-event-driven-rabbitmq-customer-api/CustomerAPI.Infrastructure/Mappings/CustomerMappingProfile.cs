using AutoMapper;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Domain.Entities;

namespace CustomerAPI.Infrastructure.Mappings;

public sealed class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile()
    {
        CreateMap<CustomerCreate, Customer>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
            .ForMember(d => d.Active, o => o.Ignore())
            .ForMember(d => d.Created, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore());

        CreateMap<Customer, CustomerResult>();
    }
}
