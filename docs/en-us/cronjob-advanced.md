# CronJob - Advanced Features

This document describes the advanced features of the CronJob module, including execution context, 6-field CRON expressions, job dependencies, distributed locking, state persistence, pause/resume control, and event hooks.

## Features

- **ICronJobContext**: Execution context with metadata (JobId, StartTime, Attempt)
- **6-field CRON Expressions**: Second-level precision scheduling
- **Job Dependencies**: Execute jobs after others complete
- **Distributed Locking**: Prevent duplicate executions in clusters
- **ICronJobStateStore**: Job state persistence
- **Pause/Resume**: Runtime job control
- **Event Hooks**: Lifecycle event callbacks

## ICronJobContext - Execution Context

The `ICronJobContext` provides metadata about the current job execution:

```csharp
public interface ICronJobContext
{
    // Identifiers
    Guid JobId { get; }
    string JobName { get; }
    Guid ExecutionId { get; }
    
    // Timing
    DateTimeOffset StartTime { get; }
    DateTimeOffset? ScheduledTime { get; }
    TimeSpan Elapsed { get; }
    bool IsTimedOut { get; }
    
    // Attempts
    int CurrentAttempt { get; }
    int MaxAttempts { get; }
    bool IsRetry { get; }
    
    // Metadata
    long ExecutionCount { get; }
    string? CorrelationId { get; }
    Guid? ParentJobId { get; }
    
    // Custom properties
    IDictionary<string, object?> Properties { get; }
    CancellationToken CancellationToken { get; }
}
```

### Accessing the Context

Use `ICronJobContextAccessor` to access the current context:

```csharp
public class MyJobService
{
    private readonly ICronJobContextAccessor _contextAccessor;
    
    public MyJobService(ICronJobContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }
    
    public async Task ProcessAsync()
    {
        var context = _contextAccessor.Context;
        
        if (context != null)
        {
            _logger.LogInformation(
                "Executing job {JobName}, attempt {Attempt} of {MaxAttempts}",
                context.JobName,
                context.CurrentAttempt,
                context.MaxAttempts);
            
            // Add custom properties
            context.Properties["ProcessedItems"] = 100;
            context.Properties["Status"] = "In progress";
            
            // Check timeout
            if (context.IsTimedOut)
            {
                throw new OperationCanceledException("Job timeout exceeded");
            }
        }
    }
}
```

### Configuration

```csharp
services.AddCronJobAdvancedInfrastructure(); // Registers ICronJobContextAccessor and other services
```

## 6-Field CRON Expressions (Seconds)

The module supports 6-field CRON expressions for second-level precision:

### Format

| Fields | Format |
|--------|--------|
| 5 fields | `minute hour day-of-month month day-of-week` |
| 6 fields | `second minute hour day-of-month month day-of-week` |

### Usage

```csharp
using Mvp24Hours.Infrastructure.CronJob.Scheduling;

// Standard 5-field expression
var nextRun5 = CronExpressionParser.GetNextOccurrence("*/5 * * * *");

// 6-field expression (with seconds)
var nextRun6 = CronExpressionParser.GetNextOccurrence("*/30 * * * * *"); // Every 30 seconds

// Auto-detect format
var format = CronExpressionParser.DetectFormat("*/30 * * * * *"); 
// Returns: CronExpressionFormat.WithSeconds

// Get human-readable description
var description = CronExpressionParser.GetDescription("*/30 * * * * *");
// Returns: "Every 30 seconds"
```

### 6-Field Expression Examples

| Expression | Description |
|------------|-------------|
| `*/30 * * * * *` | Every 30 seconds |
| `0 */5 * * * *` | Every 5 minutes, at second 0 |
| `15 30 * * * *` | At second 15 of every minute 30 |
| `0 0 9 * * 1-5` | At 9 AM on weekdays, at second 0 |

## Job Dependencies

Execute jobs in order, respecting dependencies:

### Configuration

```csharp
// Define dependencies
services.AddCronJobDependency<ProcessDataJob, CollectDataJob>();
services.AddCronJobDependency<SendReportJob, ProcessDataJob>();

// Register advanced services
services.AddCronJobAdvancedInfrastructure();
```

### Execution Flow

```
CollectDataJob → ProcessDataJob → SendReportJob
```

### Interface

```csharp
public interface ICronJobDependency
{
    IReadOnlyList<Type> GetDependencies(Type jobType);
    void AddDependency<TJob, TDependsOn>() where TJob : class where TDependsOn : class;
    bool HasPendingDependencies(Type jobType);
    void MarkCompleted(Type jobType);
    void Reset(Type jobType);
}
```

### Programmatic Usage

