using Microsoft.Extensions.Caching.Memory;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Providers;

[Trait("Category", "Unit")]
public class MemoryCacheProviderTest
{
    [Fact]
    public async Task SetAndGet_ShouldRoundTripTypedValue()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();
        var entity = new TestEntity { Id = 1, Name = "Alpha" };

        await provider.SetAsync("key1", entity);
        TestEntity? result = await provider.GetAsync<TestEntity>("key1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task SetStringAndGetString_ShouldRoundTrip()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();

        await provider.SetStringAsync("str-key", "hello");
        string? result = await provider.GetStringAsync("str-key");

        result.Should().Be("hello");
    }

    [Fact]
    public async Task GetAsync_Miss_ShouldReturnNull()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();

        TestEntity? result = await provider.GetAsync<TestEntity>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReflectPresence()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();
        await provider.SetStringAsync("exists", "yes");

        (await provider.ExistsAsync("exists")).Should().BeTrue();
        (await provider.ExistsAsync("missing")).Should().BeFalse();
        (await provider.ExistsAsync("")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveKey()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();
        await provider.SetStringAsync("remove-me", "value");

        await provider.RemoveAsync("remove-me");

        (await provider.ExistsAsync("remove-me")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveManyAsync_ShouldRemoveMultipleKeys()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();
        await provider.SetStringAsync("a", "1");
        await provider.SetStringAsync("b", "2");
        await provider.SetStringAsync("c", "3");

        await provider.RemoveManyAsync(["a", "b"]);

        (await provider.ExistsAsync("a")).Should().BeFalse();
        (await provider.ExistsAsync("b")).Should().BeFalse();
        (await provider.ExistsAsync("c")).Should().BeTrue();
    }

    [Fact]
    public async Task GetManyAsync_ShouldReturnExistingEntries()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();
        await provider.SetAsync("k1", new TestEntity { Id = 1, Name = "One" });
        await provider.SetAsync("k2", new TestEntity { Id = 2, Name = "Two" });

        Dictionary<string, TestEntity> result = await provider.GetManyAsync<TestEntity>(["k1", "k2", "missing"]);

        result.Should().HaveCount(2);
        result["k1"].Name.Should().Be("One");
        result["k2"].Name.Should().Be("Two");
    }

    [Fact]
    public async Task SetAsync_NullValue_ShouldThrow()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.SetAsync<TestEntity>("key", null!));
    }

    [Fact]
    public async Task SetAsync_EmptyKey_ShouldThrow()
    {
        MemoryCacheProvider provider = CacheTestHelpers.CreateMemoryProvider();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.SetAsync(" ", new TestEntity { Id = 1, Name = "X" }));
    }

    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        Action act = () => new MemoryCacheProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_WithByteArrayStoredViaSerializer_ShouldDeserialize()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var serializer = new JsonCacheSerializer();
        var provider = new MemoryCacheProvider(memoryCache, serializer);
        byte[] bytes = await serializer.SerializeAsync(new TestEntity { Id = 7, Name = "Bytes" });
        memoryCache.Set("bytes-key", bytes);

        TestEntity? result = await provider.GetAsync<TestEntity>("bytes-key");

        result.Should().NotBeNull();
        result!.Id.Should().Be(7);
    }
}

[Trait("Category", "Unit")]
public class DistributedCacheProviderTest
{
    [Fact]
    public async Task SetAndGet_ShouldRoundTripTypedValue()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        var entity = new TestEntity { Id = 10, Name = "Distributed" };

