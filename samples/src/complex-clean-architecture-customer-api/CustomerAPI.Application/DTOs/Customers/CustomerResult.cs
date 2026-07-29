using AutoMapper;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Customers;

public class CustomerResult : IMapFrom
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public bool Active { get; set; }

    public virtual void Mapping(Profile profile)
    {
        profile.CreateMap<Customer, CustomerResult>();
    }
}
