# EF Core Advanced

Install `Mvp24Hours.Infrastructure.Data.EFCore`. The examples target .NET 10 and use public APIs exercised by `Mvp24Hours.Infrastructure.Data.EFCore.Test`.

## Complete registration

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
    options.UseSplitQueries = true;
});
```

### EFCoreRepositoryOptions

| Name | Type | Default | Description |
|---|---|---|---|
| MaxQtyByQueryPage | int | `ContantsHelper.Data.MaxQtyByQueryPage` | Maximum page size. |
| TransactionIsolationLevel | IsolationLevel? | `null` | Transaction isolation, or provider default. |
| DefaultTrackingBehavior | QueryTrackingBehavior | `TrackAll` | Default EF query tracking. |
| UseSplitQueries | bool | `false` | Uses split queries for includes. |
| EnableQueryTags | bool | `false` | Adds repository query tags. |
| QueryTagPrefix | string | `"Mvp24Hours"` | Prefix for generated tags. |
| EnableSensitiveDataLogging | bool | `false` | Includes parameter values in logs. |
| SlowQueryThresholdMs | int | `1000` | Slow-query threshold; `0` disables it. |
| StreamingBufferSize | int | `100` | Default streaming buffer size. |
| UseAutoMapperProjection | bool | `false` | Enables AutoMapper `ProjectTo` projections. |

## Resilience

`AddMvp24HoursDbContextWithResilience<TDbContext>` configures the SQL Server execution strategy, command timeout, and optional pooling. Register circuit breaker and pool monitoring explicitly when needed.

```csharp
builder.Services.AddMvp24HoursDbContextWithResilience<AppDbContext>(
    connectionString,
    options =>
    {
        options.EnableRetryOnFailure = true;
        options.MaxRetryCount = 6;
        options.CommandTimeoutSeconds = 30;
        options.EnableDbContextPooling = true;
    });

