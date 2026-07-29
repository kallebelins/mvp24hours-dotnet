using AutoMapper;
using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Customers;

public class CustomerIdResult : CustomerResult, IMapFrom
{
    public string Note { get; set; }

    public ICollection<ContactIdResult> Contacts { get; set; }

    public override void Mapping(Profile profile)
    {
        profile.CreateMap<Customer, CustomerIdResult>();
    }
}
