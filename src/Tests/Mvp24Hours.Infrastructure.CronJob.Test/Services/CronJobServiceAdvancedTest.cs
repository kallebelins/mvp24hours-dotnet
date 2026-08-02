using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Services;

/// <summary>
/// Advanced unit tests for <see cref="CronJobService{T}"/> lifecycle and execute-once paths.
/// </summary>
[Trait("Category", "Unit")]
public class CronJobServiceAdvancedTest
{
    private static (ServiceProvider Sp, Mock<IHostApplicationLifetime> Host, ExecutionTracker Tracker, CronJobMetricsService Metrics)
        CreateServices()
    {
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var metrics = new CronJobMetricsService();
        var host = new Mock<IHostApplicationLifetime>();
        host.SetupGet(x => x.ApplicationStopping).Returns(CancellationToken.None);

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics>(metrics);
        services.AddSingleton(host.Object);
        return (services.BuildServiceProvider(), host, tracker, metrics);
    }

    private static ControllableCronJob CreateJob(
        ServiceProvider sp,
        Mock<IHostApplicationLifetime> host,
        string? cronExpression = null,
        Func<CancellationToken, Task>? work = null,
        TimeProvider? timeProvider = null)
    {
        var config = new ScheduleConfig<ControllableCronJob>
        {
            CronExpression = cronExpression,
            TimeZoneInfo = TimeZoneInfo.Utc
        };

        return new ControllableCronJob(
            config,
            host.Object,
            sp,
            NullLogger<CronJobService<ControllableCronJob>>.Instance,
            timeProvider ?? TimeProvider.System,
            work);
    }

    [Fact]
    public async Task ExecuteOnce_ShouldComplete_AndStopApplication()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, ExecutionTracker tracker, _) = CreateServices();
        ControllableCronJob job = CreateJob(sp, host);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        job.DoWorkInvocationCount.Should().Be(1);
        job.ExecutionCount.Should().Be(1);
        job.JobName.Should().Be(nameof(ControllableCronJob));
        tracker.ExecutionCount.Should().Be(1);
        host.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteOnce_ShouldRecordFailure_WhenDoWorkThrows()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, ExecutionTracker tracker, _) = CreateServices();
        ControllableCronJob job = CreateJob(sp, host, work: _ => throw new InvalidOperationException("boom"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(CancellationToken.None);

        job.DoWorkInvocationCount.Should().Be(1);
        tracker.HasFailures.Should().BeTrue();
        host.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteOnce_ShouldHandleCancellation()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, _, _) = CreateServices();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ControllableCronJob job = CreateJob(sp, host, work: async ct =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        });

        using var cts = new CancellationTokenSource();
        await job.StartAsync(cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await cts.CancelAsync();
        await Task.Delay(100);
        await job.StopAsync(CancellationToken.None);

        job.DoWorkInvocationCount.Should().Be(1);
        host.Verify(x => x.StopApplication(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAndStop_ShouldExposeJobMetadata()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, _, _) = CreateServices();
        ControllableCronJob job = CreateJob(sp, host, cronExpression: "0 0 * * *");

        using var cts = new CancellationTokenSource();
        await job.StartAsync(cts.Token);
        await Task.Delay(50);
        await cts.CancelAsync();
        await job.StopAsync(CancellationToken.None);

        job.CronExpression.Should().Be("0 0 * * *");
        job.JobName.Should().Be(nameof(ControllableCronJob));
        job.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public async Task ScheduledJob_ShouldStopCleanly_WhenCancelledDuringWait()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, _, _) = CreateServices();
        ControllableCronJob job = CreateJob(sp, host, cronExpression: "0 0 * * *");

        using var cts = new CancellationTokenSource();
        await job.StartAsync(cts.Token);
        await Task.Delay(80);
        await cts.CancelAsync();
        await job.StopAsync(CancellationToken.None);
        await job.DisposeAsync();

        job.DoWorkInvocationCount.Should().Be(0);
        job.ExecutionCount.Should().Be(0);
    }

    [Fact]
    public async Task Dispose_ShouldBeIdempotent()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, _, _) = CreateServices();
        ControllableCronJob job = CreateJob(sp, host);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(150);
        await job.StopAsync(CancellationToken.None);

        job.Dispose();
        job.Dispose();
        await job.DisposeAsync();
        await job.DisposeAsync();

        job.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task MultipleJobs_ShouldExecuteIndependently()
    {
        (ServiceProvider sp, Mock<IHostApplicationLifetime> host, ExecutionTracker tracker, _) = CreateServices();
        ControllableCronJob job1 = CreateJob(sp, host);
        ControllableCronJob job2 = CreateJob(sp, host);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job1.StartAsync(cts.Token);
        await job2.StartAsync(cts.Token);
        await Task.Delay(250);
        await job1.StopAsync(CancellationToken.None);
        await job2.StopAsync(CancellationToken.None);

        job1.ExecutionCount.Should().Be(1);
        job2.ExecutionCount.Should().Be(1);
        tracker.ExecutionCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenHostIsNull()
    {
        var config = new ScheduleConfig<ControllableCronJob>();
        ServiceProvider sp = new ServiceCollection().BuildServiceProvider();

        Action act = () => _ = new ControllableCronJob(
            config,
            null!,
            sp,
            NullLogger<CronJobService<ControllableCronJob>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("hostApplication");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenServiceProviderIsNull()
    {
        var config = new ScheduleConfig<ControllableCronJob>();
        var host = new Mock<IHostApplicationLifetime>();

        Action act = () => _ = new ControllableCronJob(
            config,
            host.Object,
            null!,
            NullLogger<CronJobService<ControllableCronJob>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("rootServiceProvider");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        var config = new ScheduleConfig<ControllableCronJob>();
        var host = new Mock<IHostApplicationLifetime>();
        ServiceProvider sp = new ServiceCollection().BuildServiceProvider();

        Action act = () => _ = new ControllableCronJob(
            config,
            host.Object,
            sp,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
