# CronJob Observability

Complete observability features for CronJob services including health checks, metrics, distributed tracing, and structured logging.

## Overview

The CronJob module provides enterprise-grade observability through:

- **Health Checks**: Monitor CronJob status via ASP.NET Core Health Checks
- **Metrics**: Prometheus-compatible metrics with `ICronJobMetrics`
- **Tracing**: Distributed tracing with OpenTelemetry `ActivitySource`
- **Structured Logging**: High-performance logging with `[LoggerMessage]` source generators

## Installation

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Quick Setup

Add all observability features with a single extension:

```csharp
// In Program.cs
builder.Services.AddMvp24HoursCronJobObservability();
```

Or configure individually:

```csharp
// Add metrics only
builder.Services.AddCronJobMetrics();

// Add health check only
builder.Services.AddHealthChecks()
    .AddCronJobHealthCheck(name: "CronJobs", tags: new[] { "ready", "cronjob" });
```

## Health Checks

### Configuration

```csharp
// Add CronJob health check
builder.Services.AddHealthChecks()
    .AddCronJobHealthCheck(
        name: "CronJob Health",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cronjob", "background" });

// Map health endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Health Check Response

The health check provides detailed status for each registered CronJob:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0012345",
  "entries": {
    "CronJob Health": {
      "data": {
        "EmailSenderJob_LastExecution": "2024-12-31T10:00:00.0000000Z",
        "EmailSenderJob_NextExecution": "2024-12-31T10:05:00.0000000Z",
        "EmailSenderJob_ExecutionCount": 42,
        "EmailSenderJob_IsRunning": false,
        "ReportGeneratorJob_LastExecution": "2024-12-31T09:30:00.0000000Z",
        "ReportGeneratorJob_NextExecution": "2024-12-31T10:30:00.0000000Z",
        "ReportGeneratorJob_ExecutionCount": 12,
        "ReportGeneratorJob_IsRunning": true
      },
      "status": "Healthy"
    }
  }
}
```

### ICronJobServiceStatus Interface

CronJob services implement `ICronJobServiceStatus` for health reporting:

```csharp
public interface ICronJobServiceStatus
{
    string JobName { get; }
    DateTimeOffset? LastExecutionTime { get; }
    DateTimeOffset? NextExecutionTime { get; }
    long ExecutionCount { get; }
    bool IsRunning { get; }
}
```

## Metrics

### ICronJobMetrics Interface

The `ICronJobMetrics` interface provides a standardized way to record CronJob metrics:

```csharp
public interface ICronJobMetrics
{
    void RecordExecution(string jobType, double durationMs, bool success);
    void IncrementActive(string jobType);
    void DecrementActive(string jobType);
    void UpdateScheduledCount(int delta);
    void RecordLastExecutionAge(string jobType, double ageSeconds);
    void RecordRetry(string jobType, int attempt);
    void RecordSkipped(string jobType, string reason);
    void RecordCircuitBreakerStateChange(string jobType, string newState);
}
```

### Available Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `mvp24hours.cronjob.executions.total` | Counter | Total number of job executions |
| `mvp24hours.cronjob.executions.failed.total` | Counter | Total number of failed executions |
| `mvp24hours.cronjob.execution.duration` | Histogram | Duration of executions in milliseconds |
| `mvp24hours.cronjob.active.count` | UpDownCounter | Number of currently running jobs |
| `mvp24hours.cronjob.scheduled.count` | UpDownCounter | Number of scheduled jobs |
| `mvp24hours.cronjob.skipped.total` | Counter | Total number of skipped executions |
| `mvp24hours.cronjob.retries.total` | Counter | Total number of retry attempts |
| `mvp24hours.cronjob.circuit_breaker.state_changes` | Counter | Circuit breaker state changes |

### Prometheus Configuration

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Mvp24HoursMeters.CronJob.Name) // "Mvp24Hours.CronJob"
            .AddPrometheusExporter();
    });

app.MapPrometheusScrapingEndpoint("/metrics");
```

### Custom Metrics

You can inject `ICronJobMetrics` to record custom metrics:

```csharp
public class CustomReportJob : CronJobService<CustomReportJob>
{
    private readonly ICronJobMetrics _metrics;

    public CustomReportJob(
        ICronJobMetrics metrics,
        // ... other dependencies
    ) : base(/* ... */)
    {
        _metrics = metrics;
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await GenerateReportsAsync(cancellationToken);
            _metrics.RecordExecution(JobName, sw.ElapsedMilliseconds, success: true);
        }
        catch (Exception)
        {
            _metrics.RecordExecution(JobName, sw.ElapsedMilliseconds, success: false);
            throw;
        }
    }
}
```

## Distributed Tracing

### ActivitySource Configuration

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.CronJob.Name) // "Mvp24Hours.CronJob"
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });
```

