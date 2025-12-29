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
    /// </remarks>
    public abstract class CronJobService<T> : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly IHostApplicationLifetime _hostApplication;
        private readonly IServiceProvider _rootServiceProvider;
        private readonly ILogger<CronJobService<T>> _logger;
        private IServiceScope _currentScope;
        private readonly string _jobName;
        private readonly string _cronExpressionString;
        private long _executionCount;

        /// <summary>
        /// Gets the current scoped service provider for dependency resolution within DoWork.
        /// </summary>
        public IServiceProvider _serviceProvider;

        protected CronJobService(
            IScheduleConfig<T> config,
            IHostApplicationLifetime hostApplication,
            IServiceProvider serviceProvider,
            ILogger<CronJobService<T>> logger)
        {
            _cronExpressionString = config.CronExpression;
            if (!string.IsNullOrEmpty(config.CronExpression))
            {
                _expression = CronExpression.Parse(config.CronExpression);
            }
            _timeZoneInfo = config.TimeZoneInfo;
            _hostApplication = hostApplication;
            _rootServiceProvider = serviceProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _jobName = typeof(T).Name;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            using var activity = CronJobActivitySource.StartStartActivity(_jobName, _cronExpressionString);
            
            _logger.LogDebug("CronJob starting. Name: {CronJobName}, Scheduler: {CronExpression}", _jobName, _cronExpressionString);
            
            if (_expression != null)
            {
                await ScheduleJob(cancellationToken);
                return;
            }

            await ExecuteOnce(cancellationToken);
        }

        protected virtual async Task ExecuteOnce(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            using var activity = CronJobActivitySource.StartExecuteActivity(
                _jobName,
                _cronExpressionString,
                _timeZoneInfo?.Id);
            
            Interlocked.Increment(ref _executionCount);
            activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, _executionCount);
            
            try
            {
                _logger.LogDebug("CronJob execute once before. Name: {CronJobName}", _jobName);
                await DoWork(cancellationToken);
                
                stopwatch.Stop();
                activity.SetExecutionResult(success: true, durationMs: stopwatch.Elapsed.TotalMilliseconds);
                _logger.LogDebug("CronJob execute once after. Name: {CronJobName}, Duration: {DurationMs}ms", _jobName, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity.RecordError(ex);
                _logger.LogError(ex, "CronJob execute once failure. Name: {CronJobName}, Duration: {DurationMs}ms", _jobName, stopwatch.Elapsed.TotalMilliseconds);
            }
            finally
            {
                _logger.LogDebug("CronJob execute once ending. Name: {CronJobName}", _jobName);
                _hostApplication.StopApplication();
            }
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
            if (next.HasValue)
            {
                using var scheduleActivity = CronJobActivitySource.StartScheduleActivity(
                    _jobName,
                    _cronExpressionString,
                    next.Value);
                
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds <= 0)
                {
                    await ScheduleJob(cancellationToken);
                    return;
                }
                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    using var activity = CronJobActivitySource.StartExecuteActivity(
                        _jobName,
                        _cronExpressionString,
                        _timeZoneInfo?.Id);
                    
                    Interlocked.Increment(ref _executionCount);
                    activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, _executionCount);
                    
                    try
                    {
                        ResetServiceProvider();
                        _timer.Dispose();
                        _timer = null;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogDebug("CronJob execute before. Name: {CronJobName}, ExecutionCount: {ExecutionCount}", _jobName, _executionCount);
                            await DoWork(cancellationToken);
                            
                            stopwatch.Stop();
                            activity.SetExecutionResult(success: true, durationMs: stopwatch.Elapsed.TotalMilliseconds);
                            _logger.LogDebug("CronJob execute after. Name: {CronJobName}, Duration: {DurationMs}ms, ExecutionCount: {ExecutionCount}", _jobName, stopwatch.Elapsed.TotalMilliseconds, _executionCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        activity.RecordError(ex);
                        _logger.LogError(ex, "CronJob execute failure. Name: {CronJobName}, Duration: {DurationMs}ms, ExecutionCount: {ExecutionCount}", _jobName, stopwatch.Elapsed.TotalMilliseconds, _executionCount);
                    }
                    finally
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScheduleJob(cancellationToken);
                        }
                    }
                };
                _logger.LogDebug("CronJob next execution. Name: {CronJobName}, Time: {NextExecutionTime}, IntervalMs: {IntervalMs}", _jobName, next, _timer.Interval);
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            using var activity = CronJobActivitySource.StartStopActivity(_jobName);
            
            _timer?.Stop();
            
            activity?.SetTag(CronJobActivitySource.Tags.ExecutionCount, _executionCount);
            _logger.LogDebug("CronJob stopped. Name: {CronJobName}, TotalExecutions: {ExecutionCount}", _jobName, _executionCount);
            await Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _timer?.Dispose();
            _currentScope?.Dispose();
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
    }
}