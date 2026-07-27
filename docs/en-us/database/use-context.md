# Database Context

Register the provider-specific EF Core `DbContext` first, then expose it to Mvp24Hours with `AddMvp24HoursDbContext<TDbContext>()`. The Mvp24Hours extension does not configure a database provider.

## Define the context

```csharp
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : Mvp24HoursContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

Inherit directly from `DbContext` if the legacy `IEntityDateLog` automation is not needed.

## Register EF Core and repositories

This setup matches the SQL integration tests:

```csharp
string connectionString = builder.Configuration.GetConnectionString("DataContext")
    ?? throw new InvalidOperationException("ConnectionStrings:DataContext is required.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddMvp24HoursDbContext<AppDbContext>();
builder.Services.AddMvp24HoursRepositoryAsync(options =>
{
    options.MaxQtyByQueryPage = 100;
    options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
});
```

Use `AddMvp24HoursRepository(...)` for the synchronous contracts. Both registrations are scoped by default.

## Legacy date-log behavior

`Mvp24HoursContext` applies its `IEntityDateLog` rules only when explicitly enabled:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : Mvp24HoursContext(options)
{
    public override bool CanApplyEntityLog => true;
    public override object? EntityLogBy => "system";

    public DbSet<CustomerLog> Customers => Set<CustomerLog>();
}
```

This path uses `Created`, `Modified`, and `Removed`. For the newer `IAuditableEntity`, `ISoftDeletable`, and tenant contracts, use the tested interceptors described in [EF Core Advanced](efcore-advanced.md).

## Context lifetime

Treat a context as one unit-of-work scope. Do not keep it in a singleton or share it across concurrent operations. In web applications, the default scoped lifetime creates one instance per request.

See [Entities](use-entity.md), [Unit of Work](use-unitofwork.md), and [Relational Databases](relational.md).
