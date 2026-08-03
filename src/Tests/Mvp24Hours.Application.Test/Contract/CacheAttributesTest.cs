//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Cache;

namespace Mvp24Hours.Application.Test.Contract;

[Trait("Category", "Unit")]
public class CacheAttributesTest
{
    [Fact]
    public void CacheableAttribute_Defaults_ShouldMatchExpectedValues()
    {
        var attribute = new CacheableAttribute();

        attribute.DurationSeconds.Should().Be(300);
        attribute.UseSlidingExpiration.Should().BeFalse();
        attribute.EnableStampedePrevention.Should().BeTrue();
        attribute.Duration.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void CacheableAttribute_GetTags_ShouldSplitAndTrim()
    {
        var attribute = new CacheableAttribute { Tags = " products , catalog , " };

        attribute.GetTags().Should().BeEquivalentTo("products", "catalog");
    }

    [Fact]
    public void CacheableAttribute_GetTags_WhenEmpty_ShouldReturnEmptyArray()
    {
        var attribute = new CacheableAttribute();

        attribute.GetTags().Should().BeEmpty();
    }

    [Fact]
    public void CacheableAttribute_ToOptions_ShouldMapConfiguration()
    {
        var attribute = new CacheableAttribute
        {
            DurationSeconds = 120,
            UseSlidingExpiration = true,
            Region = "Orders",
            Tags = "orders,list",
            EnableStampedePrevention = false
        };

        QueryCacheEntryOptions options = attribute.ToOptions("DefaultRegion");

        options.Duration.Should().Be(TimeSpan.FromSeconds(120));
        options.UseSlidingExpiration.Should().BeTrue();
        options.Region.Should().Be("Orders");
        options.Tags.Should().BeEquivalentTo("orders", "list");
        options.EnableStampedePrevention.Should().BeFalse();
    }

    [Fact]
    public void CacheableAttribute_ToOptions_WithoutRegion_ShouldUseDefaultRegion()
    {
        var attribute = new CacheableAttribute();

        QueryCacheEntryOptions options = attribute.ToOptions("Products");

        options.Region.Should().Be("Products");
    }

    [Fact]
    public void CacheInvalidateAttribute_DefaultTiming_ShouldBeAfterSuccess()
    {
        var attribute = new CacheInvalidateAttribute();

        attribute.Timing.Should().Be(CacheInvalidationTiming.AfterSuccess);
        attribute.InvalidateAll.Should().BeFalse();
    }

    [Fact]
    public void CacheInvalidateAttribute_GetTags_ShouldSplitAndTrim()
    {
        var attribute = new CacheInvalidateAttribute { Tags = " products , catalog " };

        attribute.GetTags().Should().BeEquivalentTo("products", "catalog");
    }

    [Fact]
    public void CacheInvalidateAttribute_GetKeys_ShouldSplitAndTrim()
    {
        var attribute = new CacheInvalidateAttribute { Keys = " product_{id} , user_{userId} " };

        attribute.GetKeys().Should().BeEquivalentTo("product_{id}", "user_{userId}");
    }

    [Fact]
    public void CacheInvalidateAttribute_GetKeys_WhenEmpty_ShouldReturnEmptyArray()
    {
        var attribute = new CacheInvalidateAttribute();

        attribute.GetKeys().Should().BeEmpty();
    }
}
