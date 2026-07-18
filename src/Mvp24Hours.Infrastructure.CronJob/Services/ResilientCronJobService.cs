//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;

namespace Mvp24Hours.Infrastructure.CronJob.Services;

/// <summary>
/// Resilient version of CronJobService with retry, circuit breaker, overlapping prevention,
/// and graceful shutdown capabilities.
/// </summary>
/// <typeparam name="T">The type of the CronJob service (for configuration resolution)</typeparam>
/// <remarks>
/// <para>
/// This class extends the base CronJob functionality with enterprise-grade resilience patterns:
/// </para>
/// <list type="bullet">
/// <item><b>Retry Policy:</b> Configurable retry with exponential backoff and jitter</item>
/// <item><b>Circuit Breaker:</b> Prevents repeated execution of failing jobs</item>
/// <item><b>Overlapping Prevention:</b> Ensures only one execution runs at a time</item>
/// <item><b>Graceful Shutdown:</b> Properly handles application shutdown with configurable timeout</item>
/// <item><b>Cancellation Token Propagation:</b> Correctly propagates cancellation to all nested operations</item>
/// </list>
/// <para>
/// <b>OpenTelemetry Integration:</b> All resilience operations are instrumented for observability.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyResilientJob : ResilientCronJobService&lt;MyResilientJob&gt;
/// {
///     public MyResilientJob(
///         IResilientScheduleConfig&lt;MyResilientJob&gt; config,
///         IHostApplicationLifetime hostApplication,
///         IServiceProvider rootServiceProvider,
///         ICronJobExecutionLock executionLock,
///         CronJobCircuitBreaker circuitBreaker,
///         ILogger&lt;MyResilientJob&gt; logger,
///         TimeProvider? timeProvider = null)
///         : base(config, hostApplication, rootServiceProvider, executionLock, circuitBreaker, logger, timeProvider)
///     {
///     }
///
///     public override async Task DoWork(CancellationToken cancellationToken)
///     {
///         // Your job logic here
///     }
/// }
/// </code>
/// </example>
public abstract class ResilientCronJobService<T> : BackgroundService, IAsyncDisposable
{
    #region Fields

    private readonly CronExpression? _expression;
    private readonly TimeZoneInfo _timeZoneInfo;
    private readonly IHostApplicationLifetime _hostApplication;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly ICronJobExecutionLock _executionLock;
    private readonly CronJobCircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientCronJobService<T>> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ICronJobResilienceConfig<T> _resilienceConfig;
    private readonly ICronJobMetrics? _metrics;
    private readonly Random _random = new();

    private IServiceScope? _currentScope;
    private long _executionCount;
    private long _retryCount;
    private long _skippedCount;
    private bool _disposed;
    private CancellationTokenSource? _shutdownCts;

    /// <summary>
    /// Protected service provider for derived classes to access scoped services.
    /// </summary>
    protected IServiceProvider? _serviceProvider;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current execution count.
    /// </summary>
    public long ExecutionCount => Interlocked.Read(ref _executionCount);

    /// <summary>
    /// Gets the total retry count across all executions.
    /// </summary>
    public long RetryCount => Interlocked.Read(ref _retryCount);

    /// <summary>
    /// Gets the count of skipped executions (due to overlapping or circuit breaker).
    /// </summary>
    public long SkippedCount => Interlocked.Read(ref _skippedCount);

    /// <summary>
    /// Gets the job name.
    /// </summary>
    public string JobName { get; }

