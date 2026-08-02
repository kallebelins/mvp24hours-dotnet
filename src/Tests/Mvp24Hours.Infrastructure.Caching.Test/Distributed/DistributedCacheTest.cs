using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Providers;
using Mvp24Hours.Infrastructure.Caching.Resilience;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Distributed;

[Trait("Category", "Unit")]
public class DistributedCacheProviderAdvancedTest
{
    [Fact]
    public async Task SetAsync_WithAbsoluteExpiration_ShouldRoundTrip()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        var options = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await provider.SetAsync("abs-key", new TestEntity { Id = 1, Name = "Abs" }, options);
        TestEntity? result = await provider.GetAsync<TestEntity>("abs-key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Abs");
    }

    [Fact]
    public async Task SetAsync_WithSlidingAndAbsoluteExpiration_ShouldRoundTrip()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        var options = new CacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        await provider.SetAsync("slide-key", new TestEntity { Id = 2, Name = "Slide" }, options);

        (await provider.ExistsAsync("slide-key")).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_EmptyKey_ShouldThrow()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetAsync<TestEntity>(" "));
    }

    [Fact]
    public async Task GetStringAsync_EmptyKey_ShouldThrow()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetStringAsync(""));
    }

    [Fact]
    public async Task SetStringAsync_NullValue_ShouldThrow()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.SetStringAsync("key", null!));
    }

    [Fact]
    public async Task RemoveManyAsync_NullOrEmpty_ShouldNoOp()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await provider.RemoveManyAsync(null!);
        await provider.RemoveManyAsync([]);

        (await provider.ExistsAsync("any")).Should().BeFalse();
    }

    [Fact]
    public async Task GetManyAsync_EmptyKeys_ShouldReturnEmpty()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        Dictionary<string, TestEntity> result = await provider.GetManyAsync<TestEntity>([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetManyAsync_Empty_ShouldNoOp()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await provider.SetManyAsync(new Dictionary<string, TestEntity>());

        (await provider.ExistsAsync("none")).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_EmptyKey_ShouldReturnFalse()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        (await provider.ExistsAsync(" ")).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_EmptyKey_ShouldNoOp()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        Func<Task> act = async () => await provider.RefreshAsync(" ");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WhenUnderlyingThrows_ShouldReturnNull()
    {
        var failingCache = new Mock<IDistributedCache>();
        failingCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("backend down"));
        var provider = new DistributedCacheProvider(failingCache.Object);

        TestEntity? result = await provider.GetAsync<TestEntity>("key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenUnderlyingThrows_ShouldReturnFalse()
    {
        var failingCache = new Mock<IDistributedCache>();
        failingCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("timeout"));
        var provider = new DistributedCacheProvider(failingCache.Object);

        bool exists = await provider.ExistsAsync("key");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SetAsync_WhenUnderlyingThrows_ShouldPropagate()
    {
        var failingCache = new Mock<IDistributedCache>();
        failingCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("write failed"));
        var provider = new DistributedCacheProvider(failingCache.Object);

        await Assert.ThrowsAsync<IOException>(() =>
            provider.SetAsync("key", new TestEntity { Id = 1, Name = "X" }));
    }

    [Fact]
    public async Task WithResilience_WhenDistributedFails_ShouldDegradeGracefully()
    {
        var failingCache = new Mock<IDistributedCache>();
        failingCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("redis down"));
        var inner = new DistributedCacheProvider(failingCache.Object);
        ICacheProvider resilient = inner.WithResilience(new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false,
            EnableGracefulDegradation = true
        });

        TestEntity? result = await resilient.GetAsync<TestEntity>("key");

        result.Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class MultiLevelCacheCoordinationTest
{
    [Fact]
    public async Task GetStringAsync_L2Hit_ShouldPromoteToL1()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
        await l2.SetStringAsync("str-key", "from-l2");
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618

        string? result = await cache.GetStringAsync("str-key");

        result.Should().Be("from-l2");
        (await l1.GetStringAsync("str-key")).Should().Be("from-l2");
    }

    [Fact]
    public async Task SetStringAsync_ShouldWriteBothLevels()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618

        await cache.SetStringAsync("both-str", "value");

        (await l1.GetStringAsync("both-str")).Should().Be("value");
        (await l2.GetStringAsync("both-str")).Should().Be("value");
    }

    [Fact]
    public async Task RemoveBothAsync_ShouldClearL1AndL2()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await cache.SetAsync("rm-key", new TestEntity { Id = 1, Name = "R" });

        await cache.RemoveBothAsync("rm-key");

        (await l1.ExistsAsync("rm-key")).Should().BeFalse();
        (await l2.ExistsAsync("rm-key")).Should().BeFalse();
    }

    [Fact]
    public async Task PromoteToL1Async_WhenPresentInL2_ShouldCopy()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await l2.SetAsync("promote", new TestEntity { Id = 9, Name = "Promo" });

        bool promoted = await cache.PromoteToL1Async<TestEntity>("promote");

        promoted.Should().BeTrue();
        (await l1.ExistsAsync("promote")).Should().BeTrue();
    }

    [Fact]
    public async Task PromoteToL1Async_WhenMissingInL2_ShouldReturnFalse()
    {
#pragma warning disable CS0618
        MultiLevelCache cache = CacheTestHelpers.CreateMultiLevelCache();
#pragma warning restore CS0618

        bool promoted = await cache.PromoteToL1Async<TestEntity>("missing");

        promoted.Should().BeFalse();
    }

    [Fact]
    public async Task GetFromL1AndL2_ShouldIsolateLevels()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await l1.SetAsync("l1-only", new TestEntity { Id = 1, Name = "L1" });
        await l2.SetAsync("l2-only", new TestEntity { Id = 2, Name = "L2" });

        TestEntity? fromL1 = await cache.GetFromL1Async<TestEntity>("l1-only");
        TestEntity? fromL2 = await cache.GetFromL2Async<TestEntity>("l2-only");
        TestEntity? missingL1 = await cache.GetFromL1Async<TestEntity>("l2-only");

        fromL1!.Name.Should().Be("L1");
        fromL2!.Name.Should().Be("L2");
        missingL1.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldCheckL1ThenL2()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await l2.SetStringAsync("l2-exists", "yes");

        (await cache.ExistsAsync("l2-exists")).Should().BeTrue();
        (await cache.ExistsAsync(" ")).Should().BeFalse();
    }

    [Fact]
    public async Task GetManyAsync_ShouldFillMissingFromL2AndPromote()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await l1.SetAsync("a", new TestEntity { Id = 1, Name = "A" });
        await l2.SetAsync("b", new TestEntity { Id = 2, Name = "B" });

        Dictionary<string, TestEntity> result = await cache.GetManyAsync<TestEntity>(["a", "b", "c"]);

        result.Should().HaveCount(2);
        result["a"].Name.Should().Be("A");
        result["b"].Name.Should().Be("B");
        (await l1.ExistsAsync("b")).Should().BeTrue();
    }

    [Fact]
    public async Task SetManyAsync_ShouldWriteBothAndPublishInvalidation()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
        var synchronizer = new Mock<ICacheSynchronizer>();
        synchronizer.Setup(x => x.PublishInvalidationManyAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2, synchronizer.Object);
