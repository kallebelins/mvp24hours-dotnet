using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Mvp24Hours.Infrastructure.CronJob;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Test
{
    /// <summary>
    /// Tests for CronJob module functionality.
    /// </summary>
    public class CronJobTest
    {
        #region [ Fields ]
        #endregion

        #region [ Configure ]
        #endregion

        [Fact]
        public async Task CronJobWithCorrectScheduler()
        {
            var timerService = new TimerService();
            var services = new ServiceCollection();
            services.AddSingleton(timerService);
            var serviceProvider = services.BuildServiceProvider();
            var scheduleConfig = new ScheduleConfig<CustomerCronJob>()
            {
                TimeZoneInfo = TimeZoneInfo.Utc,
                CronExpression = "* * * * *"
            };
            var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
            var cronjobHostedService = new CustomerCronJob(scheduleConfig, hostApplicationLifetimeMock.Object, serviceProvider);

            var cts = new CancellationTokenSource();
            timerService.Start();
            await cronjobHostedService.StartAsync(cts.Token);
            await Task.Delay(120 * 1000); // 2 minutes
            await cronjobHostedService.StopAsync(cts.Token);

            Assert.Equal(2, timerService.Counters.Count);
        }
    }
}

