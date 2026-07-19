using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Prefetching;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Prefetching;

[Trait("Category", "Unit")]
public class CachePrefetcherTest
{
    [Fact]
    public async Task PrefetchAsync_WhenAlreadyCached_ShouldSkipFactory()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        await cache.SetAsync("cached", new TestEntity { Id = 1, Name = "Cached" });
        var prefetcher = new CachePrefetcher(cache);
        int factoryCalls = 0;

        await prefetcher.PrefetchAsync(
            "cached",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new TestEntity { Id = 2, Name = "New" });
            });

        factoryCalls.Should().Be(0);
    }

    [Fact]
    public async Task PrefetchAsync_WhenMissing_ShouldLoadAndCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var prefetcher = new CachePrefetcher(cache);

        await prefetcher.PrefetchAsync(
            "missing",
            _ => Task.FromResult(new TestEntity { Id = 3, Name = "Prefetched" }));

        TestEntity? result = await cache.GetAsync<TestEntity>("missing");
        result!.Name.Should().Be("Prefetched");
    }

    [Fact]
    public async Task PrefetchAsync_FactoryReturnsNull_ShouldNotCache()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var prefetcher = new CachePrefetcher(cache);

        await prefetcher.PrefetchAsync<TestEntity>(
            "null-prefetch",
            _ => Task.FromResult<TestEntity>(null!));

        (await cache.ExistsAsync("null-prefetch")).Should().BeFalse();
    }

    [Fact]
    public async Task PrefetchAsync_FactoryThrows_ShouldNotThrow()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var prefetcher = new CachePrefetcher(cache);

        Func<Task> act = async () => await prefetcher.PrefetchAsync<TestEntity>(
            "error",
            _ => throw new InvalidOperationException("prefetch failed"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PrefetchManyAsync_ShouldPrefetchAllRequests()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var prefetcher = new CachePrefetcher(cache);
        var requests = new[]
        {
            new PrefetchRequest<TestEntity>
            {
                Key = "p1",
                ValueFactory = _ => Task.FromResult(new TestEntity { Id = 1, Name = "One" })
            },
            new PrefetchRequest<TestEntity>
            {
                Key = "p2",
                ValueFactory = _ => Task.FromResult(new TestEntity { Id = 2, Name = "Two" })
            }
        };

        await prefetcher.PrefetchManyAsync(requests);

        (await cache.ExistsAsync("p1")).Should().BeTrue();
        (await cache.ExistsAsync("p2")).Should().BeTrue();
    }

    [Fact]
    public async Task PrefetchManyAsync_InvalidConcurrency_ShouldThrow()
    {
        var prefetcher = new CachePrefetcher(CacheTestHelpers.CreateMemoryProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            prefetcher.PrefetchManyAsync(Array.Empty<PrefetchRequest<TestEntity>>(), maxConcurrency: 0));
    }
}
