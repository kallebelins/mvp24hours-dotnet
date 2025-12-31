//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;

/// <summary>
/// Helper class for creating and testing CronJob services in unit tests.
/// </summary>
/// <typeparam name="T">The type of the CronJob service being tested.</typeparam>
public class TestCronJobService<T> : IAsyncDisposable
    where T : class
{
    private readonly ServiceCollection _services = new();
    private readonly Mock<IHostApplicationLifetime> _hostLifetimeMock = new();
    private ServiceProvider? _serviceProvider;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the TimeProvider for time-related operations.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Gets the mock IHostApplicationLifetime.
    /// </summary>
    public Mock<IHostApplicationLifetime> HostLifetimeMock => _hostLifetimeMock;

    /// <summary>
    /// Gets the service collection for additional registrations.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Gets the execution tracker for verifying job executions.
    /// </summary>
    public ExecutionTracker Tracker { get; } = new();

    /// <summary>
    /// Gets the built service provider. Null until BuildServiceProvider() is called.
    /// </summary>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>
    /// Configures the IHostApplicationLifetime mock.
    /// </summary>
    public TestCronJobService<T> ConfigureHostLifetime(Action<Mock<IHostApplicationLifetime>> configure)
    {
        configure(_hostLifetimeMock);
        return this;
    }

    /// <summary>
    /// Configures the service collection with additional services.
    /// </summary>
    public TestCronJobService<T> ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Sets a custom TimeProvider (e.g., FakeTimeProvider for testing).
    /// </summary>
    public TestCronJobService<T> UseTimeProvider(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
        return this;
    }

    /// <summary>
    /// Builds the service provider with all configured services.
    /// </summary>
    public TestCronJobService<T> BuildServiceProvider()
    {
        // Add default services if not already registered
        _services.AddSingleton(TimeProvider);
        _services.AddSingleton(Tracker);
        _services.AddSingleton(_hostLifetimeMock.Object);
        _services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        _services.AddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
        _services.AddSingleton<CronJobCircuitBreaker>();

        _serviceProvider = _services.BuildServiceProvider();
        return this;
    }

    /// <summary>
    /// Creates and starts a basic CronJob service.
    /// </summary>
    public async Task<TJob> StartBasicJobAsync<TJob>(
        IScheduleConfig<TJob> config,
        Func<CancellationToken, Task>? doWorkAction = null,
        CancellationToken cancellationToken = default)
        where TJob : CronJobService<TJob>
    {
        EnsureServiceProvider();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var logger = NullLogger<CronJobService<TJob>>.Instance;
        
        // Create job instance using Activator for flexibility
        var job = (TJob)Activator.CreateInstance(
            typeof(TJob),
            config,
            _hostLifetimeMock.Object,
            _serviceProvider!,
            logger,
            TimeProvider)!;

        await job.StartAsync(_cts.Token);
        return job;
    }

    /// <summary>
    /// Creates and starts a resilient CronJob service.
    /// </summary>
    public async Task<TJob> StartResilientJobAsync<TJob>(
        IResilientScheduleConfig<TJob> config,
        CancellationToken cancellationToken = default)
        where TJob : ResilientCronJobService<TJob>
    {
        EnsureServiceProvider();
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var logger = _serviceProvider!.GetService<ILogger<ResilientCronJobService<TJob>>>() 
            ?? NullLogger<ResilientCronJobService<TJob>>.Instance;
        var executionLock = _serviceProvider.GetRequiredService<ICronJobExecutionLock>();
        var circuitBreaker = _serviceProvider.GetRequiredService<CronJobCircuitBreaker>();
        
        var job = (TJob)Activator.CreateInstance(
            typeof(TJob),
            config,
            _hostLifetimeMock.Object,
            _serviceProvider,
            executionLock,
            circuitBreaker,
            logger,
            TimeProvider)!;

        await job.StartAsync(_cts.Token);
        return job;
    }

    /// <summary>
    /// Stops the currently running job.
    /// </summary>
    public async Task StopJobAsync<TJob>(TJob job, TimeSpan? timeout = null) 
        where TJob : BackgroundService
    {
        var stopCts = timeout.HasValue 
            ? new CancellationTokenSource(timeout.Value) 
            : new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        await job.StopAsync(stopCts.Token);
        
        if (job is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (job is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Runs a job for the specified duration then stops it.
    /// </summary>
    public async Task<ExecutionTracker> RunForDurationAsync<TJob>(
        TJob job,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
        where TJob : BackgroundService
    {
        using var timedCts = new CancellationTokenSource(duration);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timedCts.Token);

        try
        {
            await Task.Delay(duration, linkedCts.Token);
        }
        catch (OperationCanceledException) { }

        await StopJobAsync(job);
        return Tracker;
    }

    /// <summary>
    /// Waits until the job has executed the specified number of times.
    /// </summary>
    public async Task<ExecutionTracker> WaitForExecutionsAsync(
        int count,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        while (Tracker.ExecutionCount < count && !linkedCts.Token.IsCancellationRequested)
        {
            await Task.Delay(10, linkedCts.Token);
        }

        return Tracker;
    }

    /// <summary>
    /// Cancels the job execution.
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }

    private void EnsureServiceProvider()
    {
        if (_serviceProvider == null)
        {
            BuildServiceProvider();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _cts?.Cancel();
        _cts?.Dispose();
        
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Tracks execution information for test assertions.
/// </summary>
public class ExecutionTracker
{
    private readonly List<ExecutionRecord> _executions = new();
    private int _executionCount;

    /// <summary>
    /// Gets the number of executions.
    /// </summary>
    public int ExecutionCount => Interlocked.CompareExchange(ref _executionCount, 0, 0);

    /// <summary>
    /// Gets all recorded executions.
    /// </summary>
    public IReadOnlyList<ExecutionRecord> Executions => _executions.AsReadOnly();

    /// <summary>
    /// Gets whether any execution failed.
    /// </summary>
    public bool HasFailures => _executions.Exists(e => e.Exception != null);

    /// <summary>
    /// Records a successful execution.
    /// </summary>
    public void RecordExecution(TimeSpan? duration = null)
    {
        Interlocked.Increment(ref _executionCount);
        lock (_executions)
        {
            _executions.Add(new ExecutionRecord
            {
                StartTime = DateTimeOffset.UtcNow,
                Duration = duration,
                Success = true
            });
        }
    }

    /// <summary>
    /// Records a failed execution.
    /// </summary>
    public void RecordFailure(Exception exception, TimeSpan? duration = null)
    {
        Interlocked.Increment(ref _executionCount);
        lock (_executions)
        {
            _executions.Add(new ExecutionRecord
            {
                StartTime = DateTimeOffset.UtcNow,
                Duration = duration,
                Success = false,
                Exception = exception
            });
        }
    }

    /// <summary>
    /// Clears all recorded executions.
    /// </summary>
    public void Clear()
    {
        lock (_executions)
        {
            _executions.Clear();
            Interlocked.Exchange(ref _executionCount, 0);
        }
    }
}

/// <summary>
/// Represents a single execution record.
/// </summary>
public class ExecutionRecord
{
    /// <summary>
    /// Gets or sets the execution start time.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets or sets the execution duration.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets or sets whether the execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the exception if the execution failed.
    /// </summary>
    public Exception? Exception { get; init; }
}

