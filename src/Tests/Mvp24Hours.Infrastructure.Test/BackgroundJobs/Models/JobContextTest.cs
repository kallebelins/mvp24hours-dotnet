//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Models;

[Trait("Category", "Unit")]
public class JobContextTest
{
    [Fact]
    public void Constructor_WithValidArgs_ShouldSetProperties()
    {
        var metadata = new Dictionary<string, string> { { "key", "value" } };
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        using var cts = new CancellationTokenSource();

        var context = new JobContext(
            "job-1",
            1,
            cts.Token,
            metadata,
            startedAt,
            "Test.Job",
            "high-priority");

        context.JobId.Should().Be("job-1");
        context.AttemptNumber.Should().Be(1);
        context.CancellationToken.Should().Be(cts.Token);
        context.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
        context.StartedAt.Should().Be(startedAt);
        context.JobType.Should().Be("Test.Job");
        context.Queue.Should().Be("high-priority");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyJobId_ShouldThrowArgumentException(string? jobId)
    {
        Action act = () => _ = new JobContext(
            jobId!,
            1,
            CancellationToken.None,
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow,
            "Test.Job");

        act.Should().Throw<ArgumentException>().WithParameterName("jobId");
    }

    [Fact]
    public void Constructor_WithZeroAttemptNumber_ShouldThrowArgumentException()
    {
        Action act = () => _ = new JobContext(
            "job-1",
            0,
            CancellationToken.None,
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow,
            "Test.Job");

        act.Should().Throw<ArgumentException>().WithParameterName("attemptNumber");
    }

    [Fact]
    public void Constructor_WithNullMetadata_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new JobContext(
            "job-1",
            1,
            CancellationToken.None,
            null!,
            DateTimeOffset.UtcNow,
            "Test.Job");

        act.Should().Throw<ArgumentNullException>().WithParameterName("metadata");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyJobType_ShouldThrowArgumentException(string? jobType)
    {
        Action act = () => _ = new JobContext(
            "job-1",
            1,
            CancellationToken.None,
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow,
            jobType!);

        act.Should().Throw<ArgumentException>().WithParameterName("jobType");
    }
}
