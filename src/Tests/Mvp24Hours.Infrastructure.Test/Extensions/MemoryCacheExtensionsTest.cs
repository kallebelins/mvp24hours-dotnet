//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections;
using Microsoft.Extensions.Caching.Memory;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Extensions;

[Trait("Category", "Unit")]
public class MemoryCacheExtensionsTest
{
    [Fact]
    public void GetKeys_ShouldReturnAllCacheKeys()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("alpha", 1);
        cache.Set("beta", 2);
        cache.Set("gamma", 3);

        IEnumerable keys = cache.GetKeys();

        keys.Cast<object>().Should().BeEquivalentTo(["alpha", "beta", "gamma"]);
    }

    [Fact]
    public void GetKeys_WithGenericFilter_ShouldReturnMatchingKeys()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("text-key", "value");
        cache.Set(42, "numeric-key");
        cache.Set("another-text", "value");

        IEnumerable<string> stringKeys = cache.GetKeys<string>();

        stringKeys.Should().BeEquivalentTo(["text-key", "another-text"]);
    }

    [Fact]
    public void GetKeys_WithEmptyCache_ShouldReturnEmptyCollection()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        cache.GetKeys().Cast<object>().Should().BeEmpty();
    }
}
