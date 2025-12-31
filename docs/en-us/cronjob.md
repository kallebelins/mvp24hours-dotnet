# CronJob

Solution created to enable scheduled background tasks using CRON expressions. This module integrates with the .NET hosting model and provides OpenTelemetry tracing support for observability.

## Features

- **CRON Expression Support**: Standard 5-field format (minute hour day-of-month month day-of-week)
- **Timezone Support**: Configure jobs to run in specific timezones
- **OpenTelemetry Tracing**: Built-in distributed tracing with `CronJobActivitySource`
- **TimeProvider Integration**: Testable time abstraction for unit testing
- **PeriodicTimer**: Modern async/await patterns with proper cancellation support
- **IAsyncDisposable**: Proper async resource cleanup
- **Scoped DI**: Each execution creates a new DI scope for proper service lifetime

## Installation

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Creating a CronJob Service

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;

public class MyBackgroundJob : CronJobService<MyBackgroundJob>
{
    private readonly ILogger<MyBackgroundJob> _jobLogger;

    public MyBackgroundJob(
        IScheduleConfig<MyBackgroundJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<CronJobService<MyBackgroundJob>> logger,
        ILogger<MyBackgroundJob> jobLogger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, logger, timeProvider)
    {
        _jobLogger = jobLogger;
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        _jobLogger.LogInformation("Starting background job execution...");
        
        // Access scoped services via _serviceProvider
        using var scope = _serviceProvider!.CreateScope();
        var myService = scope.ServiceProvider.GetRequiredService<IMyService>();
        
        await myService.ProcessAsync(cancellationToken);
        
        _jobLogger.LogInformation("Background job completed successfully.");
    }
}
```

## Configuration

### Basic Configuration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddCronJob<MyBackgroundJob>(config =>
{
    config.CronExpression = "*/5 * * * *"; // Every 5 minutes
    config.TimeZoneInfo = TimeZoneInfo.Utc;
});
```

### Convenience Overloads

```csharp
// Simple: CRON expression only (uses local timezone)
services.AddCronJob<HourlyReportJob>("0 * * * *"); // Every hour

// With timezone
services.AddCronJob<DailyCleanupJob>(
    "30 2 * * *", // Daily at 2:30 AM
    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

// Run once immediately (no CRON expression)
services.AddCronJobRunOnce<DatabaseMigrationJob>();
```

## CRON Expression Reference

| Expression | Description |
|------------|-------------|
| `* * * * *` | Every minute |
| `*/5 * * * *` | Every 5 minutes |
| `0 * * * *` | Every hour at minute 0 |
| `0 0 * * *` | Daily at midnight |
| `0 0 * * 0` | Weekly on Sunday at midnight |
| `0 0 1 * *` | Monthly on the 1st at midnight |
| `0 9 * * 1-5` | Weekdays at 9 AM |

Use [Crontab Guru](https://crontab.guru/) to build and validate your expressions.

## OpenTelemetry Tracing

The module provides built-in tracing via `CronJobActivitySource`:

```csharp
// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(CronJobActivitySource.SourceName) // "Mvp24Hours.CronJob"
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });
```

### Activity Names

- `Mvp24Hours.CronJob.Execute` - Job execution operations
- `Mvp24Hours.CronJob.Schedule` - Job scheduling operations  
- `Mvp24Hours.CronJob.Start` - Job startup operations
- `Mvp24Hours.CronJob.Stop` - Job stop operations

### Semantic Tags

| Tag | Description |
|-----|-------------|
| `cronjob.name` | Name of the CronJob |
| `cronjob.expression` | CRON expression |
| `cronjob.timezone` | Timezone used |
| `cronjob.duration_ms` | Execution duration in milliseconds |
| `cronjob.success` | Whether execution succeeded |
| `cronjob.execution_count` | Total execution count |

## Testing with TimeProvider

Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` for unit tests:

```csharp
[Fact]
public async Task CronJob_Should_Execute_At_Scheduled_Time()
{
    var fakeTimeProvider = new FakeTimeProvider();
    fakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    
    var job = new MyBackgroundJob(
        config,
        hostApplication,
        serviceProvider,
        logger,
        jobLogger,
        fakeTimeProvider);
    
    // Advance time to trigger execution
    fakeTimeProvider.Advance(TimeSpan.FromMinutes(5));
    
    // Assert job executed
}
```

## Accessing Scoped Services

Each execution creates a new DI scope. Access services via `_serviceProvider`:

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // _serviceProvider is already scoped for this execution
    var dbContext = _serviceProvider!.GetRequiredService<MyDbContext>();
    var repository = _serviceProvider!.GetRequiredService<IMyRepository>();
    
    // Services are properly scoped and disposed after execution
    await repository.ProcessPendingItemsAsync(cancellationToken);
}
```

## Graceful Shutdown

The `CronJobService` properly handles cancellation and graceful shutdown:

- Supports `CancellationToken` propagation
- Implements `IAsyncDisposable` for async cleanup
- Uses `PeriodicTimer` for cancellation-friendly waiting
- Disposes scoped services after each execution

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ExecutionCount` | `long` | Total number of executions |
| `JobName` | `string` | Name of the CronJob type |
| `CronExpression` | `string` | Configured CRON expression |

## See Also

- [Advanced Features](cronjob-advanced.md) - Context, dependencies, distributed locking, event hooks
- [CronJob Resilience](cronjob-resilience.md) - Retry, circuit breaker, overlapping prevention
- [CronJob Observability](cronjob-observability.md) - Health checks, metrics, structured logging
- [PeriodicTimer Modernization](modernization/periodic-timer.md)
- [TimeProvider Abstraction](modernization/time-provider.md)
- [Observability](observability/home.md)
