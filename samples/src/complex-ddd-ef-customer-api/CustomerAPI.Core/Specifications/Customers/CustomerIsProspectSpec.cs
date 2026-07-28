using CustomerAPI.Core.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System;
using System.Linq.Expressions;

namespace CustomerAPI.Core.Specifications.Customers
{
    /// <summary>
    /// Selects customers whose note contains the word "prospect" (case-sensitive).
    /// </summary>
    public class CustomerIsProspectSpec : ISpecificationQuery<Customer>
    {
        public Expression<Func<Customer, bool>> IsSatisfiedByExpression =>
            x => x.Note != null && x.Note.Contains("prospect");
    }
}
