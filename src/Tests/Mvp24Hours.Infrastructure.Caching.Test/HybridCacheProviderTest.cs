using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.HybridCache;

namespace Mvp24Hours.Infrastructure.Caching.Test;

[Trait("Category", "Unit")]
public class HybridCacheProviderTest
{
    private static HybridCacheProvider CreateProvider(
        Action<MvpHybridCacheOptions>? configure = null,
        IHybridCacheTagManager? tagManager = null)
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();
#pragma warning disable EXTEXP0018
        services.AddMvpHybridCache(options =>
        {
            options.EnableDetailedLogging = true;
            options.KeyPrefix = "test:";
            options.DefaultTags = ["global"];
            configure?.Invoke(options);
        });
#pragma warning restore EXTEXP0018

        if (tagManager is not null)
        {
            services.AddSingleton(tagManager);
        }

        ServiceProvider provider = services.BuildServiceProvider();
        return (HybridCacheProvider)provider.GetRequiredService<ICacheProvider>();
    }

    private sealed class CacheItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyNotCached()
    {
        HybridCacheProvider provider = CreateProvider();

        CacheItem? result = await provider.GetAsync<CacheItem>($"missing-{Guid.NewGuid():N}");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_ShouldRoundTripValue()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"item-{Guid.NewGuid():N}";
        var item = new CacheItem { Id = 1, Name = "Alpha" };

        await provider.SetAsync(key, item);
        CacheItem? cached = await provider.GetAsync<CacheItem>(key);

        cached.Should().NotBeNull();
        cached!.Id.Should().Be(1);
        cached.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task SetStringAsync_And_GetStringAsync_ShouldRoundTrip()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"str-{Guid.NewGuid():N}";

        await provider.SetStringAsync(key, "hello");
        string? value = await provider.GetStringAsync(key);

        value.Should().Be("hello");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveCachedEntry()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"remove-{Guid.NewGuid():N}";
        await provider.SetAsync(key, new CacheItem { Id = 2, Name = "RemoveMe" });

        await provider.RemoveAsync(key);

        (await provider.GetAsync<CacheItem>(key)).Should().BeNull();
    }

    [Fact]
    public async Task RemoveManyAsync_ShouldRemoveAllKeys()
    {
        HybridCacheProvider provider = CreateProvider();
        string key1 = $"many-1-{Guid.NewGuid():N}";
        string key2 = $"many-2-{Guid.NewGuid():N}";
        await provider.SetAsync(key1, new CacheItem { Id = 1, Name = "One" });
        await provider.SetAsync(key2, new CacheItem { Id = 2, Name = "Two" });

        await provider.RemoveManyAsync([key1, key2]);

        (await provider.ExistsAsync(key1)).Should().BeFalse();
        (await provider.ExistsAsync(key2)).Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"exists-{Guid.NewGuid():N}";
        await provider.SetAsync<object>(key, new CacheItem { Id = 3, Name = "Exists" });

        (await provider.ExistsAsync(key)).Should().BeTrue();
    }

    [Fact]
    public async Task GetManyAsync_ShouldReturnOnlyExistingKeys()
    {
        HybridCacheProvider provider = CreateProvider();
        string key1 = $"gm-1-{Guid.NewGuid():N}";
        string key2 = $"gm-2-{Guid.NewGuid():N}";
        await provider.SetAsync(key1, new CacheItem { Id = 1, Name = "One" });

        Dictionary<string, CacheItem> result = await provider.GetManyAsync<CacheItem>([key1, key2]);

        result.Should().ContainKey(key1);
        result.Should().NotContainKey(key2);
    }

    [Fact]
    public async Task SetManyAsync_ShouldStoreAllEntries()
    {
        HybridCacheProvider provider = CreateProvider();
        string key1 = $"sm-1-{Guid.NewGuid():N}";
        string key2 = $"sm-2-{Guid.NewGuid():N}";
        var values = new Dictionary<string, CacheItem>
        {
            [key1] = new CacheItem { Id = 1, Name = "One" },
            [key2] = new CacheItem { Id = 2, Name = "Two" }
        };

        await provider.SetManyAsync(values);

        (await provider.GetAsync<CacheItem>(key1))!.Name.Should().Be("One");
        (await provider.GetAsync<CacheItem>(key2))!.Name.Should().Be("Two");
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldInvokeFactoryOnMiss()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"goc-{Guid.NewGuid():N}";
        int factoryCalls = 0;

        CacheItem? result = await provider.GetOrCreateAsync(
            key,
            _ =>
            {
                factoryCalls++;
                return ValueTask.FromResult(new CacheItem { Id = 7, Name = "Created" });
            });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Created");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateByTagAsync_ShouldRemoveTaggedEntries()
    {
        var tagManager = new InMemoryHybridCacheTagManager();
        HybridCacheProvider provider = CreateProvider(tagManager: tagManager);
        string key = $"tag-{Guid.NewGuid():N}";
        await provider.SetAsync(
            key,
            new CacheItem { Id = 8, Name = "Tagged" },
            new CacheEntryOptions { Tags = ["products"] });

        await provider.InvalidateByTagAsync("products");

        (await provider.GetAsync<CacheItem>(key)).Should().BeNull();
    }

    [Fact]
    public async Task InvalidateByTagsAsync_ShouldInvalidateEachTag()
    {
        HybridCacheProvider provider = CreateProvider();
        string key1 = $"tags-1-{Guid.NewGuid():N}";
        string key2 = $"tags-2-{Guid.NewGuid():N}";
        await provider.SetAsync(key1, new CacheItem { Id = 1, Name = "A" }, new CacheEntryOptions { Tags = ["t1"] });
        await provider.SetAsync(key2, new CacheItem { Id = 2, Name = "B" }, new CacheEntryOptions { Tags = ["t2"] });

        await provider.InvalidateByTagsAsync(["t1", "t2"]);

        (await provider.ExistsAsync(key1)).Should().BeFalse();
        (await provider.ExistsAsync(key2)).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_ShouldCompleteWithoutError()
    {
        HybridCacheProvider provider = CreateProvider();

        await provider.Invoking(p => p.RefreshAsync("any-key")).Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_ShouldThrow_WhenKeyIsEmpty()
    {
        HybridCacheProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetAsync<CacheItem>(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_ShouldThrow_WhenValueIsNull()
    {
        HybridCacheProvider provider = CreateProvider();

        Func<Task> act = () => provider.SetAsync<CacheItem>("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InvalidateByTagAsync_ShouldThrow_WhenTagIsEmpty()
    {
        HybridCacheProvider provider = CreateProvider();

        Func<Task> act = () => provider.InvalidateByTagAsync("  ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldThrow_WhenFactoryIsNull()
    {
        HybridCacheProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetOrCreateAsync<CacheItem>("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RemoveManyAsync_WithEmptyArray_ShouldNoOp()
    {
        HybridCacheProvider provider = CreateProvider();

        await provider.Invoking(p => p.RemoveManyAsync([])).Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_WithExpirationOptions_ShouldStoreValue()
    {
        HybridCacheProvider provider = CreateProvider();
        string key = $"exp-{Guid.NewGuid():N}";
        var options = CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(10));

        await provider.SetAsync(key, new CacheItem { Id = 9, Name = "Expiring" }, options);

        (await provider.GetAsync<CacheItem>(key))!.Name.Should().Be("Expiring");
    }
}
