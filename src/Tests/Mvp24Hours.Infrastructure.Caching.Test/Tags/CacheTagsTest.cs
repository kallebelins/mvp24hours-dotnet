using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Extensions;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Tags;

[Trait("Category", "Unit")]
public class CacheTagManagerAdvancedTest
{
    [Fact]
    public void Constructor_NullCacheProvider_ShouldThrow()
    {
        Action act = () => _ = new CacheTagManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task TagKeyAsync_EmptyKey_ShouldThrow()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tagManager.TagKeyAsync(" ", ["tag"]));
    }

    [Fact]
    public async Task TagKeyAsync_NullTags_ShouldThrow()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            tagManager.TagKeyAsync("key", null!));
    }

    [Fact]
    public async Task TagKeyAsync_EmptyTags_ShouldNoOp()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        Func<Task> act = async () => await tagManager.TagKeyAsync("key", []);

        await act.Should().NotThrowAsync();
        (await tagManager.GetKeysByTagAsync("any")).Should().BeEmpty();
    }

    [Fact]
    public async Task TagKeyAsync_WhitespaceTags_ShouldIgnore()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await tagManager.TagKeyAsync("key", [" ", "valid", ""]);

        IEnumerable<string> keys = await tagManager.GetKeysByTagAsync("valid");
        keys.Should().ContainSingle("key");
        (await tagManager.GetKeysByTagAsync(" ")).Should().BeEmpty();
    }

    [Fact]
    public async Task GetKeysByTagAsync_EmptyTag_ShouldReturnEmpty()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        IEnumerable<string> keys = await tagManager.GetKeysByTagAsync(" ");

        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidateByTagAsync_EmptyTag_ShouldReturnZero()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        int count = await tagManager.InvalidateByTagAsync("");

        count.Should().Be(0);
    }

    [Fact]
    public async Task InvalidateByTagAsync_UnknownTag_ShouldReturnZero()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        int count = await tagManager.InvalidateByTagAsync("unknown");

        count.Should().Be(0);
    }

    [Fact]
    public async Task InvalidateByTagsAsync_NullOrEmpty_ShouldReturnZero()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        (await tagManager.InvalidateByTagsAsync(null!)).Should().Be(0);
        (await tagManager.InvalidateByTagsAsync([])).Should().Be(0);
    }

    [Fact]
    public async Task RemoveTagsAsync_EmptyKey_ShouldThrow()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tagManager.RemoveTagsAsync(" ", ["a"]));
    }

    [Fact]
    public async Task RemoveAllTagsAsync_EmptyKey_ShouldThrow()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tagManager.RemoveAllTagsAsync(" "));
    }

    [Fact]
    public async Task TagKeyAsync_DuplicateTag_ShouldKeepSingleAssociation()
    {
        var tagManager = new CacheTagManager(CacheTestHelpers.CreateMemoryProvider());

        await tagManager.TagKeyAsync("item", ["group"]);
        await tagManager.TagKeyAsync("item", ["group"]);

        IEnumerable<string> keys = await tagManager.GetKeysByTagAsync("group");
        keys.Should().ContainSingle("item");
    }
}

[Trait("Category", "Unit")]
public class TagBasedInvalidationExtensionsTest
{
    [Fact]
    public async Task SetWithInvalidationAsync_WithTags_ShouldAllowTagInvalidation()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        var options = new CacheEntryOptions { Tags = ["products"] };

        await cache.SetWithInvalidationAsync(
            "product:1",
            new TestEntity { Id = 1, Name = "P1" },
            options,
            tagManager);

        int invalidated = await tagManager.InvalidateByTagAsync("products");

        invalidated.Should().Be(1);
        (await cache.ExistsAsync("product:1")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveWithCleanupAsync_ShouldRemoveTagsAndPublishEvent()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        var publisher = new InMemoryCacheInvalidationEventPublisher();
        await cache.SetAsync("key", new TestEntity { Id = 1, Name = "X" });
        await tagManager.TagKeyAsync("key", ["t1"]);

        await cache.RemoveWithCleanupAsync("key", tagManager, eventPublisher: publisher);

        (await cache.ExistsAsync("key")).Should().BeFalse();
        (await tagManager.GetKeysByTagAsync("t1")).Should().BeEmpty();
        publisher.GetPublishedEvents().Should().ContainSingle(e => e.Key == "key");
    }

    [Fact]
    public async Task GetOrSetAsync_WithStampedeAndTags_ShouldTagComputedValue()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var tagManager = new CacheTagManager(cache);
        var stampede = new CacheStampedePrevention();
        var options = new CacheEntryOptions { Tags = ["computed"] };
        int factoryCalls = 0;

        TestEntity result = await CacheInvalidationExtensions.GetOrSetAsync(
            cache,
            "computed-key",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new TestEntity { Id = 5, Name = "Computed" });
            },
            options,
            stampede,
            tagManager);

        result.Name.Should().Be("Computed");
        factoryCalls.Should().Be(1);
        (await tagManager.GetKeysByTagAsync("computed")).Should().Contain("computed-key");
    }
}

[Trait("Category", "Unit")]
public class InMemoryHybridCacheTagManagerAdvancedTest
{
    [Fact]
    public async Task TrackAndInvalidate_ShouldRemoveTrackedKeys()
    {
        var manager = new InMemoryHybridCacheTagManager();
        await manager.TrackKeyWithTagsAsync("k1", ["tag-a"]);
        await manager.TrackKeyWithTagsAsync("k2", ["tag-a"]);

        IEnumerable<string> keys = await manager.GetKeysByTagAsync("tag-a");
        keys.Should().BeEquivalentTo(["k1", "k2"]);

        await manager.InvalidateTagAsync("tag-a");

        (await manager.GetKeysByTagAsync("tag-a")).Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatistics_ShouldReportTrackedTags()
    {
        var manager = new InMemoryHybridCacheTagManager();
        await manager.TrackKeyWithTagsAsync("item", ["alpha", "beta"]);

        HybridCacheTagStatistics stats = manager.GetStatistics();

        stats.TotalTags.Should().BeGreaterThanOrEqualTo(2);
        stats.TotalAssociations.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task TrackKeyWithTagsAsync_EmptyKey_ShouldThrow()
    {
        var manager = new InMemoryHybridCacheTagManager();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.TrackKeyWithTagsAsync(" ", ["t"]));
    }

    [Fact]
    public async Task RemoveKeyFromTagsAsync_ShouldRemoveAssociations()
    {
        var manager = new InMemoryHybridCacheTagManager();
        await manager.TrackKeyWithTagsAsync("key", ["tag"]);

        await manager.RemoveKeyFromTagsAsync("key");

        (await manager.GetKeysByTagAsync("tag")).Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllTracking()
    {
        var manager = new InMemoryHybridCacheTagManager();
        await manager.TrackKeyWithTagsAsync("key", ["tag"]);

        await manager.ClearAsync();

        manager.GetStatistics().TotalTags.Should().Be(0);
        (await manager.GetKeysByTagAsync("tag")).Should().BeEmpty();
    }
}
