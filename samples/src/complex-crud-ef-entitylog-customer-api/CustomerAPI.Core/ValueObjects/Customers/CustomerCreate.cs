using AutoMapper;
using CustomerAPI.Core.Entities;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Extensions;

namespace CustomerAPI.Core.ValueObjects.Customers
{
    public class CustomerCreate : IMapFrom
    {
        public required string Name { get; set; }
        public string? Note { get; set; }

        public virtual void Mapping(Profile profile)
        {
            // Audit Created/CreatedBy are set by AuditSaveChangesInterceptor + TimeProvider.
            profile.CreateMap<CustomerCreate, Customer>()
                .MapProperty(x => true, x => x.Active);
        }
    }
}
