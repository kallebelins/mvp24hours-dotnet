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

## Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           CronJob Module Architecture                         │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                         CronJobService<T>                                │ │
│  │  ┌──────────────┐  ┌───────────────┐  ┌────────────────────────────┐   │ │
│  │  │  DoWork()    │  │ PeriodicTimer │  │   TimeProvider             │   │ │
│  │  │  (abstract)  │  │ (scheduling)  │  │   (testable time)          │   │ │
│  │  └──────────────┘  └───────────────┘  └────────────────────────────┘   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                          │
│                        ┌───────────┴───────────┐                             │
│                        ▼                       ▼                             │
│  ┌─────────────────────────────┐  ┌─────────────────────────────────────┐   │
│  │  ResilientCronJobService<T> │  │     AdvancedCronJobService<T>       │   │
│  │  ┌─────────────────────────┐│  │  ┌─────────────────────────────────┐│   │
│  │  │ • Retry Policy          ││  │  │ • ICronJobContext               ││   │
│  │  │ • Circuit Breaker       ││  │  │ • ICronJobStateStore            ││   │
│  │  │ • Overlapping Prevention││  │  │ • ICronJobDependency            ││   │
│  │  │ • Graceful Shutdown     ││  │  │ • IDistributedCronJobLock       ││   │
│  │  └─────────────────────────┘│  │  │ • Event Hooks (Starting,        ││   │
│  └─────────────────────────────┘  │  │   Completed, Failed, etc.)      ││   │
│                                    │  └─────────────────────────────────┘│   │
│                                    └─────────────────────────────────────┘   │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐   │
│  │                        Observability Layer                             │   │
│  │  ┌───────────────┐  ┌────────────────┐  ┌────────────────────────┐   │   │
│  │  │ HealthChecks  │  │ Metrics        │  │ OpenTelemetry Tracing  │   │   │
│  │  │ (Healthy/     │  │ (executions,   │  │ (CronJobActivitySource)│   │   │
│  │  │  Degraded/    │  │  failures,     │  │                        │   │   │
│  │  │  Unhealthy)   │  │  duration)     │  │                        │   │   │
│  │  └───────────────┘  └────────────────┘  └────────────────────────┘   │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐   │
│  │                        Configuration Layer                             │   │
│  │  ┌───────────────────┐  ┌────────────────────┐  ┌─────────────────┐   │   │
│  │  │ IScheduleConfig<T>│  │ CronJobOptions<T>  │  │ appsettings.json│   │   │
│  │  │ (basic schedule)  │  │ (full options)     │  │ (declarative)   │   │   │
│  │  └───────────────────┘  └────────────────────┘  └─────────────────┘   │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Component Descriptions

| Component | Purpose |
|-----------|---------|
| `CronJobService<T>` | Base class for all CronJobs with CRON scheduling |
| `ResilientCronJobService<T>` | Adds retry, circuit breaker, and overlapping prevention |
| `AdvancedCronJobService<T>` | Full-featured with context, state, dependencies, and events |
| `CronJobActivitySource` | OpenTelemetry tracing for distributed observability |
| `CronJobMetricsService` | Tracks execution metrics (count, duration, failures) |
| `CronJobHealthCheck` | Health checks based on job metrics and state |

## Troubleshooting

### Common Issues

#### Job Not Executing

1. **Check CRON expression**: Validate with [Crontab Guru](https://crontab.guru/)
2. **Check timezone**: Ensure timezone is set correctly
3. **Check if paused**: Use `ICronJobController.GetStatusAsync()` to verify state
4. **Check logs**: Enable debug logging for `Mvp24Hours.Infrastructure.CronJob`

```csharp
// Enable debug logging
builder.Logging.AddFilter("Mvp24Hours.Infrastructure.CronJob", LogLevel.Debug);
```

#### Job Executing Multiple Times

1. **Overlapping prevention disabled**: Enable `PreventOverlapping = true`
2. **Multiple instances**: Use distributed locking in cluster environments
3. **Check CRON expression**: Ensure expression matches expected frequency

```csharp
services.AddResilientCronJob<MyJob>(config =>
{
    config.CronExpression = "*/5 * * * *";
    config.Resilience.PreventOverlapping = true; // Enable overlapping prevention
});
```

#### Circuit Breaker Open

1. **Check failure threshold**: Default is 5 consecutive failures
2. **Check break duration**: Default is 30 seconds
3. **Fix underlying issue**: Address root cause of failures
4. **Reset manually**: Use metrics to monitor circuit breaker state

```csharp
// Check circuit breaker state via metrics
var metrics = serviceProvider.GetRequiredService<ICronJobMetrics>();
var state = metrics.GetCircuitBreakerState("MyJob");
```

#### Memory Leaks

1. **Dispose scopes properly**: Always use `using` with DI scopes
2. **Don't store scoped services**: Avoid caching scoped services in fields
3. **Check IAsyncDisposable**: Ensure proper async disposal

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // CORRECT: Scope is disposed after use
    using var scope = _serviceProvider!.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMyService>();
    await service.ProcessAsync(cancellationToken);
}
```

### Diagnostic Commands

```bash
# Run tests to verify module functionality
dotnet test src/Tests/Mvp24Hours.Infrastructure.CronJob.Test --verbosity normal

# Check health endpoint (if configured)
curl http://localhost:5000/health/cronjobs

# View logs with structured output
dotnet run | jq 'select(.Category | startswith("Mvp24Hours.Infrastructure.CronJob"))'
```

### Logging Categories

| Category | Description |
|----------|-------------|
| `Mvp24Hours.Infrastructure.CronJob.Services.CronJobService` | Base job execution |
| `Mvp24Hours.Infrastructure.CronJob.Services.ResilientCronJobService` | Resilience features |
| `Mvp24Hours.Infrastructure.CronJob.Resiliency.CronJobCircuitBreaker` | Circuit breaker state |
| `Mvp24Hours.Infrastructure.CronJob.Observability.CronJobMetricsService` | Metrics collection |

## See Also

- [Advanced Features](cronjob-advanced.md) - Context, dependencies, distributed locking, event hooks
- [CronJob Resilience](cronjob-resilience.md) - Retry, circuit breaker, overlapping prevention
- [CronJob Observability](cronjob-observability.md) - Health checks, metrics, structured logging
- [PeriodicTimer Modernization](modernization/periodic-timer.md)
- [TimeProvider Abstraction](modernization/time-provider.md)
- [Observability](observability/home.md)
