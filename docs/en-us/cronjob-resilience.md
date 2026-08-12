# CronJob resilience

Use `ResilientCronJobService<T>` with `AddResilientCronJob` or `AddResilientCronJobWithOptions`. Per-job shortcut properties shared with `CronJobOptions<T>` are listed in [advanced configuration](cronjob-advanced.md); this page owns the complete `CronJobResilienceConfig<T>` reference.

## Register a resilient job

```csharp
services.AddResilientCronJob<ImportJob>(config =>
{
    config.CronExpression = "*/5 * * * *";
    config.TimeZoneInfo = TimeZoneInfo.Utc;
    config.Resilience.EnableRetry = true;
    config.Resilience.MaxRetryAttempts = 3;
    config.Resilience.EnableCircuitBreaker = true;
    config.Resilience.PreventOverlapping = true;
    config.Resilience.ExecutionTimeout = TimeSpan.FromMinutes(4);
});
```

Option-based registration is bindable and startup-validated:

```csharp
services.AddResilientCronJobWithOptions<ImportJob>(options =>
{
    options.CronExpression = "*/5 * * * *";
    options.TimeZone = "UTC";
    options.EnableRetry = true;
    options.EnableCircuitBreaker = true;
});
```

## `CronJobResilienceConfig<T>`

| Name | Type | Default | Description |
|---|---|---|---|
| `EnableRetry` | `bool` | `false` | Enables retry after a failed execution. |
| `MaxRetryAttempts` | `int` | `3` | Maximum configured attempts. |
| `RetryDelay` | `TimeSpan` | `1 s` | Initial delay. |
| `UseExponentialBackoff` | `bool` | `true` | Increases delay exponentially. |
| `MaxRetryDelay` | `TimeSpan` | `30 s` | Backoff ceiling. |
| `RetryJitterFactor` | `double` | `0.2` | Random delay factor. |
| `ShouldRetryOnException` | `Func<Exception, bool>?` | `null` | Optional retry filter. |
| `EnableCircuitBreaker` | `bool` | `false` | Enables the circuit breaker. |
| `CircuitBreakerFailureThreshold` | `int` | `5` | Failures before opening. |
| `CircuitBreakerDuration` | `TimeSpan` | `30 s` | Open-state duration. |
| `CircuitBreakerSuccessThreshold` | `int` | `1` | Half-open successes required to close. |
| `CircuitBreakerSamplingDuration` | `TimeSpan` | `60 s` | Failure sampling window. |
| `PreventOverlapping` | `bool` | `true` | Prevents concurrent execution in one process. |
| `LogOverlappingSkipped` | `bool` | `true` | Logs skipped overlaps. |
| `OverlappingWaitTimeout` | `TimeSpan` | `TimeSpan.Zero` | Local-lock wait; zero skips immediately. |
| `EnableDistributedLocking` | `bool` | `false` | Enables distributed lock acquisition before each resilient execution. |
| `DistributedLockDuration` | `TimeSpan` | `5 min` | Lease duration requested for the distributed execution lock. |
| `DistributedLockWaitTimeout` | `TimeSpan` | `1 s` | Maximum wait while acquiring distributed lock before skipping. |
| `DistributedLockInstanceId` | `string?` | `null` | Optional lock owner ID; machine/process fallback when omitted. |
| `GracefulShutdownTimeout` | `TimeSpan` | `30 s` | Maximum shutdown wait. |
| `WaitForExecutionOnShutdown` | `bool` | `true` | Waits for an active execution. |
| `PropagateCancellation` | `bool` | `true` | Passes cancellation into job work. |
| `ExecutionTimeout` | `TimeSpan?` | `null` | Optional execution deadline. |
| `OnRetry` | `Action<Exception,int,TimeSpan>?` | `null` | Retry callback. |
| `OnCircuitBreakerStateChange` | `Action<CircuitBreakerState,CircuitBreakerState>?` | `null` | State-change callback. |
| `OnOverlappingSkipped` | `Action?` | `null` | Overlap callback. |
| `OnJobFailed` | `Action<Exception>?` | `null` | Terminal failure callback. |

Factory methods are `Default()`, `WithRetry(...)`, `WithCircuitBreaker(...)`, and `FullResilience()`.

## Local versus distributed overlap prevention

`ICronJobExecutionLock` and `PreventOverlapping` protect one process. Cluster deployments can enable resilient distributed locking through `EnableDistributedLocking` and a production `IDistributedCronJobLock` registered via `AddCronJobDistributedLock<TLock>()`.

```csharp
services.AddCronJobAdvancedInfrastructure(o => o.UseDistributedLocking = true);
services.AddCronJobDistributedLock<RedisCronJobLock>();
services.AddResilientCronJob<ImportJob>(o =>
{
    o.CronExpression = "*/5 * * * *";
    o.Resilience.EnableDistributedLocking = true;
    o.Resilience.DistributedLockDuration = TimeSpan.FromMinutes(5);
    o.Resilience.DistributedLockWaitTimeout = TimeSpan.FromSeconds(2);
});
```

Do not describe the built-in `InMemoryDistributedCronJobLock` as cluster safe.

## How to use the resilient distributed lock

Use this checklist in production:

- Register distributed-lock infrastructure and your implementation.
- Enable resilient distributed locking per job.
- Tune lease and wait timeout to your execution profile.
- Optionally set a stable `DistributedLockInstanceId` per node.

```csharp
services.AddCronJobAdvancedInfrastructure(o => o.UseDistributedLocking = true);
services.AddCronJobDistributedLock<RedisCronJobLock>();

services.AddResilientCronJobWithOptions<ImportJob>(o =>
{
        o.CronExpression = "*/5 * * * *";
        o.TimeZone = "UTC";
        o.EnableDistributedLocking = true;
        o.DistributedLockExpiry = TimeSpan.FromMinutes(5);
        o.DistributedLockWaitTimeout = TimeSpan.FromSeconds(2);
        o.DistributedLockInstanceId = "node-a";
});
```

`EnableDistributedLocking`, `DistributedLockExpiry`, `DistributedLockWaitTimeout`, and `DistributedLockInstanceId` are bindable through `CronJobOptions<T>`. They are mapped into `CronJobResilienceConfig<T>` at runtime.

```json
{
    "CronJobs": {
        "ImportJob": {
            "CronExpression": "*/5 * * * *",
            "TimeZone": "UTC",
            "EnableDistributedLocking": true,
            "DistributedLockExpiry": "00:05:00",
            "DistributedLockWaitTimeout": "00:00:02",
            "DistributedLockInstanceId": "node-a"
        }
    }
}
```

## Operational guidance

- Retry only transient failures; use `ShouldRetryOnException`.
- Keep the job duration below its schedule interval or deliberately skip overlaps.
- Set distributed lease duration beyond the normal execution time and design renewal in the custom provider when required.
- Observe circuit transitions, retries and skips through [CronJob observability](cronjob-observability.md).
- Always honor the cancellation token in long loops and I/O.

## Test reference

The module tests cover retry delay and jitter, circuit transitions, local overlap locks, cancellation, timeouts, graceful shutdown, resilient distributed-lock execution/skip paths, and the in-memory distributed lock under `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test`.
