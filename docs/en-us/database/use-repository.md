# Repository

Repositories provide query and command operations for `IEntityBase` entities. Resolve one from the unit of work so all repositories in the scope share the same context and transaction.

```csharp
public sealed class CustomerStore(IUnitOfWorkAsync unitOfWork)
{
    private readonly IRepositoryAsync<Customer> _customers =
        unitOfWork.GetRepository<Customer>();
}
```

Register synchronous or asynchronous contracts:

```csharp
builder.Services.AddMvp24HoursRepository(options =>
    options.MaxQtyByQueryPage = 100);

// Or:
builder.Services.AddMvp24HoursRepositoryAsync(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
});
```

## Queries

The core query surface includes `List`, `ListCount`, `ListAny`, `GetBy`, `GetByCount`, `GetByAny`, and `GetById`, with `Async` counterparts on `IRepositoryAsync<T>`.

```csharp
var repository = unitOfWork.GetRepository<Customer>();

IList<Customer> active = await repository.GetByAsync(
    customer => customer.Active,
    new PagingCriteria(limit: 20, offset: 0),
    cancellationToken);

Customer? customer = await repository.GetByIdAsync(customerId, cancellationToken: cancellationToken);
```

Use `PagingCriteria` for string-based navigation/order configuration or `PagingCriteriaExpression<TEntity>` for compile-time checked expressions:

```csharp
var paging = new PagingCriteriaExpression<Customer>(limit: 20, offset: 0);
paging.NavigationExpr.Add(customer => customer.Contacts);
paging.OrderByAscendingExpr.Add(customer => customer.Name);

var page = await repository.ListAsync(paging, cancellationToken);
```

## Commands

`Add`, `Modify`, `Remove`, and `RemoveById` accept one entity or a list; asynchronous repositories expose the corresponding `Async` methods. Repository commands stage changes. Commit them through the unit of work.

```csharp
await repository.AddAsync(customer, cancellationToken: cancellationToken);
customer.Name = "Updated";
await repository.ModifyAsync(customer, cancellationToken: cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

## Relations

`LoadRelation` / `LoadRelationAsync` load a reference or collection explicitly. Collection overloads support a filter and limit, plus ascending and descending variants.

```csharp
await repository.LoadRelationAsync(
    customer,
    entity => entity.Contacts,
    clause: contact => contact.Active,
    limit: 2,
    cancellationToken: cancellationToken);
```

MongoDB repositories do not support EF Core navigation loading.

## Bulk and streaming

EF Core and MongoDB add specialized asynchronous contracts:

- `IBulkOperationsRepositoryAsync<TEntity>` for bulk insert, update, and delete.
- EF Core repositories support `ExecuteUpdateAsync` and `ExecuteDeleteAsync`.
- `IStreamingRepositoryAsync<TEntity>` exposes `IAsyncEnumerable<TEntity>` for large result sets.

Use the module registration methods documented in [EF Core Advanced](efcore-advanced.md) and [MongoDB Advanced](mongodb-advanced.md); do not register interfaces manually unless replacing their implementations.

See [Unit of Work](use-unitofwork.md), [Services](use-service.md), and [Specification Pattern](../specification.md).
