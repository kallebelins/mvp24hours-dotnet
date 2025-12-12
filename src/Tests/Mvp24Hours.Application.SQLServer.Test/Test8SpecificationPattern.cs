//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.SQLServer.Test.Setup;
using Mvp24Hours.Application.SQLServer.Test.Support.Entities;
using Mvp24Hours.Application.SQLServer.Test.Support.Specifications;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Specifications;
using System;
using System.Linq;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Application.SQLServer.Test
{
    /// <summary>
    /// Tests for Specification Pattern integration with EF Core.
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class Test8SpecificationPattern
    {
        private readonly IServiceProvider serviceProvider;

        #region [ Ctor ]
        public Test8SpecificationPattern()
        {
            serviceProvider = Startup.Initialize();
        }
        #endregion

        #region [ Specification Pattern - Basic Tests ]

        [Fact, Priority(1)]
        public void Specification_IsSatisfiedBy_ValidEntity_ReturnsTrue()
        {
            // arrange
            var customer = new Customer { Name = "Test Customer", Active = true };
            var spec = new ActiveCustomerSpecification();

            // act
            var result = spec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(2)]
        public void Specification_IsSatisfiedBy_InvalidEntity_ReturnsFalse()
        {
            // arrange
            var customer = new Customer { Name = "Test Customer", Active = false };
            var spec = new ActiveCustomerSpecification();

            // act
            var result = spec.IsSatisfiedBy(customer);

            // assert
            Assert.False(result);
        }

        [Fact, Priority(3)]
        public void Specification_IsSatisfiedBy_NullEntity_ReturnsFalse()
        {
            // arrange
            var spec = new ActiveCustomerSpecification();

            // act
            var result = spec.IsSatisfiedBy(null);

            // assert
            Assert.False(result);
        }

        #endregion

        #region [ Specification Pattern - Composition ]

        [Fact, Priority(10)]
        public void Specification_And_BothConditionsMet_ReturnsTrue()
        {
            // arrange
            var customer = new Customer { Name = "Test Customer", Active = true };
            var activeSpec = new ActiveCustomerSpecification();
            var nameSpec = new CustomerByNameSpecification("Test");
            var combinedSpec = activeSpec & nameSpec;

            // act
            var result = combinedSpec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(11)]
        public void Specification_And_OneConditionNotMet_ReturnsFalse()
        {
            // arrange
            var customer = new Customer { Name = "Other Customer", Active = true };
            var activeSpec = new ActiveCustomerSpecification();
            var nameSpec = new CustomerByNameSpecification("Test");
            var combinedSpec = activeSpec & nameSpec;

            // act
            var result = combinedSpec.IsSatisfiedBy(customer);

            // assert
            Assert.False(result);
        }

        [Fact, Priority(12)]
        public void Specification_Or_OneConditionMet_ReturnsTrue()
        {
            // arrange
            var customer = new Customer { Name = "Other Customer", Active = true };
            var activeSpec = new ActiveCustomerSpecification();
            var nameSpec = new CustomerByNameSpecification("Test");
            var combinedSpec = activeSpec | nameSpec;

            // act
            var result = combinedSpec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(13)]
        public void Specification_Not_NegatesCondition()
        {
            // arrange
            var customer = new Customer { Name = "Test Customer", Active = false };
            var activeSpec = new ActiveCustomerSpecification();
            var notActiveSpec = !activeSpec;

            // act
            var result = notActiveSpec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(14)]
        public void Specification_StaticCreate_CreatesFromExpression()
        {
            // arrange
            var customer = new Customer { Name = "Premium Customer", Active = true };
            var spec = Specification<Customer>.Create(c => c.Name.Contains("Premium"));

            // act
            var result = spec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(15)]
        public void Specification_All_MatchesAllEntities()
        {
            // arrange
            var customer = new Customer { Name = "Any Customer", Active = false };
            var spec = Specification<Customer>.All();

            // act
            var result = spec.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        [Fact, Priority(16)]
        public void Specification_None_MatchesNoEntities()
        {
            // arrange
            var customer = new Customer { Name = "Any Customer", Active = true };
            var spec = Specification<Customer>.None();

            // act
            var result = spec.IsSatisfiedBy(customer);

            // assert
            Assert.False(result);
        }

        #endregion

        #region [ SpecificationEvaluator Tests ]

        [Fact, Priority(20)]
        public void SpecificationEvaluator_GetQuery_AppliesFilter()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var allCustomers = repository.List();
            var spec = new ActiveCustomerSpecification();

            // act
            var query = allCustomers.AsQueryable();
            var evaluatedQuery = SpecificationEvaluator<Customer>.Default.GetQuery(query, spec);
            var filteredCustomers = evaluatedQuery.ToList();

            // assert
            Assert.True(filteredCustomers.All(c => c.Active));
        }

        [Fact, Priority(21)]
        public void SpecificationEvaluator_GetQuery_AppliesOrdering()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var allCustomers = repository.List();
            var spec = new ActiveCustomerSpecification(); // Has OrderBy(Name)

            // act
            var query = allCustomers.AsQueryable();
            var evaluatedQuery = SpecificationEvaluator<Customer>.Default.GetQuery(query, spec);
            var orderedCustomers = evaluatedQuery.ToList();

            // assert
            var expectedOrder = orderedCustomers.OrderBy(c => c.Name).ToList();
            Assert.True(orderedCustomers.Count > 0);
            for (int i = 0; i < orderedCustomers.Count; i++)
            {
                Assert.Equal(expectedOrder[i].Name, orderedCustomers[i].Name);
            }
        }

        [Fact, Priority(22)]
        public void SpecificationEvaluator_GetQuery_AppliesPagination()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var allCustomers = repository.List();
            var spec = new PaginatedActiveCustomerSpecification(skip: 2, take: 3);

            // act
            var query = allCustomers.AsQueryable();
            var evaluatedQuery = SpecificationEvaluator<Customer>.Default.GetQuery(query, spec);
            var pagedCustomers = evaluatedQuery.ToList();

            // assert
            Assert.True(pagedCustomers.Count <= 3);
        }

        #endregion

        #region [ Specification with Repository Integration ]

        [Fact, Priority(30)]
        public void Repository_WithSpecification_FiltersCorrectly()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var spec = new ActiveCustomerSpecification();

            // act
            var allCustomers = repository.List();
            var filtered = allCustomers.AsQueryable();
            var query = SpecificationEvaluator.GetQuery(filtered, spec);
            var result = query.ToList();

            // assert
            Assert.True(result.All(c => c.Active));
            Assert.True(result.Count > 0);
        }

        [Fact, Priority(31)]
        public void Repository_WithNameSpecification_FiltersCorrectly()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var spec = new CustomerByNameSpecification("Test");

            // act
            var allCustomers = repository.List();
            var filtered = allCustomers.AsQueryable();
            var query = SpecificationEvaluator.GetQuery(filtered, spec);
            var result = query.ToList();

            // assert
            Assert.True(result.All(c => c.Name.Contains("Test")));
        }

        [Fact, Priority(32)]
        public void Repository_WithComposedSpecification_FiltersCorrectly()
        {
            // arrange
            var repository = serviceProvider.GetService<IRepository<Customer>>();
            var activeSpec = new ActiveCustomerSpecification();
            var nameSpec = new CustomerByNameSpecification("Test");
            var combinedSpec = activeSpec & nameSpec;

            // act
            var allCustomers = repository.List();
            var filtered = allCustomers.AsQueryable();
            var query = SpecificationEvaluator.GetQuery(filtered, combinedSpec);
            var result = query.ToList();

            // assert
            Assert.True(result.All(c => c.Active && c.Name.Contains("Test")));
        }

        #endregion

        #region [ Specification - Edge Cases ]

        [Fact, Priority(40)]
        public void Specification_EmptySearchTerm_Throws()
        {
            // act & assert
            Assert.Throws<ArgumentNullException>(() => new CustomerByNameSpecification(null));
        }

        [Fact, Priority(41)]
        public void Specification_ComplexComposition_WorksCorrectly()
        {
            // arrange
            var customer1 = new Customer { Name = "Test Premium", Active = true };
            var customer2 = new Customer { Name = "Test Basic", Active = false };
            var customer3 = new Customer { Name = "Other Premium", Active = true };

            var activeSpec = new ActiveCustomerSpecification();
            var testSpec = new CustomerByNameSpecification("Test");
            var premiumSpec = Specification<Customer>.Create(c => c.Name.Contains("Premium"));

            // Complex: (Active AND Test) OR Premium
            var complexSpec = (activeSpec & testSpec) | premiumSpec;

            // act & assert
            Assert.True(complexSpec.IsSatisfiedBy(customer1)); // Active AND Test (also Premium)
            Assert.False(complexSpec.IsSatisfiedBy(customer2)); // Test but not Active
            Assert.True(complexSpec.IsSatisfiedBy(customer3)); // Premium
        }

        [Fact, Priority(42)]
        public void Specification_DoubleNegation_RestoresOriginal()
        {
            // arrange
            var customer = new Customer { Name = "Test", Active = true };
            var activeSpec = new ActiveCustomerSpecification();
            var doubleNegated = !(!activeSpec);

            // act
            var result = doubleNegated.IsSatisfiedBy(customer);

            // assert
            Assert.True(result);
        }

        #endregion
    }
}

