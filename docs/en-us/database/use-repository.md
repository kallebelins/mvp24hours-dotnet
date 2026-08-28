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

## Strongly-typed identifiers

`GetById` and `RemoveById` take `object`, so a wrong identifier type only fails at runtime. `IRepository<T, TId>` and `IRepositoryAsync<T, TId>` add typed overloads for entities that declare their identifier through `IEntity<TId>`:

```csharp
public class Customer : EntityBase<int>, IEntity<int>
{
    public string Name { get; set; } = string.Empty;
}

public sealed class CustomerStore(IRepositoryAsync<Customer, int> repository)
{
    public Task<Customer?> FindAsync(int id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);
}
```

This contract is optional and additive:

- It inherits the whole surface of `IRepository<T>` / `IRepositoryAsync<T>`, so nothing is lost by depending on it.
- The typed members delegate to the `object`-based members, which remain the real implementation and remain supported. Existing code does not need to change.
- Typed members: `GetById(TId)`, `GetById(TId, IPagingCriteria?)`, `RemoveById(TId)`, `RemoveById(IList<TId>)`, plus the `Async` counterparts.

`AddMvp24HoursRepository` and `AddMvp24HoursRepositoryAsync` register it automatically for EF Core and MongoDB. Two constraints to keep in mind:

- Resolve it from the container, not from the unit of work. `GetRepository<T>()` has a single type parameter and always returns `IRepository<T>`.
- Passing a custom `repository` / `repositoryAsync` type to the registration methods leaves the typed contract unregistered, since a one-parameter implementation has no two-parameter counterpart to map to. Register `IRepository<,>` yourself in that case.

`IEntityBase.EntityKey` stays `object?`, so the identifier is still boxed inside the repository; the gain is compile-time checking and a boxing-free call site.

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
