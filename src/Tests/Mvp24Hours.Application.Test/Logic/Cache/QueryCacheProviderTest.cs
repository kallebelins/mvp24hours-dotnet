using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Logic.Cache;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic.Cache;

[Trait("Category", "Unit")]
public class QueryCacheProviderTest
{
    [Fact]
    public async Task GetAsync_WithEmptyKey_ShouldThrowArgumentException()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);

        Func<Task> act = async () => await provider.GetAsync<string>("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldRoundTripValue()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);

        await provider.SetAsync("key-1", "cached-value");
        string? value = await provider.GetAsync<string>("key-1");

        value.Should().Be("cached-value");
    }

    [Fact]
    public async Task GetOrSetAsync_ShouldExecuteFactoryOnce()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        int factoryCalls = 0;

        string first = await provider.GetOrSetAsync("factory-key", () =>
        {
            factoryCalls++;
            return Task.FromResult("generated");
        });

        string second = await provider.GetOrSetAsync("factory-key", () =>
        {
            factoryCalls++;
            return Task.FromResult("generated-again");
        });

        first.Should().Be("generated");
        second.Should().Be("generated");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrSetAsync_WithL1Cache_ShouldPopulateMemoryLayer()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _, memoryCache);

        await provider.SetAsync("l1-key", 42, new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "Test" });

        (await provider.ExistsAsync("l1-key")).Should().BeTrue();
        (await provider.GetAsync<int>("l1-key")).Should().Be(42);
    }

    [Fact]
    public async Task InvalidateRegionAsync_ShouldRemoveTrackedKeys()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        await provider.SetAsync("region:a", "A", new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "Products" });
        await provider.SetAsync("region:b", "B", new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "Products" });

        await provider.InvalidateRegionAsync("Products");

        (await provider.ExistsAsync("region:a")).Should().BeFalse();
        (await provider.ExistsAsync("region:b")).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateByPatternAsync_WithPrefixWildcard_ShouldRemoveMatchingKeys()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        await provider.SetAsync("Product:1", "one", new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "R" });
        await provider.SetAsync("Product:2", "two", new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "R" });
        await provider.SetAsync("Order:1", "order", new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "R" });

        await provider.InvalidateByPatternAsync("Product:*");

        (await provider.ExistsAsync("Product:1")).Should().BeFalse();
        (await provider.ExistsAsync("Order:1")).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteEntry()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        await provider.SetAsync("remove-me", "value");

        await provider.RemoveAsync("remove-me");

        (await provider.ExistsAsync("remove-me")).Should().BeFalse();
    }
}
