using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;

namespace Mvp24Hours.Infrastructure.CronJob.Test;

/// <summary>
/// Smoke tests for CronJob module functionality.
/// Slow wall-clock scheduler coverage was replaced by <see cref="Services.CronJobServiceAdvancedTest"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CronJobTest
{
    [Fact]
    public async Task CronJob_ExecuteOnce_ShouldRunWorkAndStopHost()
    {
        var tracker = new ExecutionTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHostApplicationLifetime>();
        var config = new ScheduleConfig<ControllableCronJob>
        {
            TimeZoneInfo = TimeZoneInfo.Utc
        };

        var job = new ControllableCronJob(
            config,
            hostMock.Object,
            serviceProvider,
            NullLogger<CronJobService<ControllableCronJob>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        job.ExecutionCount.Should().Be(1);
        tracker.ExecutionCount.Should().Be(1);
        hostMock.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }
}
