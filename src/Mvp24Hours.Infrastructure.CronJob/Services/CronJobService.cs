using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
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
        private IServiceScope _currentScope;

        /// <summary>
        /// Gets the current scoped service provider for dependency resolution within DoWork.
        /// </summary>
        public IServiceProvider _serviceProvider;

        protected CronJobService(
            IScheduleConfig<T> config,
            IHostApplicationLifetime hostApplication,
            IServiceProvider serviceProvider)
        {
            if (!string.IsNullOrEmpty(config.CronExpression))
            {
                _expression = CronExpression.Parse(config.CronExpression);
            }
            _timeZoneInfo = config.TimeZoneInfo;
            _hostApplication = hostApplication;
            _rootServiceProvider = serviceProvider;
            _serviceProvider = serviceProvider;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-starting", $"name: {typeof(CronJobService<T>)}, scheduler: {_expression}");
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
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-execute-once-before", $"name:{typeof(T)}");
                await DoWork(cancellationToken);
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-execute-once-after", $"name:{typeof(T)}");

            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "cronjob-execute-once-failure", $"name:{typeof(T)}", ex);
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-execute-once-ending", $"name:{typeof(T)}");
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
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-execute-before", $"name:{typeof(T)}");
                            await DoWork(cancellationToken);
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-execute-after", $"name:{typeof(T)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Error, "cronjob-execute-failure", $"name:{typeof(T)}", ex);
                    }
                    finally
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScheduleJob(cancellationToken);
                        }
                    }
                };
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-next-execution", $"name:{typeof(T)}, time: {next}, ms: {_timer.Interval}");
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cronjob-stoped", $"name:{typeof(T)}");
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