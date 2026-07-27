# CronJob observability

CronJob instrumentation uses the `Mvp24Hours.CronJob` meter/activity source, `ICronJobMetrics`, structured `ILogger` messages and ASP.NET Core health checks. Global/per-job enable switches are documented in [advanced configuration](cronjob-advanced.md).

## Registration

```csharp
builder.Services.AddCronJobObservability(options =>
{
    options.MaxFailureRate = 0.10;
    options.CriticalFailureRate = 0.50;
    options.MaxExecutionAge = TimeSpan.FromHours(2);
});

builder.Services.AddHealthChecks()
    .AddCronJobHealthCheck(
        name: "cronjobs",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["cronjob", "ready"]);
```

`AddCronJobObservability` registers metrics and health-check options. It does **not** add the health-check registration; call `AddHealthChecks().AddCronJobHealthCheck(...)` explicitly. Metrics-only registration is `AddCronJobMetrics()` or `AddCronJobMetrics<TMetrics>()`.

## `CronJobHealthCheckOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `MaxFailureRate` | `double` | `0.1` | Failure rate above which health degrades. |
| `CriticalFailureRate` | `double` | `0.5` | Failure rate above which health is unhealthy. |
| `MinExecutionsForRateCheck` | `int` | `10` | Samples required before rate evaluation. |
| `MaxExecutionAge` | `TimeSpan` | `2 h` | Maximum acceptable time since execution. |
| `RecentFailureWindow` | `TimeSpan` | `15 min` | Window used for recent failures. |
| `CriticalJobs` | `HashSet<string>` | empty | Jobs whose failures receive critical treatment. |
| `IgnoreStoppedJobs` | `bool` | `true` | Excludes stopped jobs from evaluation. |

The health-check extension defaults to name `cronjobs`, failure status `Unhealthy`, and tags `cronjob`, `scheduled`, `background`.

## OpenTelemetry

Register the package's source and meter with the OpenTelemetry SDK:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(Mvp24HoursActivitySources.CronJob.Name)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(Mvp24HoursMeters.CronJob.Name)
        .AddOtlpExporter());
```

For .NET Aspire, point the OTLP exporter at the endpoint supplied by the service defaults/AppHost. The Aspire dashboard then receives the same OTLP traces, metrics and logs; no CronJob-specific Aspire API is required. See [.NET Aspire](modernization/aspire.md) and [exporters](observability/exporters.md).

## Metrics

The default `CronJobMetricsService` implements the real `ICronJobMetrics` methods: execution/failure, start/stop, skipped execution, retry, circuit state, active count and next/last execution recording.

| Metric | Type | Description |
|---|---|---|
| `mvp24hours.cronjob.executions_total` | Counter | Executions by job. |
| `mvp24hours.cronjob.executions_failed_total` | Counter | Failed executions. |
| `mvp24hours.cronjob.execution_duration_ms` | Histogram | Execution duration. |
| `mvp24hours.cronjob.active_count` | UpDownCounter | Active jobs. |
| `mvp24hours.cronjob.scheduled_count` | UpDownCounter | Scheduled jobs. |
| `mvp24hours.cronjob.skipped_total` | Counter | Skipped executions. |
| `mvp24hours.cronjob.retries_total` | Counter | Retry attempts. |
| `mvp24hours.cronjob.retry_delay_ms` | Histogram | Retry delay. |
| `mvp24hours.cronjob.circuit_breaker_state_changes` | Counter | Circuit transitions. |

Avoid recording a second execution metric inside `DoWork`; the service already instruments job execution.

## Logging and tracing

Use normal category filtering:

```json
{
  "Logging": {
    "LogLevel": {
      "Mvp24Hours.Infrastructure.CronJob": "Information"
    }
  }
}
```

Do not depend on undocumented event-ID ranges or span names. Build alerts from stable metric names, health status and structured fields such as job name, retry attempt and skip reason.

## Test reference

See observability, health-check and metrics tests in `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test/Observability` and `CronJobConfigurationExtensionsTest`.

## Related pages

- [Observability home](observability/home.md)
- [Metrics](observability/metrics.md)
- [Tracing](observability/tracing.md)
- [Logging](observability/logging.md)
