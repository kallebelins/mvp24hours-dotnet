using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Enums;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CustomerAPI.Domain.Specifications.Customers
{
    public class CustomerHasEmailContactSpec : ISpecificationQuery<Customer>
    {
        public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.Email) && x.Active;
    }
}
