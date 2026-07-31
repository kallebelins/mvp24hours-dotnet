using System.Linq.Expressions;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using Mvp24Hours.Core.Contract.Domain.Specifications;

namespace CustomerAPI.Core.Specifications.Customers;

/// <summary>
/// 
/// </summary>
public class CustomerHasEmailContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.Email) && x.Active;
}
