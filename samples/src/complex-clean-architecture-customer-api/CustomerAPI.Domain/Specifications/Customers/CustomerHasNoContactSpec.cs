using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CustomerAPI.Domain.Specifications.Customers
{
    public class CustomerHasNoContactSpec : ISpecificationQuery<Customer>
    {
        public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => !x.Contacts.Any() && x.Active;
    }
}
