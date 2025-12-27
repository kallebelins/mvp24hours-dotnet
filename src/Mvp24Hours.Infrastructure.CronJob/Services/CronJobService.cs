using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Services
{
    /// <summary>
    /// Base class for implementing scheduled background tasks using CRON expressions.
    /// Integrates with .NET hosting model and provides telemetry support.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service (for configuration resolution)</typeparam>
    public abstract class CronJobService<T> : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly IHostApplicationLifetime _hostApplication;
        private readonly IServiceProvider _rootServiceProvider;
        private readonly ILogger<CronJobService<T>> _logger;
        private IServiceScope _currentScope;

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
            if (!string.IsNullOrEmpty(config.CronExpression))
            {
                _expression = CronExpression.Parse(config.CronExpression);
            }
            _timeZoneInfo = config.TimeZoneInfo;
            _hostApplication = hostApplication;
            _rootServiceProvider = serviceProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("CronJob starting. Name: {CronJobName}, Scheduler: {CronExpression}", typeof(CronJobService<T>), _expression);
            if (_expression != null)
            {
                await ScheduleJob(cancellationToken);
                return;
            }

            await ExecuteOnce(cancellationToken);
        }

        protected virtual async Task ExecuteOnce(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("CronJob execute once before. Name: {CronJobName}", typeof(T));
                await DoWork(cancellationToken);
                _logger.LogDebug("CronJob execute once after. Name: {CronJobName}", typeof(T));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CronJob execute once failure. Name: {CronJobName}", typeof(T));
            }
            finally
            {
                _logger.LogDebug("CronJob execute once ending. Name: {CronJobName}", typeof(T));
                _hostApplication.StopApplication();
            }
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                if (delay.TotalMilliseconds <= 0)
                {
                    await ScheduleJob(cancellationToken);
                }
                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    try
                    {
                        ResetServiceProvider();
                        _timer.Dispose();
                        _timer = null;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogDebug("CronJob execute before. Name: {CronJobName}", typeof(T));
                            await DoWork(cancellationToken);
                            _logger.LogDebug("CronJob execute after. Name: {CronJobName}", typeof(T));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CronJob execute failure. Name: {CronJobName}", typeof(T));
                    }
                    finally
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScheduleJob(cancellationToken);
                        }
                    }
                };
                _logger.LogDebug("CronJob next execution. Name: {CronJobName}, Time: {NextExecutionTime}, IntervalMs: {IntervalMs}", typeof(T), next, _timer.Interval);
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            _logger.LogDebug("CronJob stopped. Name: {CronJobName}", typeof(T));
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