        await provider.SetAsync("dist-key", entity);
        TestEntity? result = await provider.GetAsync<TestEntity>("dist-key");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Distributed");
    }

    [Fact]
    public async Task SetStringAndGetString_ShouldRoundTrip()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();

        await provider.SetStringAsync("dist-str", "payload");
        string? result = await provider.GetStringAsync("dist-str");

        result.Should().Be("payload");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveKey()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        await provider.SetStringAsync("to-remove", "value");

        await provider.RemoveAsync("to-remove");

        (await provider.ExistsAsync("to-remove")).Should().BeFalse();
    }

    [Fact]
    public async Task SetManyAsync_ShouldStoreAllValues()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        var values = new Dictionary<string, TestEntity>
        {
            ["m1"] = new() { Id = 1, Name = "One" },
            ["m2"] = new() { Id = 2, Name = "Two" }
        };

        await provider.SetManyAsync(values);
        Dictionary<string, TestEntity> result = await provider.GetManyAsync<TestEntity>(["m1", "m2"]);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshAsync_ShouldNotThrow()
    {
        DistributedCacheProvider provider = CacheTestHelpers.CreateDistributedProvider();
        await provider.SetStringAsync("refresh-key", "value");

        Func<Task> act = async () => await provider.RefreshAsync("refresh-key");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        Action act = () => new DistributedCacheProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

[Trait("Category", "Unit")]
public class MultiLevelCacheTest
{
    [Fact]
    public async Task GetAsync_L1Hit_ShouldReturnWithoutL2Access()
    {
        var l1 = new Mock<ICacheProvider>();
        var l2 = new Mock<ICacheProvider>();
        var entity = new TestEntity { Id = 1, Name = "L1" };
        l1.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1.Object, l2.Object);
#pragma warning restore CS0618

        TestEntity? result = await cache.GetAsync<TestEntity>("key");

        result.Should().BeSameAs(entity);
        l2.Verify(x => x.GetAsync<TestEntity>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_L2Hit_ShouldPromoteToL1()
    {
        var l1 = new Mock<ICacheProvider>();
        var l2 = new Mock<ICacheProvider>();
        var entity = new TestEntity { Id = 2, Name = "L2" };
        l1.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>())).ReturnsAsync((TestEntity?)null);
        l2.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1.Object, l2.Object);
#pragma warning restore CS0618

        TestEntity? result = await cache.GetAsync<TestEntity>("key");

        result.Should().BeSameAs(entity);
        l1.Verify(x => x.SetAsync("key", entity, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetBothAsync_ShouldWriteL2ThenL1AndPublishInvalidation()
    {
        var l1 = new Mock<ICacheProvider>();
        var l2 = new Mock<ICacheProvider>();
        var synchronizer = new Mock<ICacheSynchronizer>();
        var entity = new TestEntity { Id = 3, Name = "Both" };
        var callOrder = new List<string>();
        l2.Setup(x => x.SetAsync("key", entity, null, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("l2"))
            .Returns(Task.CompletedTask);
        l1.Setup(x => x.SetAsync("key", entity, null, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("l1"))
            .Returns(Task.CompletedTask);
        synchronizer.Setup(x => x.PublishInvalidationAsync("key", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1.Object, l2.Object, synchronizer.Object);
#pragma warning restore CS0618

        await cache.SetBothAsync("key", entity);

        callOrder.Should().ContainInOrder("l2", "l1");
        synchronizer.Verify(x => x.PublishInvalidationAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_Miss_ShouldLoadFromFactoryAndStoreBoth()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        int factoryCalls = 0;

        TestEntity? first = await cache.GetOrSetAsync(
            "factory-key",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<TestEntity>(new TestEntity { Id = 99, Name = "Factory" });
            });

        TestEntity? second = await cache.GetOrSetAsync(
            "factory-key",
            _ => Task.FromResult<TestEntity>(new TestEntity { Id = 100, Name = "ShouldNotRun" }));

        first!.Name.Should().Be("Factory");
        second!.Name.Should().Be("Factory");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_ShouldTrackL1Hits()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await cache.SetAsync("stats-key", new TestEntity { Id = 1, Name = "Stats" });

        await cache.GetAsync<TestEntity>("stats-key");
        MultiLevelCacheStatistics stats = cache.GetStatistics();

        stats.L1.Hits.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DemoteFromL1Async_WhenPresent_ShouldRemoveFromL1Only()
    {
        MemoryCacheProvider l1 = CacheTestHelpers.CreateMemoryProvider();
        DistributedCacheProvider l2 = CacheTestHelpers.CreateDistributedProvider();
#pragma warning disable CS0618
        var cache = new MultiLevelCache(l1, l2);
#pragma warning restore CS0618
        await cache.SetAsync("demote-key", new TestEntity { Id = 5, Name = "Demote" });

        bool demoted = await cache.DemoteFromL1Async("demote-key");

        demoted.Should().BeTrue();
        (await l1.ExistsAsync("demote-key")).Should().BeFalse();
        (await l2.ExistsAsync("demote-key")).Should().BeTrue();
    }
}
