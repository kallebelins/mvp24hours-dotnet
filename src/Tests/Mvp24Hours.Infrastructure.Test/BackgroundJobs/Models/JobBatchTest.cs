//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Models;

[Trait("Category", "Unit")]
public class JobBatchTest
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var options = new BatchOptions { MaxConcurrency = 2 };
        var batch = new JobBatch("order-batch", options);

        batch.BatchId.Should().NotBeNullOrWhiteSpace();
        batch.Name.Should().Be("order-batch");
        batch.Options.MaxConcurrency.Should().Be(2);
        batch.Jobs.Should().BeEmpty();
        batch.Status.Should().Be(BatchStatus.Pending);
        batch.StartedAt.Should().BeNull();
        batch.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void AddJob_WithArgs_ShouldAddBatchJob()
    {
        var batch = new JobBatch();
        var jobOptions = new JobOptions { Queue = "batch-queue" };

        BatchJob job = batch.AddJob<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs { Value = "x" },
            jobOptions,
            ["dep-1"]);

        batch.Jobs.Should().HaveCount(1);
        job.JobId.Should().NotBeNullOrWhiteSpace();
        job.JobType.Should().Contain("DummyJobWithArgs");
        job.SerializedArgs.Should().Contain("x");
        job.JobOptions!.Queue.Should().Be("batch-queue");
        job.Dependencies.Should().ContainSingle().Which.Should().Be("dep-1");
    }

    [Fact]
    public void AddJob_WithArgs_WithNullArgs_ShouldThrowArgumentNullException()
    {
        var batch = new JobBatch();

        Action act = () => batch.AddJob<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("args");
    }

    [Fact]
    public void AddJob_WithoutArgs_ShouldAddBatchJob()
    {
        var batch = new JobBatch();

        BatchJob job = batch.AddJob<BackgroundJobsTestHelpers.DummyJob>();

        batch.Jobs.Should().HaveCount(1);
        job.SerializedArgs.Should().Be("{}");
    }

    [Fact]
    public void RemoveJob_WithExistingJobId_ShouldRemoveJob()
    {
        var batch = new JobBatch();
        BatchJob job = batch.AddJob<BackgroundJobsTestHelpers.DummyJob>();

        bool removed = batch.RemoveJob(job.JobId);

        removed.Should().BeTrue();
        batch.Jobs.Should().BeEmpty();
    }

    [Fact]
    public void RemoveJob_WithUnknownJobId_ShouldReturnFalse()
    {
        var batch = new JobBatch();

        bool removed = batch.RemoveJob("missing");

        removed.Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveAllJobs()
    {
        var batch = new JobBatch();
        batch.AddJob<BackgroundJobsTestHelpers.DummyJob>();
        batch.AddJob<BackgroundJobsTestHelpers.DummyJobWithArgs, BackgroundJobsTestHelpers.DummyJobArgs>(
            new BackgroundJobsTestHelpers.DummyJobArgs());

        batch.Clear();

        batch.Jobs.Should().BeEmpty();
    }
}
