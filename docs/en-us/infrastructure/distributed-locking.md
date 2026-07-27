# Distributed Locking

Distributed locking coordinates access to a named resource across application instances. The module exposes `IDistributedLock`, named providers through `IDistributedLockFactory`, acquisition results, disposable lock handles, in-process metrics, and an ASP.NET Core health check.

## Install

The implementation is part of `Mvp24Hours.Infrastructure`:

```bash
dotnet add package Mvp24Hours.Infrastructure
```

`StackExchange.Redis`, `Microsoft.Data.SqlClient`, and `Npgsql` are dependencies of that project, so the Redis, SQL Server, and PostgreSQL provider types are available from the same package.

## Register providers

`AddDistributedLocking` registers `IDistributedLockFactory` and `DistributedLockMetrics` as singletons. Register at least one provider; resolving the factory with no providers throws `ArgumentException`.

```csharp
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using StackExchange.Redis;

IConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(
    builder.Configuration.GetConnectionString("Redis")!);

builder.Services.AddSingleton(redis);
builder.Services.AddDistributedLocking(locks =>
{
    locks.AddRedisProvider("Redis", redis);
    locks.AddInMemoryProvider("InMemory");
    locks.SetDefaultProvider("Redis");
});
```

Available registrations are:

| Provider | Registration | Important differences |
|---|---|---|
| In-memory | `AddInMemoryProvider(name = "InMemory")` | Process-local static lock state; no fenced token; suitable for tests, development, and a single application process only. |
| Redis | `AddRedisProvider(name, connection)` | Uses atomic `SET` with expiry and owner-checked Lua scripts for renewal/release. Returns a timestamp-based fenced token. |
| Redis quorum | `AddRedisRedLockProvider(name, connections)` | Requires a majority of the supplied Redis connections for acquisition and renewal. At least one connection is required. |
| SQL Server | `AddSqlServerProvider(name, connectionString, lockOwner = "Session", lockMode = "Exclusive")` | Uses `sp_getapplock`; no fenced token. The current implementation opens a separate connection for each operation, so verify ownership and release behavior under realistic load before production use. |
| PostgreSQL | `AddPostgreSqlProvider(name, connectionString, useSharedLock = false)` | Uses advisory-lock functions and a hash of the resource name; no fenced token. The current implementation also opens a separate connection for each operation, so verify the complete lifecycle before production use. |

The factory resolves names case-insensitively. `Create()` returns the configured default, or the first registered provider if no default was set.

```csharp
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;

IDistributedLockFactory factory =
    app.Services.GetRequiredService<IDistributedLockFactory>();

IDistributedLock defaultLock = factory.Create();
IDistributedLock inMemoryLock = factory.Create("inmemory");
```

## Acquire and release a lock

`TryAcquireAsync` returns `Acquired`, `Timeout`, or `Failed`. Unless `ThrowOnFailure` is enabled, contention and provider errors are represented by the result rather than thrown.

```csharp
using Mvp24Hours.Infrastructure.DistributedLocking.Options;

var options = DistributedLockOptions.ShortOperation;
var result = await defaultLock.TryAcquireAsync(
    "invoice-run:2026-07-27",
    options,
    cancellationToken);

if (result.IsAcquired && result.LockHandle is not null)
{
    await using var handle = result.LockHandle;

    // Keep all shared-state work inside the handle lifetime.
    await GenerateInvoicesAsync(cancellationToken);
}
else if (result.IsTimeout)
{
    logger.LogInformation("Another instance owns the invoice lock.");
}
else
{
    logger.LogError(result.Exception, "Lock acquisition failed: {Message}",
        result.ErrorMessage);
}
```

Always dispose the handle with `using` or `await using`. `ReleaseAsync` can release early, and `RenewAsync` can extend a held lock. A second release returns `false`; renewal after release also returns `false`. `IsLockedAsync` is only an observation and must not be used as a check-then-act synchronization primitive.

### Release behavior in 10.0.0

The 10.0.0 changelog and current `LockHandleBase` source verify a disposal fix: `Dispose` and `DisposeAsync` now call `ReleaseAsync` before setting `_disposed`. Previously, setting `_disposed` first caused `ReleaseAsync` to return without releasing the provider lock, leaving it held until expiry.

