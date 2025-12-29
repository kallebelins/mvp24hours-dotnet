using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs
{
    /// <summary>
    /// Sample CronJob implementation for testing purposes.
    /// </summary>
    public class CustomerCronJob : CronJobService<CustomerCronJob>
    {
        public CustomerCronJob(
            IScheduleConfig<CustomerCronJob> config,
            IHostApplicationLifetime hostApplication,
            IServiceProvider serviceProvider,
            ILogger<CronJobService<CustomerCronJob>> logger) : base(config, hostApplication, serviceProvider, logger)
        { }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            Console.WriteLine("CronJob started counting");
            var timerService = _serviceProvider.GetService<TimerService>();
            timerService?.CountTime();
            return Task.CompletedTask;
        }
    }
}

