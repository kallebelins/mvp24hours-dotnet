//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Services
{
    /// <summary>
    /// Base class for implementing scheduled background tasks using CRON expressions.
    /// Integrates with .NET hosting model and provides OpenTelemetry tracing support.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service (for configuration resolution)</typeparam>
    /// <remarks>
    /// <para>
    /// To enable distributed tracing for CronJobs, configure OpenTelemetry:
    /// </para>
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(builder =>
    ///     {
    ///         builder.AddSource(CronJobActivitySource.SourceName);
    ///     });
    /// </code>
    /// <para>
    /// This class uses .NET 8+ TimeProvider abstraction for time operations,
    /// enabling testability with FakeTimeProvider from Microsoft.Extensions.TimeProvider.Testing.
    /// </para>
    /// <para>
    /// <b>.NET 6+ PeriodicTimer:</b> This implementation uses PeriodicTimer instead of
    /// System.Timers.Timer for modern async/await patterns with proper cancellation support.
    /// </para>
    /// <para>
    /// <b>Observability:</b> This class integrates with OpenTelemetry for tracing and metrics.
    /// Register <see cref="ICronJobMetrics"/> for custom metrics collection.
    /// </para>
    /// </remarks>
    public abstract class CronJobService<T> : BackgroundService, IAsyncDisposable
    {
        private readonly CronExpression? _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly IHostApplicationLifetime _hostApplication;
        private readonly IServiceProvider _rootServiceProvider;
        private readonly ILogger<CronJobService<T>> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly ICronJobMetrics? _metrics;
        private IServiceScope? _currentScope;
        private readonly string _jobName;
        private readonly string _cronExpressionString;
        private long _executionCount;
        private bool _disposed;

        /// <summary>
        /// Protected service provider for derived classes to access scoped services.
        /// </summary>
        protected IServiceProvider? _serviceProvider;

        /// <summary>
        /// Gets the current execution count.
        /// </summary>
        public long ExecutionCount => Interlocked.Read(ref _executionCount);

        /// <summary>
        /// Gets the job name.
        /// </summary>
        public string JobName => _jobName;

        /// <summary>
        /// Gets the CRON expression string.
        /// </summary>
        public string CronExpression => _cronExpressionString;

        /// <summary>
        /// Creates a new instance of CronJobService.
        /// </summary>
        /// <param name="config">The schedule configuration.</param>
        /// <param name="hostApplication">The host application lifetime.</param>
        /// <param name="rootServiceProvider">The root service provider.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="timeProvider">
        /// Optional TimeProvider for time abstraction. Defaults to TimeProvider.System.
        /// Inject FakeTimeProvider for testing.
        /// </param>
        protected CronJobService(
            IScheduleConfig<T> config,
            IHostApplicationLifetime hostApplication,
            IServiceProvider rootServiceProvider,
            ILogger<CronJobService<T>> logger,
            TimeProvider? timeProvider = null)
        {
            _hostApplication = hostApplication ?? throw new ArgumentNullException(nameof(hostApplication));
            _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _metrics = rootServiceProvider.GetService<ICronJobMetrics>();

            _cronExpressionString = config?.CronExpression ?? string.Empty;
            _timeZoneInfo = config?.TimeZoneInfo ?? TimeZoneInfo.Local;

            if (!string.IsNullOrEmpty(_cronExpressionString))
            {
                _expression = Cronos.CronExpression.Parse(_cronExpressionString);
            }

            _jobName = typeof(T).Name;
        }

        /// <inheritdoc />
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var activity = CronJobActivitySource.StartStartActivity(_jobName, _cronExpressionString);
            
            CronJobLoggerMessages.LogJobStarting(_logger, _jobName, _cronExpressionString, _timeZoneInfo?.Id);
            _metrics?.RecordJobStarted(_jobName, _cronExpressionString);
            
            await base.StartAsync(cancellationToken);
            
            CronJobLoggerMessages.LogJobStarted(_logger, _jobName);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_expression == null)
            {
                await ExecuteOnce(stoppingToken);
                return;
            }

            await ScheduleJobWithPeriodicTimerAsync(stoppingToken);
        }

        /// <summary>
        /// Executes the job once (when no CRON expression is provided).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual async Task ExecuteOnce(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            using var activity = CronJobActivitySource.StartExecuteActivity(
                _jobName,
                _cronExpressionString,
                _timeZoneInfo?.Id);
            
            var executionCount = Interlocked.Increment(ref _executionCount);
            activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, executionCount);
            _metrics?.IncrementActiveJob(_jobName);
            
            try
            {
                CronJobLoggerMessages.LogExecuteOnce(_logger, _jobName);
                CronJobLoggerMessages.LogExecutionStarting(_logger, _jobName, executionCount);
                
                await DoWork(cancellationToken);
                
                stopwatch.Stop();
                activity?.SetExecutionResult(success: true, durationMs: stopwatch.Elapsed.TotalMilliseconds);
                _metrics?.RecordExecution(_jobName, stopwatch.Elapsed.TotalMilliseconds, success: true, (int)executionCount);
                _metrics?.RecordLastExecution(_jobName, _timeProvider.GetUtcNow());
                
                CronJobLoggerMessages.LogExecutionCompleted(_logger, _jobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CronJobLoggerMessages.LogExecutionCancelled(_logger, _jobName);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.RecordError(ex);
                _metrics?.RecordFailure(_jobName, ex, stopwatch.Elapsed.TotalMilliseconds, (int)executionCount);
                
                CronJobLoggerMessages.LogExecutionFailed(_logger, ex, _jobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
            }
            finally
            {
                _metrics?.DecrementActiveJob(_jobName);
                CronJobLoggerMessages.LogExecuteOnceCompleted(_logger, _jobName);
                _hostApplication.StopApplication();
            }
        }

        /// <summary>
        /// Schedules and executes the job using PeriodicTimer.
        /// Uses modern async/await patterns with proper cancellation support.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token.</param>
        protected virtual async Task ScheduleJobWithPeriodicTimerAsync(CancellationToken stoppingToken)
        {
            CronJobLoggerMessages.LogSchedulerStarted(_logger, _jobName, _cronExpressionString);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var nextOccurrence = GetNextOccurrence();
                    
                    if (!nextOccurrence.HasValue)
                    {
                        CronJobLoggerMessages.LogNoNextOccurrence(_logger, _jobName);
                        break;
                    }

                    var delay = nextOccurrence.Value - _timeProvider.GetUtcNow();
                    
                    if (delay <= TimeSpan.Zero)
                    {
                        // Execute immediately and continue to next
                        await ExecuteScheduledWorkAsync(stoppingToken);
                        continue;
                    }

                    using var scheduleActivity = CronJobActivitySource.StartScheduleActivity(
                        _jobName,
                        _cronExpressionString,
                        nextOccurrence.Value);
                    
                    _metrics?.RecordNextScheduledExecution(_jobName, nextOccurrence.Value);
                    CronJobLoggerMessages.LogNextExecution(_logger, _jobName, nextOccurrence.Value, delay.TotalMilliseconds);

                    // Wait for the next occurrence using PeriodicTimer-style waiting
                    var waited = await WaitUntilAsync(nextOccurrence.Value, stoppingToken);
                    
                    if (!waited)
                    {
                        // Cancellation was requested during wait
                        break;
                    }

                    // Execute the scheduled work
                    await ExecuteScheduledWorkAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                CronJobLoggerMessages.LogSchedulerCancelled(_logger, _jobName);
            }

            CronJobLoggerMessages.LogSchedulerStopped(_logger, _jobName);
        }

        /// <summary>
        /// Waits until the specified time using PeriodicTimer for efficient waiting.
        /// </summary>
        /// <param name="until">The time to wait until.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if wait completed, false if cancelled.</returns>
        private async Task<bool> WaitUntilAsync(DateTimeOffset until, CancellationToken cancellationToken)
        {
            const int MaxTimerPeriodMs = 60_000; // Max 1 minute intervals for responsiveness
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var remaining = until - _timeProvider.GetUtcNow();
                    
                    if (remaining <= TimeSpan.Zero)
                    {
                        return true;
                    }

                    // Use smaller intervals for better cancellation responsiveness
                    var waitTime = remaining > TimeSpan.FromMilliseconds(MaxTimerPeriodMs)
                        ? TimeSpan.FromMilliseconds(MaxTimerPeriodMs)
                        : remaining;

                    // PeriodicTimer style waiting - efficient and cancellation-friendly
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
        /// Gets the next occurrence based on the CRON expression.
        /// </summary>
        /// <returns>The next occurrence time, or null if none.</returns>
        protected DateTimeOffset? GetNextOccurrence()
        {
            return _expression?.GetNextOccurrence(_timeProvider.GetUtcNow(), _timeZoneInfo);
        }

        /// <summary>
        /// Executes the scheduled work with proper scoping and telemetry.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ExecuteScheduledWorkAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            using var activity = CronJobActivitySource.StartExecuteActivity(
                _jobName,
                _cronExpressionString,
                _timeZoneInfo?.Id);
            
            var executionCount = Interlocked.Increment(ref _executionCount);
            activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, executionCount);
            _metrics?.IncrementActiveJob(_jobName);
            
            try
            {
                ResetServiceProvider();

                if (!cancellationToken.IsCancellationRequested)
                {
                    CronJobLoggerMessages.LogExecutionStarting(_logger, _jobName, executionCount);
                    
                    await DoWork(cancellationToken);
                    
                    stopwatch.Stop();
                    activity?.SetExecutionResult(success: true, durationMs: stopwatch.Elapsed.TotalMilliseconds);
                    _metrics?.RecordExecution(_jobName, stopwatch.Elapsed.TotalMilliseconds, success: true, (int)executionCount);
                    _metrics?.RecordLastExecution(_jobName, _timeProvider.GetUtcNow());
                    
                    CronJobLoggerMessages.LogExecutionCompleted(_logger, _jobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                CronJobLoggerMessages.LogExecutionCancelled(_logger, _jobName);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.RecordError(ex);
                _metrics?.RecordFailure(_jobName, ex, stopwatch.Elapsed.TotalMilliseconds, (int)executionCount);
                
                CronJobLoggerMessages.LogExecutionFailed(_logger, ex, _jobName, stopwatch.Elapsed.TotalMilliseconds, executionCount);
            }
            finally
            {
                _metrics?.DecrementActiveJob(_jobName);
            }
        }

        /// <summary>
        /// The work to be executed by the CronJob.
        /// Override this method to implement your scheduled task.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to observe.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task DoWork(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            using var activity = CronJobActivitySource.StartStopActivity(_jobName);
            
            activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, _executionCount);
            CronJobLoggerMessages.LogJobStopping(_logger, _jobName, _executionCount);
            
            await base.StopAsync(cancellationToken);
            
            _metrics?.RecordJobStopped(_jobName, _executionCount);
            CronJobLoggerMessages.LogJobStopped(_logger, _jobName);
        }

        /// <summary>
        /// Resets the service provider by creating a new scope.
        /// Disposes the previous scope to prevent memory leaks.
        /// </summary>
        private void ResetServiceProvider()
        {
            // Dispose the previous scope to prevent memory leaks
            _currentScope?.Dispose();
            
            // Create a new scope for this execution
            _currentScope = _rootServiceProvider.CreateScope();
            _serviceProvider = _currentScope.ServiceProvider;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
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
            _currentScope?.Dispose();
            _currentScope = null;
            
            return ValueTask.CompletedTask;
        }
    }
}
