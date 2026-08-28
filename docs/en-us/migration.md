# Migration guide

Use the section matching your installed package version. This page covers
version upgrades; migrations from legacy Mvp24Hours abstractions to native .NET
APIs live in the [.NET 9+ modernization guide](modernization/migration-guide.md).

## 9.1.x → 10.8.0

> **Package availability:** the repository and changelog describe 10.8.0, but
> production package metadata remains at `9.1.21` and the public
> `Mvp24Hours.Core` feed has no 10.8.0 package. Complete the preparation and
> validation steps below, but do not request 10.8.0 until publication is
> confirmed.

### 1. Prepare the toolchain

1. Install the .NET 10 SDK.
2. Change application and test projects to `<TargetFramework>net10.0</TargetFramework>`.
3. Enable Nullable in consumer projects if it is not already enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

4. Build once before changing package versions and record the current warning
   and test baseline.

### 2. Upgrade packages together

Keep all Mvp24Hours packages on the same release. After 10.8.0 is published:

```bash
dotnet add package Mvp24Hours.Core --version 10.8.0
dotnet add package Mvp24Hours.Application --version 10.8.0
dotnet add package Mvp24Hours.Infrastructure --version 10.8.0
dotnet add package Mvp24Hours.Infrastructure.Cqrs --version 10.8.0
dotnet add package Mvp24Hours.WebAPI --version 10.8.0
```

Install only the modules your application uses. Restore and verify that no
transitive reference selects an older Mvp24Hours package.

### 3. Resolve compiler changes

Review new Nullable diagnostics instead of suppressing them globally. Public
signatures were corrected where runtime values can be null, so existing callers
may need null checks, nullable annotations, or explicit fallback behavior.

Two object-initializer members are now required:

```csharp
var propertyCall = new SetPropertyCall
{
    Property = entity => entity.Name,
    Value = "Updated"
};

var encryption = new EncryptionOptions
{
    Key = keyFromASecretProvider
};
```

Do not embed encryption keys in source or configuration committed to version
control.

### 4. Review behavior changes

#### SMTP certificate validation

`SmtpEmailOptions.ServerCertificateValidationCallback` is ignored in 10.8.0.
The old implementation relied on the obsolete process-wide
`ServicePointManager.ServerCertificateValidationCallback`. SMTP now uses the
operating system trust store and logs a warning if the callback is configured.

- Remove the callback from application configuration.
- Install the required CA/intermediate certificates in the host trust store.
- Test TLS negotiation against the production SMTP server.
- Do not replace this with a global certificate bypass.

#### SQL Server client

SQL Server distributed locking and EF Core helpers use
`Microsoft.Data.SqlClient`. If consumer code handles provider-specific
connections, exceptions, parameters, or connection-string options, replace
`System.Data.SqlClient` types and retest authentication and encryption defaults.

#### AWS credential resolution

AWS Secrets Manager now uses the AWS SDK v4 default credentials identity
resolver. Test every deployed identity source—environment variables, shared
profiles, workload identity, ECS task roles, or EC2 instance roles—and do not
assume the previous fallback-chain ordering.

#### Encryption compatibility

Password-based key derivation now uses static
`Rfc2898DeriveBytes.Pbkdf2`. `CHANGELOG.md` reports byte-for-byte equivalence,
and source has key-derivation coverage, but there is no dedicated named
ciphertext-compatibility regression suite in `src/Tests`. Treat existing
ciphertext as a consumer verification step before rollout:

1. decrypt representative values with the old release;
2. decrypt the same values with 10.8.0;
3. encrypt new values with 10.8.0 and verify round trips;
4. retain a tested rollback and key-recovery procedure.

#### Soft delete (EF Core)

`Mvp24HoursContext.ApplyLogRules` is deprecated in 10.8.0 and will be removed in
v12. Nothing changes at runtime: `SaveChanges` still calls it, and
`CanApplyEntityLog` still gates it, so the legacy path keeps working until the
removal. The deprecation exists because the EF Core module carries two
independent soft-delete mechanisms that target different interfaces and never
interact.