    /// <summary>
    /// Gets the CRON expression string.
    /// </summary>
    public string CronExpression { get; }

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerState CircuitBreakerState => _circuitBreaker.GetState(JobName);

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of ResilientCronJobService.
    /// </summary>
    /// <param name="config">The resilient schedule configuration.</param>
    /// <param name="hostApplication">The host application lifetime.</param>
    /// <param name="rootServiceProvider">The root service provider.</param>
    /// <param name="executionLock">The execution lock for preventing overlapping.</param>
    /// <param name="circuitBreaker">The circuit breaker for resilience.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">
    /// Optional TimeProvider for time abstraction. Defaults to TimeProvider.System.
    /// </param>
    protected ResilientCronJobService(
        IResilientScheduleConfig<T> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<T>> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _hostApplication = hostApplication ?? throw new ArgumentNullException(nameof(hostApplication));
        _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
        _executionLock = executionLock ?? throw new ArgumentNullException(nameof(executionLock));
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _resilienceConfig = config.Resilience;
        _metrics = rootServiceProvider.GetService<ICronJobMetrics>();

        CronExpression = config.CronExpression ?? string.Empty;
        _timeZoneInfo = config.TimeZoneInfo ?? TimeZoneInfo.Local;

        if (!string.IsNullOrEmpty(CronExpression))
        {
            _expression = Cronos.CronExpression.Parse(CronExpression);
        }

        JobName = typeof(T).Name;
    }

    #endregion

    #region BackgroundService Overrides

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        using Activity? activity = CronJobActivitySource.StartStartActivity(JobName, CronExpression);
        activity?.SetTag("resilience.retry_enabled", _resilienceConfig.EnableRetry);
        activity?.SetTag("resilience.circuit_breaker_enabled", _resilienceConfig.EnableCircuitBreaker);
        activity?.SetTag("resilience.prevent_overlapping", _resilienceConfig.PreventOverlapping);

        CronJobLoggerMessages.LogJobStarting(_logger, JobName, CronExpression, _timeZoneInfo?.Id);
        _metrics?.RecordJobStarted(JobName, CronExpression);

        _logger.LogDebug("Resilience settings. Name: {CronJobName}, " +
            "RetryEnabled: {RetryEnabled}, CircuitBreakerEnabled: {CircuitBreakerEnabled}, PreventOverlapping: {PreventOverlapping}",
            JobName,
            _resilienceConfig.EnableRetry,
            _resilienceConfig.EnableCircuitBreaker,
            _resilienceConfig.PreventOverlapping);

        await base.StartAsync(cancellationToken);

