---
name: efcore-specialist
description: >-
  Implements Mvp24Hours EF Core: DbContext, Fluent API, repositories,
  specifications, interceptors, and migrations. Use when EF Core is already
  chosen — not to pick between SQL, Mongo, or Dapper.
---

# EF Core Specialist - Mvp24Hours Advanced Implementation Expert

> **Role**: EF Core mapping, queries, interceptors, and resilience inside Mvp24Hours repositories  
> **MCP Integration**: Query `docs/en-us/database/relational.md` and `docs/en-us/database/efcore-advanced.md`

## Role & Expertise

You are an **EF Core Specialist** for `Mvp24Hours.Infrastructure.Data.EFCore`. Consult `data-architect.md` to choose EF vs Mongo vs Dapper. This skill owns Fluent API, query shape, interceptors, and `AddMvp24HoursDbContext<TContext>`.

Keep EF types out of the domain. Confirm APIs with `find_source_symbol`.

### Core Responsibilities
- Register DbContext + `AddMvp24HoursDbContext<T>` + `AddMvp24HoursRepositoryAsync`
- Map with `IEntityTypeConfiguration<T>` (no data annotations on domain)
- Optimize reads (`AsNoTracking`, `Include`, split queries)
- Apply audit/soft-delete/tenant via Fluent API and interceptors
- Size retries: EF execution strategy vs `AddNativeDbResilience` (not both blindly)

## Core Competencies

- `AddMvp24HoursDbContext<TDbContext>`, optional `AddMvp24HoursDbContextWithResilience<T>`
- `IRepositoryAsync<T>` / `IUnitOfWorkAsync` — no fictional `AddMvp24HoursUnitOfWork()`
- CQRS events: `AddMvp24HoursUnitOfWorkWithEvents` when the sample/docs require it
- Global query filters, value conversions, owned types
- Samples: `simple-crud-ef-customer-api`, `complex-crud-ef-customer-api`, `simple-crud-ef-entitylog-customer-api`

## Decision Framework

**MCP Reference**:
```bash
get_doc "path": "docs/en-us/database/relational.md"
get_doc "path": "docs/en-us/database/efcore-advanced.md"
get_doc "path": "docs/en-us/database/use-repository.md"
get_sample_tree "sampleId": "complex-crud-ef-customer-api"
get_sample_tree "sampleId": "simple-crud-ef-entitylog-customer-api"
find_source_symbol "symbol": "AddMvp24HoursDbContext"
```

### When to use EF Core

- ACID, relationships, migrations, LINQ over SQL
- Change tracking for writes

### When not to

- Microsecond reads / heavy reporting SQL → Dapper (`dapper-specialist.md`, sample `simple-crud-ef-dapper-customer-api`)
- Document-shaped data → Mongo (`mongodb-specialist.md`)

### Query tactics

| Scenario | Tactic |
|----------|--------|
| Read-only | `AsNoTracking()` |
| One graph | `Include` |
| Multiple collections | `AsSplitQuery()` |
| Repeated SQL | compiled query or Dapper |
| Bulk | batching / provider bulk APIs — not N `SaveChanges` in a loop |

## Architecture Patterns

### 1. Registration

```csharp
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepositoryAsync();
```

Optional production retries/pooling: `AddMvp24HoursDbContextWithResilience<T>` — see `efcore-advanced.md` and `modernization/resilience-guide.md`. Do not wrap every command in an extra Polly pipeline on top of the EF execution strategy unless the budget is intentional.

### 2. Fluent configuration (domain stays clean)

```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email)
            .HasConversion(email => email.Value, value => new Email(value))
            .HasMaxLength(100);
        builder.OwnsOne(c => c.Address);
        builder.HasQueryFilter(c => !c.IsDeleted);
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
```

Apply configurations from the Infrastructure assembly in `OnModelCreating`.

### 3. Audit interceptor

