# Unit of Work

`IUnitOfWork` and `IUnitOfWorkAsync` coordinate repositories that share the same database context. Obtain them through constructor injection; avoid resolving them from the root service provider.

## Registration and use

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddMvp24HoursDbContext<AppDbContext>();
builder.Services.AddMvp24HoursRepositoryAsync();

public sealed class CreateCustomerHandler(IUnitOfWorkAsync unitOfWork)
{
    public async Task<Guid> HandleAsync(string name, CancellationToken cancellationToken)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = name };
        await unitOfWork.GetRepository<Customer>()
            .AddAsync(customer, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}
```

The synchronous equivalents are `GetRepository<T>()`, `SaveChanges(...)`, and `Rollback()`. The asynchronous rollback method is `RollbackAsync()`.

## Multiple repositories, one commit

```csharp
var customers = unitOfWork.GetRepository<Customer>();
var orders = unitOfWork.GetRepository<Order>();

await customers.AddAsync(customer, cancellationToken: cancellationToken);
await orders.AddAsync(order, cancellationToken: cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

Call `SaveChanges` only after all changes for the business transaction are staged. `GetConnection()` exposes the context connection for advanced SQL/Dapper scenarios; do not dispose that connection yourself.

## Domain events

EF Core provides event-aware unit-of-work registrations. The tested registration is:

```csharp
builder.Services.AddMvp24HoursRepositoryWithEvents(options =>
    options.MaxQtyByQueryPage = 100);
```

Inject `IUnitOfWorkWithEvents` and call `SaveChangesWithEvents(...)`, or inject `IUnitOfWorkWithEventsAsync` and call `SaveChangesWithEventsAsync(...)`. Events are collected from tracked domain-event entities, persisted, dispatched, and then cleared. Configure the dispatcher before using the event-aware unit of work; `AddMvp24HoursEFCoreCqrs<TDbContext>` can wire the EF Core CQRS path.

```csharp
public sealed class PlaceOrderHandler(IUnitOfWorkWithEventsAsync unitOfWork)
{
    public async Task HandleAsync(Order order, CancellationToken cancellationToken)
    {
        await unitOfWork.GetRepository<Order>()
            .AddAsync(order, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesWithEventsAsync(cancellationToken);
    }
}
```

See [Repository](use-repository.md), [Services](use-service.md), and [EF Core Advanced](efcore-advanced.md).
