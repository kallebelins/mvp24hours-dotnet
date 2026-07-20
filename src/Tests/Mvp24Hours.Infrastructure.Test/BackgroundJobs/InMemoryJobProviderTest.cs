//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs;

[Trait("Category", "Unit")]
public class InMemoryJobProviderTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new InMemoryJobProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldCreateInstance()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        var logger = new Mock<ILogger<InMemoryJobProvider>>();

        Action act = () => _ = new InMemoryJobProvider(
            new ServiceCollection().BuildServiceProvider(),
            logger.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_WithNullArgs_ShouldThrowArgumentNullException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.EnqueueAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("args");
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_ShouldReturnJobIdAndComplete()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        string jobId = await provider.EnqueueAsync<BackgroundJobsTestHelpers.TrackingJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs { Value = "enqueue-args" });

        jobId.Should().NotBeNullOrWhiteSpace();

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            jobId,
            s => s is JobStatus.Completed);

        status.Should().Be(JobStatus.Completed);
        BackgroundJobsTestHelpers.TrackingJobWithArgs.LastValue.Should().Be("enqueue-args");
    }

    [Fact]
    public async Task EnqueueAsync_WithoutArgs_ShouldReturnJobIdAndComplete()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        string jobId = await provider.EnqueueAsync<BackgroundJobsTestHelpers.TrackingJob>();

        jobId.Should().NotBeNullOrWhiteSpace();

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            jobId,
            s => s is JobStatus.Completed);

        status.Should().Be(JobStatus.Completed);
        BackgroundJobsTestHelpers.TrackingJob.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task ScheduleAsync_WithArgsAndDelay_WithZeroDelay_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs(),
            TimeSpan.Zero);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("delay");
    }

    [Fact]
    public async Task ScheduleAsync_WithArgsAndScheduledTimeInPast_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.ScheduleAsync<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs(),
            DateTimeOffset.UtcNow.AddSeconds(-1));

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("scheduledTime");
    }

    [Fact]
    public async Task ScheduleAsync_WithDelay_ShouldExecuteAfterDelay()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        string jobId = await provider.ScheduleAsync<BackgroundJobsTestHelpers.TrackingJob>(TimeSpan.FromMilliseconds(150));

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            jobId,
            s => s is JobStatus.Completed,
            TimeSpan.FromSeconds(5));

        status.Should().Be(JobStatus.Completed);
        BackgroundJobsTestHelpers.TrackingJob.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task ScheduleRecurringAsync_ShouldEnqueueOnceWithoutThrowing()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        string jobId = await provider.ScheduleRecurringAsync<BackgroundJobsTestHelpers.TrackingJob>("0 * * * *");

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            jobId,
            s => s is JobStatus.Completed);

        status.Should().Be(JobStatus.Completed);
    }

    [Fact]
    public async Task CancelAsync_WithEmptyJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.CancelAsync("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task CancelAsync_WithUnknownJobId_ShouldReturnFalse()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        bool cancelled = await provider.CancelAsync("unknown-job");

        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_WithEmptyJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.GetStatusAsync("  ");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task GetStatusAsync_WithUnknownJobId_ShouldReturnNull()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        JobStatus? status = await provider.GetStatusAsync("missing");

        status.Should().BeNull();
    }

    [Fact]
    public async Task EnqueueAsync_WithFailingJobAndNoRetries_ShouldFail()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();
        var options = new JobOptions { MaxRetryAttempts = 0 };

        string jobId = await provider.EnqueueAsync<BackgroundJobsTestHelpers.FailingJob>(options);

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            jobId,
            s => s is JobStatus.Failed,
            TimeSpan.FromSeconds(5));

        status.Should().Be(JobStatus.Failed);
    }

    [Fact]
    public async Task ContinueWithAsync_WithArgs_AfterParentSuccess_ShouldEnqueueContinuation()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        string parentJobId = await provider.EnqueueAsync<BackgroundJobsTestHelpers.DummyJob>();
        _ = await provider.ContinueWithAsync<BackgroundJobsTestHelpers.TrackingJob>(
            parentJobId,
            new ContinuationOptions { ExecuteOnSuccessOnly = true });

        await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            parentJobId,
            s => s is JobStatus.Completed);

        await Task.Delay(200);

        BackgroundJobsTestHelpers.TrackingJob.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task ContinueWithAsync_WithEmptyParentJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.ContinueWithAsync<BackgroundJobsTestHelpers.DummyJob>("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("parentJobId");
    }

    [Fact]
    public async Task ScheduleBatchAsync_WithNullBatch_ShouldThrowArgumentNullException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.ScheduleBatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("batch");
    }

    [Fact]
    public async Task ScheduleBatchAsync_ShouldReturnBatchIdAndComplete()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();
        var batch = new JobBatch("test-batch");
        batch.AddJob<BackgroundJobsTestHelpers.DummyJob>();

        string batchId = await provider.ScheduleBatchAsync(batch);

        batchId.Should().NotBeNullOrWhiteSpace();

        BatchStatus? status = null;
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            status = await provider.GetBatchStatusAsync(batchId);
            if (status == BatchStatus.Completed)
            {
                break;
            }

            await Task.Delay(50);
        }

        status.Should().Be(BatchStatus.Completed);
    }

    [Fact]
    public async Task CancelBatchAsync_WithValidBatchId_ShouldCancelBatch()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();
        var batch = new JobBatch("cancel-batch");
        batch.AddJob<BackgroundJobsTestHelpers.DummyJob>();
        string batchId = await provider.ScheduleBatchAsync(batch);

        bool cancelled = await provider.CancelBatchAsync(batchId);

        cancelled.Should().BeTrue();

        BatchStatus? status = null;
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            status = await provider.GetBatchStatusAsync(batchId);
            if (status == BatchStatus.Cancelled)
            {
                break;
            }

            await Task.Delay(20);
        }

        status.Should().Be(BatchStatus.Cancelled);
    }

    [Fact]
    public async Task EnqueueChildAsync_WithEmptyParentJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.EnqueueChildAsync<BackgroundJobsTestHelpers.DummyJob>("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("parentJobId");
    }

    [Fact]
    public async Task EnqueueChildAsync_ShouldReturnJobIdAndExecuteChild()
    {
        BackgroundJobsTestHelpers.TrackingJob.Reset();
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();
        const string parentJobId = "parent-1";

        string childJobId = await provider.EnqueueChildAsync<BackgroundJobsTestHelpers.TrackingJob>(parentJobId);

        childJobId.Should().NotBeNullOrWhiteSpace();

        JobStatus? status = await BackgroundJobsTestHelpers.WaitForJobStatusAsync(
            provider,
            childJobId,
            s => s is JobStatus.Completed);

        status.Should().Be(JobStatus.Completed);
        BackgroundJobsTestHelpers.TrackingJob.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetChildJobStatusesAsync_WithEmptyParentJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.GetChildJobStatusesAsync("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("parentJobId");
    }

    [Fact]
    public async Task GetChildJobStatusesAsync_AfterEnqueueChild_ShouldReturnEntries()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        _ = await provider.EnqueueChildAsync<BackgroundJobsTestHelpers.DummyJob>("parent-2");

        IReadOnlyDictionary<string, JobStatus?> statuses = await provider.GetChildJobStatusesAsync("parent-2");

        statuses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WaitForChildrenAsync_WithEmptyParentJobId_ShouldThrowArgumentException()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.WaitForChildrenAsync("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("parentJobId");
    }

    [Fact]
    public async Task WaitForChildrenAsync_WithNoChildren_ShouldComplete()
    {
        InMemoryJobProvider provider = BackgroundJobsTestHelpers.CreateInMemoryProvider();

        Func<Task> act = () => provider.WaitForChildrenAsync("parent-no-children");

        await act.Should().NotThrowAsync();
    }
}
