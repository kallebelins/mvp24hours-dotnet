//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

/// <summary>
/// Test CronJob that uses ResilientCronJobService for testing resilience features.
/// </summary>
public class TestResilientCronJob : ResilientCronJobService<TestResilientCronJob>
{
    private readonly Func<CancellationToken, Task>? _workAction;
    private readonly ExecutionTracker? _tracker;

    /// <summary>
    /// Gets the number of times DoWork was invoked.
    /// </summary>
    public int DoWorkInvocationCount { get; private set; }

    public TestResilientCronJob(
        IResilientScheduleConfig<TestResilientCronJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider serviceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<TestResilientCronJob>> logger,
        TimeProvider? timeProvider = null,
        Func<CancellationToken, Task>? workAction = null)
        : base(config, hostApplication, serviceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
        _workAction = workAction;
        _tracker = serviceProvider.GetService<ExecutionTracker>();
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        DoWorkInvocationCount++;
        
        if (_workAction != null)
        {
            try
            {
                await _workAction(cancellationToken);
                _tracker?.RecordExecution();
            }
            catch (Exception ex)
            {
                _tracker?.RecordFailure(ex);
                throw;
            }
        }
        else
        {
            _tracker?.RecordExecution();
        }
    }
}

/// <summary>
/// Test CronJob that always fails after a configurable number of attempts.
/// </summary>
public class FailingCronJob : ResilientCronJobService<FailingCronJob>
{
    private readonly int _failUntilAttempt;
    private readonly ExecutionTracker? _tracker;
    
    /// <summary>
    /// Gets the number of times DoWork was invoked.
    /// </summary>
    public int DoWorkInvocationCount { get; private set; }

    public FailingCronJob(
        IResilientScheduleConfig<FailingCronJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider serviceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<FailingCronJob>> logger,
        TimeProvider? timeProvider = null,
        int failUntilAttempt = int.MaxValue)
        : base(config, hostApplication, serviceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
        _failUntilAttempt = failUntilAttempt;
        _tracker = serviceProvider.GetService<ExecutionTracker>();
    }

    public override Task DoWork(CancellationToken cancellationToken)
    {
        DoWorkInvocationCount++;
        
        if (DoWorkInvocationCount <= _failUntilAttempt)
        {
            var ex = new InvalidOperationException($"Simulated failure on attempt {DoWorkInvocationCount}");
            _tracker?.RecordFailure(ex);
            throw ex;
        }

        _tracker?.RecordExecution();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test CronJob that delays execution for simulating long-running tasks.
/// </summary>
public class SlowCronJob : ResilientCronJobService<SlowCronJob>
{
    private readonly TimeSpan _executionDuration;
    private readonly ExecutionTracker? _tracker;

    /// <summary>
    /// Gets the number of times DoWork was invoked.
    /// </summary>
    public int DoWorkInvocationCount { get; private set; }

    public SlowCronJob(
        IResilientScheduleConfig<SlowCronJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider serviceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<SlowCronJob>> logger,
        TimeProvider? timeProvider = null,
        TimeSpan? executionDuration = null)
        : base(config, hostApplication, serviceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
        _executionDuration = executionDuration ?? TimeSpan.FromSeconds(5);
        _tracker = serviceProvider.GetService<ExecutionTracker>();
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        DoWorkInvocationCount++;
        
        try
        {
            await Task.Delay(_executionDuration, cancellationToken);
            _tracker?.RecordExecution(_executionDuration);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
            throw;
        }
    }
}

/// <summary>
/// Factory for creating test CronJob instances with configurable behavior.
/// </summary>
public static class TestCronJobFactory
{
    /// <summary>
    /// Creates a ResilientScheduleConfig with the specified settings.
    /// </summary>
    public static ResilientScheduleConfig<T> CreateConfig<T>(
        string? cronExpression = null,
        TimeZoneInfo? timeZone = null,
        ICronJobResilienceConfig<T>? resilience = null)
    {
        return new ResilientScheduleConfig<T>
        {
            CronExpression = cronExpression,
            TimeZoneInfo = timeZone ?? TimeZoneInfo.Utc,
            Resilience = resilience ?? new CronJobResilienceConfig<T>()
        };
    }

    /// <summary>
    /// Creates a resilience config with retry enabled.
    /// </summary>
    public static CronJobResilienceConfig<T> CreateRetryConfig<T>(
        int maxAttempts = 3,
        TimeSpan? retryDelay = null,
        bool useExponentialBackoff = true)
    {
        return new CronJobResilienceConfig<T>
        {
            EnableRetry = true,
            MaxRetryAttempts = maxAttempts,
            RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(100),
            UseExponentialBackoff = useExponentialBackoff,
            MaxRetryDelay = TimeSpan.FromSeconds(5),
            PreventOverlapping = false
        };
    }

    /// <summary>
    /// Creates a resilience config with circuit breaker enabled.
    /// </summary>
    public static CronJobResilienceConfig<T> CreateCircuitBreakerConfig<T>(
        int failureThreshold = 3,
        TimeSpan? duration = null)
    {
        return new CronJobResilienceConfig<T>
        {
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = failureThreshold,
            CircuitBreakerDuration = duration ?? TimeSpan.FromSeconds(5),
            CircuitBreakerSuccessThreshold = 1,
            CircuitBreakerSamplingDuration = TimeSpan.FromMinutes(1),
            PreventOverlapping = false
        };
    }

    /// <summary>
    /// Creates a resilience config with overlapping prevention enabled.
    /// </summary>
    public static CronJobResilienceConfig<T> CreateOverlappingConfig<T>(
        TimeSpan? waitTimeout = null)
    {
        return new CronJobResilienceConfig<T>
        {
            PreventOverlapping = true,
            OverlappingWaitTimeout = waitTimeout ?? TimeSpan.Zero,
            LogOverlappingSkipped = true
        };
    }

    /// <summary>
    /// Creates a resilience config with full resilience features.
    /// </summary>
    public static CronJobResilienceConfig<T> CreateFullResilienceConfig<T>()
    {
        return CronJobResilienceConfig<T>.FullResilience();
    }
}