builder.Services.AddMvp24HoursDbContextResilienceInfrastructure(options =>
{
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;
});
```

### EFCoreResilienceOptions

| Name | Type | Default | Description |
|---|---|---|---|
| EnableRetryOnFailure | bool | `true` | Enables transient-failure retry. |
| MaxRetryCount | int | `6` | Maximum retry count. |
| MaxRetryDelaySeconds | int | `30` | Maximum delay between retries. |
| AdditionalTransientErrorNumbers | ICollection<int> | empty | Additional provider error numbers. |
| TransientExceptionTypes | ICollection<Type> | empty | Additional retryable exception types. |
| CommandTimeoutSeconds | int | `30` | Default command timeout. |
| ReadCommandTimeoutSeconds | int? | `null` | Read timeout override. |
| WriteCommandTimeoutSeconds | int? | `null` | Write timeout override. |
| BulkCommandTimeoutSeconds | int | `120` | Bulk timeout. |
| MigrationCommandTimeoutSeconds | int | `300` | Migration timeout. |
| EnableDbContextPooling | bool | `true` | Enables context pooling. |
| PoolSize | int | `1024` | Maximum retained contexts. |
| EnableCircuitBreaker | bool | `false` | Enables the database circuit breaker. |
| CircuitBreakerFailureThreshold | int | `5` | Failures before opening. |
| CircuitBreakerDurationSeconds | int | `30` | Open-state duration. |
| LogRetryAttempts | bool | `true` | Logs retries. |
| LogPoolStatistics | bool | `false` | Logs pool statistics. |
| PoolStatisticsLogIntervalSeconds | int | `60` | Pool-log interval. |

Presets are `Production()`, `Development()`, `AzureSql()`, and `NoResilience()`.

## Migrations

```csharp
builder.Services.AddMvp24HoursMigrationService<AppDbContext>(options =>
{
    options.AutoMigrateOnStartup = false;
    options.ThrowOnPendingMigrations = true;
    options.UseDistributedLock = true;
});
```

Use automatic migration cautiously in production. The available presets are `Development()`, `Staging()`, `Production()`, and `LogOnly()`.

Generate and apply migrations with the EF Core CLI against the infrastructure and host projects:

```bash
dotnet ef migrations add InitialCreate -p src/Product.Infrastructure -s src/Product.WebAPI
dotnet ef database update -p src/Product.Infrastructure -s src/Product.WebAPI
dotnet ef migrations script -p src/Product.Infrastructure -s src/Product.WebAPI -o migration.sql
```

For small static reference data, `HasData` in `OnModelCreating` is acceptable. Prefer dedicated seeders with `EnableDataSeeding` when seed logic is environment-specific or volume grows.

### Hybrid Dapper reads

When a hot read path needs SQL that EF Core does not express cleanly, open the EF connection and query with Dapper while still using EF for writes and unit of work:

```csharp
public sealed class OrderReadQueries(AppDbContext context)
{
    public async Task<IReadOnlyList<OrderSummaryDto>> GetActiveAsync(
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var rows = await connection.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(
                """
                SELECT Id, CustomerName, Total
                FROM Orders
                WHERE Active = 1
                """,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
```

Keep Dapper queries in the infrastructure/read side. Do not bypass the unit of work for writes that must participate in the same transaction as EF changes.

### MigrationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| AutoMigrateOnStartup | bool | `false` | Applies pending migrations at startup. |
| ThrowOnPendingMigrations | bool | `false` | Fails startup when migrations remain. |
| LogPendingMigrations | bool | `true` | Logs pending migrations. |
| MigrationTimeout | TimeSpan | `5 minutes` | Migration time limit. |
| UseTransactions | bool | `true` | Uses a transaction per migration. |
| EnsureDatabaseCreated | bool | `true` | Creates the database when absent. |
| EnableDataSeeding | bool | `false` | Runs registered seeders. |
| SeedOnlyOnMigration | bool | `true` | Seeds only after creation/migration. |
| SeedInTransaction | bool | `true` | Wraps seeding in a transaction. |
| ValidateSchemaBeforeMigration | bool | `false` | Validates before migration. |
| ValidateSchemaAfterMigration | bool | `false` | Validates after migration. |
| CreateSchemaSnapshot | bool | `false` | Creates a pre-migration snapshot. |
| SchemaSnapshotPath | string | `"./migrations/snapshots"` | Snapshot directory. |
| MaxSchemaSnapshots | int | `10` | Retained snapshots. |
| MaxRetryAttempts | int | `3` | Migration retries. |
| RetryDelay | TimeSpan | `5 seconds` | Initial retry delay. |
| UseExponentialBackoff | bool | `true` | Increases retry delay. |
| RetryableExceptions | ICollection<Type> | empty | Additional retryable exceptions. |
| UseDistributedLock | bool | `true` | Prevents concurrent migrations. |
| LockName | string | `"ef-core-migration-lock"` | Distributed lock name. |
| LockTimeout | TimeSpan | `5 minutes` | Lock acquisition timeout. |
| LockDuration | TimeSpan | `30 minutes` | Automatic lock expiry. |
| EnableDetailedLogging | bool | `true` | Logs migration details. |
| LogMigrationSql | bool | `false` | Logs migration SQL; may expose data. |
| EnableTelemetry | bool | `true` | Emits migration telemetry. |

## Schema validation

```csharp
builder.Services.AddMvp24HoursSchemaValidationOnStartup<AppDbContext>(
    SchemaValidationOptions.Production());
```

Presets are `Development()`, `Staging()`, `Production()`, and `ContinuousIntegration()`.

### SchemaValidationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| ValidateOnStartup | bool | `false` | Runs validation at startup. |
| ThrowOnValidationFailure | bool | `false` | Throws instead of logging. |
| CheckPendingMigrations | bool | `true` | Includes migration status. |
| ValidateTables | bool | `true` | Checks mapped tables. |
| ValidateColumns | bool | `false` | Checks columns and types. |
| ValidateIndexes | bool | `false` | Checks indexes. |
| ValidateForeignKeys | bool | `false` | Checks foreign keys. |
| ValidationTimeout | TimeSpan | `30 seconds` | Query timeout. |
| ExcludedTables | ICollection<string> | empty | Tables omitted from validation. |
| EnableDetailedLogging | bool | `true` | Logs validation progress. |
| CacheValidationResults | bool | `true` | Caches results. |
| CacheDuration | TimeSpan | `1 hour` | Result cache duration. |

## Read/write splitting

The registration below adds `IReplicaSelector`, scoped
`IConnectionResolver`, and `AppDbContext`. The context itself is created with
the primary SQL Server connection; selecting a replica does not transparently
replace the connection used by an already-created context.

```csharp
builder.Services.AddMvp24HoursReadWriteSplitting<AppDbContext>(options =>
{
    options.PrimaryConnectionString = primaryConnectionString;
    options.ReplicaConnectionStrings = [replica1, replica2];
    options.LoadBalancing = ReplicaLoadBalancing.RoundRobin;
    options.EnableReadAfterWriteConsistency = true;
});
```

Resolve the connection explicitly at the operation boundary. Notify the
resolver after a successful write when read-after-write consistency is enabled:

```csharp
public sealed class OrderConnectionRouter(IConnectionResolver resolver)
{
    public Task<string> GetReadConnectionAsync(CancellationToken cancellationToken) =>
        resolver.GetReadConnectionStringAsync(cancellationToken);

    public string GetWriteConnection() => resolver.GetWriteConnectionString();

    public void WriteCommitted() => resolver.NotifyWritePerformed();
}
```

`ForceReadFromPrimary()` and `ResetReadFromPrimary()` provide an explicit
consistency boundary. The resolver is scoped, so the read-after-write window is
also scoped; it is not a cross-request consistency guarantee.

Convenience registrations are:

```csharp
services.AddMvp24HoursSimpleReadWriteSplitting<AppDbContext>(
    primaryConnectionString,
    replicaConnectionString);

services.AddMvp24HoursAzureSqlGeoReplica<AppDbContext>(
    primaryConnectionString,
    replica1,
    replica2);
```

`AddMvp24HoursPostgreSqlStreaming<TContext>` configures the PostgreSQL preset,
but the current common registration still calls `UseSqlServer`. Do not use
that convenience method as a complete PostgreSQL `DbContext` registration
without reviewing and replacing the provider wiring.

`ReplicaSelector` supports `RoundRobin`, `Random`, `Weighted`,
`LeastLatency`, `LeastConnections`, and `Failover`. The option factories are
`SimpleSetup(...)`, `AzureSqlGeoReplica(...)`, and
`PostgreSqlStreaming(...)`.

### Current implementation boundaries

- `AutoDetectOperationType` is an option contract; the registration does not
  intercept EF operations to route reads and writes automatically.
- `EnableReplicaHealthChecks`, `HealthCheckInterval`, and
  `HealthCheckTimeout` do not register a background probe. Replica state
  changes through `MarkReplicaFailed(...)`, `MarkReplicaRecovered(...)`, and
  recovery timeout handling.
- `LeastLatency` and `LeastConnections` require state measurements, but the
  current public selector API has no measurement-update operation. Without
  supplied measurements, selection falls back to the first healthy candidate.
- `MaxReplicaRetries` and `RetryOnDifferentReplica` are configuration
  contracts; callers remain responsible for retrying failed operations and
  reporting replica state.
- When no healthy replica is returned, `IConnectionResolver` uses the primary
  connection. This happens even when the selector's
  `FallbackToPrimaryOnReplicaFailure` option is `false`.

### ReadWriteOptions

| Name | Type | Default | Description |
|---|---|---|---|
| PrimaryConnectionString | string | `""` | Primary/write connection. |
| ReplicaConnectionStrings | IList<string> | empty | Read replicas. |
| FallbackToPrimaryOnReplicaFailure | bool | `true` | Used by `ReplicaSelector` when no healthy replica remains. |
| LoadBalancing | ReplicaLoadBalancing | `RoundRobin` | Replica selection strategy. |
| ReplicaWeights | IList<int> | empty | Weights for weighted selection. |
| EnableReplicaHealthChecks | bool | `true` | Configuration contract; no background probe is registered. |
| HealthCheckInterval | TimeSpan | `30 seconds` | Configuration contract; unused by current probe code. |
| HealthCheckTimeout | TimeSpan | `5 seconds` | Configuration contract; unused by current probe code. |
| FailureThreshold | int | `3` | Failures before `MarkReplicaFailed` marks a replica unhealthy. |
| RecoveryTimeout | TimeSpan | `60 seconds` | Delay before retrying a previously unhealthy replica. |
| EnableReadAfterWriteConsistency | bool | `false` | Routes post-write reads to primary after `NotifyWritePerformed()`. |
| ReadAfterWriteWindow | TimeSpan | `5 seconds` | Primary-read window after a write. |
| AutoDetectOperationType | bool | `true` | Configuration contract; no automatic EF operation interception. |
| LogReplicaSelection | bool | `false` | Logs selection decisions. |
| MaxReplicaRetries | int | `2` | Configuration contract; callers own retry loops. |
| RetryOnDifferentReplica | bool | `true` | Configuration contract; callers own replica switching. |

Preset defaults differ: `AzureSqlGeoReplica` uses `LeastLatency` with a 10-second
read-after-write window; `PostgreSqlStreaming` keeps `RoundRobin` with a
2-second window. A CQRS alternative with separate read and write contexts is
`AddMvp24HoursCqrsDbContexts<TReadContext,TWriteContext>(...)`; those CQRS
interfaces are not the same as the unused ReadWriteSplitting marker interfaces.

## Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddMvp24HoursDbContextCheck<AppDbContext>("database", options =>
    {
        options.HealthQuery = "SELECT 1";
        options.CheckPendingMigrations = true;
    })
    .AddMvp24HoursDbContextLivenessCheck<AppDbContext>()
    .AddMvp24HoursDbContextReadinessCheck<AppDbContext>();
```

### DbContextHealthCheckOptions

| Name | Type | Default | Description |
|---|---|---|---|
| HealthQuery | string? | `"SELECT 1"` | Probe SQL; `null` checks connectivity. |
| QueryTimeoutSeconds | int | `5` | Probe timeout. |
| DegradedThresholdMs | int | `500` | Degraded latency. |
| FailureThresholdMs | int | `2000` | Unhealthy latency. |
| CheckPendingMigrations | bool | `false` | Checks pending migrations. |
| FailOnPendingMigrations | bool | `false` | Marks pending migrations as failure. |
| Tags | IEnumerable<string> | `db,database,efcore` | Health-check tags. |
| Name | string? | `null` | Optional check name. |
| FailureStatus | HealthStatus | `Unhealthy` | Complete-failure status. |

Presets are `SqlServer()`, `PostgreSql()`, `MySql()`, `Strict()`, and `Liveness()`.

## Interceptors and filters

The module includes `AuditSaveChangesInterceptor`, `SoftDeleteInterceptor`, `TenantSaveChangesInterceptor`, `ConcurrencyInterceptor`, `DomainEventSaveChangesInterceptor`, `SlowQueryInterceptor`, `CommandLoggingInterceptor`, and `StructuredLoggingInterceptor`. Register interceptors in DI and resolve them inside `AddDbContext`:

```csharp
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<SoftDeleteInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseSqlServer(connectionString)
        .AddInterceptors(
            serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>(),
            serviceProvider.GetRequiredService<SoftDeleteInterceptor>()));
```

Apply `ApplySoftDeleteGlobalFilter()` and `ApplyTenantQueryFilters(...)` in `OnModelCreating`; interceptors modify writes, while filters isolate reads.

## Field encryption

The real APIs are EF value converters: `HasEncryptedConversion(...)`, `HasEncryptedJsonConversion(...)`, and `ApplyEncryptedConverters(...)`. Supply an `IEncryptionProvider`; encrypted values generally cannot be queried by plaintext.

```csharp
string encryptionKey = builder.Configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption:Key is required.");

builder.Services.AddMvp24HoursEncryptionProvider(_ =>
    new AesEncryptionProvider(new EncryptionOptions
    {
        Key = encryptionKey,
        KeyId = "customer-data-v1"
    }));

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IEncryptionProvider encryptionProvider)
    : Mvp24HoursContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>()
            .Property(customer => customer.TaxId)
            .HasEncryptedConversion(encryptionProvider, maxPlainTextLength: 32);
    }
}
```

Other supported mappings are encrypted binary values through
`IExtendedEncryptionProvider` and encrypted JSON for reference types:

```csharp
modelBuilder.Entity<Customer>()
    .Property(customer => customer.Profile)
    .HasEncryptedJsonConversion(encryptionProvider);

modelBuilder.Entity<Customer>()
    .Property(customer => customer.PrivateDocument)
    .HasEncryptedConversion(extendedEncryptionProvider);
```

`EncryptedAttribute` can mark string properties before calling
`modelBuilder.ApplyEncryptedConverters(_encryptionProvider)`. Its
`CreateBlindIndex` and `BlindIndexPropertyName` properties are metadata only in
the current converter application; they do not create or maintain an index.

### EncryptionOptions

| Name | Type | Default | Description |
|---|---|---|---|
| Key | string | required | Base64 AES-256 key that decodes to exactly 32 bytes. |
| InitializationVector | string? | `null` | Fixed IV; otherwise a random IV is generated. |
| Deterministic | bool | `false` | Produces repeatable ciphertext with reduced security. |
| KeyId | string? | `null` | Key-rotation identifier. |
| BlindIndexSalt | string? | `null` | Salt for blind-index computation. |

`Key` is a required member in the .NET 10 source baseline. Keep it outside
source control and inject it from a secret provider. Wrong key length throws at
provider construction. A fixed `InitializationVector` or `Deterministic=true`
can reveal equality patterns; use either only after a threat-model review.
Changing a key, IV strategy, or provider does not re-encrypt existing rows, so
plan and test a data migration before rotation. See
[Secrets & Security](../infrastructure/secrets-security.md) for key management
and [migration encryption notes](../migration.md).

`AddMvp24HoursEncryptionProvider<TEncryptionProvider>()` and the factory
overload both register `IEncryptionProvider`. Binary encryption requires
`IExtendedEncryptionProvider`. `ComputeBlindIndex` exists on the extended
provider, but `[Encrypted].CreateBlindIndex` is not applied by
`ApplyEncryptedConverters`.

Value converters run on materialization and save. They do not make arbitrary
SQL predicates encryption-aware, and provider-side filtering, ordering, or
indexing over ciphertext will not behave like plaintext operations. This is
application-level AES through EF converters, not SQL Server Always Encrypted.

## Row-level security

`RowLevelSecurityHelper` generates SQL Server or PostgreSQL RLS scripts for `ITenantEntity` models. Apply generated scripts through a reviewed migration; generation does not execute DDL automatically. Runtime extensions are `SetTenantContextForSqlServerAsync(...)` and `SetTenantContextForPostgreSqlAsync(...)`.

Generate and review a provider-specific migration script:

```csharp
var helper = new RowLevelSecurityHelper();
string sql = helper.GenerateSqlServerRls<Order>("sales", "Orders");

// In a reviewed EF migration:
migrationBuilder.Sql(sql);
```

For all mapped `ITenantEntity` types, use
`GenerateRlsScriptsForModel(...)` or `GenerateCombinedRlsScript(...)`. Drop
helpers are available for both providers when a migration must remove a
policy.

Set the tenant on every opened database session before executing tenant-bound
commands:

```csharp
await dbContext.SetTenantContextForSqlServerAsync(
    tenantId,
    cancellationToken: cancellationToken);
```

The generated policies deliberately allow access when the session tenant value
is absent. They therefore do not provide fail-closed tenant isolation by
themselves. Review that predicate for your threat model, ensure pooled
connections cannot retain a previous tenant value, clear/reset context at the
operation boundary, and integration-test with the real database provider.
PostgreSQL tenant context is currently composed into a raw `SET` statement;
pass only trusted tenant identifiers until the implementation is parameterized.

## CQRS and domain events

```csharp
builder.Services.AddMvp24HoursEFCoreCqrs<AppDbContext>(
    (serviceProvider, options) => options.UseSqlServer(connectionString),
    cqrs =>
    {
        cqrs.UseDomainEventInterceptor = true;
        cqrs.UseUnitOfWorkWithEvents = true;
    });
```

For separate models, use `AddMvp24HoursCqrsDbContexts<TReadContext,TWriteContext>(...)`. Event-aware alternatives are `AddMvp24HoursUnitOfWorkWithEvents()` and `AddMvp24HoursRepositoryWithEvents()`.

## Testing

Integration tests use `AddDbContext<TContext>(options => options.UseInMemoryDatabase(...))`, followed by `AddMvp24HoursDbContext<TContext>()` and the repository registration. Provider-specific behavior, migrations, transactions, and raw SQL require a real provider or Testcontainers.

See [Context](use-context.md), [Repository](use-repository.md), [Unit of Work](use-unitofwork.md), and [CQRS](../cqrs/home.md).
