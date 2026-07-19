using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Cache;

[Trait("Category", "Unit")]
public class CacheInvalidatorTest
{
    [Fact]
    public async Task InvalidateEntityAsync_ShouldInvalidateRegionAndPattern()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(provider);
        await provider.SetAsync("AppTestEntity:ListAsync", "data",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "region:AppTestEntity" });

        await invalidator.InvalidateEntityAsync<AppTestEntity>();

        (await provider.ExistsAsync("AppTestEntity:ListAsync")).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateByIdAsync_ShouldRemoveCommonKeyPatterns()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(provider);
        await provider.SetAsync("AppTestEntity:GetById:42", "entity");
        await provider.SetAsync("AppTestEntity:GetByIdAsync:42", "entity");

        await invalidator.InvalidateByIdAsync<AppTestEntity>(42);

        (await provider.ExistsAsync("AppTestEntity:GetById:42")).Should().BeFalse();
        (await provider.ExistsAsync("AppTestEntity:GetByIdAsync:42")).Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateByIdAsync_WithNullId_ShouldNoOp()
    {
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(
            ApplicationTestHelpers.CreateQueryCacheProvider(out _));

        Func<Task> act = async () => await invalidator.InvalidateByIdAsync<AppTestEntity>(null!);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateKeysAsync_ShouldRemoveSpecificKeys()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(provider);
        await provider.SetAsync("k1", "v1");
        await provider.SetAsync("k2", "v2");

        await invalidator.InvalidateKeysAsync(["k1"]);

        (await provider.ExistsAsync("k1")).Should().BeFalse();
        (await provider.ExistsAsync("k2")).Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateByTagsAsync_ShouldInvalidateTagRegions()
    {
        QueryCacheProvider provider = ApplicationTestHelpers.CreateQueryCacheProvider(out _);
        CacheInvalidator invalidator = ApplicationTestHelpers.CreateCacheInvalidator(provider);
        await provider.SetAsync("tagged", "value",
            new QueryCacheEntryOptions { Duration = TimeSpan.FromMinutes(5), Region = "tag:orders" });

        await invalidator.InvalidateByTagsAsync(["orders"]);

        (await provider.ExistsAsync("tagged")).Should().BeFalse();
    }
}
