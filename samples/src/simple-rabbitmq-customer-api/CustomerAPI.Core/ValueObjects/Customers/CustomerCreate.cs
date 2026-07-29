using AutoMapper;
using CustomerAPI.Core.Entities;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Extensions;
using System;

namespace CustomerAPI.Core.ValueObjects.Customers
{
    public class CustomerCreate : IMapFrom
    {
        public required string Name { get; set; }
        public string CellPhone { get; set; }
        public string Email { get; set; }
        public string? Note { get; set; }

        public virtual void Mapping(Profile profile)
        {
            profile.CreateMap<CustomerCreate, Customer>()
                .MapProperty(x => TimeProvider.System.GetUtcNow().UtcDateTime, x => x.Created);
        }
    }
}
