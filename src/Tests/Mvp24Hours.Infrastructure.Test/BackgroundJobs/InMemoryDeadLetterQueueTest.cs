//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Management;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs;

[Trait("Category", "Unit")]
public class InMemoryDeadLetterQueueTest
{
    [Fact]
    public async Task AddFailedJobAsync_WithNullJob_ShouldThrowArgumentNullException()
    {
        var dlq = new InMemoryDeadLetterQueue();

        await Assert.ThrowsAsync<ArgumentNullException>(() => dlq.AddFailedJobAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddFailedJobAsync_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        var dlq = new InMemoryDeadLetterQueue();
        FailedJob job = BackgroundJobsTestHelpers.CreateFailedJob(jobId: jobId!);

        Func<Task> act = () => dlq.AddFailedJobAsync(job);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("failedJob");
    }

    [Fact]
    public async Task AddFailedJobAsync_ShouldStoreJobAndSetAddedToDlqAt()
    {
        var dlq = new InMemoryDeadLetterQueue();
        FailedJob job = BackgroundJobsTestHelpers.CreateFailedJob();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        await dlq.AddFailedJobAsync(job);

        FailedJob? stored = await dlq.GetFailedJobAsync("job-1");
        stored.Should().NotBeNull();
        stored!.JobId.Should().Be("job-1");
        stored.AddedToDlqAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task AddFailedJobAsync_WithSameJobId_ShouldOverwrite()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob(errorMessage: "first"));
        FailedJob second = BackgroundJobsTestHelpers.CreateFailedJob();
        second.ErrorMessage = "second";

        await dlq.AddFailedJobAsync(second);

        FailedJob? stored = await dlq.GetFailedJobAsync("job-1");
        stored!.ErrorMessage.Should().Be("second");
        (await dlq.GetFailedJobCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetFailedJobsAsync_WithoutFilter_ShouldReturnAllOrderedByAddedToDlqAtDescending()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("older"));
        await Task.Delay(5);
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("newer"));

        IReadOnlyList<FailedJob> jobs = await dlq.GetFailedJobsAsync();

        jobs.Should().HaveCount(2);
        jobs[0].JobId.Should().Be("newer");
        jobs[1].JobId.Should().Be("older");
    }

    [Fact]
    public async Task GetFailedJobsAsync_WithFiltersAndPaging_ShouldApplyCriteria()
    {
        var dlq = new InMemoryDeadLetterQueue();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("a", "TypeA", "q1"));
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("b", "TypeB", "q1"));
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("c", "TypeA", "q2"));

        var filter = new DeadLetterQueueFilter
        {
            JobType = "TypeA",
            Queue = "q1",
            StartDate = now.AddMinutes(-1),
            EndDate = now.AddMinutes(1),
            Skip = 0,
            MaxRecords = 10
        };

        IReadOnlyList<FailedJob> jobs = await dlq.GetFailedJobsAsync(filter);

        jobs.Should().ContainSingle().Which.JobId.Should().Be("a");
    }

    [Fact]
    public async Task GetFailedJobsAsync_WithSkipAndMaxRecords_ShouldPageResults()
    {
        var dlq = new InMemoryDeadLetterQueue();
        for (int i = 0; i < 5; i++)
        {
            await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob($"job-{i}"));
            await Task.Delay(2);
        }

        IReadOnlyList<FailedJob> page = await dlq.GetFailedJobsAsync(new DeadLetterQueueFilter
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
    public async Task GetFailedJobAsync_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        var dlq = new InMemoryDeadLetterQueue();

        Func<Task> act = () => dlq.GetFailedJobAsync(jobId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task GetFailedJobAsync_WhenMissing_ShouldReturnNull()
    {
        var dlq = new InMemoryDeadLetterQueue();

        FailedJob? result = await dlq.GetFailedJobAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFailedJobAsync_ShouldReturnTrueWhenRemovedAndFalseWhenMissing()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob());

        (await dlq.RemoveFailedJobAsync("job-1")).Should().BeTrue();
        (await dlq.RemoveFailedJobAsync("job-1")).Should().BeFalse();
        (await dlq.GetFailedJobAsync("job-1")).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveFailedJobAsync_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        var dlq = new InMemoryDeadLetterQueue();

        Func<Task> act = () => dlq.RemoveFailedJobAsync(jobId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task RetryFailedJobAsync_WhenFound_ShouldRemoveAndReturnJobId()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob());

        string? result = await dlq.RetryFailedJobAsync("job-1");

        result.Should().Be("job-1");
        (await dlq.GetFailedJobAsync("job-1")).Should().BeNull();
    }

    [Fact]
    public async Task RetryFailedJobAsync_WhenMissing_ShouldReturnNull()
    {
        var dlq = new InMemoryDeadLetterQueue();

        string? result = await dlq.RetryFailedJobAsync("missing");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RetryFailedJobAsync_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        var dlq = new InMemoryDeadLetterQueue();

        Func<Task> act = () => dlq.RetryFailedJobAsync(jobId!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public async Task GetFailedJobCountAsync_WithFilter_ShouldCountMatchingJobs()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("a", "TypeA", "q1"));
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("b", "TypeB", "q1"));

        int count = await dlq.GetFailedJobCountAsync(new DeadLetterQueueFilter
        {
            JobType = "TypeA",
            Queue = "q1"
        });

        count.Should().Be(1);
    }

    [Fact]
    public async Task ClearOldFailedJobsAsync_ShouldRemoveOnlyOlderJobs()
    {
        var dlq = new InMemoryDeadLetterQueue();
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("old"));
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("new"));

        DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMilliseconds(1);
        await Task.Delay(5);
        await dlq.AddFailedJobAsync(BackgroundJobsTestHelpers.CreateFailedJob("fresh"));

        int removed = await dlq.ClearOldFailedJobsAsync(cutoff);

        removed.Should().Be(2);
        (await dlq.GetFailedJobCountAsync()).Should().Be(1);
        (await dlq.GetFailedJobAsync("fresh")).Should().NotBeNull();
    }
}
