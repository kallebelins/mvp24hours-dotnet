# Background Jobs

`Mvp24Hours.Infrastructure` defines a provider-neutral background-job contract around `IJobScheduler`. It includes an executable in-memory provider and registration surfaces for Hangfire and Quartz.NET.

> [!IMPORTANT]
> In the current source, `HangfireJobProvider` and `QuartzJobProvider` are integration stubs. Their scheduling and management methods throw `NotSupportedException`; the package does not reference Hangfire or Quartz. The provider-specific option classes describe intended configuration, but registering one of these providers does not configure its storage, server, or scheduler. Integrate the chosen vendor directly until a concrete adapter is implemented.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure
```

## Define and schedule a job

Jobs with arguments implement `IBackgroundJob<TArgs>`; jobs without arguments implement `IBackgroundJob`. Register the job type in DI because the in-memory scheduler resolves each execution from a new scope.

```csharp
public sealed record RebuildIndexArgs(string IndexName);

public sealed class RebuildIndexJob(IIndexService indexes)
    : IBackgroundJob<RebuildIndexArgs>
{
    public Task ExecuteAsync(
        RebuildIndexArgs args,
        IJobContext context,
        CancellationToken cancellationToken) =>
        indexes.RebuildAsync(args.IndexName, cancellationToken);
}

builder.Services.AddScoped<RebuildIndexJob>();
builder.Services.AddInMemoryBackgroundJobs();

var scheduler = app.Services.GetRequiredService<IJobScheduler>();
string jobId = await scheduler.EnqueueAsync<RebuildIndexJob, RebuildIndexArgs>(
    new("products"),
    new JobOptions
    {
        Queue = "maintenance",
        Priority = JobPriority.High,
        Metadata = new Dictionary<string, string>
        {
            ["correlation-id"] = Activity.Current?.TraceId.ToString() ?? string.Empty
        }
    });
```

`IJobScheduler` exposes immediate, delayed, date/time, recurring, continuation, batch, parent-child, cancellation, and status operations. Actual support depends on the provider; see [Provider capability status](#provider-capability-status).

## Provider registration

Register one default scheduler:

```csharp
// Development and tests
services.AddInMemoryBackgroundJobs();

// Equivalent builder form; selecting no provider throws InvalidOperationException.
services.AddBackgroundJobs(jobs => jobs.AddInMemoryProvider());
```

The following registrations are valid DI APIs, but currently resolve stub providers:

```csharp
services.AddHangfireBackgroundJobs(options =>
{
    options.ConnectionString = configuration.GetConnectionString("Hangfire");
    options.StorageProvider = HangfireStorageProvider.SqlServer;
});