```csharp
public class JobManager
{
    private readonly ICronJobDependencyTracker _tracker;
    
    public async Task ExecutePipelineAsync()
    {
        // Check if can execute
        if (_tracker.CanExecute(typeof(ProcessDataJob)))
        {
            await ExecuteJobAsync<ProcessDataJob>();
            _tracker.MarkAsCompleted(typeof(ProcessDataJob));
        }
        
        // Get next ready jobs
        var ready = _tracker.GetReadyJobs().ToList();
    }
}
```

## Distributed Locking - Prevent Duplicate Executions

Prevent the same job from running simultaneously across multiple instances (cluster):

### Interface

```csharp
public interface IDistributedCronJobLock
{
    Task<IDistributedCronJobLockHandle?> TryAcquireAsync(
        string jobName, 
        TimeSpan duration, 
        CancellationToken cancellationToken = default);
    
    Task<bool> IsLockedAsync(string jobName, CancellationToken cancellationToken = default);
    
    Task<DistributedLockInfo?> GetLockInfoAsync(
        string jobName, 
        CancellationToken cancellationToken = default);
}
```

### In-Memory Implementation (Single Instance)

```csharp
services.AddSingleton<IDistributedCronJobLock, InMemoryDistributedCronJobLock>();
```

### Redis Implementation (Cluster)

```csharp
// Install: Mvp24Hours.Infrastructure.CronJob.Redis
services.AddRedisCronJobLock(options =>
{
    options.ConnectionString = "localhost:6379";
    options.KeyPrefix = "cronjob:lock:";
});
```

### Usage in AdvancedCronJobService

The `AdvancedCronJobService` automatically uses distributed locking:

```csharp
services.AddAdvancedCronJob<MyJob>(options =>
{
    options.CronExpression = "*/5 * * * *";
    options.UseDistributedLock = true;
    options.LockTimeout = TimeSpan.FromMinutes(5);
});
```

## ICronJobStateStore - State Persistence

Persist job state between executions:

### Interface

```csharp
public interface ICronJobStateStore
{
    Task<CronJobState> GetStateAsync(string jobName, CancellationToken cancellationToken = default);
    Task SaveStateAsync(string jobName, CronJobState state, CancellationToken cancellationToken = default);
    Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default);
    Task SetPausedAsync(string jobName, bool isPaused, CancellationToken cancellationToken = default);
    Task ResetStateAsync(string jobName, CancellationToken cancellationToken = default);
}
```

### Job State

```csharp
public class CronJobState
{
    public bool IsPaused { get; set; }
    public DateTimeOffset? LastExecutionTime { get; set; }
    public DateTimeOffset? NextExecutionTime { get; set; }
    public DateTimeOffset? LastSuccessTime { get; set; }
    public DateTimeOffset? LastFailureTime { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan? LastExecutionDuration { get; set; }
    public string? LastError { get; set; }
    public IDictionary<string, object?> CustomData { get; set; }
}
```

### Implementations

```csharp
// In-Memory (default)
services.AddSingleton<ICronJobStateStore, InMemoryCronJobStateStore>();

// Redis (persistent)
services.AddRedisCronJobStateStore(options =>
{
    options.ConnectionString = "localhost:6379";
    options.KeyPrefix = "cronjob:state:";
});

// SQL Server
services.AddSqlCronJobStateStore(options =>
{
    options.ConnectionString = "...";
    options.TableName = "CronJobStates";
});
```

## Pause/Resume - Runtime Control

Pause and resume jobs programmatically:

### Interface

```csharp
public interface ICronJobController
{
    Task PauseAsync(string jobName, CancellationToken cancellationToken = default);
    Task ResumeAsync(string jobName, CancellationToken cancellationToken = default);
    Task TriggerAsync(string jobName, CancellationToken cancellationToken = default);
    Task<CronJobStatus> GetStatusAsync(string jobName, CancellationToken cancellationToken = default);
    Task<IEnumerable<CronJobStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default);
    Task PauseAllAsync(CancellationToken cancellationToken = default);
    Task ResumeAllAsync(CancellationToken cancellationToken = default);
}
```

### Usage

```csharp
[ApiController]
[Route("api/cronjobs")]
public class CronJobApiController : ControllerBase
{
    private readonly ICronJobController _controller;
    
    [HttpPost("{jobName}/pause")]
    public async Task<IActionResult> Pause(string jobName)
    {
        await _controller.PauseAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' paused." });
    }
    
    [HttpPost("{jobName}/resume")]
    public async Task<IActionResult> Resume(string jobName)
    {
        await _controller.ResumeAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' resumed." });
    }
    
    [HttpPost("{jobName}/trigger")]
    public async Task<IActionResult> TriggerNow(string jobName)
    {
        await _controller.TriggerAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' triggered manually." });
    }
    
    [HttpGet("{jobName}/status")]
    public async Task<IActionResult> GetStatus(string jobName)
    {
        var status = await _controller.GetStatusAsync(jobName);
        return Ok(status);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var statuses = await _controller.GetAllStatusesAsync();
        return Ok(statuses);
    }
}
```