| Aspect | `ApplyLogRules` (legacy) | `SoftDeleteInterceptor` (recommended) |
| --- | --- | --- |
| Interface | `IEntityDateLog` / `IEntityLog<T>` / `EntityBaseLog<,>` | `ISoftDeletable` / `ISoftDeletable<T>` |
| Fields | `Created`, `Modified`, `Removed` (plus `CreatedBy`, `ModifiedBy`, `RemovedBy`) | `IsDeleted`, `DeletedAt`, `DeletedBy` |
| Converts `Deleted` to soft delete? | No. `ApplyLogRules` takes no action on `EntityState.Deleted`; `Repository.Remove` performs the conversion by setting `Removed` and calling `Modify` | Yes, in `SavingChanges`/`SavingChangesAsync` |
| Read filter | `ApplyGlobalFilters<IEntityDateLog>(e => e.Removed == null)`, applied automatically by `Mvp24HoursContext.OnModelCreating` when `CanApplyEntityLog` is true | `ApplySoftDeleteGlobalFilter()`, which you call yourself in `OnModelCreating`. It covers the non-generic `ISoftDeletable` only |
| User source | `EntityLogBy` (override on the context) | `ICurrentUserProvider` (DI, optional) |
| Time source | `TimeZoneHelper.GetTimeZoneNow()` (configured time zone) | `IClock` (DI, optional), falling back to `DateTime.UtcNow` |

Because the two mechanisms read different properties, deprecating one does not
migrate your entities. Migrating means changing the entity contract and the
stored data, so plan it per aggregate:

1. register the interceptor and wire it into the context:

```csharp
builder.Services.AddMvp24HoursEFCoreSoftDeleteInterceptor();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<SoftDeleteInterceptor>()));
```

2. apply the read filter in the context, otherwise soft-deleted rows keep
   showing up in queries:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteGlobalFilter();
}
```

3. change the entity from `IEntityDateLog`/`IEntityLog<T>` to `ISoftDeletable`,
   add a migration for `IsDeleted`/`DeletedAt`/`DeletedBy`, and backfill
   `IsDeleted = Removed IS NOT NULL` before dropping the legacy columns;
4. audit-field stamping (`Created`/`Modified`) is not part of
   `SoftDeleteInterceptor`. Pair it with `AuditSaveChangesInterceptor` and
   `IAuditableEntity` (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`).

Two limitations are worth knowing before you commit to the interceptor:
`ApplySoftDeleteGlobalFilter()` skips entities that implement only
`ISoftDeletable<TUserId>` (that interface does not inherit `ISoftDeletable`), and
the interceptor writes a `string` into `DeletedBy`, so `ISoftDeletable<TUserId>`
with a non-string `TUserId` needs its own handling. Entities that stay on
`IEntityDateLog` are unaffected by either limitation.

#### Static helpers replaced by DI

Three static helpers that carried process-wide mutable state were retired. None of
them was ever registered in or resolved from the container, so replacing them is a
call-site change, not a wiring change.

| Helper | 10.8.0 status | Replacement |
| --- | --- | --- |
| `TelemetryHelper` (`Mvp24Hours.Core`) | **Removed** | `ILogger<T>` plus the OpenTelemetry surface in `Mvp24Hours.Core.Observability` — see [Telemetry](telemetry.md) |
| `TimeZoneHelper` (`Mvp24Hours.Infrastructure`) | `[Obsolete]`, removal in v12 | `IClock` (`Mvp24Hours.Core.Contract.Infrastructure`) or `TimeProvider` |
| `ConfigurationHelper` (`Mvp24Hours.Infrastructure`) | `[Obsolete]`, removal in v12 | `IConfiguration` / `IOptions<T>` bound at the host — see [Configuration reference](configuration-reference.md) |

`AddMvp24HoursTimeZone(clearList, ids)` is `[Obsolete]` for the same reason: it
registers nothing in the container. It only mutates the static
`TimeZoneHelper.TimeZoneIds` list, and the helper caches the resolved
`TimeZoneInfo` on the first call — so calling it after the first
`GetTimeZoneNow()` has no effect at all.

