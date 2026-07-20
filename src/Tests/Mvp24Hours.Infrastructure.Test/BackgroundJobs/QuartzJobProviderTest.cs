//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs;

/// <summary>
/// Quartz provider is currently a stub (Quartz packages not referenced).
/// Tests cover constructor guards and NotSupportedException on all operations.
/// </summary>
[Trait("Category", "Unit")]
public class QuartzJobProviderTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new QuartzJobProvider(
            null!,
            BackgroundJobsTestHelpers.CreateQuartzOptions());

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new QuartzJobProvider(
            new ServiceCollection().BuildServiceProvider(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldCreateInstance()
    {
        QuartzJobProvider provider = CreateProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        var logger = new Mock<ILogger<QuartzJobProvider>>();

        Action act = () => _ = new QuartzJobProvider(
            new ServiceCollection().BuildServiceProvider(),
            BackgroundJobsTestHelpers.CreateQuartzOptions(),
            logger.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.EnqueueAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task EnqueueAsync_WithoutArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.EnqueueAsync<BackgroundJobsTestHelpers.DummyJob>();

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleAsync_WithArgsAndDelay_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs(),
            TimeSpan.FromMinutes(1));

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleAsync_WithoutArgsAndDelay_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJob>(TimeSpan.FromMinutes(1));

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleAsync_WithArgsAndScheduledTime_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs(),
            DateTimeOffset.UtcNow.AddHours(1));

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleAsync_WithoutArgsAndScheduledTime_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJob>(DateTimeOffset.UtcNow.AddHours(1));

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleRecurringAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            "0 * * * *",
            new BackgroundJobsTestHelpers.DummyJobArgs());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WithoutArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleRecurringAsync<BackgroundJobsTestHelpers.DummyJob>("0 * * * *");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ContinueWithAsync_WithArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ContinueWithAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            "parent-1",
            new BackgroundJobsTestHelpers.DummyJobArgs());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ContinueWithAsync_WithoutArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ContinueWithAsync<BackgroundJobsTestHelpers.DummyJob>("parent-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task ScheduleBatchAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.ScheduleBatchAsync(new JobBatch("batch"));

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task GetBatchStatusAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetBatchStatusAsync("batch-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task CancelBatchAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.CancelBatchAsync("batch-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task EnqueueChildAsync_WithArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.EnqueueChildAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            "parent-1",
            new BackgroundJobsTestHelpers.DummyJobArgs());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task EnqueueChildAsync_WithoutArgs_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.EnqueueChildAsync<BackgroundJobsTestHelpers.DummyJob>("parent-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task WaitForChildrenAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.WaitForChildrenAsync("parent-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task GetChildJobStatusesAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetChildJobStatusesAsync("parent-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task CancelChildrenAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.CancelChildrenAsync("parent-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.CancelAsync("job-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldThrowNotSupportedException()
    {
        QuartzJobProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetStatusAsync("job-1");

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Quartz*");
    }

    private static QuartzJobProvider CreateProvider(IOptions<QuartzJobOptions>? options = null)
    {
        return new QuartzJobProvider(
            new ServiceCollection().BuildServiceProvider(),
            options ?? BackgroundJobsTestHelpers.CreateQuartzOptions());
    }
}
