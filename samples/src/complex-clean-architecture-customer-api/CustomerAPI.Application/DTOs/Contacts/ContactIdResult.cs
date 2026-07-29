using AutoMapper;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Contacts;

public class ContactIdResult : ContactResult, IMapFrom
{
    public int Id { get; set; }

    public override void Mapping(Profile profile)
    {
        profile.CreateMap<Contact, ContactIdResult>();
    }
}
