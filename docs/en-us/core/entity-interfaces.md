# Entity Interfaces

The Mvp24Hours.Core module provides a set of interfaces and base classes for building domain entities following DDD principles.

## Interface Hierarchy

```
IEntity<TId>
├── IAuditableEntity    - Tracks creation and modification
├── ISoftDeletable      - Supports logical deletion
├── ITenantEntity       - Multi-tenancy support
└── IVersionedEntity    - Optimistic concurrency
```

## IEntity&lt;TId&gt;

Base interface for all entities with a strongly-typed identifier.

```csharp
public interface IEntity<TId> where TId : IEquatable<TId>
{
    TId Id { get; }
}
```

### Usage

```csharp
public class Customer : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    public Customer(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
```

---

## IAuditableEntity

Tracks when and by whom an entity was created and modified.

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string ModifiedBy { get; set; }
}
```

### Usage

```csharp
public class Product : IEntity<Guid>, IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}
```

### Automatic Population with EF Core

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var currentUser = _currentUserProvider.UserId;
    var now = _clock.UtcNow;
    
    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUser;
                break;
            case EntityState.Modified:
                entry.Entity.ModifiedAt = now;
                entry.Entity.ModifiedBy = currentUser;
                break;
        }
    }
    
    return base.SaveChangesAsync(cancellationToken);
}
```

---

## ISoftDeletable

Supports logical (soft) deletion instead of physical deletion.

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

### Usage

```csharp
public class Document : IEntity<Guid>, ISoftDeletable
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    public void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
    
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
```

### Global Query Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Automatically exclude soft-deleted entities
    modelBuilder.Entity<Document>()
        .HasQueryFilter(d => !d.IsDeleted);
}

// To include deleted items
var allDocs = await _context.Documents
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## ITenantEntity

Enables multi-tenancy by associating entities with tenants.

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

### Usage

```csharp
public class Invoice : IEntity<Guid>, ITenantEntity
{
    public Guid Id { get; private set; }
    public string Number { get; private set; }
    public decimal Total { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; set; } = string.Empty;
}
```

### Automatic Tenant Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Invoice>()
        .HasQueryFilter(i => i.TenantId == _tenantProvider.TenantId);
}
```

---

## IVersionedEntity

Supports optimistic concurrency using a version/row version field.

```csharp
public interface IVersionedEntity
{
    byte[] RowVersion { get; set; }
}
```

### Usage

```csharp
public class Order : IEntity<Guid>, IVersionedEntity
{
    public Guid Id { get; private set; }
    public decimal Total { get; private set; }
    
    // Concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### EF Core Configuration

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.RowVersion)
    .IsRowVersion();
```

### Handling Concurrency Conflicts

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    throw new ConflictException(
        "The record was modified by another user",
        "CONCURRENCY_CONFLICT"
    );
}
```

---

## Base Classes

### EntityBase&lt;TId&gt;

Base class implementing `IEntity<TId>` with equality support.

```csharp
public class Customer : EntityBase<Guid>
{
    public string Name { get; private set; }
    
    public Customer(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
```

Features:
- Implements `IEquatable<EntityBase<TId>>`
- Equality based on Id
- Proper `GetHashCode()` implementation

### AuditableEntity&lt;TId&gt;

Combines entity identity with audit tracking.

```csharp
public class Product : AuditableEntity<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    
    public Product(string name, decimal price)
    {
        Id = Guid.NewGuid();
        Name = name;
        Price = price;
    }
}
```

### Convenience Classes

```csharp
// For Guid IDs
public class MyEntity : AuditableGuidEntity { }

// For int IDs
public class MyEntity : AuditableIntEntity { }

// For long IDs
public class MyEntity : AuditableLongEntity { }
```

### SoftDeletableEntity&lt;TId&gt;

Combines entity, audit, and soft delete.

```csharp
public class Article : SoftDeletableEntity<Guid>
{
    public string Title { get; private set; }
    public string Content { get; private set; }
}
```

---

## Combining Interfaces

You can combine multiple interfaces:

```csharp
public class TenantDocument : EntityBase<Guid>, 
    IAuditableEntity, 
    ISoftDeletable, 
    ITenantEntity,
    IVersionedEntity
{
    public string Title { get; private set; }
    
    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    
    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // ITenantEntity
    public string TenantId { get; set; } = string.Empty;
    
    // IVersionedEntity
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

---

## EF Core Interceptor Pattern

Use interceptors to automatically populate all interface fields:

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserProvider _userProvider;
    private readonly IClock _clock;
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return new ValueTask<InterceptionResult<int>>(result);
        
        var now = _clock.UtcNow;
        var userId = _userProvider.UserId;
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.ModifiedAt = now;
                    auditable.ModifiedBy = userId;
                }
            }
            
            if (entry.Entity is ISoftDeletable softDeletable && 
                entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = userId;
            }
        }
        
        return new ValueTask<InterceptionResult<int>>(result);
    }
}
```

## Best Practices

1. **Use appropriate interfaces** - Don't add audit fields if you don't need them
2. **Prefer base classes** - Use `AuditableEntity<TId>` instead of implementing everything manually
3. **Configure EF Core properly** - Set up query filters and interceptors
4. **Keep entities focused** - Entity should represent domain concepts, not infrastructure concerns