### Activity Names

| Activity | Description |
|----------|-------------|
| `Mvp24Hours.CronJob.JobExecution` | Individual job execution |
| `Mvp24Hours.CronJob.JobScheduling` | Job scheduling operations |

### Semantic Tags

| Tag | Description |
|-----|-------------|
| `cronjob.name` | Name of the CronJob |
| `cronjob.expression` | CRON expression |
| `cronjob.timezone` | Timezone used |
| `cronjob.duration_ms` | Execution duration in milliseconds |
| `cronjob.success` | Whether execution succeeded |
| `cronjob.execution_count` | Total execution count |
| `cronjob.retry.enabled` | Whether retry is enabled (ResilientCronJob) |
| `cronjob.circuit_breaker.enabled` | Whether circuit breaker is enabled |
| `cronjob.prevent_overlapping` | Whether overlapping prevention is enabled |

### Tracing Example

```csharp
// Traces are automatically created for each execution
// Example span:
// - Name: Mvp24Hours.CronJob.JobExecution
// - Duration: 1234ms
// - Status: Ok
// - Tags:
//   - cronjob.name: EmailSenderJob
//   - cronjob.expression: */5 * * * *
//   - cronjob.success: true
//   - cronjob.execution_count: 42
```

## Structured Logging

### High-Performance Logging

The module uses source-generated `[LoggerMessage]` for high-performance, allocation-free logging:

```csharp
// Automatically logged events with structured data:

// Starting job
// EventId: 1001, Level: Debug
// "CronJob starting. Name: {CronJobName}, Scheduler: {CronExpression}"

// Execution completed
// EventId: 1003, Level: Debug  
// "CronJob execute once after. Name: {CronJobName}, Duration: {DurationMs}ms"

// Execution failed
// EventId: 1005, Level: Error
// "CronJob execute once failure. Name: {CronJobName}, Duration: {DurationMs}ms"
```

### Event ID Reference

| EventId | Level | Description |
|---------|-------|-------------|
| 1001 | Debug | CronJob starting |
| 1002 | Debug | Execute once before |
| 1003 | Debug | Execute once after |
| 1004 | Debug | Execute once cancelled |
| 1005 | Error | Execute once failure |
| 1006 | Debug | Execute once ending |
| 1007 | Debug | Scheduler started |
| 1008 | Warning | No next occurrence |
| 1009 | Debug | Next execution scheduled |
| 1010 | Debug | Scheduler cancelled |
| 1011 | Debug | Scheduler stopped |
| 1012 | Debug | Execute before |
| 1013 | Debug | Execute after |
| 1014 | Debug | Execution cancelled |
| 1015 | Error | Execute failure |
| 1016 | Debug | CronJob stopping |
| 1017 | Debug | CronJob stopped |
| 1018 | Warning | Shutdown timed out |
| 1019 | Warning | Skipped - circuit breaker open |
| 1020 | Warning | Skipped - overlapping |
| 1021 | Warning | Execution timed out |
| 1022 | Warning | Retry attempt |
| 1023 | Debug | Resilient CronJob starting |
| 1024 | Debug | Resilient CronJob stopping |

### Log Configuration

```csharp
// Configure log levels per namespace
builder.Logging.AddFilter("Mvp24Hours.Infrastructure.CronJob", LogLevel.Debug);

// Or via appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Mvp24Hours.Infrastructure.CronJob": "Debug"
    }
  }
}
```

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add CronJob with all observability
builder.Services.AddMvp24HoursCronJobObservability();

// Register your CronJob
builder.Services.AddResilientCronJob<EmailSenderJob>(config =>
{
    config.CronExpression = "*/5 * * * *";
    config.TimeZoneInfo = TimeZoneInfo.Utc;
},
resilience =>
{
    resilience.EnableRetry = true;
    resilience.MaxRetryAttempts = 3;
    resilience.EnableCircuitBreaker = true;
    resilience.PreventOverlapping = true;
});

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.CronJob.Name)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Mvp24HoursMeters.CronJob.Name)
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();
```

## Integration with Aspire Dashboard

When using .NET Aspire, all CronJob observability data is automatically visible in the Aspire Dashboard:

```csharp
// In AppHost
var app = DistributedApplication.CreateBuilder(args);

var api = app.AddProject<Projects.MyApi>("api")
    .WithOtlpExporter();

app.Build().Run();
```

## See Also

- [CronJob Overview](cronjob.md)
- [Advanced Features](cronjob-advanced.md) - Context, dependencies, distributed locking, event hooks
- [CronJob Resilience](cronjob-resilience.md)
- [OpenTelemetry Metrics](observability/metrics.md)
- [OpenTelemetry Tracing](observability/tracing.md)
- [.NET Aspire Integration](modernization/aspire.md)

