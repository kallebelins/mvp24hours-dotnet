# simple-cronjob-worker

Worker service demonstrating `Mvp24Hours.Infrastructure.CronJob`: scheduled background jobs,
resilience patterns (retry, circuit breaker, overlapping prevention), and observability hooks
(health checks, metrics, OpenTelemetry activity sources) using .NET 10's `TimeProvider` /
`PeriodicTimer` abstractions.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- `CronJobService<T>` — base class for simple scheduled jobs with built-in OpenTelemetry tracing
- `ResilientCronJobService<T>` — extends the base with retry (exponential back-off + jitter),
  circuit breaker, and overlapping-execution prevention
- `AddCronJobObservability()` — in-memory execution counters / metrics via `ICronJobMetrics`
- `AddCronJobHealthCheck()` — ASP.NET Core health check reporting job success rates and
  circuit-breaker state at `/health`, `/health/live`, and `/health/ready`
- `TimeProvider` injection for full testability (swap `FakeTimeProvider` in unit tests)
- `PeriodicTimer`-based internal scheduler — modern async/await, no `System.Timers.Timer`
- CronJob configuration from `appsettings.json` via `CronJobs:{JobName}:CronExpression`

## Architecture

- Tier: `Simple`
- Shape: Single `Microsoft.NET.Sdk.Web` project acting as both worker host and health endpoint server
- Why this shape fits: a web host allows mapping `/health*` endpoints without extra infrastructure;
  the CronJob services run as `IHostedService` background workers alongside the Kestrel listener

## Layers

- `CronJobWorker/` — single host project; owns `Program.cs`, job classes, and configuration
- `CronJobWorker/Jobs/HeartbeatJob.cs` — simple job (`CronJobService<T>`): pulses every minute
- `CronJobWorker/Jobs/CleanupJob.cs` — resilient job (`ResilientCronJobService<T>`): runs every 5 minutes

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- No external services required — all state is in-memory

## Configuration

Override any key via environment variable or `appsettings.Development.json`.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `CronJobs:HeartbeatJob:CronExpression` | No | 5-field Cronos expression for `HeartbeatJob` | `"* * * * *"` |
| `CronJobs:CleanupJob:CronExpression` | No | 5-field Cronos expression for `CleanupJob` | `"*/5 * * * *"` |

> **Cronos format** — the library uses 5-field standard format (`minute hour day month weekday`).
> Seconds-precision scheduling requires calling `CronExpression.Parse` with `CronFormat.IncludeSeconds`
> and is not configured by default.

## Run

From this sample's solution directory:

```bash
dotnet restore
dotnet run --project CronJobWorker/CronJobWorker.csproj
```

Verify jobs are running:

```bash
# Aggregate health (includes CronJob metrics)
curl http://localhost:5000/health

# Liveness (returns 200 while process is up)
curl http://localhost:5000/health/live

# Readiness (CronJob-tagged checks only)
curl http://localhost:5000/health/ready
```

### Simulating failures (circuit breaker demo)

Uncomment the throw in `CleanupJob.DoWork` to make every third execution fail:

```csharp
if (ExecutionCount % 3 == 0)
    throw new InvalidOperationException("Simulated transient failure.");
```

Re-run and watch `/health` transition to `Degraded` → `Unhealthy` as the circuit opens,
then self-heal after the 30-second break duration.

## Explore the API

- Health endpoint: `http://localhost:5000/health`
- Liveness: `http://localhost:5000/health/live`
- Readiness: `http://localhost:5000/health/ready`

## Related documentation

- [Getting started](../../../docs/en-us/getting-started.md)
- [CronJob overview](../../../docs/en-us/cronjob.md)
- [Advanced scheduling](../../../docs/en-us/cronjob-advanced.md)
- [Resilience patterns](../../../docs/en-us/cronjob-resilience.md)
- [Observability hooks](../../../docs/en-us/cronjob-observability.md)
- [PeriodicTimer modernization](../../../docs/en-us/modernization/periodic-timer.md)

## What this sample intentionally does not cover

- Distributed locking across a cluster — the in-process `InMemoryCronJobExecutionLock` only
  prevents overlapping within a single process instance; for multi-replica deployments add a
  Redis- or database-backed `IDistributedCronJobLock` implementation
- Persisted job state across restarts — metrics are in-memory only; wire `ICronJobStateStore`
  to a durable store for production
- Advanced job orchestration (dependencies, pause/resume) — see `AdvancedCronJobService<T>`
  and `AddAdvancedCronJob<T>` if you need those features
- OpenTelemetry exporter configuration — connect to a collector by adding
  `AddSource(CronJobActivitySource.SourceName)` in `AddOpenTelemetry().WithTracing(...)`
