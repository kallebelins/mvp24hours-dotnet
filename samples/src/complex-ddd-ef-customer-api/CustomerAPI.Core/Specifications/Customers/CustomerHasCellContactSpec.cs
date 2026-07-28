using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CustomerAPI.Core.Specifications.Customers
{
    /// <summary>
    /// Selects active customers that have at least one cell-phone contact.
    /// </summary>
    public class CustomerHasCellContactSpec : ISpecificationQuery<Customer>
    {
        public Expression<Func<Customer, bool>> IsSatisfiedByExpression =>
            x => x.Active && x.Contacts.Any(c => c.Type == ContactType.Cell);
    }
}
