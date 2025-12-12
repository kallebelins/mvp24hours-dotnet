//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Core.Domain.Specifications;
using System;
using System.Linq.Expressions;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Specifications
{
    /// <summary>
    /// Specification that filters active customers.
    /// </summary>
    public class ActiveCustomerSpecification : Specification<Customer>
    {
        protected override Expression<Func<Customer, bool>> Criteria => c => c.Active;

        public ActiveCustomerSpecification()
        {
            // Add default ordering by name
            AddOrderBy(c => c.Name);
        }
    }

    /// <summary>
    /// Specification that filters customers by name containing a search term.
    /// </summary>
    public class CustomerByNameSpecification : Specification<Customer>
    {
        private readonly string _searchTerm;

        public CustomerByNameSpecification(string searchTerm)
        {
            _searchTerm = searchTerm ?? throw new ArgumentNullException(nameof(searchTerm));
        }

        protected override Expression<Func<Customer, bool>> Criteria => c => c.Name.Contains(_searchTerm);
    }

    /// <summary>
    /// Specification that filters active customers with contacts loaded.
    /// </summary>
    public class ActiveCustomerWithContactsSpecification : Specification<Customer>
    {
        protected override Expression<Func<Customer, bool>> Criteria => c => c.Active;

        public ActiveCustomerWithContactsSpecification()
        {
            // Include contacts navigation property
            AddInclude(c => c.Contacts);
            
            // Order by name descending
            AddOrderByDescending(c => c.Name);
        }
    }

    /// <summary>
    /// Specification with pagination for active customers.
    /// </summary>
    public class PaginatedActiveCustomerSpecification : Specification<Customer>
    {
        protected override Expression<Func<Customer, bool>> Criteria => c => c.Active;

        public PaginatedActiveCustomerSpecification(int skip, int take)
        {
            ApplyPaging(skip, take);
            AddOrderBy(c => c.Id);
        }
    }

    /// <summary>
    /// Specification that uses string-based include for multi-level loading.
    /// </summary>
    public class CustomerWithContactsStringIncludeSpecification : Specification<Customer>
    {
        protected override Expression<Func<Customer, bool>> Criteria => c => true;

        public CustomerWithContactsStringIncludeSpecification()
        {
            // Using string-based include for navigation
            AddInclude("Contacts");
        }
    }
}