## Event Hooks - Job Lifecycle

Implement handlers for lifecycle events:

### Handler Interfaces

```csharp
// Before starting
public interface ICronJobStartingHandler
{
    Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken);
}

// After successful completion
public interface ICronJobCompletedHandler
{
    Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);
}

// After failure
public interface ICronJobFailedHandler
{
    Task OnJobFailedAsync(ICronJobContext context, Exception exception, CancellationToken cancellationToken);
}

// When cancelled
public interface ICronJobCancelledHandler
{
    Task OnJobCancelledAsync(ICronJobContext context, CancellationToken cancellationToken);
}

// Before retry
public interface ICronJobRetryHandler
{
    Task OnJobRetryAsync(ICronJobContext context, Exception exception, int attempt, CancellationToken cancellationToken);
}

// When skipped (e.g., paused, pending dependency)
public interface ICronJobSkippedHandler
{
    Task OnJobSkippedAsync(ICronJobContext context, string reason, CancellationToken cancellationToken);
}
```

### Implementation

```csharp
public class NotificationJobHandler : 
    ICronJobStartingHandler, 
    ICronJobCompletedHandler, 
    ICronJobFailedHandler
{
    private readonly ILogger<NotificationJobHandler> _logger;
    private readonly INotificationService _notifications;
    
    public async Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobName} starting, execution #{Count}", 
            context.JobName, context.ExecutionCount);
    }
    
    public async Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobName} completed in {Duration:c}", 
            context.JobName, duration);
        
        // Notify if took too long
        if (duration > TimeSpan.FromMinutes(5))
        {
            await _notifications.SendAsync(
                $"Job {context.JobName} took {duration.TotalMinutes:F1} minutes");
        }
    }
    
    public async Task OnJobFailedAsync(ICronJobContext context, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Job {JobName} failed on attempt {Attempt}", 
            context.JobName, context.CurrentAttempt);
        
        // Critical alert
        await _notifications.SendAlertAsync(
            $"FAILURE: Job {context.JobName} - {exception.Message}");
    }
}
```

### Registration

```csharp
// Register individual handlers
services.AddCronJobEventHandler<ICronJobStartingHandler, NotificationJobHandler>();
services.AddCronJobEventHandler<ICronJobCompletedHandler, NotificationJobHandler>();
services.AddCronJobEventHandler<ICronJobFailedHandler, NotificationJobHandler>();

// Or use the extension that registers all
services.AddCronJobEventHandlers<NotificationJobHandler>();
```

### Handler Base Class

Use `CronJobEventHandlerBase` to implement only desired events:

```csharp
public class MyHandler : CronJobEventHandlerBase
{
    public override Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken ct)
    {
        // Only need to implement this one
        return base.OnJobCompletedAsync(context, duration, ct);
    }
}
```

## AdvancedCronJobService - Complete Service

The `AdvancedCronJobService<T>` integrates all advanced features:

```csharp
public class MyAdvancedJob : AdvancedCronJobService<MyAdvancedJob>
{
    public MyAdvancedJob(
        IScheduleConfig<MyAdvancedJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<AdvancedCronJobService<MyAdvancedJob>> logger,
        TimeProvider? timeProvider = null,
        ICronJobStateStore? stateStore = null,
        ICronJobDependency? dependencyTracker = null,
        IDistributedCronJobLock? distributedLock = null,
        CronJobEventDispatcher? eventDispatcher = null,
        ICronJobContextAccessor? contextAccessor = null)
        : base(config, hostApplication, rootServiceProvider, logger, 
               timeProvider, stateStore, dependencyTracker, distributedLock, 
               eventDispatcher, contextAccessor)
    {
    }
    
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Your code here - context and events are managed automatically
    }
}
```

### Complete Configuration

```csharp
services
    .AddCronJobAdvancedInfrastructure()    // Context, State, Dependencies, Events
    .AddAdvancedCronJob<MyAdvancedJob>(config =>
    {
        config.CronExpression = "*/5 * * * *";
        config.TimeZoneInfo = TimeZoneInfo.Utc;
    })
    .AddCronJobEventHandlers<NotificationJobHandler>();
```

## See Also

- [CronJob Basic](cronjob.md) - Main module documentation
- [CronJob Resilience](cronjob-resilience.md) - Retry, circuit breaker, overlapping prevention
- [CronJob Observability](cronjob-observability.md) - Health checks, metrics, structured logging