services.AddQuartzBackgroundJobs(options =>
{
    options.ConnectionString = configuration.GetConnectionString("Quartz");
    options.StorageProvider = QuartzStorageProvider.SqlServer;
});
```

Builder equivalents are `AddHangfireProvider(...)` and `AddQuartzProvider(...)`. All registration methods use `TryAddSingleton<IJobScheduler>`; the first scheduler registration wins. They are unkeyed and do not register job types.

### Provider capability status

| Provider | Current behavior | Persistence | Recurring work | Batches and parent-child |
|---|---|---|---|---|
| `InMemoryJobProvider` | Executes immediate and delayed jobs in process | No | Accepts a CRON string but enqueues once | APIs exist; batch execution is bookkeeping-only and does not execute its `BatchJob` entries |
| `HangfireJobProvider` | Resolves from DI; every scheduler operation throws `NotSupportedException` | Not configured | Not implemented | Not implemented |
| `QuartzJobProvider` | Resolves from DI; every scheduler operation throws `NotSupportedException` | Not configured | Not implemented | Not implemented |

The in-memory provider serializes arguments with `System.Text.Json`, creates a DI scope per execution, runs one job at a time, tracks status in memory, and applies retry delay. It is intended for tests and development, not durable or distributed workloads.

## `JobOptions`

| Property | Type | Default | Notes |
|---|---|---|---|
| `MaxRetryAttempts` | `int` | `3` | `0` disables retries. Validation rejects negative values. |
| `InitialRetryDelay` | `TimeSpan` | 5 seconds | Must be greater than zero. |
| `MaxRetryDelay` | `TimeSpan` | 1 hour | Must be positive and at least `InitialRetryDelay`. |
| `UseExponentialBackoff` | `bool` | `true` | Doubles the delay per attempt, capped by `MaxRetryDelay`. |
| `Timeout` | `TimeSpan?` | 1 hour | Validation rejects zero or negative values; `null` disables the declared timeout. The in-memory provider does not enforce this property. |
| `Priority` | `JobPriority` | `Normal` | Values are `Low`, `Normal`, `High`, and `Critical`. The in-memory scheduler does not route through `PriorityQueueManager`. |
| `Queue` | `string?` | `null` | Exposed through `IJobContext`; the in-memory scheduler does not isolate workers by queue. |
| `Metadata` | `IDictionary<string,string>` | empty | Copied into `IJobContext.Metadata`. |
| `DeleteOnSuccess` | `bool` | `false` | The in-memory scheduler does not apply retention cleanup. |
| `RetentionDays` | `int?` | `30` | Validation rejects negative values; `null` means indefinite retention. The in-memory scheduler does not apply it. |

Call `options.Validate()` when accepting externally supplied values; registration does not invoke this method automatically.

## `HangfireJobOptions`

These values are stored in `IOptions<HangfireJobOptions>` by `AddHangfireBackgroundJobs`. They are not forwarded to Hangfire in the current implementation.

| Property | Type | Default |
|---|---|---|
| `ConnectionString` | `string?` | `null` |
| `StorageProvider` | `HangfireStorageProvider` | `SqlServer` |
| `SchemaName` | `string?` | `null` |
| `WorkerCount` | `int` | `5` |
| `DashboardPath` | `string` | `/hangfire` |
| `EnableDashboard` | `bool` | `true` |
| `DashboardAuthorizationPolicy` | `string?` | `null` |
| `DefaultQueue` | `string` | `default` |
| `JobExpirationTimeout` | `TimeSpan` | 7 days |
| `ServerCheckInterval` | `TimeSpan` | 15 seconds |
| `HeartbeatInterval` | `TimeSpan` | 30 seconds |

`HangfireStorageProvider` contains `SqlServer`, `PostgreSql`, `MySql`, `MongoDb`, `Redis`, and `Memory`. A matching Hangfire storage package would still have to be installed and configured by the application.

## `QuartzJobOptions`

These values are stored in `IOptions<QuartzJobOptions>` by `AddQuartzBackgroundJobs`. They are not forwarded to Quartz in the current implementation.

| Property | Type | Default |
|---|---|---|
| `ConnectionString` | `string?` | `null` |
| `StorageProvider` | `QuartzStorageProvider` | `SqlServer` |
| `TablePrefix` | `string` | `QRTZ_` |
| `InstanceId` | `string?` | `null` |
| `InstanceName` | `string` | `Mvp24HoursScheduler` |
| `EnableClustering` | `bool` | `false` |
| `ClusterCheckinInterval` | `TimeSpan` | 20 seconds |
| `MaxConcurrency` | `int` | `10` |
| `MisfireThreshold` | `TimeSpan` | 60 seconds |
| `ThreadPriority` | `ThreadPriority` | `Normal` |
| `UseUtcTimezone` | `bool` | `true` |
| `SerializationType` | `QuartzSerializationType` | `Json` |

`QuartzStorageProvider` contains `SqlServer`, `PostgreSql`, `MySql`, `Sqlite`, `Oracle`, and `Memory`; serialization values are `Binary` and `Json`. The corresponding Quartz packages and scheduler configuration remain application responsibilities.

## Scheduling patterns

### Immediate and delayed work

```csharp
string immediate = await scheduler.EnqueueAsync<RebuildIndexJob, RebuildIndexArgs>(
    new("products"));

string delayed = await scheduler.ScheduleAsync<RebuildIndexJob, RebuildIndexArgs>(
    new("archive"),
    TimeSpan.FromMinutes(15));

JobStatus? status = await scheduler.GetStatusAsync(immediate);
bool cancelled = await scheduler.CancelAsync(delayed);
```

### Continuations

```csharp
string parentId = await scheduler.EnqueueAsync<ImportJob>();

await scheduler.ContinueWithAsync<PublishJob>(
    parentId,
    new ContinuationOptions
    {
        ExecuteOnSuccessOnly = true,
        MaxWaitTime = TimeSpan.FromHours(2),
        JobOptions = new JobOptions { Queue = "publishing" }
    });
