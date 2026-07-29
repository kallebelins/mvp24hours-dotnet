using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using Mvp24Hours.Core.Contract.Mappings;

namespace CustomerAPI.Core.ValueObjects.Contacts
{
    public class ContactCreate : IMapFrom
    {
        public ContactType Type { get; set; }
        public required string Description { get; set; }

        public virtual void Mapping(Profile profile)
        {
            profile.CreateMap<ContactCreate, Contact>();
        }
    }
}
