//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Results;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Results;

[Trait("Category", "Unit")]
public class JobExecutionResultTest
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;
        TimeSpan duration = TimeSpan.FromMilliseconds(120);

        JobExecutionResult result = JobExecutionResult.Success(duration, completedAt);

        result.Status.Should().Be(JobExecutionStatus.Success);
        result.Duration.Should().Be(duration);
        result.CompletedAt.Should().Be(completedAt);
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.IsCancelled.Should().BeFalse();
        result.IsRetrying.Should().BeFalse();
        result.WillRetry.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failed_WithMessage_ShouldCreateFailedResult()
    {
        JobExecutionResult result = JobExecutionResult.Failed("boom", willRetry: false);

        result.Status.Should().Be(JobExecutionStatus.Failed);
        result.ErrorMessage.Should().Be("boom");
        result.IsFailure.Should().BeTrue();
        result.WillRetry.Should().BeFalse();
    }

    [Fact]
    public void Failed_WithMessageAndRetry_ShouldCreateRetryingResult()
    {
        JobExecutionResult result = JobExecutionResult.Failed("retry-me", willRetry: true);

        result.Status.Should().Be(JobExecutionStatus.Retrying);
        result.IsRetrying.Should().BeTrue();
        result.WillRetry.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failed_WithEmptyMessage_ShouldThrowArgumentException(string? message)
    {
        Action act = () => JobExecutionResult.Failed(message!);

        act.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_WithException_ShouldUseExceptionMessage()
    {
        var exception = new InvalidOperationException("exception boom");

        JobExecutionResult result = JobExecutionResult.Failed(exception);

        result.Status.Should().Be(JobExecutionStatus.Failed);
        result.ErrorMessage.Should().Be("exception boom");
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Failed_WithNullException_ShouldThrowArgumentNullException()
    {
        Action act = () => JobExecutionResult.Failed((Exception)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    [Fact]
    public void Cancelled_ShouldCreateCancelledResult()
    {
        JobExecutionResult result = JobExecutionResult.Cancelled(TimeSpan.FromSeconds(1));

        result.Status.Should().Be(JobExecutionStatus.Cancelled);
        result.IsCancelled.Should().BeTrue();
        result.Duration.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Retrying_ShouldCreateRetryingResult()
    {
        var exception = new TimeoutException("timeout");

        JobExecutionResult result = JobExecutionResult.Retrying("timeout", exception);

        result.Status.Should().Be(JobExecutionStatus.Retrying);
        result.IsRetrying.Should().BeTrue();
        result.WillRetry.Should().BeTrue();
        result.Exception.Should().BeSameAs(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Retrying_WithEmptyMessage_ShouldThrowArgumentException(string? message)
    {
        Action act = () => JobExecutionResult.Retrying(message!);

        act.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }
}
