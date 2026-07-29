using System.Linq.Expressions;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Enums;
using Mvp24Hours.Core.Contract.Domain.Specifications;

namespace CustomerAPI.Domain.Specifications.Customers;

public class CustomerHasEmailContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.Email) && x.Active;
}
