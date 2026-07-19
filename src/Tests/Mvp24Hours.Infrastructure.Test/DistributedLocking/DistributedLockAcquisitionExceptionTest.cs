//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Exceptions;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class DistributedLockAcquisitionExceptionTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseFailedStatusAndEmptyResource()
    {
        var ex = new DistributedLockAcquisitionException();

        ex.Resource.Should().BeEmpty();
        ex.Status.Should().Be(LockAcquisitionStatus.Failed);
        ex.Message.Should().Be("Distributed lock acquisition failed.");
    }

    [Fact]
    public void Constructor_WithNullResource_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new DistributedLockAcquisitionException(
            null!,
            LockAcquisitionStatus.Timeout,
            "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("resource");
    }

    [Fact]
    public void Constructor_WithResourceAndStatus_ShouldSetProperties()
    {
        var ex = new DistributedLockAcquisitionException(
            "res-1",
            LockAcquisitionStatus.Timeout,
            "timed out");

        ex.Resource.Should().Be("res-1");
        ex.Status.Should().Be(LockAcquisitionStatus.Timeout);
        ex.Message.Should().Be("timed out");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldPreserveInner()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new DistributedLockAcquisitionException(
            "res-1",
            LockAcquisitionStatus.Failed,
            "failed",
            inner);

        ex.InnerException.Should().BeSameAs(inner);
        ex.Resource.Should().Be("res-1");
        ex.Status.Should().Be(LockAcquisitionStatus.Failed);
    }
}
