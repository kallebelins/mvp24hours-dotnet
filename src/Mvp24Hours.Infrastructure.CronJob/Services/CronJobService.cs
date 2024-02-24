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
    public abstract class CronJobService<T> : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly IHostApplicationLifetime _hostApplication;
        private IServiceScope _serviceScope;
        private IServiceProvider _serviceProvider;

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
            _serviceProvider = serviceProvider;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("::StartAsync");
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
                //_logger.LogInformation("::ExecuteOnce");
                //_logger.LogInformation("::Before DoWork");
                await DoWork(cancellationToken);
                //_logger.LogInformation("::After DoWork");
                //_logger.LogInformation("::Shutdown application...");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Ocorreu um erro durante o processamento");
            }
            finally
            {
                _hostApplication.StopApplication();
            }
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("::ScheduleJob");
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
                            //_logger.LogInformation("::Before DoWork");
                            await DoWork(cancellationToken);
                            //_logger.LogInformation("::After DoWork");
                        }
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(ex, "Ocorreu um erro durante o processamento");
                    }
                    finally
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScheduleJob(cancellationToken);
                        }
                    }
                };
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("::StopAsync");
            _timer?.Stop();
            await Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _logger.LogInformation("::Dispose");
            _timer?.Dispose();
        }
    
        private void ResetServiceProvider() => _serviceProvider = _serviceProvider.CreateScope().ServiceProvider;
    }
}