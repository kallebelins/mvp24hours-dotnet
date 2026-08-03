using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Services;

/// <summary>
/// Advanced coverage for <see cref="ResilientCronJobService{T}"/> and <see cref="AdvancedCronJobService{T}"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ResilientAndAdvancedServiceAdvancedTest
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
    public async Task Resilient_OnJobFailed_ShouldFire_WhenRetriesExhausted()
    {
        Exception? failed = null;
        var services = new ServiceCollection();
        services.AddSingleton(new ExecutionTracker());
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        ServiceProvider sp = services.BuildServiceProvider();

        ResilientScheduleConfig<TestResilientCronJob> config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 1,
                RetryDelay = TimeSpan.FromMilliseconds(5),
                UseExponentialBackoff = false,
                PreventOverlapping = false,
                OnJobFailed = ex => failed = ex
            });

        var job = new TestResilientCronJob(
            config,
            new Mock<IHostApplicationLifetime>().Object,
            sp,
            new InMemoryCronJobExecutionLock(),
            new CronJobCircuitBreaker(),
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            _ => throw new InvalidOperationException("always fails"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(400);
        await job.StopAsync(CancellationToken.None);

        failed.Should().NotBeNull();
        failed!.Message.Should().Be("always fails");
        job.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Resilient_OnOverlappingSkipped_ShouldFire()
    {
        bool skipped = false;
        var executionLock = new InMemoryCronJobExecutionLock();
        await using ICronJobLockHandle? held = await executionLock.TryAcquireAsync(
            nameof(TestResilientCronJob),
            TimeSpan.Zero);

        held.Should().NotBeNull();

        var services = new ServiceCollection();
        services.AddSingleton(new ExecutionTracker());
        ServiceProvider sp = services.BuildServiceProvider();

        ResilientScheduleConfig<TestResilientCronJob> config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero,
                LogOverlappingSkipped = true,
                OnOverlappingSkipped = () => skipped = true
            });

        var job = new TestResilientCronJob(
            config,
            new Mock<IHostApplicationLifetime>().Object,
            sp,
            executionLock,
            new CronJobCircuitBreaker(),
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        skipped.Should().BeTrue();
        job.SkippedCount.Should().BeGreaterThan(0);
        job.DoWorkInvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Resilient_OnCircuitBreakerStateChange_ShouldFire_WhenOpened()
    {
        var transitions = new List<(CircuitBreakerState From, CircuitBreakerState To)>();
        var services = new ServiceCollection();
        services.AddSingleton(new ExecutionTracker());
        ServiceProvider sp = services.BuildServiceProvider();

        ResilientScheduleConfig<TestResilientCronJob> config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 1,
                CircuitBreakerDuration = TimeSpan.FromMinutes(1),
                PreventOverlapping = false,
                EnableRetry = false,
                OnCircuitBreakerStateChange = (from, to) => transitions.Add((from, to))
            });

        var job = new TestResilientCronJob(
            config,
            new Mock<IHostApplicationLifetime>().Object,
            sp,
            new InMemoryCronJobExecutionLock(),
            new CronJobCircuitBreaker(),
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            _ => throw new InvalidOperationException("fail"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(250);
        await job.StopAsync(CancellationToken.None);

        transitions.Should().Contain(t => t.To == CircuitBreakerState.Open);
        job.CircuitBreakerState.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task Resilient_ScheduledJob_ShouldCancelDuringWait()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ExecutionTracker());
        ServiceProvider sp = services.BuildServiceProvider();

        ResilientScheduleConfig<TestResilientCronJob> config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            cronExpression: "0 0 * * *",
            resilience: new CronJobResilienceConfig<TestResilientCronJob> { PreventOverlapping = false });

        var job = new TestResilientCronJob(
            config,
            new Mock<IHostApplicationLifetime>().Object,
            sp,
            new InMemoryCronJobExecutionLock(),
            new CronJobCircuitBreaker(),
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance);

        using var cts = new CancellationTokenSource();
        await job.StartAsync(cts.Token);
        await Task.Delay(80);
        await cts.CancelAsync();
        await job.StopAsync(CancellationToken.None);

        job.DoWorkInvocationCount.Should().Be(0);
        job.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public void Advanced_GetContext_ShouldThrow_OutsideDoWork()
    {
        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(sp);

        Action act = () => job.CallGetContext();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*only available during DoWork*");
    }

    [Fact]
    public async Task Advanced_SetContextProperty_ShouldBeAvailableDuringExecution()
    {
        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(sp);

        await job.DoWork(CancellationToken.None);

        job.LastContextProperty.Should().Be("ok");
        job.LastContext.Should().NotBeNull();
    }

    [Fact]
    public async Task Advanced_Cancellation_ShouldDispatchCancelledEvent()
    {
        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices(services => RegisterEventHandler(services, new RecordingCronJobEventHandler()));

        RecordingCronJobEventHandler handler = sp.GetRequiredService<RecordingCronJobEventHandler>();
        using var cts = new CancellationTokenSource();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(
            sp,
            execute: async (_, token) =>
            {
                await cts.CancelAsync();
                token.ThrowIfCancellationRequested();
            });

        Func<Task> act = () => job.DoWork(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Events.Should().Contain("Cancelled");
    }

    [Fact]
    public async Task Advanced_Failure_ShouldRecordDependencyCompletionAsFalse()
    {
        var tracker = new InMemoryCronJobDependencyTracker();
        tracker.RegisterDependency(new CronJobDependency(
            "ConsumerJob",
            [nameof(TestAdvancedCronJob)],
            requireSuccess: true));

        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices(services => services.AddSingleton<ICronJobDependencyTracker>(tracker));

        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(
            sp,
            execute: (_, _) => throw new InvalidOperationException("dep fail"));

        Func<Task> act = () => job.DoWork(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        (await tracker.AreDependenciesSatisfiedAsync("ConsumerJob")).Should().BeFalse();
    }

    [Fact]
    public async Task Advanced_ControllerRegister_ShouldAllowImmediateTrigger()
    {
        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(sp);
        ICronJobController controller = sp.GetRequiredService<ICronJobController>();

        bool triggered = await controller.TriggerAsync(nameof(TestAdvancedCronJob));

        triggered.Should().BeTrue();
        job.ExecuteCount.Should().Be(1);
    }

    [Fact]
    public async Task Advanced_DistributedLock_ShouldReleaseAfterSuccess()
    {
        ServiceProvider sp = CronJobTestHelpers.BuildAdvancedJobServices(useDistributedLocking: true);
        IDistributedCronJobLock distributedLock = sp.GetRequiredService<IDistributedCronJobLock>();
        TestAdvancedCronJob job = CronJobTestHelpers.CreateAdvancedJob(sp);

        await job.DoWork(CancellationToken.None);

        (await distributedLock.IsLockedAsync(nameof(TestAdvancedCronJob))).Should().BeFalse();
        job.ExecuteCount.Should().Be(1);
    }
}
