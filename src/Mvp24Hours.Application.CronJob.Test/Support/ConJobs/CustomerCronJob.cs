using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Application.CronJob.Test.Support.Services;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.CronJob.Test.Support.ConJobs
{
    public class CustomerCronJob : CronJobService<CustomerCronJob>
    {
        public CustomerCronJob(
            IScheduleConfig<CustomerCronJob> config, 
            IHostApplicationLifetime hostApplication, 
            IServiceProvider serviceProvider) : base(config, hostApplication, serviceProvider)
        { }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            Console.WriteLine("Cronjob começou a contar");
            var timerService = _serviceProvider.GetService<TimerService>();
            timerService.CountTime(); 
            return Task.CompletedTask;
        }
    }
}
