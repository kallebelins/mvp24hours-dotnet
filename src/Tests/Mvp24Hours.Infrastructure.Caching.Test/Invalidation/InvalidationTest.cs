using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using Mvp24Hours.Infrastructure.Caching.Test.Support;
using InvalidationEvent = Mvp24Hours.Infrastructure.Caching.Invalidation.CacheInvalidationEvent;
using InvalidationEventType = Mvp24Hours.Infrastructure.Caching.Invalidation.CacheInvalidationEventType;

namespace Mvp24Hours.Infrastructure.Caching.Test.Invalidation;

[Trait("Category", "Unit")]
public class CacheDependencyManagerTest
{
    [Fact]
    public async Task RegisterAndInvalidateDependents_ShouldRemoveDependentKeys()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var manager = new CacheDependencyManager(cache);
        await cache.SetAsync("dependent", new TestEntity { Id = 1, Name = "Dependent" });
        await manager.RegisterDependenciesAsync("dependent", ["parent"]);

        int invalidated = await manager.InvalidateDependentsAsync("parent");

        invalidated.Should().Be(1);
        (await cache.ExistsAsync("dependent")).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterDependenciesAsync_EmptyDependencies_ShouldNoOp()
    {
        var manager = new CacheDependencyManager(CacheTestHelpers.CreateMemoryProvider());

        Func<Task> act = async () => await manager.RegisterDependenciesAsync("key", []);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateDependentsAsync_EmptyKey_ShouldReturnZero()
    {
        var manager = new CacheDependencyManager(CacheTestHelpers.CreateMemoryProvider());

        int count = await manager.InvalidateDependentsAsync(" ");

        count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAllDependenciesAsync_ShouldCompleteWithoutThrowing()
    {
        var manager = new CacheDependencyManager(CacheTestHelpers.CreateMemoryProvider());

        Func<Task> act = async () => await manager.RemoveAllDependenciesAsync("key");

        await act.Should().NotThrowAsync();
    }
}

[Trait("Category", "Unit")]
public class CacheTagManagerTest
{
    [Fact]
    public async Task TagKeyAndInvalidateByTag_ShouldRemoveTaggedKeys()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        await cache.SetAsync("product:1", new TestEntity { Id = 1, Name = "P1" });
        await cache.SetAsync("product:2", new TestEntity { Id = 2, Name = "P2" });
        await tagManager.TagKeyAsync("product:1", ["products"]);
        await tagManager.TagKeyAsync("product:2", ["products"]);

        int invalidated = await tagManager.InvalidateByTagAsync("products");

        invalidated.Should().Be(2);
        (await cache.ExistsAsync("product:1")).Should().BeFalse();
        (await cache.ExistsAsync("product:2")).Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysByTagAsync_ShouldReturnTrackedKeys()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        await tagManager.TagKeyAsync("item:a", ["group-a"]);
        await tagManager.TagKeyAsync("item:b", ["group-a"]);

        IEnumerable<string> keys = await tagManager.GetKeysByTagAsync("group-a");

        keys.Should().BeEquivalentTo(["item:a", "item:b"]);
    }

    [Fact]
    public async Task InvalidateByTagsAsync_ShouldDeduplicateKeys()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        await cache.SetAsync("shared", new TestEntity { Id = 1, Name = "Shared" });
        await tagManager.TagKeyAsync("shared", ["tag1", "tag2"]);

        int invalidated = await tagManager.InvalidateByTagsAsync(["tag1", "tag2"]);

        invalidated.Should().Be(1);
    }

    [Fact]
    public async Task RemoveTagsAsync_ShouldRemoveSpecificTags()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        await tagManager.TagKeyAsync("key", ["a", "b"]);

        await tagManager.RemoveTagsAsync("key", ["a"]);
        IEnumerable<string> remaining = await tagManager.GetKeysByTagAsync("a");

