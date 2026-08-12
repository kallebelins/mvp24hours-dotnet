# CronJob advanced configuration

This page is the canonical reference for global defaults, per-job options and advanced infrastructure. Resilience-only properties are in [CronJob resilience](cronjob-resilience.md), and health/metrics configuration is in [CronJob observability](cronjob-observability.md).

## Global defaults versus per-job values

`CronJobGlobalOptions` binds from `CronJobs:Global`. `CronJobOptions<TJob>` binds from `CronJobs:{TJobName}`.

There are two explicit defaulting operations:

- `ApplyDefaultsTo(job)` fills only a missing `TimeZone`; it does not copy every global setting.
- `CreateWithDefaults<TJob>()` creates a new per-job object and copies the global time-zone, resilience, distributed-lock and observability defaults.

Global options are not automatically merged into every job registration. Use configuration registration as designed, or create an options instance with `CreateWithDefaults<TJob>()` before applying job overrides.

```csharp
services.AddCronJobGlobalOptions(global =>
{
    global.DefaultTimeZone = "UTC";
    global.EnableRetryByDefault = true;
    global.DefaultMaxRetryAttempts = 3;
});

var global = new CronJobGlobalOptions
{
    DefaultTimeZone = "UTC",
    EnableRetryByDefault = true
};
var job = global.CreateWithDefaults<CleanupJob>();
job.CronExpression = "0 2 * * *";
```

## `CronJobGlobalOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultTimeZone` | `string?` | `null` | Default time-zone ID; null means local time. |
| `JobsEnabledByDefault` | `bool` | `true` | Initial per-job enabled value created by `CreateWithDefaults`. |
| `EnableRetryByDefault` | `bool` | `false` | Initial retry switch. |
| `DefaultMaxRetryAttempts` | `int` | `3` | Initial retry-attempt limit. |
| `DefaultRetryDelay` | `TimeSpan` | `1 s` | Initial retry delay. |
| `UseExponentialBackoffByDefault` | `bool` | `true` | Initial backoff switch. |
| `EnableCircuitBreakerByDefault` | `bool` | `false` | Initial circuit-breaker switch. |
| `DefaultCircuitBreakerFailureThreshold` | `int` | `5` | Initial opening threshold. |
| `DefaultCircuitBreakerBreakDuration` | `TimeSpan` | `30 s` | Initial open duration. |
| `PreventOverlappingByDefault` | `bool` | `true` | Initial local overlap policy. |
| `DefaultGracefulShutdownTimeout` | `TimeSpan` | `30 s` | Initial shutdown wait. |
| `EnableDistributedLockingByDefault` | `bool` | `false` | Initial cluster-lock switch. |
| `DefaultDistributedLockExpiry` | `TimeSpan` | `5 min` | Initial distributed-lock lease. |
| `EnableObservability` | `bool` | `true` | Initial per-job observability switch. |
| `EnableHealthChecks` | `bool` | `true` | Initial per-job health switch. |
| `RegisterAggregateHealthCheck` | `bool` | `true` | Declares aggregate-check intent. Register the check explicitly. |
| `AggregateHealthCheckName` | `string` | `cronjobs` | Aggregate check name. |
| `HealthCheckTags` | `string[]?` | `cronjob`, `background` | Global health tags. |
| `ValidateCronExpressionsOnStartup` | `bool` | `true` | Enables fail-fast validation. |
| `LogConfigurationWarnings` | `bool` | `true` | Enables configuration warnings. |
| `EnableRuntimeControl` | `bool` | `true` | Enables runtime-control intent. |
| `EnableStatePersistence` | `bool` | `true` | Enables state-persistence intent. |

## `CronJobOptions<TJob>`

