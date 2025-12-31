# CronJob - Advanced Features

This document describes the advanced features of the CronJob module, including advanced configuration, execution context, 6-field CRON expressions, job dependencies, distributed locking, state persistence, pause/resume control, and event hooks.

## Features

- **CronJobOptions<T>**: Comprehensive configuration via code or appsettings.json
- **CronJobGlobalOptions**: Global defaults for all CronJobs
- **Configuration Validation**: CRON expression validation on startup
- **Multiple Instances**: Register same job type with different configurations
- **ICronJobContext**: Execution context with metadata (JobId, StartTime, Attempt)
- **6-field CRON Expressions**: Second-level precision scheduling
- **Job Dependencies**: Execute jobs after others complete
- **Distributed Locking**: Prevent duplicate executions in clusters
- **ICronJobStateStore**: Job state persistence
- **Pause/Resume**: Runtime job control
- **Event Hooks**: Lifecycle event callbacks

## Advanced Configuration

The CronJob module provides comprehensive configuration options through `CronJobOptions<T>` and `CronJobGlobalOptions`.

### CronJobOptions<T> - Per-Job Configuration

Configure each CronJob with full control over schedule, resilience, and observability:

```csharp
services.AddCronJobWithOptions<MyJob>(options =>
{
    // Schedule
    options.CronExpression = "*/5 * * * *";   // Every 5 minutes
    options.TimeZone = "UTC";                  // or "America/Sao_Paulo", etc.
    options.Enabled = true;                    // Enable/disable job
    options.Description = "Processes pending orders";
    
    // Resilience
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
    options.RetryDelay = TimeSpan.FromSeconds(5);
    options.UseExponentialBackoff = true;
    
    // Circuit Breaker
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;
    options.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
    
    // Overlapping and Shutdown
    options.PreventOverlapping = true;
    options.GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    
    // Distributed Locking
    options.EnableDistributedLocking = true;
    options.DistributedLockExpiry = TimeSpan.FromMinutes(5);
    
    // Observability
    options.EnableObservability = true;
    options.EnableHealthCheck = true;
    
    // Dependencies
    options.DependsOn = new[] { "DataCollectionJob", "ValidationJob" };
});
```

### CronJobGlobalOptions - Global Defaults

Configure global defaults that apply to all CronJobs:

```csharp
services.AddCronJobGlobalOptions(options =>
{
    // Default timezone for all jobs
    options.DefaultTimeZone = "UTC";
    options.JobsEnabledByDefault = true;
    
    // Default resilience settings
    options.EnableRetryByDefault = true;
    options.DefaultMaxRetryAttempts = 3;
    options.DefaultRetryDelay = TimeSpan.FromSeconds(1);
    options.UseExponentialBackoffByDefault = true;
    
    // Default circuit breaker
    options.EnableCircuitBreakerByDefault = false;
    options.DefaultCircuitBreakerFailureThreshold = 5;
    options.DefaultCircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
    
    // Overlapping and shutdown
    options.PreventOverlappingByDefault = true;
    options.DefaultGracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    
    // Observability
    options.EnableObservability = true;
    options.EnableHealthChecks = true;
    options.RegisterAggregateHealthCheck = true;
    options.AggregateHealthCheckName = "cronjobs";
    options.HealthCheckTags = new[] { "cronjob", "background" };
    
    // Validation
    options.ValidateCronExpressionsOnStartup = true;
    options.LogConfigurationWarnings = true;
});
```

### Configuration via appsettings.json

Configure CronJobs declaratively using `appsettings.json`:

