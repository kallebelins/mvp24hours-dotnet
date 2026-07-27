# Entities

Mvp24Hours repositories accept classes that implement `IEntityBase`. New domain models can use the strongly typed `IEntity<TId>` contract; `IEntityBase` remains the compatibility contract used by repositories.

## Basic entity

```csharp
using Mvp24Hours.Core.Contract.Domain.Entity;

public sealed class Customer : IEntity<Guid>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    object? IEntityBase.EntityKey => Id;
}
```

`EntityBase<TKey>` from `Mvp24Hours.Core.Entities` is the legacy convenience base class:

```csharp
public sealed class Customer : EntityBase<Guid>
{
    public required string Name { get; set; }
}
```

For EF Core, map `Id` normally with conventions, data annotations, or `IEntityTypeConfiguration<T>`. `EntityKey` is a CLR contract and does not need to be stored as a second column.

## Audit contracts

Two audit models exist:

- `IEntityDateLog` uses `Created`, `Modified`, and `Removed`. `Mvp24HoursContext` can populate these fields and filters rows whose `Removed` is not null when `CanApplyEntityLog` is enabled.
- `IEntityLog<TUserId>` adds `CreatedBy`, `ModifiedBy`, and `RemovedBy`.
- `IAuditableEntity` and `IAuditableEntity<TUserId>` use `CreatedAt`, `CreatedBy`, `ModifiedAt`, and `ModifiedBy`; use them with `AuditSaveChangesInterceptor`.

```csharp
public sealed class Order : EntityBase<int>, IAuditableEntity
{
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

## Soft delete

`ISoftDeletable` exposes `IsDeleted`, `DeletedAt`, and `DeletedBy`. Register `SoftDeleteInterceptor` and add its global filter; implementing the interface alone does not hide deleted rows.

```csharp
public sealed class Product : EntityBase<int>, ISoftDeletable
{
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteGlobalFilter();
}
```

## Tenant ownership

Use `ITenantEntity` for string tenant identifiers or `ITenantEntity<TTenantId>` for another identifier type. EF Core tenant filtering and automatic assignment require a registered `ITenantProvider`, query filters, and `TenantSaveChangesInterceptor`; MongoDB uses the matching MongoDB tenant interceptor.

```csharp
public sealed class Invoice : EntityBase<Guid>, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

See [Context](use-context.md), [Repository](use-repository.md), and [EF Core Advanced](efcore-advanced.md).
