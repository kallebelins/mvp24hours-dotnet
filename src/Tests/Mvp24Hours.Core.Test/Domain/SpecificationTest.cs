using System.Linq.Expressions;
using Mvp24Hours.Core.Domain.Specifications;

namespace Mvp24Hours.Core.Test.Domain;

[Trait("Category", "Unit")]
public class SpecificationTest
{
    private sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Active { get; set; }
    }

    private sealed class ActiveProductsSpecification : Specification<Product>
    {
        protected override Expression<Func<Product, bool>> Criteria => p => p.Active && p.Price > 0;

        public ActiveProductsSpecification WithIncludesAndPaging()
        {
            AddInclude(p => p.Name);
            AddInclude("Category");
            AddOrderBy(p => p.Name);
            AddOrderByDescending(p => p.Price);
            ApplyPaging(10, 5);
            return this;
        }

        public ActiveProductsSpecification WithTopResult()
        {
            AddOrderByDescending(p => p.Price);
            ApplyPaging(0, 1);
            return this;
        }
    }

    private static List<Product> CreateProducts() =>
    [
        new() { Id = 1, Name = "Alpha", Price = 10m, Active = true },
        new() { Id = 2, Name = "Beta", Price = 0m, Active = true },
        new() { Id = 3, Name = "Gamma", Price = 20m, Active = false },
        new() { Id = 4, Name = "Delta", Price = 30m, Active = true }
    ];

    [Fact]
    public void Create_WithExpression_FiltersEntities()
    {
        Specification<Product> spec = Specification<Product>.Create(p => p.Price >= 20m);

        spec.IsSatisfiedBy(new Product { Price = 25m }).Should().BeTrue();
        spec.IsSatisfiedBy(new Product { Price = 5m }).Should().BeFalse();
    }

    [Fact]
    public void All_And_None_MatchExpected()
    {
        var product = new Product { Active = false };

        Specification<Product>.All().IsSatisfiedBy(product).Should().BeTrue();
        Specification<Product>.None().IsSatisfiedBy(product).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNull_ReturnsFalse()
    {
        Specification<Product> spec = Specification<Product>.Create(p => p.Active);

        spec.IsSatisfiedBy(null!).Should().BeFalse();
    }

    [Fact]
    public void CompositeOperators_AndOrNot_WorkCorrectly()
    {
        Specification<Product> active = Specification<Product>.Create(p => p.Active);
        Specification<Product> expensive = Specification<Product>.Create(p => p.Price >= 20m);
        var product = new Product { Active = true, Price = 30m };

        (active & expensive).IsSatisfiedBy(product).Should().BeTrue();
        (active | expensive).IsSatisfiedBy(new Product { Active = false, Price = 30m }).Should().BeTrue();
        (!active).IsSatisfiedBy(new Product { Active = false }).Should().BeTrue();
    }

    [Fact]
    public void EnhancedSpecification_ExposesIncludesOrderAndPaging()
    {
        var spec = new ActiveProductsSpecification().WithIncludesAndPaging();

        spec.Includes.Should().HaveCount(1);
        spec.IncludeStrings.Should().Contain("Category");
        spec.OrderBy.Should().HaveCount(2);
        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(5);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void InMemorySpecificationEvaluator_AppliesCriteriaOrderingAndPaging()
    {
        List<Product> products = CreateProducts();
        var spec = new ActiveProductsSpecification().WithTopResult();
        var evaluator = InMemorySpecificationEvaluator<Product>.Default;

        List<Product> result = evaluator.GetQuery(products.AsQueryable(), spec).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Delta");
    }

    [Fact]
    public void InMemorySpecificationEvaluator_NonGenericDelegatesToGeneric()
    {
        List<Product> products = CreateProducts();
        Specification<Product> spec = Specification<Product>.Create(p => p.Active && p.Price > 0);

        List<Product> result = InMemorySpecificationEvaluator.Default
            .GetQuery(products.AsQueryable(), spec)
            .ToList();

        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().BeEquivalentTo(["Alpha", "Delta"]);
    }
}
