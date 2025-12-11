//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Queries;
using System;
using System.Linq.Expressions;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test
{
    #region [ Test Entities ]

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    #endregion

    #region [ Test Specifications ]

    public class ActiveProductSpec : Specification<Product>
    {
        protected override Expression<Func<Product, bool>> Criteria =>
            p => p.IsActive;
    }

    public class ExpensiveProductSpec : Specification<Product>
    {
        private readonly decimal _minPrice;

        public ExpensiveProductSpec(decimal minPrice = 100m)
        {
            _minPrice = minPrice;
        }

        protected override Expression<Func<Product, bool>> Criteria =>
            p => p.Price >= _minPrice;
    }

    public class InStockProductSpec : Specification<Product>
    {
        protected override Expression<Func<Product, bool>> Criteria =>
            p => p.Stock > 0;
    }

    public class CategoryProductSpec : Specification<Product>
    {
        private readonly string _category;

        public CategoryProductSpec(string category)
        {
            _category = category;
        }

        protected override Expression<Func<Product, bool>> Criteria =>
            p => p.Category == _category;
    }

    public class ProductWithIncludesSpec : Specification<Product>
    {
        public ProductWithIncludesSpec()
        {
            AddOrderBy(p => p.Name);
            AddOrderByDescending(p => p.Price);
            ApplyPaging(0, 10);
        }

        protected override Expression<Func<Product, bool>> Criteria =>
            p => p.IsActive;
    }

    #endregion

    #region [ Test Queries ]

    public class GetProductsQuery : PaginatedQuery<Product, IEnumerable<Product>>
    {
        public string? CategoryFilter { get; set; }
        public decimal? MinPrice { get; set; }

        public GetProductsQuery(int page, int pageSize) : base(page, pageSize)
        {
        }
    }

    public class GetActiveProductsQuery : SortedQuery<Product, IEnumerable<Product>>
    {
        public GetActiveProductsQuery()
        {
            SortByAsc(p => p.Name);
            SortByDesc(p => p.Price);
        }
    }

    #endregion

    public class SpecificationTest
    {
        #region [ Specification<T> - IsSatisfiedBy Tests ]

        [Fact]
        public void Specification_IsSatisfiedBy_ActiveProduct_ReturnsTrue()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Test", IsActive = true };
            var spec = new ActiveProductSpec();

            // Act
            var result = spec.IsSatisfiedBy(product);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Specification_IsSatisfiedBy_InactiveProduct_ReturnsFalse()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Test", IsActive = false };
            var spec = new ActiveProductSpec();

            // Act
            var result = spec.IsSatisfiedBy(product);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Specification_IsSatisfiedBy_NullEntity_ReturnsFalse()
        {
            // Arrange
            var spec = new ActiveProductSpec();

            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type - testing null behavior
            var result = spec.IsSatisfiedBy(null);
#pragma warning restore CS8625

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Specification_IsSatisfiedBy_ExpensiveProduct_ReturnsTrue()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Test", Price = 150m };
            var spec = new ExpensiveProductSpec(100m);

            // Act
            var result = spec.IsSatisfiedBy(product);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region [ Specification<T> - Expression Tests ]

        [Fact]
        public void Specification_IsSatisfiedByExpression_ReturnsValidExpression()
        {
            // Arrange
            var spec = new ActiveProductSpec();

            // Act
            var expression = spec.IsSatisfiedByExpression;

            // Assert
            Assert.NotNull(expression);
            Assert.IsAssignableFrom<Expression<Func<Product, bool>>>(expression);
        }

        #endregion

        #region [ Specification<T> - Static Factory Tests ]

        [Fact]
        public void Specification_Create_FromExpression_Works()
        {
            // Arrange
            Expression<Func<Product, bool>> expr = p => p.Price > 50;

            // Act
            var spec = Specification<Product>.Create(expr);
            var product = new Product { Price = 75 };

            // Assert
            Assert.True(spec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_All_MatchesAllEntities()
        {
            // Arrange
            var spec = Specification<Product>.All();
            var product = new Product { Id = 1, Name = "Test" };

            // Act
            var result = spec.IsSatisfiedBy(product);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Specification_None_MatchesNoEntities()
        {
            // Arrange
            var spec = Specification<Product>.None();
            var product = new Product { Id = 1, Name = "Test" };

            // Act
            var result = spec.IsSatisfiedBy(product);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region [ Composition - AND Tests ]

        [Fact]
        public void Specification_AndOperator_BothTrue_ReturnsTrue()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = true, Price = 150 };
            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);

            // Act
            var combinedSpec = activeSpec & expensiveSpec;

            // Assert
            Assert.True(combinedSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_AndOperator_OneFalse_ReturnsFalse()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = true, Price = 50 };
            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);

            // Act
            var combinedSpec = activeSpec & expensiveSpec;

            // Assert
            Assert.False(combinedSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_AndSpec_Extension_Works()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = true, Price = 150, Stock = 5 };
            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);
            var inStockSpec = new InStockProductSpec();

            // Act
            var combinedSpec = activeSpec.AndSpec(expensiveSpec).AndSpec(inStockSpec);

            // Assert
            Assert.True(combinedSpec.IsSatisfiedBy(product));
        }

        #endregion

        #region [ Composition - OR Tests ]

        [Fact]
        public void Specification_OrOperator_OneTrue_ReturnsTrue()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = false, Price = 150 };
            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);

            // Act
            var combinedSpec = activeSpec | expensiveSpec;

            // Assert
            Assert.True(combinedSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_OrOperator_BothFalse_ReturnsFalse()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = false, Price = 50 };
            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);

            // Act
            var combinedSpec = activeSpec | expensiveSpec;

            // Assert
            Assert.False(combinedSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_OrSpec_Extension_Works()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = false, Price = 50, Category = "Electronics" };
            var activeSpec = new ActiveProductSpec();
            var electronicsSpec = new CategoryProductSpec("Electronics");

            // Act
            var combinedSpec = activeSpec.OrSpec(electronicsSpec);

            // Assert
            Assert.True(combinedSpec.IsSatisfiedBy(product));
        }

        #endregion

        #region [ Composition - NOT Tests ]

        [Fact]
        public void Specification_NotOperator_NegatesResult()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = true };
            var activeSpec = new ActiveProductSpec();

            // Act
            var negatedSpec = !activeSpec;

            // Assert
            Assert.False(negatedSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_NotSpec_Extension_Works()
        {
            // Arrange
            var product = new Product { Id = 1, IsActive = false };
            var activeSpec = new ActiveProductSpec();

            // Act
            var negatedSpec = activeSpec.NotSpec();

            // Assert
            Assert.True(negatedSpec.IsSatisfiedBy(product));
        }

        #endregion

        #region [ Complex Composition Tests ]

        [Fact]
        public void Specification_ComplexComposition_Works()
        {
            // Arrange
            // (Active AND Expensive) OR (InStock AND Electronics)
            var product = new Product
            {
                Id = 1,
                IsActive = false,
                Price = 50,
                Stock = 10,
                Category = "Electronics"
            };

            var activeSpec = new ActiveProductSpec();
            var expensiveSpec = new ExpensiveProductSpec(100);
            var inStockSpec = new InStockProductSpec();
            var electronicsSpec = new CategoryProductSpec("Electronics");

            // Act
            var complexSpec = (activeSpec & expensiveSpec) | (inStockSpec & electronicsSpec);

            // Assert
            Assert.True(complexSpec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Specification_ChainedComposition_Works()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                IsActive = true,
                Price = 150,
                Stock = 5,
                Category = "Electronics"
            };

            var spec = Specification<Product>.Create(p => p.IsActive)
                .AndSpec(p => p.Price >= 100)
                .AndSpec(p => p.Stock > 0);

            // Act & Assert
            Assert.True(spec.IsSatisfiedBy(product));
        }

        #endregion

        #region [ ISpecificationQuery - Enhanced Features ]

        [Fact]
        public void Specification_Includes_AreTracked()
        {
            // Arrange
            var spec = new ProductWithIncludesSpec();

            // Act & Assert
            Assert.NotNull(spec.Includes);
        }

        [Fact]
        public void Specification_OrderBy_AreTracked()
        {
            // Arrange
            var spec = new ProductWithIncludesSpec();

            // Act
            var orderBy = spec.OrderBy;

            // Assert
            Assert.Equal(2, orderBy.Count);
            Assert.False(orderBy[0].Descending); // First is ascending
            Assert.True(orderBy[1].Descending);  // Second is descending
        }

        [Fact]
        public void Specification_Paging_IsTracked()
        {
            // Arrange
            var spec = new ProductWithIncludesSpec();

            // Act & Assert
            Assert.True(spec.IsPagingEnabled);
            Assert.Equal(0, spec.Skip);
            Assert.Equal(10, spec.Take);
        }

        #endregion

        #region [ Conversion Tests ]

        [Fact]
        public void ISpecificationQuery_ToSpecification_Works()
        {
            // Arrange
            ISpecificationQuery<Product> specQuery = new ActiveProductSpec();

            // Act
            var spec = specQuery.ToSpecification();
            var product = new Product { IsActive = true };

            // Assert
            Assert.True(spec.IsSatisfiedBy(product));
        }

        [Fact]
        public void Expression_ToSpecification_Works()
        {
            // Arrange
            Expression<Func<Product, bool>> expr = p => p.Price > 100;

            // Act
            var spec = expr.ToSpecification();
            var product = new Product { Price = 150 };

            // Assert
            Assert.True(spec.IsSatisfiedBy(product));
        }

        #endregion

        #region [ PaginatedQuery Tests ]

        [Fact]
        public void PaginatedQuery_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var query = new GetProductsQuery(0, 20);

            // Assert
            Assert.Equal(0, query.Page);
            Assert.Equal(20, query.PageSize);
        }

        [Fact]
        public void PaginatedQuery_Implements_IPagingCriteria()
        {
            // Arrange
            var query = new GetProductsQuery(2, 25);

            // Act
            var pagingCriteria = (Core.Contract.ValueObjects.Logic.IPagingCriteria)query;

            // Assert
            Assert.Equal(25, pagingCriteria.Limit);
            Assert.Equal(2, pagingCriteria.Offset);
        }

        [Fact]
        public void PaginatedQuery_FluentMethods_Work()
        {
            // Arrange
            var query = new GetProductsQuery(0, 10)
                .WithOrderBy("Name", "-Price")
                .WithNavigation("Category");

            // Assert
            Assert.Contains("Name", query.OrderBy);
            Assert.Contains("-Price", query.OrderBy);
            Assert.Contains("Category", query.Navigation);
        }

        [Fact]
        public void PaginatedQuery_MaxPageSize_IsRespected()
        {
            // Arrange & Act
            var query = new GetProductsQuery(0, 500); // Over default max of 100

            // Assert
            Assert.True(query.PageSize <= 100);
        }

        [Fact]
        public void PaginatedQuery_WithExpressions_Works()
        {
            // Arrange
            var query = new GetProductsQuery(0, 10);

            // Act
            query.OrderByAsc(p => p.Name);
            query.OrderByDesc(p => p.Price);

            // Assert
            Assert.Single(query.OrderByAscendingExpr);
            Assert.Single(query.OrderByDescendingExpr);
        }

        #endregion

        #region [ SortedQuery Tests ]

        [Fact]
        public void SortedQuery_AddSort_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery();

            // Act
            query.AddSort("Category", SortDirection.Ascending);

            // Assert
            // SortCriteria contains string-based sorts (added via AddSort(string))
            Assert.Single(query.SortCriteria); // 1 from AddSort("Category")
            // SortExpressions contains expression-based sorts (added via SortByAsc/SortByDesc with expressions)
            Assert.Equal(2, query.SortExpressions.Count); // 2 from constructor
        }

        [Fact]
        public void SortedQuery_SortByAsc_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery();

            // Act
            query.SortByAsc("Stock");

            // Assert
            var lastSort = query.SortCriteria[^1];
            Assert.Equal("Stock", lastSort.PropertyName);
            Assert.Equal(SortDirection.Ascending, lastSort.Direction);
        }

        [Fact]
        public void SortedQuery_SortByDesc_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery();

            // Act
            query.SortByDesc("Stock");

            // Assert
            var lastSort = query.SortCriteria[^1];
            Assert.Equal("Stock", lastSort.PropertyName);
            Assert.Equal(SortDirection.Descending, lastSort.Direction);
        }

        [Fact]
        public void SortedQuery_ClearSort_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery();
            var initialExprCount = query.SortExpressions.Count;

            // Act
            query.ClearSort();

            // Assert
            Assert.Empty(query.SortCriteria);
            Assert.Empty(query.SortExpressions);
            Assert.True(initialExprCount > 0); // Verify there were sorts before
        }

        [Fact]
        public void SortedQuery_WithIncludes_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery()
                .WithIncludes("Category", "Supplier");

            // Assert
            Assert.Contains("Category", query.Includes);
            Assert.Contains("Supplier", query.Includes);
        }

        [Fact]
        public void SortedQuery_WithExpressions_Works()
        {
            // Arrange
            var query = new GetActiveProductsQuery();

            // Act
            query.Include(p => p.Category);

            // Assert
            Assert.Single(query.IncludeExpressions);
        }

        #endregion

        #region [ SortCriteria Static Factory Tests ]

        [Fact]
        public void SortCriteria_Asc_CreatesAscendingSort()
        {
            // Act
            var criteria = SortCriteria.Asc("Name");

            // Assert
            Assert.Equal("Name", criteria.PropertyName);
            Assert.Equal(SortDirection.Ascending, criteria.Direction);
        }

        [Fact]
        public void SortCriteria_Desc_CreatesDescendingSort()
        {
            // Act
            var criteria = SortCriteria.Desc("Price");

            // Assert
            Assert.Equal("Price", criteria.PropertyName);
            Assert.Equal(SortDirection.Descending, criteria.Direction);
        }

        [Fact]
        public void SortCriteria_Generic_Asc_CreatesAscendingSort()
        {
            // Act
            var criteria = SortCriteria<Product>.Asc(p => p.Name);

            // Assert
            Assert.NotNull(criteria.PropertySelector);
            Assert.Equal(SortDirection.Ascending, criteria.Direction);
        }

        [Fact]
        public void SortCriteria_Generic_Desc_CreatesDescendingSort()
        {
            // Act
            var criteria = SortCriteria<Product>.Desc(p => p.Price);

            // Assert
            Assert.NotNull(criteria.PropertySelector);
            Assert.Equal(SortDirection.Descending, criteria.Direction);
        }

        #endregion
    }
}