**`IClock` is not a drop-in replacement for `TimeZoneHelper`.**
`GetTimeZoneNow()` returns the first system timezone matching `TimeZoneIds`
(`E. South America Standard Time`, `Brazil/East`, `America/Sao_Paulo` by default)
regardless of the machine's local timezone. The default clock registrations
(`SystemClock`, `AddTimeProvider()`, `AddSystemClock()`) use `TimeZoneInfo.Local`.
The two agree only when both offsets happen to match, so register the timezone
explicitly to preserve the current values:

```csharp
// Before
services.AddMvp24HoursTimeZone(clearList: true, "America/Sao_Paulo");
DateTime now = TimeZoneHelper.GetTimeZoneNow();

// After — Program.cs
TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
builder.Services.AddTimeProvider(TimeProvider.System, zone);   // registers IClock and TimeProvider

// After — consumer
public sealed class MyService(IClock clock)
{
    public DateTime Now => clock.Now;      // same zone as the helper
    public DateTime UtcNow => clock.UtcNow;
}
```

Prefer `clock.UtcNow` for persisted timestamps and convert on display. Inside
Mvp24Hours, the remaining `TimeZoneHelper` calls all belong to the legacy
`IEntityDateLog` stamping path (`Repository.Remove`, `RepositoryAsync.RemoveAsync`,
`Mvp24HoursContext.ApplyLogRules`) plus the no-`IClock` fallback in the MongoDB
`AuditInterceptor`/`SoftDeleteInterceptor`. They keep the current behavior and are
suppressed locally; registering an `IClock` already takes over the two MongoDB
interceptors.

### 5. Audit dependencies and build strictly

```bash
dotnet restore
dotnet list package --vulnerable --include-transitive
dotnet build --configuration Release /p:TreatWarningsAsErrors=true
```

The 10.8.0 source pins `System.Security.Cryptography.Xml` to `10.0.10`. Do not
downgrade or remove that direct pin without confirming that the transitive
`System.ServiceModel` dependency is patched.

### 6. Run the integration checklist

- [ ] Unit tests pass on .NET 10.
- [ ] SQL Server/PostgreSQL/MySQL repository and migration tests pass.
- [ ] MongoDB, Redis, and RabbitMQ integrations pass where used.
- [ ] SMTP TLS succeeds with the operating system trust store.
- [ ] AWS Secrets Manager resolves credentials in every deployment environment.
- [ ] Existing encrypted data decrypts and new ciphertext round-trips.
- [ ] Distributed locks release on both `Dispose` and `DisposeAsync`.
- [ ] Health, readiness, and liveness endpoints pass.
- [ ] Release build completes with warnings treated as errors.
- [ ] The vulnerable-package audit is clean.

## 9.0.x → 9.1.x

Version 9.1 introduced the Mvp24Hours Mediator and expanded observability and
infrastructure modules. Register CQRS with the APIs that exist in the current
source:

```csharp
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors();
});
```

Commands implement `IMediatorCommand<TResponse>` and handlers implement
`IMediatorCommandHandler<TCommand, TResponse>` (a semantic alias of
`IMediatorRequestHandler<TRequest, TResponse>`):

```csharp
public sealed record CreateOrderCommand(string CustomerId)
    : IMediatorCommand<OrderResult>;

public sealed class CreateOrderHandler
    : IMediatorCommandHandler<CreateOrderCommand, OrderResult>
{
    public Task<OrderResult> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Create and persist the order.
        throw new NotImplementedException();
    }
}
```

Choose behavior groups explicitly when needed:

```csharp
builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithObservabilityBehaviors();
    options.WithAuditBehavior(auditAllCommands: true);
    options.WithSecurityBehaviors();
    options.WithResiliencyBehaviors();
});
```

See [CQRS Getting Started](cqrs/getting-started.md) for the canonical API guide.