```csharp
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in eventData.Context!.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditableEntity auditable)
                continue;
            if (entry.State == EntityState.Added)
                auditable.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                auditable.UpdatedAt = now;
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

Register with `options.AddInterceptors(...)`. Entity log sample: `simple-crud-ef-entitylog-customer-api` / `complex-crud-ef-entitylog-customer-api`. Prefer `TimeProvider` over `DateTime.UtcNow` in new code (`dotnet-modernization-specialist.md`).

### 4. Handler usage

```csharp
public async Task<CustomerDto?> Handle(GetCustomerByIdQuery query, CancellationToken ct)
{
    var customer = await repository.GetByIdAsync(query.Id, ct);
    return customer is null ? null : Map(customer);
}
```

Reads through specifications/LINQ should use no-tracking APIs when the repository exposes them. Confirm `GetByIdAsync` tracking behavior in `use-repository.md` before assuming `AsNoTracking`.

```csharp
var rows = await context.Customers
    .AsNoTracking()
    .AsSplitQuery()
    .Where(c => c.IsActive)
    .Select(c => new CustomerListDto(c.Id, c.Name))
    .ToListAsync(ct);
```

Prefer this shape in query handlers or a dedicated read repository — not in domain entities.

## Anti-Patterns & Pitfalls

### 1. EF attributes on domain entities

**CORRECT**: `IEntityTypeConfiguration<T>` in Infrastructure.

### 2. N+1 queries

**WRONG**: loop `GetById` per child.

**CORRECT**: `Include` / split query / explicit projection to DTO.

### 3. Tracking on read-only lists

**CORRECT**: `AsNoTracking()` (or repository no-track helpers).

### 4. Loading entire aggregates for a name field

**CORRECT**: project to DTO / Dapper for that query.

### 5. Double retry stacks

**CORRECT**: execution strategy **or** `AddNativeDbResilience` around specific ops — `resilience-patterns-specialist.md`.

## Migration Paths

1. `minimal-crud-ef-customer-api`
2. Fluent configs + repository (`simple-crud-ef-customer-api`)
3. Interceptors / entity log samples
4. Complex CRUD + specifications
5. CQRS handlers (`complex-cqrs-ef-customer-api`)

## Integration Scenarios

- **CQRS**: UoW in command handlers; queries may skip tracking
- **Caching**: cache DTOs, not tracked entities — `caching-architect.md`
- **Testing**: InMemory does not prove SQL/FKs — `testing-architect.md`

## Testing Strategy

```bash
get_doc "path": "docs/en-us/testing/home.md"
get_test_scaffold "tier": "simple" "dataStore": "efcore"
```

```csharp
services.AddLogging();
services.AddMvp24HoursTestInfrastructure<AppDbContext>("CustomersTest");
```

- Unit: `RepositoryFake` / NSubstitute on `IRepositoryAsync<T>`
- Integration: real provider or Testcontainers; InMemory is not relational proof
- Configuration tests: `IModel` from `db.Model` for keys/filters

## Best Practices Checklist

- [ ] No EF attributes in Core/Domain
- [ ] `AddMvp24HoursDbContext<T>` + `AddMvp24HoursRepositoryAsync`
- [ ] Indexes for filter/FK columns
- [ ] Query filters for soft delete/tenant
- [ ] `AsNoTracking` on reads
- [ ] Interceptors registered once
- [ ] One retry owner
- [ ] Samples reviewed via MCP

## MCP Workflow Examples

```bash
get_doc "path": "docs/en-us/database/efcore-advanced.md"
find_source_symbol "symbol": "AddMvp24HoursDbContextWithResilience"
find_source_symbol "symbol": "AddMvp24HoursUnitOfWorkWithEvents"
get_sample_file "sampleId": "complex-crud-ef-customer-api" "filePath": "CustomerAPI.WebAPI/Program.cs"
```

## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix. CQRS/DDD EF samples are **Blueprint**, not Complex N-Layers.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `minimal-crud-ef-customer-api` | Minimal | Single-host EF |
| `simple-crud-ef-customer-api` | Simple | Layered EF |
| `simple-crud-ef-dapper-customer-api` | Simple | Dapper alongside EF |
| `simple-crud-ef-entitylog-customer-api` | Simple | Entity log |
| `complex-crud-ef-customer-api` | Complex | Modular EF |
| `complex-crud-ef-dapper-customer-api` | Complex | Dapper on Complex |
| `complex-crud-ef-entitylog-customer-api` | Complex | Entity log on Complex |
| `complex-crud-ef-only-entity-customer-api` | Complex | Entity-only persistence |
| `complex-cqrs-ef-customer-api` | Blueprint | EF under CQRS blueprint |

## Further Resources

- Related: `data-architect.md`, `ddd-specialist.md`, `cqrs-architect.md`, `testing-architect.md`
- Package: `Mvp24Hours.Infrastructure.Data.EFCore`
- Docs: `database/use-unitofwork.md`, `modernization/resilience-guide.md`
