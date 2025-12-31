# CronJob Resilience

The CronJob module provides comprehensive resilience patterns for production-grade background job execution. This includes retry policies, circuit breaker pattern, overlapping execution prevention, and graceful shutdown handling.

## Features

- **Retry Policy**: Configurable retry with exponential backoff and jitter
- **Circuit Breaker**: Prevents repeated execution of failing jobs
- **Overlapping Prevention**: Ensures only one execution runs at a time
- **Graceful Shutdown**: Properly handles application shutdown with configurable timeout
- **Execution Timeout**: Cancels long-running jobs after a configured duration
- **CancellationToken Propagation**: Correctly propagates cancellation to all nested operations
- **OpenTelemetry Integration**: All resilience operations are instrumented for observability

## Installation

The resilience features are included in the base package:

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Creating a Resilient CronJob

Inherit from `ResilientCronJobService<T>` instead of `CronJobService<T>`:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;

public class MyResilientJob : ResilientCronJobService<MyResilientJob>
{
    public MyResilientJob(
        IResilientScheduleConfig<MyResilientJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<MyResilientJob>> logger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Your job logic here
        // Retries, circuit breaker, and overlapping prevention are handled automatically
        
        var service = _serviceProvider!.GetRequiredService<IMyService>();
        await service.ProcessAsync(cancellationToken);
    }
}
```

## Configuration

### Full Resilience Configuration

```csharp
services.AddResilientCronJob<MyResilientJob>(config =>
{
    // Schedule configuration
    config.CronExpression = "*/5 * * * *"; // Every 5 minutes
    config.TimeZoneInfo = TimeZoneInfo.Utc;
    
    // Retry configuration
    config.Resilience.EnableRetry = true;
    config.Resilience.MaxRetryAttempts = 3;
    config.Resilience.RetryDelay = TimeSpan.FromSeconds(1);
    config.Resilience.UseExponentialBackoff = true;
    config.Resilience.MaxRetryDelay = TimeSpan.FromSeconds(30);
    config.Resilience.RetryJitterFactor = 0.2; // 20% jitter
    
    // Circuit breaker configuration
    config.Resilience.EnableCircuitBreaker = true;
    config.Resilience.CircuitBreakerFailureThreshold = 5;
    config.Resilience.CircuitBreakerDuration = TimeSpan.FromSeconds(30);
    config.Resilience.CircuitBreakerSuccessThreshold = 1;
    config.Resilience.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(60);
    
    // Overlapping prevention
    config.Resilience.PreventOverlapping = true;
    config.Resilience.LogOverlappingSkipped = true;
    config.Resilience.OverlappingWaitTimeout = TimeSpan.Zero; // Skip immediately
    
    // Graceful shutdown
    config.Resilience.GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    config.Resilience.WaitForExecutionOnShutdown = true;
    
    // Execution timeout
    config.Resilience.ExecutionTimeout = TimeSpan.FromMinutes(5);
    config.Resilience.PropagateCancellation = true;
    
    // Callbacks
    config.Resilience.OnRetry = (ex, attempt, delay) =>
    {
        Console.WriteLine($"Retry {attempt}, waiting {delay.TotalSeconds}s: {ex.Message}");
    };
    
    config.Resilience.OnCircuitBreakerStateChange = (oldState, newState) =>
    {
        Console.WriteLine($"Circuit breaker: {oldState} -> {newState}");
    };
    
    config.Resilience.OnOverlappingSkipped = () =>
    {
        Console.WriteLine("Execution skipped - previous still running");
    };
    
    config.Resilience.OnJobFailed = (ex) =>
    {
        Console.WriteLine($"Job failed after all retries: {ex.Message}");
    };
});
```

### Convenience Methods

```csharp
// Simple resilient job (overlapping prevention only)
services.AddResilientCronJob<MyJob>("*/5 * * * *");

// Full resilience (retry + circuit breaker + overlapping)
services.AddResilientCronJobWithFullResilience<MyJob>("*/5 * * * *", TimeZoneInfo.Utc);