For Telemetry, HTTP/database resilience, cache, Pipeline, OpenAPI, time, and
Options transitions, use the
[.NET 9+ modernization guide](modernization/migration-guide.md). Detailed
Telemetry steps remain in the
[legacy telemetry migration](observability/migration.md) — `TelemetryHelper`,
`AddMvp24HoursTelemetry*`, `ITelemetryService`, and `TelemetryLevels` were
**removed** in 10.8.0, so that migration is now mandatory rather than optional.

## 8.x → 9.x

1. Move the application to .NET 9.
2. Replace legacy Telemetry with `ILogger<T>` and OpenTelemetry.
3. Prefer native resilience, HybridCache, TimeProvider, native rate limiting,
   Channels, ProblemDetails, and Native OpenAPI where they fit.
4. Retest background services after moving timer logic to `PeriodicTimer`.

The task-oriented procedures are maintained in the
[.NET 9+ modernization guide](modernization/migration-guide.md); they are not
duplicated here.

## 4.x → 8.x

The following source migrations remain relevant for older applications.

### EntityBase

```csharp
// Before
public class MyEntity : EntityBase<MyEntity, int>

// After
public class MyEntity : EntityBase<int>
```

### IMapFrom

```csharp
// Before
public class MyDto : IMapFrom<MyEntity>

// After
public class MyDto : IMapFrom
```

### Mapping

Inject `IMapper`; replace the old singleton helper and `MapTo` calls:

```csharp
public sealed class MyService(IMapper mapper)
{
    public MyEntity Map(MyDto dto) => mapper.Map<MyEntity>(dto);
}
```

### Service access and startup

- Replace `ServiceProviderHelper` and static facades with constructor injection.
- Remove the obsolete `UseMvp24Hours()` startup call.
- Move `Startup.cs` registration to the minimal hosting model when the target
  ASP.NET Core version supports it.

### MongoDB class maps

For applications predating 4.2.101, remove the generic type from
`IBsonClassMap<T>` implementations and implement `IBsonClassMap`.

## Using samples with published NuGet packages

The [`samples/`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/samples) folder defaults to **ProjectReference** mode so in-repo development tracks unreleased library changes. After Mvp24Hours **10.8.0** (or a matching release) is published, validate standalone consumption as follows.

### 1. Flip the MSBuild switch

From the repository root or `samples/` directory:

```bash
dotnet build samples/Mvp24Hours.Samples.slnx \
  -p:Mvp24HoursUseProjectReferences=false \
  -p:Mvp24HoursPackageVersion=10.8.0
```

Each sample `.csproj` already declares conditional `PackageReference` entries; the properties above disable local project references and pin the NuGet version through Central Package Management.

### 2. Verify restore from nuget.org

1. Clone the repository to a clean directory (or temporarily rename `src/Mvp24Hours.*` folders) so MSBuild cannot resolve local projects.
2. Run `dotnet restore samples/Mvp24Hours.Samples.slnx -p:Mvp24HoursUseProjectReferences=false -p:Mvp24HoursPackageVersion=10.8.0`.
3. Confirm every `Mvp24Hours.*` package resolves from nuget.org (or your private feed) at the expected version — no 4.x, 8.x, or 9.x packages in the dependency graph.

### 3. Smoke-test one sample

Pick a representative host (for example `complex-crud-ef-customer-api` or `complex-cqrs-ef-customer-api`), build its per-sample `.slnx`, and run its health endpoint or unit tests. Document any feed URL or authentication requirements in your organization's internal runbook.

> **Until publication:** keep `Mvp24HoursUseProjectReferences=true` (the default). Do not commit `false` as the repository default until package availability is confirmed in [Release notes](release.md).

## Related resources

- [Release notes](release.md)
- [Mvp24Hours samples catalog](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/samples)
- [.NET 9+ native API modernization](modernization/migration-guide.md)
- [Observability migration](observability/migration.md)
- [CQRS Getting Started](cqrs/getting-started.md)
- [Native OpenAPI](modernization/native-openapi.md)