#pragma warning restore CS0618
        var values = new Dictionary<string, TestEntity>
        {
            ["m1"] = new() { Id = 1, Name = "One" },
            ["m2"] = new() { Id = 2, Name = "Two" }
        };

        await cache.SetManyAsync(values);

        (await l1.ExistsAsync("m1")).Should().BeTrue();
        (await l2.ExistsAsync("m2")).Should().BeTrue();
        synchronizer.Verify(
            x => x.PublishInvalidationManyAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveManyAsync_ShouldRemoveFromBothLevels()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await cache.SetAsync("x", new TestEntity { Id = 1, Name = "X" });
        await cache.SetAsync("y", new TestEntity { Id = 2, Name = "Y" });

        await cache.RemoveManyAsync(["x", "y"]);

        (await cache.ExistsAsync("x")).Should().BeFalse();
        (await cache.ExistsAsync("y")).Should().BeFalse();
    }

    [Fact]
    public async Task Constructor_NullL1_ShouldThrow()
    {
#pragma warning disable CS0618
        Action act = () => _ = new MultiLevelCache(null!, CacheTestHelpers.CreateDistributedProvider());
#pragma warning restore CS0618

        act.Should().Throw<ArgumentNullException>().WithParameterName("l1Cache");
    }

    [Fact]
    public async Task GetOrSetAsync_NullFactory_ShouldThrow()
    {
#pragma warning disable CS0618
        MultiLevelCache cache = CacheTestHelpers.CreateMultiLevelCache();
#pragma warning restore CS0618

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            cache.GetOrSetAsync<TestEntity>("key", null!));
    }
}
