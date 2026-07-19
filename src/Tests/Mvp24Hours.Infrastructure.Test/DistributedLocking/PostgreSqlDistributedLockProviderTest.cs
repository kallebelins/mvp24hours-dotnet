//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Providers;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

/// <summary>
/// PostgreSQL provider requires a live database for advisory lock semantics.
/// Unit tests cover constructor guards and controlled failure paths with an unreachable host.
/// </summary>
[Trait("Category", "Unit")]
public class PostgreSqlDistributedLockProviderTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidConnectionString_ShouldThrow(string? connectionString)
    {
        Action act = () => _ = new PostgreSqlDistributedLockProvider(connectionString!);

        act.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithValidConnectionString_ShouldCreateInstance()
    {
        var provider = new PostgreSqlDistributedLockProvider(
            DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString());

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithSharedLockAndLogger_ShouldNotThrow()
    {
        var logger = new Mock<ILogger<PostgreSqlDistributedLockProvider>>();

        Action act = () => _ = new PostgreSqlDistributedLockProvider(
            DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString(),
            logger.Object,
            useSharedLock: true);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task IsLockedAsync_WithUnreachableServer_ShouldReturnFalse()
    {
        var provider = new PostgreSqlDistributedLockProvider(
            DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString());

        bool locked = await provider.IsLockedAsync(DistributedLockingTestHelpers.UniqueResource());

        locked.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WithUnreachableServerAndZeroTimeout_ShouldNotAcquire()
    {
        var provider = new PostgreSqlDistributedLockProvider(
            DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString());

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            DistributedLockingTestHelpers.UniqueResource(),
            DistributedLockingTestHelpers.FastFailOptions(acquisitionTimeout: TimeSpan.Zero));

        result.IsAcquired.Should().BeFalse();
        (result.IsTimeout || result.IsFailed).Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_WithUnreachableServer_ShouldFailWithoutThrowing()
    {
        var provider = new PostgreSqlDistributedLockProvider(
            DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString());

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            DistributedLockingTestHelpers.UniqueResource(),
            DistributedLockingTestHelpers.FastFailOptions(
                acquisitionTimeout: TimeSpan.FromMilliseconds(100),
                retryDelay: TimeSpan.FromMilliseconds(10)));

        result.IsAcquired.Should().BeFalse();
        (result.IsTimeout || result.IsFailed).Should().BeTrue();
    }
}