        remaining.Should().BeEmpty();
        IEnumerable<string> tagBKeys = await tagManager.GetKeysByTagAsync("b");
        tagBKeys.Should().Contain("key");
    }

    [Fact]
    public async Task RemoveAllTagsAsync_ShouldClearAllAssociations()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        await tagManager.TagKeyAsync("key", ["x", "y"]);

        await tagManager.RemoveAllTagsAsync("key");

        (await tagManager.GetKeysByTagAsync("x")).Should().BeEmpty();
        (await tagManager.GetKeysByTagAsync("y")).Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class CacheStampedePreventionTest
{
    [Fact]
    public async Task ExecuteAsync_ConcurrentCalls_ShouldReturnConsistentResults()
    {
        var prevention = new CacheStampedePrevention();
        int factoryCalls = 0;

        IEnumerable<Task<int>> tasks = Enumerable.Range(0, 10).Select(_ => prevention.ExecuteAsync(
            "stampede-key",
            _ =>
            {
                Interlocked.Increment(ref factoryCalls);
                return Task.FromResult(42);
            }));

        int[] results = await Task.WhenAll(tasks);

        results.Should().AllBeEquivalentTo(42);
        factoryCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_FactoryThrows_ShouldPropagateException()
    {
        var prevention = new CacheStampedePrevention();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prevention.ExecuteAsync<int>(
                "error-key",
                _ => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task ExecuteAsync_NullFactory_ShouldThrow()
    {
        var prevention = new CacheStampedePrevention();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            prevention.ExecuteAsync<int>("key", null!));
    }
}

[Trait("Category", "Unit")]
public class InMemoryCacheInvalidationEventPublisherTest
{
    [Fact]
    public async Task PublishKeyInvalidationAsync_ShouldRecordEvent()
    {
        var publisher = new InMemoryCacheInvalidationEventPublisher();

        await publisher.PublishKeyInvalidationAsync("key-1");
        InvalidationEvent[] events = publisher.GetPublishedEvents();

        events.Should().ContainSingle(e => e.Type == InvalidationEventType.Key && e.Key == "key-1");
    }

    [Fact]
    public async Task PublishTagInvalidationAsync_ShouldRecordEvent()
    {
        var publisher = new InMemoryCacheInvalidationEventPublisher();

        await publisher.PublishTagInvalidationAsync("products");
        InvalidationEvent[] events = publisher.GetPublishedEvents();

        events.Should().ContainSingle(e => e.Type == InvalidationEventType.Tag && e.Tag == "products");
    }

    [Fact]
    public async Task PublishTagsInvalidationAsync_ShouldRecordPerTag()
    {
        var publisher = new InMemoryCacheInvalidationEventPublisher();

        await publisher.PublishTagsInvalidationAsync(["a", "b"]);
        InvalidationEvent[] events = publisher.GetPublishedEvents();

        events.Should().HaveCount(2);
        events.Should().OnlyContain(e => e.Type == InvalidationEventType.Tags);
    }

    [Fact]
    public async Task PublishKeyInvalidationAsync_EmptyKey_ShouldThrow()
    {
        var publisher = new InMemoryCacheInvalidationEventPublisher();

        await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishKeyInvalidationAsync(" "));
    }

    [Fact]
    public async Task ClearEvents_ShouldRemoveAllEvents()
    {
        var publisher = new InMemoryCacheInvalidationEventPublisher();
        await publisher.PublishKeyInvalidationAsync("k");

        publisher.ClearEvents();

        publisher.GetPublishedEvents().Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
public class RedisCacheInvalidationEventPublisherTest
{
    [Fact]
    public async Task PublishKeyInvalidationAsync_WhenConnected_ShouldPublishToRedis()
    {
        var connection = new FakeRedisConnection();
        var publisher = new RedisCacheInvalidationEventPublisher(connection);

        await publisher.PublishKeyInvalidationAsync("redis-key");

        connection.GetDatabase().PublishedMessages.Should().ContainSingle(m =>
            m.Channel == "mvp24hours:cache:invalidate:key" &&
            m.Message.Contains("redis-key"));
    }

    [Fact]
    public async Task PublishKeyInvalidationAsync_WhenDisconnected_ShouldNoOp()
    {
        var connection = new FakeRedisConnection { IsConnected = false };
        var publisher = new RedisCacheInvalidationEventPublisher(connection);

        await publisher.PublishKeyInvalidationAsync("redis-key");

        connection.GetDatabase().PublishedMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishTagsInvalidationAsync_EmptyTags_ShouldNoOp()
    {
        var connection = new FakeRedisConnection();
        var publisher = new RedisCacheInvalidationEventPublisher(connection);

        await publisher.PublishTagsInvalidationAsync([]);

        connection.GetDatabase().PublishedMessages.Should().BeEmpty();
    }
}