// With retry only
services.AddResilientCronJobWithRetry<MyJob>(
    "0 * * * *",
    maxRetryAttempts: 5,
    useExponentialBackoff: true);

// With circuit breaker only
services.AddResilientCronJobWithCircuitBreaker<MyJob>(
    "* * * * *",
    failureThreshold: 3,
    breakDuration: TimeSpan.FromMinutes(1));
```

## Retry Policy

The retry policy automatically retries failed job executions with configurable behavior.

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `EnableRetry` | `false` | Enable retry policy |
| `MaxRetryAttempts` | `3` | Maximum number of retry attempts |
| `RetryDelay` | `1 second` | Initial delay between retries |
| `UseExponentialBackoff` | `true` | Use exponential backoff (1s, 2s, 4s, ...) |
| `MaxRetryDelay` | `30 seconds` | Maximum delay when using exponential backoff |
| `RetryJitterFactor` | `0.2` | Jitter factor (0-1) to prevent thundering herd |
| `ShouldRetryOnException` | `null` | Predicate to filter retryable exceptions |

### Exponential Backoff with Jitter

When `UseExponentialBackoff` is enabled, delays follow the pattern:

```
delay = min(initialDelay * 2^(attempt-1), maxDelay) ± jitter
```

Example with default settings:
- Attempt 1: ~1s (800ms - 1.2s with jitter)
- Attempt 2: ~2s (1.6s - 2.4s with jitter)
- Attempt 3: ~4s (3.2s - 4.8s with jitter)

### Filtering Retryable Exceptions

```csharp
config.Resilience.ShouldRetryOnException = ex =>
{
    // Only retry transient errors
    return ex is HttpRequestException 
        || ex is TimeoutException
        || ex is SqlException { IsTransient: true };
};
```

## Circuit Breaker

The circuit breaker pattern prevents repeated execution of a job that's consistently failing, allowing the system to recover.

### States

| State | Description |
|-------|-------------|
| **Closed** | Normal operation, executions allowed |
| **Open** | Executions blocked after reaching failure threshold |
| **Half-Open** | Test executions allowed after break duration |

### State Transitions

```
Closed ─── failures ≥ threshold ──→ Open
  ↑                                   │
  │                                   │ break duration elapsed
  │                                   ↓
  └──── success ≥ threshold ──── Half-Open
                                      │
                                      │ failure
                                      ↓
                                    Open
```

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `EnableCircuitBreaker` | `false` | Enable circuit breaker |
| `CircuitBreakerFailureThreshold` | `5` | Failures before opening |
| `CircuitBreakerDuration` | `30 seconds` | How long circuit stays open |
| `CircuitBreakerSuccessThreshold` | `1` | Successes needed to close from half-open |
| `CircuitBreakerSamplingDuration` | `60 seconds` | Window for counting failures |

### Monitoring Circuit Breaker State

```csharp
public class MyJob : ResilientCronJobService<MyJob>
{
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Access current state
        var state = CircuitBreakerState;
        _logger.LogInformation("Current circuit breaker state: {State}", state);
        
        // Execution count and skip count are available
        _logger.LogInformation("Executions: {Count}, Skipped: {Skipped}", 
            ExecutionCount, SkippedCount);
    }
}
```

## Overlapping Prevention

Prevents concurrent executions of the same job, useful for jobs that shouldn't run in parallel.

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `PreventOverlapping` | `true` | Enable overlapping prevention |
| `LogOverlappingSkipped` | `true` | Log when execution is skipped |
| `OverlappingWaitTimeout` | `TimeSpan.Zero` | Time to wait for lock (0 = skip immediately) |

### Behavior

- **Immediate Skip**: With `OverlappingWaitTimeout = TimeSpan.Zero`, if a previous execution is still running, the new execution is skipped immediately.
- **Wait with Timeout**: Set a timeout to wait for the previous execution to complete before skipping.

```csharp
// Wait up to 10 seconds for lock before skipping
config.Resilience.OverlappingWaitTimeout = TimeSpan.FromSeconds(10);
```

## Graceful Shutdown

Properly handles application shutdown, giving running jobs time to complete.

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `GracefulShutdownTimeout` | `30 seconds` | Maximum time to wait for job completion |
| `WaitForExecutionOnShutdown` | `true` | Whether to wait for current execution |

### Behavior

1. When shutdown is requested, the job receives cancellation
2. The framework waits up to `GracefulShutdownTimeout` for completion
3. If timeout is exceeded, the job is forcefully cancelled
4. Resources are properly disposed

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // Check cancellation periodically for responsive shutdown
    foreach (var item in items)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessItemAsync(item, cancellationToken);
    }
}
```

