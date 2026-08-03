using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Services;

[Trait("Category", "Unit")]
public class AdvancedCronJobServiceTest
{
    private static void RegisterEventHandler(IServiceCollection services, RecordingCronJobEventHandler handler)
    {
        services.AddSingleton(handler);
        services.AddSingleton<ICronJobStartingHandler>(handler);
        services.AddSingleton<ICronJobCompletedHandler>(handler);
        services.AddSingleton<ICronJobFailedHandler>(handler);
        services.AddSingleton<ICronJobCancelledHandler>(handler);
        services.AddSingleton<ICronJobRetryHandler>(handler);
        services.AddSingleton<ICronJobSkippedHandler>(handler);
    }

    [Fact]
    public async Task DoWork_ShouldExecuteAndPersistSuccessState()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);
        ICronJobStateStore stateStore = serviceProvider.GetRequiredService<ICronJobStateStore>();

        await job.DoWork(CancellationToken.None);

        job.ExecuteCount.Should().Be(1);
        job.LastContext.Should().NotBeNull();
        job.LastStartingContext.Should().NotBeNull();
        job.LastCompletedContext.Should().NotBeNull();

        CronJobState? state = await stateStore.GetStateAsync(nameof(TestAdvancedCronJob));
        state!.SuccessCount.Should().Be(1);
        state.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task DoWork_ShouldSkip_WhenJobIsPaused()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => RegisterEventHandler(services, new RecordingCronJobEventHandler()));

        ICronJobStateStore stateStore = serviceProvider.GetRequiredService<ICronJobStateStore>();
        await stateStore.SetPausedAsync(nameof(TestAdvancedCronJob), true);
        RecordingCronJobEventHandler handler = serviceProvider.GetRequiredService<RecordingCronJobEventHandler>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        job.ExecuteCount.Should().Be(0);
        handler.SkippedEvents.Should().ContainSingle()
            .Which.Should().Be((nameof(TestAdvancedCronJob), SkipReason.Paused));
    }

    [Fact]
    public async Task DoWork_ShouldSkip_WhenDependenciesNotMet()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services =>
        {
            RegisterEventHandler(services, new RecordingCronJobEventHandler());
            services.AddSingleton<ICronJobDependencyTracker>(sp =>
            {
                var tracker = new InMemoryCronJobDependencyTracker();
                tracker.RegisterDependency(new CronJobDependency(
                    nameof(TestAdvancedCronJob),
                    ["PrerequisiteJob"]));
                return tracker;
            });
        });

        RecordingCronJobEventHandler handler = serviceProvider.GetRequiredService<RecordingCronJobEventHandler>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        job.ExecuteCount.Should().Be(0);
        handler.SkippedEvents.Should().ContainSingle()
            .Which.Should().Be((nameof(TestAdvancedCronJob), SkipReason.DependencyNotMet));
    }

    [Fact]
    public async Task DoWork_ShouldRun_WhenDependenciesAreSatisfied()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => services.AddSingleton<ICronJobDependencyTracker>(sp =>
            {
                var tracker = new InMemoryCronJobDependencyTracker();
                tracker.RegisterDependency(new CronJobDependency(
                    nameof(TestAdvancedCronJob),
                    ["PrerequisiteJob"]));
                tracker.RecordCompletionAsync("PrerequisiteJob", true, Guid.NewGuid()).GetAwaiter().GetResult();
                return tracker;
            }));

        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        job.ExecuteCount.Should().Be(1);
    }

    [Fact]
    public async Task DoWork_ShouldSkip_WhenDistributedLockUnavailable()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(
            configure: services => RegisterEventHandler(services, new RecordingCronJobEventHandler()),
            useDistributedLocking: true);

        IDistributedCronJobLock distributedLock = serviceProvider.GetRequiredService<IDistributedCronJobLock>();
        await distributedLock.TryAcquireAsync(
            nameof(TestAdvancedCronJob),
            "other-instance",
            TimeSpan.FromMinutes(5));

        RecordingCronJobEventHandler handler = serviceProvider.GetRequiredService<RecordingCronJobEventHandler>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        job.ExecuteCount.Should().Be(0);
        handler.SkippedEvents.Should().ContainSingle()
            .Which.Should().Be((nameof(TestAdvancedCronJob), SkipReason.Overlapping));
    }

    [Fact]
    public async Task DoWork_ShouldPersistFailureAndRethrow()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(
            serviceProvider,
            execute: (_, _) => throw new InvalidOperationException("work failed"));
        ICronJobStateStore stateStore = serviceProvider.GetRequiredService<ICronJobStateStore>();

        Func<Task> act = () => job.DoWork(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("work failed");
        job.LastFailure.Should().NotBeNull();

        CronJobState? state = await stateStore.GetStateAsync(nameof(TestAdvancedCronJob));
        state!.FailureCount.Should().Be(1);
        state.LastErrorMessage.Should().Be("work failed");
    }

    [Fact]
    public async Task DoWork_ShouldDispatchLifecycleEvents()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => RegisterEventHandler(services, new RecordingCronJobEventHandler()));

        RecordingCronJobEventHandler handler = serviceProvider.GetRequiredService<RecordingCronJobEventHandler>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        handler.Events.Should().ContainInOrder("Starting", "Completed");
    }

    [Fact]
    public async Task DoWork_ShouldClearContextAfterExecution()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => services.AddSingleton<ICronJobContextAccessor, CronJobContextAccessor>());

        ICronJobContextAccessor accessor = serviceProvider.GetRequiredService<ICronJobContextAccessor>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        await job.DoWork(CancellationToken.None);

        accessor.Context.Should().BeNull();
    }

    [Fact]
    public async Task DoWork_ShouldRecordDependencyCompletionOnSuccess()
    {
        ServiceProvider serviceProvider = CronJobTestHelpers.BuildAdvancedJobServices(services => services.AddSingleton<ICronJobDependencyTracker>(sp =>
            {
                var tracker = new InMemoryCronJobDependencyTracker();
                tracker.RegisterDependency(new CronJobDependency(
                    "ConsumerJob",
                    [nameof(TestAdvancedCronJob)]));
                return tracker;
            }));

        ICronJobDependencyTracker tracker = serviceProvider.GetRequiredService<ICronJobDependencyTracker>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(serviceProvider);

        (await tracker.AreDependenciesSatisfiedAsync("ConsumerJob")).Should().BeFalse();

        await job.DoWork(CancellationToken.None);

        (await tracker.AreDependenciesSatisfiedAsync("ConsumerJob")).Should().BeTrue();
    }
}
