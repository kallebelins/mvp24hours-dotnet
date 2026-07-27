# CronJob

`Mvp24Hours.Infrastructure.CronJob` runs hosted services from five- or six-field CRON expressions on .NET 10. This page owns installation, the base service, and basic scheduling. Global and per-job options are documented once in [Advanced configuration](cronjob-advanced.md).

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Create a job

```csharp
public sealed class CleanupJob : CronJobService<CleanupJob>
{
    public CleanupJob(
        IScheduleConfig<CleanupJob> config,
        IHostApplicationLifetime lifetime,
        IServiceProvider services,
        ILogger<CronJobService<CleanupJob>> logger,
        TimeProvider? timeProvider = null)
        : base(config, lifetime, services, logger, timeProvider)
    {
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider!.CreateAsyncScope();
        var cleanup = scope.ServiceProvider.GetRequiredService<ICleanupService>();
        await cleanup.RunAsync(cancellationToken);
    }
}
```

`CronJobService<T>` is a hosted service. Respect the supplied cancellation token and resolve scoped dependencies from an execution scope.

## Register

```csharp
builder.Services.AddCronJob<CleanupJob>(config =>
{
    config.CronExpression = "0 2 * * *";
    config.TimeZoneInfo = TimeZoneInfo.Utc;
});
```

Convenience overloads also exist:

```csharp
services.AddCronJob<HourlyJob>("0 * * * *");
services.AddCronJob<DailyJob>("0 2 * * *", TimeZoneInfo.Utc);
services.AddCronJobRunOnce<MigrationJob>();
```

For validated `CronJobOptions<T>`, configuration binding, resilience or advanced infrastructure, use:

```csharp
services.AddCronJobWithOptions<CleanupJob>(options =>
{
    options.CronExpression = "0 2 * * *";
    options.TimeZone = "UTC";
    options.Description = "Deletes expired records";
});
```

## CRON formats

| Expression | Meaning |
|---|---|
| `*/5 * * * *` | Every five minutes |
| `0 * * * *` | Hourly |
| `0 2 * * *` | Daily at 02:00 |
| `0 */30 * * * *` | Every 30 minutes, at second zero |

Five fields are `minute hour day-of-month month day-of-week`; six fields add `second` first. A null or empty expression is the run-once form. Time-zone identifiers are platform dependent; UTC is the portable default for distributed deployments.

## Choose a service

| Service | Use when |
|---|---|
| `CronJobService<T>` | Scheduling and cancellation are sufficient. |
| `ResilientCronJobService<T>` | Retry, circuit breaker, timeout or overlap prevention is required. |
| `AdvancedCronJobService<T>` | State, runtime control, dependencies, distributed lock or lifecycle events are required. |

## Testability

The constructors accept `TimeProvider`, so tests can use `FakeTimeProvider`. The module test project also supplies job fixtures and verifies schedule, cancellation, resilience, state and observability behavior.

## Related pages

- [Advanced configuration](cronjob-advanced.md)
- [Resilience](cronjob-resilience.md)
- [Observability](cronjob-observability.md)
- [TimeProvider](modernization/time-provider.md)
- [PeriodicTimer](modernization/periodic-timer.md)
