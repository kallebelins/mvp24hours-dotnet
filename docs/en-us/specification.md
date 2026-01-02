# Specification Pattern
The specification standard we use as requirements for search filters. Each specification must be created with the aim of defining concrete cases. Each specification can be part of a composition or applied individually.

>In computer programming, specification pattern is a specific software design pattern through which business rules can be recombined by chaining business rules together using Boolean logic. The pattern is often used in the context of domain-driven design. [Wikipedia](https://en.wikipedia.org/wiki/Specification_pattern)

## Basic Example
In this example we created a specification that filters people who have a cell phone number in the contact record.

```csharp
/// CustomerHasCellContactSpec.cs

public class CustomerHasCellContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.CellPhone);
}

/// CustomerService.cs -> Get Method

Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.And<Customer, CustomerHasCellContactSpec>();
var paging = new PagingCriteriaExpression<Customer>(3, 0);
paging.NavigationExpr.Add(x => x.Contacts);
var boResult = service.GetBy(filter, paging);
```

## Specification Composition

Specifications can be composed using logical operators `And`, `Or`, and `Not`:

### Using And Operator

```csharp
// Combine multiple specifications with AND
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter
    .And<Customer, CustomerHasCellContactSpec>()
    .And<Customer, CustomerHasEmailSpec>();
```

### Using Or Operator

```csharp
// Combine specifications with OR
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.Or<Customer, CustomerIsVIPSpec>();
```

### Using Not Operator

```csharp
// Negate a specification
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.Not<Customer, CustomerIsBlockedSpec>();
```

## Specification Base Class

For more complex specifications, use the `Specification<T>` base class:

```csharp
public class ActiveCustomerWithContactsSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Active && customer.Contacts.Any();
    }
}

// Usage
var spec = new ActiveCustomerWithContactsSpec();
var customers = await repository.GetByAsync(spec.ToExpression());
```

## SpecificationEvaluator for EF Core

Use `SpecificationEvaluator<T>` for advanced queries with EF Core:

```csharp
public class CustomerByStatusSpec : Specification<Customer>
{
    private readonly CustomerStatus _status;
    
    public CustomerByStatusSpec(CustomerStatus status)
    {
        _status = status;
    }
    
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Status == _status;
    }
}

// With SpecificationEvaluator
var spec = new CustomerByStatusSpec(CustomerStatus.Active);
var query = SpecificationEvaluator<Customer>.GetQuery(dbContext.Customers, spec);
var results = await query.ToListAsync();
```

## Parameterized Specifications

Create reusable specifications with parameters:

```csharp
public class CustomerByNameSpec : ISpecificationQuery<Customer>
{
    private readonly string _name;
    
    public CustomerByNameSpec(string name)
    {
        _name = name;
    }
    
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => 
        x => x.Name.Contains(_name);
}

// Usage
var spec = new CustomerByNameSpec("John");
var filter = Expression<Func<Customer, bool>>.And(
    x => x.Active,
    spec.IsSatisfiedByExpression);
```

---

## Related Documentation

- [CQRS Specifications](cqrs/specifications.md) - Using specifications with CQRS pattern
- [Repository Pattern](database/use-repository.md) - Repository with specifications
