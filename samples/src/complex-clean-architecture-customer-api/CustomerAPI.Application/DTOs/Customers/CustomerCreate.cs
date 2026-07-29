using AutoMapper;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Customers;

public class CustomerCreate : IMapFrom
{
    public required string Name { get; set; }
    public string? Note { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<CustomerCreate, Customer>();
    }
}
