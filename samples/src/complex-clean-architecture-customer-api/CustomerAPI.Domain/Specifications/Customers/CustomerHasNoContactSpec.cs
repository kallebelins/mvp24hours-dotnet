using System.Linq.Expressions;
using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;

namespace CustomerAPI.Domain.Specifications.Customers;

public class CustomerHasNoContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => !x.Contacts.Any() && x.Active;
}
