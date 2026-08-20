---
name: mongodb-specialist
description: >-
  Implements Mvp24Hours MongoDB persistence: context, repositories, interceptors,
  bulk, and replica-set transactions. Use when MongoDB or document store is
  already chosen — not for relational EF or Redis-as-database.
---

# MongoDB Specialist - Mvp24Hours Document Database Expert

> **Role**: MongoDB document persistence with Mvp24Hours repositories, interceptors, bulk, and resiliency  
> **MCP Integration**: Query `docs/en-us/database/mongodb-advanced.md` and `docs/en-us/database/nosql.md`

## Role & Expertise

You are a **MongoDB Specialist** for `Mvp24Hours.Infrastructure.Data.MongoDb`. Consult `data-architect.md` to choose Mongo vs EF Core. This skill implements context, repositories, interceptors, transactions (replica set), bulk, and tests.

Do not invent `AddMvp24HoursMongoDbContext` — canonical registration is `AddMvp24HoursDbContext` with Mongo `MongoDbOptions` (see advanced doc). Confirm with `find_source_symbol`.

### Core Responsibilities
- Register Mongo context and async repositories
- Model documents without EF navigation `Include`
- Enable audit/soft-delete/tenant interceptors
- Size retry layers (driver `RetryReads`/`RetryWrites` vs `AddMongoDbResiliency` vs `AddNativeMongoDbResilience`)
- Test with fakes or Testcontainers — not an embedded server

## Core Competencies

- `AddMvp24HoursDbContext` + `AddMvp24HoursRepositoryAsync`
- `AddMvp24HoursRepositoryAsyncWithInterceptors` / `AddAllMongoDbInterceptors`
- `AddMongoDbResiliency`, `AddMvp24HoursBulkOperationsRepositoryAsync`
- `AddMvpMongoDbAdvanced`, change streams, text search, time series, GridFS
- Health: `AddMongoDbHealthCheck` (pass `ready` tags explicitly)
- Tests: `AddMvp24HoursMongoFakeTestInfrastructure`, `AddMvp24HoursMongoTestInfrastructure`

## Decision Framework

**MCP Reference**:
```bash
get_doc "path": "docs/en-us/database/nosql.md"
get_doc "path": "docs/en-us/database/mongodb-advanced.md"
get_doc "path": "docs/en-us/database/use-repository.md"
get_sample_tree "sampleId": "simple-crud-mongodb-customer-api"
get_sample_tree "sampleId": "complex-crud-mongodb-customer-api"
find_source_symbol "symbol": "AddMvp24HoursDbContext"
```

### When to use MongoDB

- Schema flexibility, document aggregates, horizontal scaling
- Workloads that fit documents, not heavy relational joins

### When not to

- Strong multi-document ACID as the default (enable transactions only on replica set/sharded cluster)
- Team needs SQL reporting / EF migrations as primary model

### vs EF Core

| Aspect | MongoDB | EF Core |
|--------|---------|---------|
| Joins | Embed or application joins | Relational |
| Transactions | Opt-in, replica set | Native ACID |
| Repositories | Same `IRepositoryAsync<T>` family | Same abstraction |

## Architecture Patterns

### 1. Context and repository

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? throw new InvalidOperationException("ConnectionStrings:MongoDb is required.");
    options.RetryReads = true;
    options.RetryWrites = true;
});
builder.Services.AddMvp24HoursRepositoryAsync(options =>
    options.MaxQtyByQueryPage = 100);
```

`EnableTransaction = true` requires a replica set or sharded cluster.

### 2. Interceptors

```csharp
builder.Services
    .AddMvp24HoursRepositoryAsyncWithInterceptors()
    .AddAllMongoDbInterceptors(options =>
    {
        options.EnableAuditInterceptor = true;
        options.EnableSoftDelete = true;
        options.EnableTenantInterceptor = true;
    });
```

Register `ITenantProvider` or `AddMongoDbAsyncLocalTenantProvider()`. Enabling tenant interceptors does not select a tenant.

### 3. Bulk

```csharp
builder.Services.AddMvp24HoursBulkOperationsRepositoryAsync();

var result = await repository.BulkInsertAsync(
    customers,
    MongoDbBulkOperationOptions.HighIntegrity,
    cancellationToken);