```

`ContinuationOptions` defaults to success-only execution, no failure-only execution, and a 24-hour maximum wait. In the in-memory provider the returned continuation ID is a tracking ID, while execution creates a separate scheduled job ID.

### Priority and queues

`JobPriority` and `JobOptions.Queue` are portable contract values. `PriorityQueueManager` has strict `Critical` → `High` → `Normal` → `Low` dequeue ordering within a named queue, but it is internal and is not used by the registered in-memory scheduler. Do not assume priority ordering without a concrete provider adapter that maps these values.

### Batches and parent-child work

`JobBatch`, `BatchOptions`, `ParentJob`, `ChildJob`, and `ParentChildJobOptions` model parallel, sequential, and dependency-based execution. The contracts are useful to adapter authors, but the current in-memory provider does not enforce batch execution mode, dependencies, concurrency, batch retry, batch timeout, or parent-child options. Its batch worker marks the batch complete without invoking the contained jobs.

### Dead-letter queue

`IDeadLetterQueue` and `InMemoryDeadLetterQueue` support storing, filtering, removing, and clearing `FailedJob` records. They are not registered by the background-job extensions and are not connected automatically to scheduler failures. `RetryFailedJobAsync` on the in-memory store removes the record and returns its existing ID; it does not reschedule a job.

## Keyed service constants

`Mvp24Hours.Core.Extensions.KeyedServices.ServiceKeys.BackgroundJobs` defines:

| Constant | Value |
|---|---|
| `Hangfire` | `BackgroundJobs:Hangfire` |
| `Quartz` | `BackgroundJobs:Quartz` |
| `InMemory` | `BackgroundJobs:InMemory` |
| `Default` | `BackgroundJobs:Default` |

The built-in background-job registration methods do **not** call `AddKeyedSingleton`. These constants are stable keys for application-owned registrations, not evidence that keyed schedulers are already available:

```csharp
services.AddKeyedSingleton<IJobScheduler>(
    ServiceKeys.BackgroundJobs.InMemory,
    (sp, _) => new InMemoryJobProvider(
        sp,
        sp.GetService<ILogger<InMemoryJobProvider>>()));
```

See [Keyed Services](../modernization/keyed-services.md) for native DI resolution patterns.

## Dashboard and health

`DashboardIntegrationHelpers.UseHangfireDashboard(...)` and `UseQuartzDashboard(...)` both throw `NotSupportedException`. Quartz has no built-in dashboard, and the Hangfire helper cannot call vendor middleware because Hangfire is not referenced. Configure a vendor dashboard directly in the application and protect it with authentication and authorization.

`MapJobHealthChecks("/health/jobs")` maps a plain-text placeholder endpoint; it does not execute ASP.NET Core health checks. Use the real registration instead:

```csharp
builder.Services.AddHealthChecks()
    .AddBackgroundJobHealthCheck(
        name: "background-jobs",
        configureOptions: options =>
        {
            options.ScheduleTestJob = false;
            options.TimeoutSeconds = 5;
        },
        tags: ["background-jobs", "ready"]);
```

The current check confirms only that `IJobScheduler` can be resolved and returns `Healthy`. Even when `ScheduleTestJob` is `true`, no job is scheduled. `DegradedThresholdMs` (1,000) and `FailureThresholdMs` (5,000) are declared but not evaluated. See the [Health Checks catalog](health-checks.md) for all defaults and endpoint routing.

## Background Jobs or CronJob?

| Choose | When |
|---|---|
| Background Jobs abstraction | You need the `IJobScheduler` contract, immediate or delayed dispatch, status/cancellation, continuations, batches, parent-child modeling, or plan to implement a durable provider adapter. Today, only the in-memory provider executes jobs. |
| [CronJob](../cronjob.md) | You need recurring in-process hosted services now, with five- or six-field CRON expressions, `TimeProvider`, overlap control, resilience, state, distributed locks, runtime control, metrics, and a functional health check. |
| Hangfire or Quartz directly | You need durable production scheduling now. Configure the vendor package, storage, workers, and dashboard directly; the Mvp24Hours adapters are not implemented. |

CronJob is host-bound: schedules run while the application is running. A fully implemented Hangfire or Quartz adapter could provide durable, provider-backed scheduling across restarts and nodes, but the current abstraction does not yet deliver that behavior.

## Related pages

- [Infrastructure modules](home.md)
- [Health Checks](health-checks.md)
- [CronJob advanced configuration](../cronjob-advanced.md)
- [CronJob observability](../cronjob-observability.md)
- [Hangfire documentation](https://www.hangfire.io/)
- [Quartz.NET documentation](https://www.quartz-scheduler.net/)