## `DistributedLockOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `AcquisitionTimeout` | `TimeSpan` | 30 seconds | Maximum time spent retrying acquisition. |
| `LockDuration` | `TimeSpan` | 5 minutes | Provider lock expiry or the handle's tracked validity period. |
| `EnableAutoRenewal` | `bool` | `false` | Starts a background renewal loop for the handle. |
| `RenewalInterval` | `TimeSpan` | 2 minutes | Delay between automatic renewal checks; keep it shorter than `LockDuration`. |
| `EnableFencing` | `bool` | `false` | Requests fenced-token behavior. Unsupported providers continue without a token. |
| `RetryDelay` | `TimeSpan` | 100 milliseconds | Delay between unsuccessful acquisition attempts. |
| `ThrowOnFailure` | `bool` | `false` | Throws `DistributedLockAcquisitionException` instead of returning timeout/failed results. |

### Presets

| Preset | Acquisition | Duration | Auto-renewal | Renewal | Fencing | Retry |
|---|---:|---:|---|---:|---|---:|
| `Default` | 30 s | 5 min | No | 2 min | No | 100 ms |
| `ShortOperation` | 5 s | 1 min | No | 2 min | No | 50 ms |
| `LongOperation` | 1 min | 10 min | Yes | 4 min | No | 200 ms |
| `CriticalOperation` | 2 min | 5 min | Yes | 2 min | Yes | 100 ms |
| `HighContention` | 5 s | 2 min | No | 2 min | No | 25 ms |

Presets return new mutable option instances. `CriticalOperation` requests fencing, but only the Redis implementation overrides fencing support. A fenced token must be enforced by the downstream shared resource to protect against stale writers; acquiring a token alone does not provide that enforcement.

## Health check

Register the provider first, then add its health check:

```csharp
using Mvp24Hours.Infrastructure.HealthChecks;

builder.Services.AddHealthChecks()
    .AddDistributedLockHealthCheck(
        name: "distributed-lock-redis",
        configureOptions: options =>
        {
            options.ProviderName = "Redis";
            options.LockTimeoutSeconds = 5;
            options.LockExpirationSeconds = 10;
        },
        timeout: TimeSpan.FromSeconds(8));
```

The check acquires a unique lock, calls `ReleaseAsync`, and records response data. Acquisition failure is unhealthy; a thrown release error is degraded; response thresholds can produce degraded or unhealthy status. The current check does not inspect a `false` return from `ReleaseAsync`, so monitor release metrics/logs separately when cleanup assurance matters.

### `DistributedLockHealthCheckOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `ProviderName` | `string?` | `null` | Named provider; null or empty selects the factory default. |
| `LockTimeoutSeconds` | `int` | `5` | Acquisition timeout used by the probe. |
| `LockExpirationSeconds` | `int` | `10` | Duration of the probe lock. |
| `DegradedThresholdMs` | `int` | `500` | Total response time at or above which the check is degraded. |
| `FailureThresholdMs` | `int` | `2000` | Total response time at or above which the check is unhealthy. |
| `Tags` | `IEnumerable<string>` | `["distributed-lock", "locking", "ready"]` | Default tags when registration does not supply tags. |

## Metrics and logs

`DistributedLockMetrics` is an in-process collector registered by `AddDistributedLocking`. Providers record acquisition attempts and some provider release paths. Query snapshots with `GetMetrics(resource)` or `GetAllMetrics()`, and reset them with `ResetMetrics(resource)` or `ResetAllMetrics()`.

```csharp
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;

var metrics = app.Services.GetRequiredService<DistributedLockMetrics>();
LockResourceMetrics? snapshot = metrics.GetMetrics("invoice-run:2026-07-27");
```

Snapshots include attempts, successes, failures, approximate timeout count, releases, average/max wait, contention rate, and success rate. This collector does not itself publish `System.Diagnostics.Metrics`; export snapshots through your application's chosen monitoring integration. Provider classes also emit structured `ILogger` messages for acquisition, renewal, release, and backend errors.

## Testing guidance

- Use `AddInMemoryProvider` for deterministic service-level tests that exercise the real acquisition result and handle lifecycle.
- Use a unique resource name per test because in-memory lock state is static within the process.
- Assert contention, timeout, explicit release, synchronous and asynchronous disposal, renewal, and cancellation.
- Test Redis quorum and owner-checked release against disposable Redis instances before relying on failure behavior.
- Treat SQL Server/PostgreSQL constructor and guard tests as unit coverage, not proof of cross-connection lock ownership; add environment-backed integration tests for the deployment topology.
- Keep test durations and retry delays short; do not use production presets in fast unit tests.

## Related

- [Infrastructure Modules](home.md)
- [CronJob resilience and locking](../cronjob-resilience.md)
- [CronJob advanced features](../cronjob-advanced.md)
- [Observability](../observability/home.md)
- [10.0.0 release notes](../release.md)
- [Redis distributed-lock pattern](https://redis.io/docs/latest/develop/clients/patterns/distributed-locks/)
