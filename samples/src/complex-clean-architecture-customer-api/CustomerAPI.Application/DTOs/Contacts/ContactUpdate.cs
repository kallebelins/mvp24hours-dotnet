using AutoMapper;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Enums;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Application.DTOs.Contacts
{
    public class ContactUpdate : IMapFrom
    {
        public ContactType Type { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }

        public virtual void Mapping(Profile profile)
        {
            profile.CreateMap<ContactUpdate, Contact>();
        }
    }
}