```json
{
  "CronJobs": {
    "Global": {
      "DefaultTimeZone": "UTC",
      "EnableObservability": true,
      "ValidateCronExpressionsOnStartup": true,
      "EnableRetryByDefault": true,
      "DefaultMaxRetryAttempts": 3
    },
    "OrderProcessingJob": {
      "CronExpression": "*/5 * * * *",
      "TimeZone": "UTC",
      "Enabled": true,
      "Description": "Processes pending orders every 5 minutes",
      "EnableRetry": true,
      "MaxRetryAttempts": 3,
      "PreventOverlapping": true
    },
    "ReportGenerationJob": {
      "CronExpression": "0 0 * * *",
      "TimeZone": "America/New_York",
      "Enabled": true,
      "Description": "Generates daily reports at midnight",
      "EnableCircuitBreaker": true,
      "CircuitBreakerFailureThreshold": 3
    }
  }
}
```

Register jobs from configuration:

```csharp
// Load global options from configuration
services.AddCronJobGlobalOptionsFromConfiguration(configuration);

// Register jobs from configuration
services.AddCronJobFromConfiguration<OrderProcessingJob>(configuration);
services.AddResilientCronJobFromConfiguration<ReportGenerationJob>(configuration);
services.AddAdvancedCronJobFromConfiguration<DataSyncJob>(configuration);
```

### Startup Validation

CRON expressions and configuration are validated at startup:

```csharp
// Invalid expression will fail application startup
services.AddCronJobWithOptions<MyJob>(options =>
{
    options.CronExpression = "invalid expression"; // ❌ Will fail at startup
});
```

Validation includes:
- CRON expression syntax (5-field or 6-field)
- Timezone identifier validity
- Retry and circuit breaker parameter ranges
- Timeout value validation
- Instance name format (alphanumeric, hyphens, underscores)

### Multiple Instances of Same Job Type

Register multiple instances of the same job type with different configurations:

```csharp
// Via code
services.AddCronJobInstances<DataSyncJob>(
    new CronJobOptions<DataSyncJob>
    {
        InstanceName = "DataSync-US",
        CronExpression = "0 0 * * *",
        TimeZone = "America/New_York",
        Description = "US data sync at midnight EST"
    },
    new CronJobOptions<DataSyncJob>
    {
        InstanceName = "DataSync-EU",
        CronExpression = "0 0 * * *",
        TimeZone = "Europe/London",
        Description = "EU data sync at midnight GMT"
    },
    new CronJobOptions<DataSyncJob>
    {
        InstanceName = "DataSync-APAC",
        CronExpression = "0 0 * * *",
        TimeZone = "Asia/Tokyo",
        Description = "APAC data sync at midnight JST"
    }
);
```

Via appsettings.json:

```json
{
  "CronJobs": {
    "DataSyncJob": {
      "Instances": {
        "DataSync-US": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "America/New_York"
        },
        "DataSync-EU": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "Europe/London"
        },
        "DataSync-APAC": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "Asia/Tokyo"
        }
      }
    }
  }
}
```

```csharp
services.AddCronJobInstancesFromConfiguration<DataSyncJob>(configuration);
```

### Disable Jobs Without Code Changes

Disable a job via configuration without modifying code:

```json
{
  "CronJobs": {
    "MaintenanceJob": {
      "Enabled": false
    }
  }
}
```

Or via environment variables:

```bash
CronJobs__MaintenanceJob__Enabled=false
```

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

## Unit Testing

The CronJob module provides a comprehensive test infrastructure to facilitate unit testing of your CronJobs.

### TestCronJobService&lt;T&gt; Helper

The `TestCronJobService<T>` class is a test helper that simplifies CronJob testing:

```csharp
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;
using Microsoft.Extensions.TimeProvider.Testing;

public class MyCronJobTests
{
    [Fact]
    public async Task Job_ShouldExecuteSuccessfully()
    {
        // Arrange
        await using var testService = new TestCronJobService<MyJob>();
        
        // Configure a FakeTimeProvider for controlled time manipulation
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        testService.UseTimeProvider(fakeTime);
        
        // Add additional services if needed
        testService.ConfigureServices(services =>
        {
            services.AddSingleton<IMyDependency, MyDependency>();
        });
        
        testService.BuildServiceProvider();
        
        var config = CreateTestConfig();
        
        // Act
        var job = await testService.StartResilientJobAsync<MyJob>(config);
        
        // Advance time to trigger execution
        fakeTime.Advance(TimeSpan.FromMinutes(1));
        await Task.Delay(100); // Allow job to execute
        
        // Assert
        testService.Tracker.ExecutionCount.Should().BeGreaterThan(0);
        testService.Tracker.HasFailures.Should().BeFalse();
        
        // Cleanup
        await testService.StopJobAsync(job);
    }
}
```

### ExecutionTracker

The `ExecutionTracker` class helps track job executions for assertions:

```csharp
public class ExecutionTracker
{
    // Number of executions
    public int ExecutionCount { get; }
    
    // All recorded executions
    public IReadOnlyList<ExecutionRecord> Executions { get; }
    
    // Whether any execution failed
    public bool HasFailures { get; }
    
    // Record a successful execution
    public void RecordExecution(TimeSpan? duration = null);
    
    // Record a failed execution
    public void RecordFailure(Exception exception, TimeSpan? duration = null);
    
    // Clear all recorded executions
    public void Clear();
}
```

### Testing Resilience

Test retry behavior and circuit breaker:

```csharp
[Fact]
public async Task Job_ShouldRetryOnFailure()
{
    // Arrange
    await using var testService = new TestCronJobService<FailingCronJob>();
    var fakeTime = new FakeTimeProvider();
    testService.UseTimeProvider(fakeTime);
    testService.BuildServiceProvider();

    var config = TestCronJobFactory.CreateConfig<FailingCronJob>(
        cronExpression: "* * * * *",
        resilience: new CronJobResilienceConfig<FailingCronJob>
        {
            EnableRetry = true,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        });

    // Act
    var job = await testService.StartResilientJobAsync<FailingCronJob>(config);
    fakeTime.Advance(TimeSpan.FromMinutes(1));
    await Task.Delay(200);

    // Assert - Should have attempted 3 times (1 initial + 2 retries)
    job.DoWorkInvocationCount.Should().Be(3);
    
    await testService.StopJobAsync(job);
}
```

### Testing Health Checks

```csharp
[Fact]
public async Task HealthCheck_ShouldReturnHealthy_WhenJobSucceeds()
{
    // Arrange
    var metrics = new CronJobMetricsService();
    metrics.RecordExecution("TestJob", TimeSpan.FromMilliseconds(100), true);
    
    var healthCheck = new CronJobHealthCheck(
        metrics,
        Options.Create(new CronJobHealthCheckOptions
        {
            HealthyThresholdPercentage = 80
        }));
    
    // Act
    var result = await healthCheck.CheckHealthAsync(
        new HealthCheckContext(),
        CancellationToken.None);
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
}
```

### Test Coverage

The CronJob module includes **91 unit tests** covering:

| Category | Tests | Description |
|----------|-------|-------------|
| Retry & Resilience | 15 | Retry policies, exponential backoff, jitter |
| Circuit Breaker | 12 | State transitions, half-open testing, failure thresholds |
| Health Checks | 18 | Healthy/Degraded/Unhealthy states, metrics-based health |
| Overlapping Execution | 14 | Lock acquisition, timeout, cancellation |
| Graceful Shutdown | 10 | Proper cleanup, timeout handling |
| Metrics | 12 | Execution tracking, failure counting |
| Core Service | 10 | Basic job execution, scheduling |

Run tests with:

```bash
dotnet test src/Tests/Mvp24Hours.Infrastructure.CronJob.Test
```

## See Also

- [CronJob Basic](cronjob.md) - Main module documentation
- [CronJob Resilience](cronjob-resilience.md) - Retry, circuit breaker, overlapping prevention
- [CronJob Observability](cronjob-observability.md) - Health checks, metrics, structured logging

