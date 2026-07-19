//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Providers;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using StackExchange.Redis;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class RedisDistributedLockProviderTest
{
    [Fact]
    public void Constructor_WithNullConnections_ShouldThrowArgumentException()
    {
        Action act = () => _ = new RedisDistributedLockProvider((IConnectionMultiplexer[])null!);

        act.Should().Throw<ArgumentException>().WithParameterName("redisConnections");
    }

    [Fact]
    public void Constructor_WithEmptyConnections_ShouldThrowArgumentException()
    {
        Action act = () => _ = new RedisDistributedLockProvider([]);

        act.Should().Throw<ArgumentException>().WithParameterName("redisConnections");
    }

    [Fact]
    public void Constructor_WithSingleConnection_ShouldCreateInstance()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();

        var provider = new RedisDistributedLockProvider(redis.Object);

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        var logger = new Mock<ILogger<RedisDistributedLockProvider>>();

        Action act = () => _ = new RedisDistributedLockProvider(redis.Object, logger.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenSetSucceeds_ShouldAcquireWithFencedToken()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis(stringSetSucceeds: true);
        var provider = new RedisDistributedLockProvider(redis.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            result.IsAcquired.Should().BeTrue();
            result.FencedToken.Should().NotBeNull();
            result.LockHandle!.FencedToken.Should().Be(result.FencedToken);
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenSetFails_ShouldNotAcquire()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis(stringSetSucceeds: false);
        var provider = new RedisDistributedLockProvider(redis.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.Zero);

        LockAcquisitionResult result = await provider.TryAcquireAsync(resource, options);

        result.IsAcquired.Should().BeFalse();
        (result.IsTimeout || result.IsFailed).Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireWithFenceAsync_ShouldEnableFencingAndReturnToken()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        var provider = new RedisDistributedLockProvider(redis.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireWithFenceAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            result.IsAcquired.Should().BeTrue();
            result.FencedToken.Should().NotBeNull();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task IsLockedAsync_WhenKeyExists_ShouldReturnTrue()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis(stringSetSucceeds: true);
        var provider = new RedisDistributedLockProvider(redis.Object);

        bool locked = await provider.IsLockedAsync(DistributedLockingTestHelpers.UniqueResource());

        locked.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenKeyMissing_ShouldReturnFalse()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis(stringSetSucceeds: false);
        var provider = new RedisDistributedLockProvider(redis.Object);

        bool locked = await provider.IsLockedAsync(DistributedLockingTestHelpers.UniqueResource());

        locked.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenRedisThrows_ShouldReturnFalse()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        redis.Database
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("unavailable"));
        var provider = new RedisDistributedLockProvider(redis.Object);

        bool locked = await provider.IsLockedAsync(DistributedLockingTestHelpers.UniqueResource());

        locked.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_WhenScriptSucceeds_ShouldReturnTrue()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        var provider = new RedisDistributedLockProvider(redis.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());
        result.IsAcquired.Should().BeTrue();

        bool released = await result.LockHandle!.ReleaseAsync();

        released.Should().BeTrue();
        redis.Database.Verify(
            d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RenewAsync_WhenScriptSucceeds_ShouldReturnTrue()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        redis.Database
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(true));
        var provider = new RedisDistributedLockProvider(redis.Object);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            bool renewed = await result.LockHandle!.RenewAsync();
            renewed.Should().BeTrue();
        }
        finally
        {
            await result.LockHandle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenStringSetThrows_ShouldFailAcquisition()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        redis.Database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists))
            .ThrowsAsync(new RedisException("boom"));
        var provider = new RedisDistributedLockProvider(redis.Object);
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.Zero);

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            DistributedLockingTestHelpers.UniqueResource(),
            options);

        result.IsAcquired.Should().BeFalse();
    }

    [Fact]
    public async Task RedLock_WhenQuorumReached_ShouldAcquire()
    {
        DistributedLockingTestHelpers.MockRedis r1 = DistributedLockingTestHelpers.CreateMockRedis(true);
        DistributedLockingTestHelpers.MockRedis r2 = DistributedLockingTestHelpers.CreateMockRedis(true);
        DistributedLockingTestHelpers.MockRedis r3 = DistributedLockingTestHelpers.CreateMockRedis(false);
        var provider = new RedisDistributedLockProvider([r1.Object, r2.Object, r3.Object]);
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions(lockDuration: TimeSpan.FromSeconds(5)));

        try
        {
            result.IsAcquired.Should().BeTrue();
            result.FencedToken.Should().NotBeNull();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task RedLock_WhenQuorumNotReached_ShouldFailAndReleasePartialLocks()
    {
        DistributedLockingTestHelpers.MockRedis r1 = DistributedLockingTestHelpers.CreateMockRedis(true);
        DistributedLockingTestHelpers.MockRedis r2 = DistributedLockingTestHelpers.CreateMockRedis(false);
        DistributedLockingTestHelpers.MockRedis r3 = DistributedLockingTestHelpers.CreateMockRedis(false);
        var provider = new RedisDistributedLockProvider([r1.Object, r2.Object, r3.Object]);
        DistributedLockOptions options = DistributedLockingTestHelpers.FastFailOptions(
            acquisitionTimeout: TimeSpan.FromMilliseconds(50),
            retryDelay: TimeSpan.FromMilliseconds(10),
            lockDuration: TimeSpan.FromSeconds(5));

        LockAcquisitionResult result = await provider.TryAcquireAsync(
            DistributedLockingTestHelpers.UniqueResource(),
            options);

        result.IsAcquired.Should().BeFalse();
        r1.Database.Verify(
            d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RedLock_RenewAsync_WhenQuorumRenewed_ShouldReturnTrue()
    {
        DistributedLockingTestHelpers.MockRedis r1 = DistributedLockingTestHelpers.CreateMockRedis(true);
        DistributedLockingTestHelpers.MockRedis r2 = DistributedLockingTestHelpers.CreateMockRedis(true);
        DistributedLockingTestHelpers.MockRedis r3 = DistributedLockingTestHelpers.CreateMockRedis(true);

        foreach (DistributedLockingTestHelpers.MockRedis redis in new[] { r1, r2, r3 })
        {
            redis.Database
                .Setup(d => d.ScriptEvaluateAsync(
                    It.IsAny<string>(),
                    It.IsAny<RedisKey[]>(),
                    It.IsAny<RedisValue[]>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisResult.Create(true));
        }

        var provider = new RedisDistributedLockProvider([r1.Object, r2.Object, r3.Object]);
        LockAcquisitionResult result = await provider.TryAcquireAsync(
            DistributedLockingTestHelpers.UniqueResource(),
            DistributedLockingTestHelpers.FastFailOptions(lockDuration: TimeSpan.FromSeconds(5)));

        try
        {
            result.IsAcquired.Should().BeTrue();
            (await result.LockHandle!.RenewAsync()).Should().BeTrue();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }
}
