//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Management;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs;

[Trait("Category", "Unit")]
public class InMemoryJobMetricsTest
{
    [Fact]
    public async Task RecordMetricAsync_WithNullMetric_ShouldThrowArgumentNullException()
    {
        var metrics = new InMemoryJobMetrics();

        await Assert.ThrowsAsync<ArgumentNullException>(() => metrics.RecordMetricAsync(null!));
    }

    [Fact]
    public async Task RecordMetricAsync_WithSuccess_ShouldIncrementCompletedJobs()
    {
        var metrics = new InMemoryJobMetrics();

        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(success: true, status: JobStatus.Completed));

        QueueStatistics stats = await metrics.GetQueueStatisticsAsync("default");
        stats.CompletedJobs.Should().Be(1);
        stats.FailedJobs.Should().Be(0);
        stats.AverageExecutionTime.Should().NotBeNull();
        stats.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordMetricAsync_WithFailure_ShouldIncrementFailedJobs()
    {
        var metrics = new InMemoryJobMetrics();

        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            success: false,
            status: JobStatus.Failed,
            duration: TimeSpan.FromMilliseconds(200)));

        QueueStatistics stats = await metrics.GetQueueStatisticsAsync("default");
        stats.CompletedJobs.Should().Be(0);
        stats.FailedJobs.Should().Be(1);
    }

    [Fact]
    public async Task RecordMetricAsync_WithNullQueue_ShouldUseDefaultQueue()
    {
        var metrics = new InMemoryJobMetrics();

        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(queue: null));

        QueueStatistics stats = await metrics.GetQueueStatisticsAsync("default");
        stats.CompletedJobs.Should().Be(1);
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldUpdateAverageExecutionTimeAcrossDurations()
    {
        var metrics = new InMemoryJobMetrics();
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(duration: TimeSpan.FromMilliseconds(100)));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            jobId: "job-2",
            duration: TimeSpan.FromMilliseconds(300)));

        QueueStatistics stats = await metrics.GetQueueStatisticsAsync("default");
        stats.AverageExecutionTime.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMetricsAsync_WithEmptyJobType_ShouldThrowArgumentException(string? jobType)
    {
        var metrics = new InMemoryJobMetrics();

        Func<Task> act = () => metrics.GetMetricsAsync(jobType!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobType");
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldAggregateByJobTypeAndDateRange()
    {
        var metrics = new InMemoryJobMetrics();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-1);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddHours(1);

        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            jobType: "JobA",
            success: true,
            status: JobStatus.Completed,
            duration: TimeSpan.FromMilliseconds(10)));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            jobId: "j2",
            jobType: "JobA",
            success: false,
            status: JobStatus.Failed,
            duration: TimeSpan.FromMilliseconds(30)));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            jobId: "j3",
            jobType: "JobA",
            success: false,
            status: JobStatus.Cancelled,
            duration: TimeSpan.FromMilliseconds(20)));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(
            jobId: "j4",
            jobType: "JobB",
            duration: TimeSpan.FromMilliseconds(5)));

        JobMetricsAggregate aggregate = await metrics.GetMetricsAsync("JobA", start, end);

        aggregate.JobType.Should().Be("JobA");
        aggregate.TotalExecutions.Should().Be(3);
        aggregate.SuccessfulExecutions.Should().Be(1);
        aggregate.FailedExecutions.Should().Be(1);
        aggregate.CancelledExecutions.Should().Be(1);
        aggregate.MinDuration.Should().Be(TimeSpan.FromMilliseconds(10));
        aggregate.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(30));
        aggregate.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(20));
        aggregate.P50Duration.Should().NotBeNull();
        aggregate.P95Duration.Should().NotBeNull();
        aggregate.P99Duration.Should().NotBeNull();
        aggregate.Throughput.Should().BeGreaterThan(0);
        aggregate.SuccessRate.Should().BeApproximately(1.0 / 3.0, 0.001);
        aggregate.FailureRate.Should().BeApproximately(1.0 / 3.0, 0.001);
    }

    [Fact]
    public async Task GetMetricsAsync_WithoutDurations_ShouldLeavePercentilesNull()
    {
        var metrics = new InMemoryJobMetrics();
        await metrics.RecordMetricAsync(new JobMetric
        {
            JobId = "j1",
            JobType = "JobA",
            Success = true,
            Status = JobStatus.Completed,
            Duration = null
        });

        JobMetricsAggregate aggregate = await metrics.GetMetricsAsync("JobA");

        aggregate.TotalExecutions.Should().Be(1);
        aggregate.AverageDuration.Should().BeNull();
        aggregate.MinDuration.Should().BeNull();
        aggregate.MaxDuration.Should().BeNull();
    }

    [Fact]
    public async Task GetAllMetricsAsync_ShouldReturnMetricsForAllJobTypes()
    {
        var metrics = new InMemoryJobMetrics();
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(jobType: "JobA"));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(jobId: "j2", jobType: "JobB"));

        IReadOnlyDictionary<string, JobMetricsAggregate> all = await metrics.GetAllMetricsAsync();

        all.Should().ContainKeys("JobA", "JobB");
        all["JobA"].TotalExecutions.Should().Be(1);
        all["JobB"].TotalExecutions.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetQueueStatisticsAsync_WithEmptyQueueName_ShouldThrowArgumentException(string? queueName)
    {
        var metrics = new InMemoryJobMetrics();

        Func<Task> act = () => metrics.GetQueueStatisticsAsync(queueName!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("queueName");
    }

    [Fact]
    public async Task GetQueueStatisticsAsync_WhenQueueMissing_ShouldReturnEmptyStats()
    {
        var metrics = new InMemoryJobMetrics();

        QueueStatistics stats = await metrics.GetQueueStatisticsAsync("unknown");

        stats.QueueName.Should().Be("unknown");
        stats.CompletedJobs.Should().Be(0);
        stats.FailedJobs.Should().Be(0);
    }

    [Fact]
    public async Task GetAllQueueStatisticsAsync_ShouldReturnRecordedQueues()
    {
        var metrics = new InMemoryJobMetrics();
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(queue: "q1"));
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric(jobId: "j2", queue: "q2"));

        IReadOnlyDictionary<string, QueueStatistics> all = await metrics.GetAllQueueStatisticsAsync();

        all.Should().ContainKeys("q1", "q2");
    }

    [Fact]
    public async Task ResetMetricsAsync_ShouldClearMetricsAndQueueStats()
    {
        var metrics = new InMemoryJobMetrics();
        await metrics.RecordMetricAsync(BackgroundJobsTestHelpers.CreateMetric());

        await metrics.ResetMetricsAsync();

        (await metrics.GetAllMetricsAsync()).Should().BeEmpty();
        (await metrics.GetAllQueueStatisticsAsync()).Should().BeEmpty();
        (await metrics.GetQueueStatisticsAsync("default")).CompletedJobs.Should().Be(0);
    }
}