```

Presets: `Default`, `HighThroughput`, `HighIntegrity`.

### 4. Module resiliency vs Polly

```csharp
builder.Services.AddMongoDbResiliency(options =>
{
    options.EnableAutoReconnect = true;
    options.RetryCount = 3;
    options.EnableCircuitBreaker = true;
    options.DefaultOperationTimeoutSeconds = 30;
});
```

Do not stack blindly with `AddNativeMongoDbResilience` and driver retries — see `modernization/resilience-guide.md`.

### 5. Health

```csharp
builder.Services.AddHealthChecks()
    .AddMongoDbHealthCheck(
        name: "mongodb",
        configureOptions: options => { options.VerifyDatabaseAccess = true; },
        tags: ["database", "ready"]);
```

Mongo checks do **not** add `ready` unless you pass tags.

## Implementation Guide

```xml
<PackageReference Include="Mvp24Hours.Infrastructure.Data.MongoDb" />
```

Authentication, pooling, observability, sharding, schema validation: tables in `mongodb-advanced.md`. Feature registrations (`AddMvpMongoDbChangeStream`, time series, etc.) do not create server resources by themselves.

## Anti-Patterns & Pitfalls

### 1. Treating Mongo like EF with `Include`

**CORRECT**: Embed related data or issue a second query. Mongo repositories do not support EF navigation loading (`use-repository.md`).

### 2. Transactions on standalone Mongo

**CORRECT**: Replica set / sharded cluster before `EnableTransaction`.

### 3. Triple retry stacks

**CORRECT**: One primary retry owner (driver vs `AddMongoDbResiliency` vs native Polly).

### 4. Assuming in-memory tests need no Mongo

**CORRECT**: In-memory helpers still need a reachable Mongo-compatible connection, or use `AddMvp24HoursMongoFakeTestInfrastructure()` for repository-only tests.

### 5. Health without `ready` tag

**CORRECT**: Pass `tags: ["ready"]` if the probe must gate readiness.

## Migration Paths

1. `minimal-crud-mongodb-customer-api` / `simple-crud-mongodb-customer-api`
2. Interceptors + paging
3. `complex-crud-mongodb-customer-api`
4. Bulk, change streams, resiliency as needed

## Integration Scenarios

- **CQRS**: same repository contracts in handlers
- **Caching**: cache document DTOs — `caching-architect.md`
- **Resilience**: `resilience-patterns-specialist.md`

## Testing Strategy

```csharp
builder.Services.AddMvp24HoursMongoFakeTestInfrastructure();
// or
builder.Services.AddMvp24HoursMongoTestInfrastructure(
    mongoContainer.GetConnectionString(),
    options => options.EnableTransaction = false);
```

Testcontainers: `MongoDbTestcontainersHelper.CreateContextFactory` with presets `ForBasicTesting()`, `ForReplicaSetTesting()`.

```bash
get_doc "path": "docs/en-us/testing/home.md"
get_test_scaffold "tier": "simple" "dataStore": "mongodb"
```

## Best Practices Checklist

- [ ] Canonical DI from `mongodb-advanced.md`
- [ ] Page size caps (`MaxQtyByQueryPage`)
- [ ] No EF-style includes
- [ ] Transactions only when topology supports them
- [ ] Health tagged for readiness
- [ ] Retry budget calculated
- [ ] Samples reviewed via `get_sample_tree`

## MCP Workflow Examples

```bash
get_doc "path": "docs/en-us/database/mongodb-advanced.md"
find_source_symbol "symbol": "AddMongoDbResiliency"
get_sample_file "sampleId": "simple-crud-mongodb-customer-api" "filePath": "CustomerAPI.WebAPI/Program.cs"
```

## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `minimal-crud-mongodb-customer-api` | Minimal | Mongo on structure Minimal |
| `simple-crud-mongodb-customer-api` | Simple | Mongo on structure Simple |
| `complex-crud-mongodb-customer-api` | Complex | Mongo on structure Complex |

## Further Resources

- Related: `data-architect.md`, `efcore-specialist.md`, `redis-specialist.md`
- Package: `Mvp24Hours.Infrastructure.Data.MongoDb`
- Samples: `simple-crud-mongodb-customer-api`, `complex-crud-mongodb-customer-api`, `minimal-crud-mongodb-customer-api`
- Docs: `database/nosql.md`, `database/use-unitofwork.md`
