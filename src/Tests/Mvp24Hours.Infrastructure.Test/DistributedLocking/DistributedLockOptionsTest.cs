//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Options;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class DistributedLockOptionsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        DistributedLockOptions options = DistributedLockOptions.Default;

        options.AcquisitionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.LockDuration.Should().Be(TimeSpan.FromMinutes(5));
        options.EnableAutoRenewal.Should().BeFalse();
        options.RenewalInterval.Should().Be(TimeSpan.FromMinutes(2));
        options.EnableFencing.Should().BeFalse();
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.ThrowOnFailure.Should().BeFalse();
    }

    [Fact]
    public void ShortOperation_ShouldDisableAutoRenewalAndUseShortTimeouts()
    {
        DistributedLockOptions options = DistributedLockOptions.ShortOperation;

        options.AcquisitionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.LockDuration.Should().Be(TimeSpan.FromMinutes(1));
        options.EnableAutoRenewal.Should().BeFalse();
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void LongOperation_ShouldEnableAutoRenewal()
    {
        DistributedLockOptions options = DistributedLockOptions.LongOperation;

        options.AcquisitionTimeout.Should().Be(TimeSpan.FromMinutes(1));
        options.LockDuration.Should().Be(TimeSpan.FromMinutes(10));
        options.EnableAutoRenewal.Should().BeTrue();
        options.RenewalInterval.Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void CriticalOperation_ShouldEnableFencingAndAutoRenewal()
    {
        DistributedLockOptions options = DistributedLockOptions.CriticalOperation;

        options.EnableFencing.Should().BeTrue();
        options.EnableAutoRenewal.Should().BeTrue();
        options.AcquisitionTimeout.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void HighContention_ShouldFailFast()
    {
        DistributedLockOptions options = DistributedLockOptions.HighContention;

        options.AcquisitionTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(25));
        options.EnableAutoRenewal.Should().BeFalse();
    }
}