        CronJobLoggerMessages.LogJobStarted(_logger, JobName);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_expression == null)
        {
            await ExecuteOnceWithResilienceAsync(stoppingToken);
            return;
        }

        await ScheduleJobWithPeriodicTimerAsync(stoppingToken);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        using Activity? activity = CronJobActivitySource.StartStopActivity(JobName);
        activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, _executionCount);
        activity?.SetTag("resilience.retry_count", _retryCount);
        activity?.SetTag("resilience.skipped_count", _skippedCount);

        CronJobLoggerMessages.LogJobStopping(_logger, JobName, _executionCount);
        _logger.LogDebug("Resilience stats. Name: {CronJobName}, TotalRetries: {RetryCount}, SkippedExecutions: {SkippedCount}",
            JobName, _retryCount, _skippedCount);

        if (_resilienceConfig.WaitForExecutionOnShutdown)
        {
            try
            {
                // Create a linked token that combines the cancellation token with the graceful shutdown timeout
                using var timeoutCts = new CancellationTokenSource(_resilienceConfig.GracefulShutdownTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // Signal shutdown to running operations
                _shutdownCts?.Cancel();

                await base.StopAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                CronJobLoggerMessages.LogGracefulShutdownTimeout(_logger, JobName, _resilienceConfig.GracefulShutdownTimeout.TotalMilliseconds);
            }
        }
        else
        {
            _shutdownCts?.Cancel();
            await base.StopAsync(cancellationToken);
        }

        _metrics?.RecordJobStopped(JobName, _executionCount);
        CronJobLoggerMessages.LogJobStopped(_logger, JobName);
    }

    #endregion

    #region Execution Methods

    /// <summary>
    /// Executes the job once with full resilience policies.
    /// </summary>
    private async Task ExecuteOnceWithResilienceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteWithResilienceAsync(cancellationToken);
        }
        finally
        {
            _logger.LogDebug("CronJob execute once ending. Name: {CronJobName}", JobName);
            _hostApplication.StopApplication();
        }
    }

    /// <summary>
    /// Schedules and executes the job using PeriodicTimer.
    /// </summary>
    private async Task ScheduleJobWithPeriodicTimerAsync(CancellationToken stoppingToken)
    {
        CronJobLoggerMessages.LogSchedulerStarted(_logger, JobName, CronExpression);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTimeOffset? nextOccurrence = GetNextOccurrence();

                if (!nextOccurrence.HasValue)
                {
                    CronJobLoggerMessages.LogNoNextOccurrence(_logger, JobName);
                    break;
                }

                TimeSpan delay = nextOccurrence.Value - _timeProvider.GetUtcNow();

                if (delay <= TimeSpan.Zero)
                {
                    await ExecuteWithResilienceAsync(stoppingToken);
                    continue;
                }

                using Activity? scheduleActivity = CronJobActivitySource.StartScheduleActivity(
                    JobName,
                    CronExpression,
                    nextOccurrence.Value);

                _metrics?.RecordNextScheduledExecution(JobName, nextOccurrence.Value);
                CronJobLoggerMessages.LogNextExecution(_logger, JobName, nextOccurrence.Value, delay.TotalMilliseconds);

                bool waited = await WaitUntilAsync(nextOccurrence.Value, stoppingToken);

                if (!waited)
                {
                    break;
                }

                await ExecuteWithResilienceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            CronJobLoggerMessages.LogSchedulerCancelled(_logger, JobName);
        }

        CronJobLoggerMessages.LogSchedulerStopped(_logger, JobName);
    }

    /// <summary>
    /// Executes the work with all resilience policies applied.
    /// </summary>
    private async Task ExecuteWithResilienceAsync(CancellationToken cancellationToken)
    {
        // Check circuit breaker
        if (_resilienceConfig.EnableCircuitBreaker)
        {
            if (!_circuitBreaker.AllowExecution(
                JobName,
                _resilienceConfig.CircuitBreakerFailureThreshold,
                _resilienceConfig.CircuitBreakerDuration,
                _resilienceConfig.CircuitBreakerSamplingDuration))
            {
                Interlocked.Increment(ref _skippedCount);
                _metrics?.RecordSkippedExecution(JobName, "circuit_breaker_open");
                CronJobLoggerMessages.LogSkippedCircuitBreakerOpen(_logger, JobName, CircuitBreakerState.ToString());
                return;
            }
        }

        // Check overlapping execution
        ICronJobLockHandle? lockHandle = null;
        if (_resilienceConfig.PreventOverlapping)
        {
            lockHandle = await _executionLock.TryAcquireAsync(
                JobName,
                _resilienceConfig.OverlappingWaitTimeout,
                cancellationToken);

            if (lockHandle == null)
            {
                Interlocked.Increment(ref _skippedCount);
                _metrics?.RecordSkippedExecution(JobName, "overlapping");

                if (_resilienceConfig.LogOverlappingSkipped)
                {
                    CronJobLoggerMessages.LogSkippedOverlapping(_logger, JobName);
                }

                _resilienceConfig.OnOverlappingSkipped?.Invoke();
                return;
            }
        }

        try
        {
            await using ICronJobLockHandle? _ = lockHandle;
            await ExecuteScheduledWorkAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Lock handle is automatically released via await using
            throw;
        }
    }

    /// <summary>
    /// Executes the scheduled work with retry policy.
    /// </summary>
    private async Task ExecuteScheduledWorkAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using Activity? activity = CronJobActivitySource.StartExecuteActivity(
            JobName,
            CronExpression,
            _timeZoneInfo?.Id);

        long executionCount = Interlocked.Increment(ref _executionCount);
        activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, executionCount);
        activity?.SetTag("resilience.retry_enabled", _resilienceConfig.EnableRetry);
        _metrics?.IncrementActiveJob(JobName);

        // Create execution timeout token if configured
        using CancellationTokenSource? timeoutCts = _resilienceConfig.ExecutionTimeout.HasValue
            ? new CancellationTokenSource(_resilienceConfig.ExecutionTimeout.Value)
            : null;

        using CancellationTokenSource? linkedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        CancellationToken effectiveToken = _resilienceConfig.PropagateCancellation
            ? (linkedCts?.Token ?? cancellationToken)
            : cancellationToken;

        try
        {
            ResetServiceProvider();

            if (!effectiveToken.IsCancellationRequested)
            {
                CronJobLoggerMessages.LogExecutionStarting(_logger, JobName, executionCount);

                if (_resilienceConfig.EnableRetry)
                {
                    await ExecuteWithRetryAsync(effectiveToken);
                }
                else
                {
                    await DoWork(effectiveToken);
                }

                stopwatch.Stop();
                activity?.SetExecutionResult(success: true, durationMs: stopwatch.Elapsed.TotalMilliseconds);
                _metrics?.RecordExecution(JobName, stopwatch.Elapsed.TotalMilliseconds, success: true, (int)executionCount);
                _metrics?.RecordLastExecution(JobName, _timeProvider.GetUtcNow());

                // Record success for circuit breaker
                if (_resilienceConfig.EnableCircuitBreaker)
                {
                    CircuitBreakerState previousState = _circuitBreaker.GetState(JobName);
                    _circuitBreaker.RecordSuccess(
                        JobName,
                        _resilienceConfig.CircuitBreakerSuccessThreshold,
                        (prevState, newState) =>
                        {
                            _metrics?.RecordCircuitBreakerStateChange(JobName, prevState.ToString(), newState.ToString());
                            _resilienceConfig.OnCircuitBreakerStateChange?.Invoke(prevState, newState);
                        });
                }

                CronJobLoggerMessages.LogExecutionCompleted(_logger, JobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
            }
        }
        catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            if (timeoutCts?.IsCancellationRequested == true)
            {
                CronJobLoggerMessages.LogExecutionTimedOut(_logger, JobName, _resilienceConfig.ExecutionTimeout?.TotalMilliseconds ?? 0);
                activity?.SetExecutionResult(success: false, durationMs: stopwatch.Elapsed.TotalMilliseconds, errorMessage: "Execution timed out");
            }
            else
            {
                CronJobLoggerMessages.LogExecutionCancelled(_logger, JobName);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.RecordError(ex);
            _metrics?.RecordFailure(JobName, ex, stopwatch.Elapsed.TotalMilliseconds, (int)executionCount);

            // Record failure for circuit breaker
            if (_resilienceConfig.EnableCircuitBreaker)
            {
                CircuitBreakerState previousState = _circuitBreaker.GetState(JobName);
                _circuitBreaker.RecordFailure(
                    JobName,
                    _resilienceConfig.CircuitBreakerFailureThreshold,
                    _resilienceConfig.CircuitBreakerDuration,
                    (prevState, newState) =>
                    {
                        _metrics?.RecordCircuitBreakerStateChange(JobName, prevState.ToString(), newState.ToString());
                        CronJobLoggerMessages.LogCircuitBreakerStateChanged(_logger, JobName, prevState.ToString(), newState.ToString());
                        _resilienceConfig.OnCircuitBreakerStateChange?.Invoke(prevState, newState);
                    });
            }

            _resilienceConfig.OnJobFailed?.Invoke(ex);

            CronJobLoggerMessages.LogExecutionFailed(_logger, ex, JobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
        }
        finally
        {
            _metrics?.DecrementActiveJob(JobName);
        }
    }

    /// <summary>
    /// Executes DoWork with retry policy.
    /// </summary>
    private async Task ExecuteWithRetryAsync(CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _resilienceConfig.MaxRetryAttempts)
        {
            try
            {
                await DoWork(cancellationToken);
                return; // Success, exit retry loop
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                // Check if we should retry this exception
                if (_resilienceConfig.ShouldRetryOnException != null &&
                    !_resilienceConfig.ShouldRetryOnException(ex))
                {
                    throw; // Don't retry this exception type
                }

                if (attempt > _resilienceConfig.MaxRetryAttempts)
                {
                    break; // No more retries
                }

                // Calculate delay with exponential backoff and jitter
                TimeSpan delay = CalculateRetryDelay(attempt);

                Interlocked.Increment(ref _retryCount);
                _metrics?.RecordRetryAttempt(JobName, attempt, _resilienceConfig.MaxRetryAttempts + 1, delay.TotalMilliseconds);

                CronJobLoggerMessages.LogRetryAttempt(_logger, ex, JobName, attempt, _resilienceConfig.MaxRetryAttempts + 1, delay.TotalMilliseconds);

                _resilienceConfig.OnRetry?.Invoke(ex, attempt, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // All retries exhausted
        if (lastException != null)
        {
            CronJobLoggerMessages.LogRetriesExhausted(_logger, lastException, JobName, attempt);
        }
        throw lastException ?? new InvalidOperationException("Retry loop completed without exception");
    }

    /// <summary>
    /// Calculates the retry delay with exponential backoff and jitter.
    /// </summary>
    private TimeSpan CalculateRetryDelay(int attempt)
    {
        TimeSpan baseDelay;

        if (_resilienceConfig.UseExponentialBackoff)
        {
            // Exponential backoff: delay * 2^(attempt-1)
            double exponentialMs = _resilienceConfig.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
            baseDelay = TimeSpan.FromMilliseconds(Math.Min(exponentialMs, _resilienceConfig.MaxRetryDelay.TotalMilliseconds));
        }
        else
        {
            baseDelay = _resilienceConfig.RetryDelay;
        }

        // Apply jitter
        if (_resilienceConfig.RetryJitterFactor > 0)
        {
            double jitterMs = baseDelay.TotalMilliseconds * _resilienceConfig.RetryJitterFactor;
            double randomJitter = (_random.NextDouble() * 2 - 1) * jitterMs; // -jitterMs to +jitterMs
            baseDelay = TimeSpan.FromMilliseconds(Math.Max(0, baseDelay.TotalMilliseconds + randomJitter));
        }

        return baseDelay;
    }

    /// <summary>
    /// Gets the next occurrence based on the CRON expression.
    /// </summary>
    protected DateTimeOffset? GetNextOccurrence()
    {
        return _expression?.GetNextOccurrence(_timeProvider.GetUtcNow(), _timeZoneInfo);
    }

    /// <summary>
    /// Waits until the specified time using PeriodicTimer for efficient waiting.
    /// </summary>
    private async Task<bool> WaitUntilAsync(DateTimeOffset until, CancellationToken cancellationToken)
    {
        const int MaxTimerPeriodMs = 60_000;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan remaining = until - _timeProvider.GetUtcNow();

                if (remaining <= TimeSpan.Zero)
                {
                    return true;
                }

                TimeSpan waitTime = remaining > TimeSpan.FromMilliseconds(MaxTimerPeriodMs)
                    ? TimeSpan.FromMilliseconds(MaxTimerPeriodMs)
                    : remaining;

                using var timer = new PeriodicTimer(waitTime);
                await timer.WaitForNextTickAsync(cancellationToken);
            }

            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// The work to be executed by the CronJob.
    /// Override this method to implement your scheduled task.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task DoWork(CancellationToken cancellationToken);

    #endregion

    #region Helper Methods

    /// <summary>
    /// Resets the service provider by creating a new scope.
    /// Disposes the previous scope to prevent memory leaks.
    /// </summary>
    private void ResetServiceProvider()
    {
        _currentScope?.Dispose();
        _currentScope = _rootServiceProvider.CreateScope();
        _serviceProvider = _currentScope.ServiceProvider;
    }

    #endregion

    #region Dispose Methods

    /// <inheritdoc />
    public override void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                _shutdownCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS was already disposed, ignore
            }
            _shutdownCts?.Dispose();
            _shutdownCts = null;
            _currentScope?.Dispose();
            _currentScope = null;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs async cleanup of managed resources.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            try
            {
                _shutdownCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS was already disposed, ignore
            }
            _shutdownCts?.Dispose();
            _shutdownCts = null;
            _currentScope?.Dispose();
            _currentScope = null;
        }

        return ValueTask.CompletedTask;
    }

    #endregion
}

