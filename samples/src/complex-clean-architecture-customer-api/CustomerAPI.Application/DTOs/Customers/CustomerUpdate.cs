using AutoMapper;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Customers;

public class CustomerUpdate : IMapFrom
{
    public string Name { get; set; }
    public string Note { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<CustomerUpdate, Customer>();
    }
}
