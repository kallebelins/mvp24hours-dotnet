//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class LockAcquisitionResultTest
{
    [Fact]
    public void Acquired_WithNullHandle_ShouldThrowArgumentNullException()
    {
        Action act = () => LockAcquisitionResult.Acquired(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("lockHandle");
    }

    [Fact]
    public void Acquired_ShouldSetStatusHandleAndFencedToken()
    {
        var handle = new Mock<ILockHandle>();
        DateTimeOffset attempted = DateTimeOffset.UtcNow.AddSeconds(-1);
        DateTimeOffset completed = DateTimeOffset.UtcNow;

        var result = LockAcquisitionResult.Acquired(handle.Object, 42, attempted, completed);

        result.IsAcquired.Should().BeTrue();
        result.IsTimeout.Should().BeFalse();
        result.IsFailed.Should().BeFalse();
        result.Status.Should().Be(LockAcquisitionStatus.Acquired);
        result.LockHandle.Should().BeSameAs(handle.Object);
        result.FencedToken.Should().Be(42);
        result.AttemptedAt.Should().Be(attempted);
        result.CompletedAt.Should().Be(completed);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Timeout_ShouldSetTimeoutStatusAndDefaultMessage()
    {
        var result = LockAcquisitionResult.Timeout();

        result.IsTimeout.Should().BeTrue();
        result.IsAcquired.Should().BeFalse();
        result.LockHandle.Should().BeNull();
        result.ErrorMessage.Should().Be("Lock acquisition timed out.");
    }

    [Fact]
    public void Timeout_WithCustomMessage_ShouldUseMessage()
    {
        var result = LockAcquisitionResult.Timeout("custom timeout");

        result.ErrorMessage.Should().Be("custom timeout");
    }

    [Fact]
    public void Failed_WithEmptyMessage_ShouldThrowArgumentException()
    {
        Action act = () => LockAcquisitionResult.Failed("  ");

        act.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_ShouldSetFailedStatusExceptionAndMessage()
    {
        var ex = new InvalidOperationException("boom");

        var result = LockAcquisitionResult.Failed("failed", ex);

        result.IsFailed.Should().BeTrue();
        result.Status.Should().Be(LockAcquisitionStatus.Failed);
        result.ErrorMessage.Should().Be("failed");
        result.Exception.Should().BeSameAs(ex);
        result.LockHandle.Should().BeNull();
    }
}
