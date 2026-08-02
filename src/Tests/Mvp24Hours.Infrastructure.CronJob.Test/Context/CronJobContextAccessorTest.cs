using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Context;

[Trait("Category", "Unit")]
public class CronJobContextAccessorTest
{
    [Fact]
    public async Task Context_ShouldBeAvailableDuringAdvancedJobExecution()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => services.AddSingleton<ICronJobContextAccessor, CronJobContextAccessor>());

        ICronJobContextAccessor accessor = serviceProvider.GetRequiredService<ICronJobContextAccessor>();
        ICronJobContext? capturedContext = null;

        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(
            serviceProvider,
            execute: (_, _) =>
            {
                capturedContext = accessor.Context;
                return Task.CompletedTask;
            });

        await job.DoWork(CancellationToken.None);

        capturedContext.Should().NotBeNull();
        capturedContext!.JobName.Should().Be(nameof(TestAdvancedCronJob));
        accessor.Context.Should().BeNull();
    }

    [Fact]
    public void Context_ShouldBeNull_OutsideExecution()
    {
        var accessor = new CronJobContextAccessor();

        accessor.Context.Should().BeNull();
    }
}
