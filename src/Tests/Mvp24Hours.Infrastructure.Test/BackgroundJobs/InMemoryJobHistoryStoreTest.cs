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
public class InMemoryJobHistoryStoreTest
{
    [Fact]
    public async Task RecordExecutionAsync_WithNullRecord_ShouldThrowArgumentNullException()
    {
        var store = new InMemoryJobHistoryStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RecordExecutionAsync(null!));
    }

    [Fact]
    public async Task RecordExecutionAsync_ShouldStoreRecord()
    {
        var store = new InMemoryJobHistoryStore();
        JobExecutionRecord record = BackgroundJobsTestHelpers.CreateRecord();

        await store.RecordExecutionAsync(record);

        IReadOnlyList<JobExecutionRecord> history = await store.GetJobHistoryAsync("job-1");
        history.Should().ContainSingle().Which.JobId.Should().Be("job-1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetJobHistoryAsync_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        var store = new InMemoryJobHistoryStore();

        Func<Task> act = () => store.GetJobHistoryAsync(jobId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task GetJobHistoryAsync_ShouldReturnOnlyMatchingJobOrderedByStartedAtDescending()
    {
        var store = new InMemoryJobHistoryStore();
        DateTimeOffset older = DateTimeOffset.UtcNow.AddMinutes(-10);
        DateTimeOffset newer = DateTimeOffset.UtcNow;

        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(startedAt: older));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "job-1",
            startedAt: newer,
            duration: TimeSpan.FromMilliseconds(10)));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(jobId: "other"));

        IReadOnlyList<JobExecutionRecord> history = await store.GetJobHistoryAsync("job-1");

        history.Should().HaveCount(2);
        history[0].StartedAt.Should().Be(newer);
        history[1].StartedAt.Should().Be(older);
    }

    [Fact]
    public async Task QueryHistoryAsync_WithNullFilter_ShouldThrowArgumentNullException()
    {
        var store = new InMemoryJobHistoryStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.QueryHistoryAsync(null!));
    }

    [Fact]
    public async Task QueryHistoryAsync_ShouldApplyAllFiltersAndPaging()
    {
        var store = new InMemoryJobHistoryStore();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "a",
            jobType: "TypeA",
            status: JobStatus.Completed,
            queue: "q1",
            startedAt: now));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "b",
            jobType: "TypeB",
            status: JobStatus.Failed,
            queue: "q1",
            startedAt: now));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "c",
            jobType: "TypeA",
            status: JobStatus.Completed,
            queue: "q2",
            startedAt: now));

        IReadOnlyList<JobExecutionRecord> results = await store.QueryHistoryAsync(new JobHistoryFilter
        {
            JobId = "a",
            JobType = "TypeA",
            Status = JobStatus.Completed,
            Queue = "q1",
            StartDate = now.AddMinutes(-1),
            EndDate = now.AddMinutes(1),
            Skip = 0,
            MaxRecords = 5
        });

        results.Should().ContainSingle().Which.JobId.Should().Be("a");
    }

    [Fact]
    public async Task QueryHistoryAsync_WithSkipAndMaxRecords_ShouldPageResults()
    {
        var store = new InMemoryJobHistoryStore();
        for (int i = 0; i < 5; i++)
        {
            await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
                jobId: $"job-{i}",
                startedAt: DateTimeOffset.UtcNow.AddMinutes(-i)));
        }

        IReadOnlyList<JobExecutionRecord> page = await store.QueryHistoryAsync(new JobHistoryFilter
        {
            Skip = 1,
            MaxRecords = 2
        });

        page.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStatisticsAsync_WithEmptyJobType_ShouldThrowArgumentException(string? jobType)
    {
        var store = new InMemoryJobHistoryStore();

        Func<Task> act = () => store.GetStatisticsAsync(jobType!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobType");
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldAggregateStatusesAndDurations()
    {
        var store = new InMemoryJobHistoryStore();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-1);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddHours(1);

        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobType: "JobA",
            status: JobStatus.Completed,
            duration: TimeSpan.FromMilliseconds(10)));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "j2",
            jobType: "JobA",
            status: JobStatus.Failed,
            duration: TimeSpan.FromMilliseconds(30)));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "j3",
            jobType: "JobA",
            status: JobStatus.Cancelled,
            duration: TimeSpan.FromMilliseconds(20)));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "j4",
            jobType: "JobB",
            status: JobStatus.Completed));

        JobExecutionStatistics stats = await store.GetStatisticsAsync("JobA", start, end);

        stats.JobType.Should().Be("JobA");
        stats.TotalExecutions.Should().Be(3);
        stats.SuccessfulExecutions.Should().Be(1);
        stats.FailedExecutions.Should().Be(1);
        stats.CancelledExecutions.Should().Be(1);
        stats.MinDuration.Should().Be(TimeSpan.FromMilliseconds(10));
        stats.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(30));
        stats.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(20));
        stats.SuccessRate.Should().BeApproximately(1.0 / 3.0, 0.001);
        stats.FailureRate.Should().BeApproximately(1.0 / 3.0, 0.001);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithoutDurations_ShouldLeaveDurationFieldsNull()
    {
        var store = new InMemoryJobHistoryStore();
        await store.RecordExecutionAsync(new JobExecutionRecord
        {
            JobId = "j1",
            JobType = "JobA",
            Status = JobStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow,
            Duration = null
        });

        JobExecutionStatistics stats = await store.GetStatisticsAsync("JobA");

        stats.TotalExecutions.Should().Be(1);
        stats.AverageDuration.Should().BeNull();
        stats.MinDuration.Should().BeNull();
        stats.MaxDuration.Should().BeNull();
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_WithNegativeRetention_ShouldThrowArgumentException()
    {
        var store = new InMemoryJobHistoryStore();

        Func<Task> act = () => store.CleanupOldRecordsAsync(-1);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("retentionDays");
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_ShouldRemoveRecordsOlderThanRetention()
    {
        var store = new InMemoryJobHistoryStore();
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "old",
            startedAt: DateTimeOffset.UtcNow.AddDays(-10)));
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            jobId: "recent",
            startedAt: DateTimeOffset.UtcNow));

        int removed = await store.CleanupOldRecordsAsync(retentionDays: 5);

        removed.Should().Be(1);
        (await store.GetJobHistoryAsync("old")).Should().BeEmpty();
        (await store.GetJobHistoryAsync("recent")).Should().ContainSingle();
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_WithZeroRetention_ShouldRemoveAllPastRecords()
    {
        var store = new InMemoryJobHistoryStore();
        await store.RecordExecutionAsync(BackgroundJobsTestHelpers.CreateRecord(
            startedAt: DateTimeOffset.UtcNow.AddSeconds(-1)));

        int removed = await store.CleanupOldRecordsAsync(0);

        removed.Should().Be(1);
    }
}
