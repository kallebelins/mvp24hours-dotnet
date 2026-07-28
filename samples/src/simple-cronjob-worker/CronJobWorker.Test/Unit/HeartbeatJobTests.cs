using CronJobWorker.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Mvp24Hours.Infrastructure.CronJob;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace CronJobWorker.Test.Unit;

[Trait("Category", "Unit")]
public class HeartbeatJobTests
{
    [Fact]
    public async Task HeartbeatJob_DoWork_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHostApplicationLifetime>();
        hostMock.SetupGet(x => x.ApplicationStopping).Returns(CancellationToken.None);

        var config = new ScheduleConfig<HeartbeatJob>
        {
            CronExpression = null,
            TimeZoneInfo = TimeZoneInfo.Utc
        };

        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));

        var job = new HeartbeatJob(
            config,
            hostMock.Object,
            serviceProvider,
            NullLogger<CronJobService<HeartbeatJob>>.Instance,
            fakeTime);

        Func<Task> act = () => job.DoWork(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