| Name | Type | Default | Description |
|---|---|---|---|
| `CronExpression` | `string?` | `null` | Five/six-field expression; null is run once. |
| `TimeZone` | `string?` | `null` | Bindable time-zone ID; null resolves to local time. |
| `TimeZoneInfo` | `TimeZoneInfo?` | derived | Runtime view over `TimeZone`. |
| `Description` | `string?` | `null` | Operational description. |
| `Enabled` | `bool` | `true` | Disabled jobs receive a null schedule. |
| `InstanceName` | `string?` | `null` | Key for multi-instance registration; type name is fallback. |
| `EnableRetry` | `bool` | `false` | Enables retries. |
| `MaxRetryAttempts` | `int` | `3` | Maximum attempts. |
| `RetryDelay` | `TimeSpan` | `1 s` | Initial retry delay. |
| `UseExponentialBackoff` | `bool` | `true` | Enables exponential backoff. |
| `EnableCircuitBreaker` | `bool` | `false` | Enables the circuit breaker. |
| `CircuitBreakerFailureThreshold` | `int` | `5` | Consecutive failures before opening. |
| `CircuitBreakerBreakDuration` | `TimeSpan` | `30 s` | Open duration. |
| `PreventOverlapping` | `bool` | `true` | Prevents local concurrent execution. |
| `GracefulShutdownTimeout` | `TimeSpan` | `30 s` | Shutdown wait. |
| `EnableDistributedLocking` | `bool` | `false` | Enables advanced distributed-lock behavior. |
| `DistributedLockExpiry` | `TimeSpan` | `5 min` | Distributed lock lease. |
| `DistributedLockWaitTimeout` | `TimeSpan` | `1 s` | Maximum wait when acquiring resilient distributed lock. |
| `DistributedLockInstanceId` | `string?` | `null` | Optional resilient lock owner ID; runtime fallback when null. |
| `EnableObservability` | `bool` | `true` | Registers CronJob observability services in option-based registration. |
| `EnableHealthCheck` | `bool` | `true` | Per-job health intent. |
| `DependsOn` | `string[]?` | `null` | Names of prerequisite jobs. |

## Configuration binding

```json
{
  "CronJobs": {
    "Global": {
      "DefaultTimeZone": "UTC",
      "EnableRetryByDefault": true
    },
    "CleanupJob": {
      "CronExpression": "0 2 * * *",
      "TimeZone": "UTC",
      "EnableRetry": true,
      "MaxRetryAttempts": 3,
      "EnableDistributedLocking": true,
      "DistributedLockExpiry": "00:05:00",
      "DistributedLockWaitTimeout": "00:00:02",
      "DistributedLockInstanceId": "node-a"
    }
  }
}
```

```csharp
services.AddCronJobGlobalOptionsFromConfiguration(configuration);
services.AddCronJobFromConfiguration<CleanupJob>(configuration);
// Use the matching methods for derived services:
// AddResilientCronJobFromConfiguration<T>()
// AddAdvancedCronJobFromConfiguration<T>()
```

All option-based registrations call `ValidateOnStart`. Missing configuration sections cause configuration-based registration to throw.

## Advanced infrastructure

```csharp
services.AddCronJobAdvancedInfrastructure(options =>
{
    options.UseStatePersistence = true;
    options.UseController = true;
    options.UseDependencies = true;
    options.UseEventHandlers = true;
    options.UseDistributedLocking = true;
});
```

| Name | Type | Default | Description |
|---|---|---|---|
| `UseStatePersistence` | `bool` | `true` | Registers `InMemoryCronJobStateStore`. |
| `UseController` | `bool` | `true` | Registers `ICronJobController`. |
| `UseDependencies` | `bool` | `true` | Registers the dependency tracker. |
| `UseEventHandlers` | `bool` | `true` | Registers the event dispatcher. |
| `UseDistributedLocking` | `bool` | `false` | Registers the in-memory distributed-lock implementation. |

Replace in-memory infrastructure with real production implementations:

```csharp
services.AddCronJobStateStore<SqlCronJobStateStore>();
services.AddCronJobDistributedLock<RedisCronJobLock>();
services.AddCronJobEventHandler<CleanupJobEvents>();
services.AddCronJobDependency<ReportJob>(d =>
    d.DependsOn<CleanupJob>().WithSuccessRequired());
```

The package does not contain Redis or SQL implementations named in older documentation; application code or another package must implement the interfaces.

## Multiple instances

`AddCronJobInstances<T>` registers keyed options and schedules, but it does not create keyed hosted-service instances. The source explicitly requires an application factory or wrapper hosted service to execute them.

```csharp
services.AddCronJobInstances<RegionalSyncJob>(
    new() { InstanceName = "US", CronExpression = "0 0 * * *", TimeZone = "UTC" },
    new() { InstanceName = "EU", CronExpression = "0 1 * * *", TimeZone = "UTC" });
```

## Test reference

See `CronJobGlobalOptionsTest`, `CronJobConfigurationExtensionsTest`, state, dependency, event and distributed-lock tests in `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test`.
