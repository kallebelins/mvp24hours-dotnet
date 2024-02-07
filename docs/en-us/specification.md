# Specification Pattern
The specification standard we use as requirements for search filters. Each specification must be created with the aim of defining concrete cases. Each specification can be part of a composition or applied individually.

>In computer programming, specification pattern is a specific software design pattern through which business rules can be recombined by chaining business rules together using Boolean logic. The pattern is often used in the context of domain-driven design. [Wikipedia](https://en.wikipedia.org/wiki/Specification_pattern)

### Example
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
