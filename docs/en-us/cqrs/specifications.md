# Specifications in CQRS Queries

Specifications keep filtering rules outside handlers while preserving an expression tree that EF Core can translate.

## Which page should I use?

- Read [Specification pattern](../specification.md) for the Core contracts, composition, and in-memory evaluation.
- Use this page for query handlers, repository integration, ordering, includes, paging, and EF Core evaluation.

## Query contract

A handler can accept any `ISpecificationQuery<TEntity>`. A `Specification<TEntity>` also implements the enhanced contract and can carry includes, ordering, `Skip`, and `Take`.

```csharp
using System.Linq.Expressions;
using Mvp24Hours.Core.Domain.Specifications;

public sealed class TopActiveProductsSpecification : Specification<Product>
{
    public TopActiveProductsSpecification(int skip, int take)
    {
        AddInclude(product => product.Category);
        AddOrderByDescending(product => product.Price);
        ApplyPaging(skip, take);
    }

    protected override Expression<Func<Product, bool>> Criteria =>
        product => product.IsActive;
}
```

## EF Core evaluator

`Mvp24Hours.Infrastructure.Data.EFCore.Specifications.SpecificationEvaluator<T>` applies features in this order:

1. criteria (`Where`);
2. expression and string includes;
3. `OrderBy` followed by `ThenBy`;
4. `Skip` and `Take`.

`T` must implement `IEntityBase`.

```csharp
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Specifications;

public sealed class GetProductsHandler
    : IMediatorQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly AppDbContext _dbContext;

    public GetProductsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var specification = new TopActiveProductsSpecification(
            request.Offset,
            request.Limit);

        return await SpecificationEvaluator<Product>.Default
            .GetQuery(_dbContext.Products, specification)
            .Select(product => new ProductDto(product.Id, product.Name, product.Price))
            .ToListAsync(cancellationToken);
    }
}
```

The non-generic facade is also available:

```csharp
IQueryable<Product> query =
    SpecificationEvaluator.GetQuery(dbContext.Products, specification);
```

## Simple specifications in application services

If includes and paging are supplied separately, keep the specification small:

```csharp
public sealed class PriceRangeSpecification(decimal minimum, decimal maximum)
    : ISpecificationQuery<Product>
{
    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        product => product.Price >= minimum && product.Price <= maximum;
}

var specification = new PriceRangeSpecification(10m, 100m);
var products = await productService.GetByAsync(
    specification.IsSatisfiedByExpression,
    cancellationToken);
```

Use the exact overload exposed by your repository or application service; the common interop surface is `IsSatisfiedByExpression`.

## Dynamic query composition

Build optional filters from `All()` and compose them before the handler reaches EF Core:

```csharp
using Mvp24Hours.Extensions;

Specification<Product> specification = Specification<Product>.All();

if (request.ActiveOnly)
{
    specification = specification.AndSpec(
        Specification<Product>.Create(product => product.IsActive));
}

if (request.MinimumPrice is decimal minimumPrice)
{
    specification = specification.AndSpec(
        product => product.Price >= minimumPrice);
}
```

For Application-layer combinators, `AndAll([])` returns `All()`, while `OrAll([])` returns `None()`. This makes optional filter lists deterministic.

## Paging interoperability

Core provides conversion extensions between specifications and paging criteria:

```csharp
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

IPagingCriteriaExpression<Product> paging =
    specification.ToPagingCriteria(limit: 20, offset: 0);

Specification<Product> fromPaging =
    paging.ToSpecification(product => product.IsActive);
```

These conversions copy ordering and navigation information. The EF Core evaluator can instead consume the rich specification directly.

## Testing a query rule

Test the rule without a database first:

```csharp
var specification = Specification<Product>.Create(
    product => product.IsActive && product.Price > 0);

specification.IsSatisfiedBy(activeProduct).Should().BeTrue();
specification.IsSatisfiedBy(inactiveProduct).Should().BeFalse();
```

Then add an EF Core test when translation, includes, order, or paging matters. Repository tests verify that the evaluator returns active entities and applies descending order before `Skip`/`Take`.

## Related documentation

- [Specification pattern](../specification.md)
- [Queries](queries.md)
- [Repository integration](integration-repository.md)
- [CQRS getting started](getting-started.md)