## Execution Timeout

Automatically cancels job executions that take too long.

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `ExecutionTimeout` | `null` | Maximum execution time (null = no timeout) |
| `PropagateCancellation` | `true` | Propagate cancellation to nested operations |

```csharp
// Cancel execution after 5 minutes
config.Resilience.ExecutionTimeout = TimeSpan.FromMinutes(5);
```

## OpenTelemetry Tracing

Resilience operations are fully instrumented:

### Additional Tags

| Tag | Description |
|-----|-------------|
| `cronjob.resilience.retry_enabled` | Whether retry is enabled |
| `cronjob.resilience.retry_attempt` | Current retry attempt |
| `cronjob.resilience.retry_count` | Total retries across all executions |
| `cronjob.resilience.circuit_breaker_enabled` | Whether circuit breaker is enabled |
| `cronjob.resilience.circuit_breaker_state` | Current circuit breaker state |
| `cronjob.resilience.prevent_overlapping` | Whether overlapping prevention is enabled |
| `cronjob.resilience.execution_skipped` | Whether execution was skipped |
| `cronjob.resilience.skip_reason` | Reason for skipping |
| `cronjob.resilience.timed_out` | Whether execution timed out |

## Distributed Lock Implementation

For multi-instance deployments, implement `ICronJobExecutionLock`:

```csharp
public class RedisDistributedCronJobLock : ICronJobExecutionLock
{
    private readonly IDistributedLockFactory _lockFactory;
    
    public async Task<ICronJobLockHandle?> TryAcquireAsync(
        string jobName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var lockKey = $"cronjob:lock:{jobName}";
        var handle = await _lockFactory.TryAcquireAsync(lockKey, timeout);
        
        return handle != null ? new RedisLockHandle(handle, jobName) : null;
    }
    
    // ... implement other methods
}

// Register custom lock
services.AddCronJobResilienceInfrastructure<RedisDistributedCronJobLock>();
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ExecutionCount` | `long` | Total number of executions |
| `RetryCount` | `long` | Total retries across all executions |
| `SkippedCount` | `long` | Executions skipped (overlapping/circuit breaker) |
| `CircuitBreakerState` | `CircuitBreakerState` | Current circuit breaker state |

## Best Practices

1. **Start Conservative**: Begin with low retry counts and increase based on observed behavior
2. **Use Jitter**: Always enable jitter to prevent thundering herd problems
3. **Monitor Circuit Breaker**: Set up alerts for circuit breaker state changes
4. **Respect Cancellation**: Always check `cancellationToken` in long-running operations
5. **Log Appropriately**: Use the callback hooks to implement custom logging/alerting
6. **Test Failure Scenarios**: Write tests that simulate failures to verify resilience behavior

## See Also

- [CronJob Basics](cronjob.md)
- [Advanced Features](cronjob-advanced.md) - Context, dependencies, distributed locking, event hooks
- [CronJob Observability](cronjob-observability.md) - Health checks, metrics, structured logging
- [PeriodicTimer Modernization](modernization/periodic-timer.md)
- [TimeProvider Abstraction](modernization/time-provider.md)
- [Generic Resilience](modernization/generic-resilience.md)
- [Observability](observability/home.md)

