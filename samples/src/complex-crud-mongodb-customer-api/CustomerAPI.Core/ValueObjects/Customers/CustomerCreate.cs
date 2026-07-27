using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.ValueObjects.Contacts;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Extensions;
using System.Collections.Generic;

namespace CustomerAPI.Core.ValueObjects.Customers
{
    public class CustomerCreate : IMapFrom
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public ICollection<ContactCreate> Contacts { get; set; }

        public virtual void Mapping(Profile profile)
        {
            // Id and Created are assigned in CustomerService with ObjectId + TimeProvider.
            profile.CreateMap<CustomerCreate, Customer>()
                .MapProperty(x => true, x => x.Active);
        }
    }
}
