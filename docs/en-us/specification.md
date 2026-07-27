# Specification Pattern

A specification gives a business rule a name and exposes it as an expression that can be evaluated in memory or translated by an `IQueryable` provider. Mvp24Hours supports the small `ISpecificationQuery<T>` contract and the richer `Specification<T>` base class.

> For the pattern background, see Martin Fowler's [Specification](https://martinfowler.com/apsupp/spec.pdf).

## Which page should I use?

- Stay on this page to model, compose, and evaluate domain rules.
- Go to [CQRS specifications](cqrs/specifications.md) to apply specifications in query handlers, repositories, paging, and EF Core.

## A small query specification

Use `ISpecificationQuery<T>` when a named predicate is enough:

```csharp
using System.Linq.Expressions;
using Mvp24Hours.Core.Contract.Domain.Specifications;

public sealed class ActiveProductSpecification : ISpecificationQuery<Product>
{
    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        product => product.IsActive;
}

var specification = new ActiveProductSpecification();
IQueryable<Product> query = products.Where(specification.IsSatisfiedByExpression);
```

This is the same contract used by the Application integration tests for active products, price ranges, and low stock.

## Rich specifications

Derive from `Specification<T>` when the rule also needs in-memory evaluation, includes, ordering, or paging. `Criteria` is `protected`; consumers use `IsSatisfiedByExpression`.

```csharp
using System.Linq.Expressions;
using Mvp24Hours.Core.Domain.Specifications;

public sealed class ActiveProductsByPriceSpecification : Specification<Product>
{
    private readonly decimal _minimumPrice;

    public ActiveProductsByPriceSpecification(decimal minimumPrice)
    {
        _minimumPrice = minimumPrice;
        AddOrderByDescending(product => product.Price);
    }

    protected override Expression<Func<Product, bool>> Criteria =>
        product => product.IsActive && product.Price >= _minimumPrice;
}

var specification = new ActiveProductsByPriceSpecification(20m);
bool matches = specification.IsSatisfiedBy(product);
IQueryable<Product> query = products.Where(specification.IsSatisfiedByExpression);
```

The protected builder methods are:

| Method | Effect |
|---|---|
| `AddInclude(expression)` | Adds a typed navigation include |
| `AddInclude(string)` | Adds a string navigation path |
| `AddOrderBy(expression)` | Adds ascending ordering |
| `AddOrderByDescending(expression)` | Adds descending ordering |
| `ApplyPaging(skip, take)` | Sets `Skip`, `Take`, and `IsPagingEnabled` |

## Factory methods and composition

`Specification<T>` exposes `Create`, `All`, and `None` as methods. Specifications compose with operators or the `AndSpec`, `OrSpec`, and `NotSpec` extensions:

```csharp
using Mvp24Hours.Extensions;

Specification<Product> active =
    Specification<Product>.Create(product => product.IsActive);
Specification<Product> inStock =
    Specification<Product>.Create(product => product.StockQuantity > 0);

Specification<Product> available = active & inStock;
Specification<Product> visible = available | !active;

// Equivalent extension form:
Specification<Product> availableAgain = active.AndSpec(inStock);
```

The composition keeps a single expression tree, so the result works with both `IsSatisfiedBy` and `IQueryable.Where`. `SpecificationCombinators` in the Application package additionally provides `And`, `Or`, `Not`, `AndAll`, `OrAll`, `If`, and `IfElse`.

```csharp
using Mvp24Hours.Application.Specifications;

Specification<Product> filtered = SpecificationCombinators.If(
    activeOnly,
    Specification<Product>.Create(product => product.IsActive));
```

## In-memory evaluation

Call `IsSatisfiedBy` for one object. It returns `false` for `null` and caches the compiled predicate. Use `InMemorySpecificationEvaluator<T>` when ordering and paging must also be applied:

```csharp
List<Product> result = InMemorySpecificationEvaluator<Product>.Default
    .GetQuery(products.AsQueryable(), specification)
    .ToList();
```

For database includes and server-side paging, continue with [CQRS specifications](cqrs/specifications.md).

## Related documentation

- [CQRS specifications](cqrs/specifications.md)
- [Repository pattern](database/use-repository.md)
