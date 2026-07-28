using CustomerAPI.Domain.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System;
using System.Linq.Expressions;

namespace CustomerAPI.Domain.Specifications.Customers;

/// <summary>
/// Matches customers that are currently active.
/// </summary>
public class CustomerIsActiveSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Active;
}
