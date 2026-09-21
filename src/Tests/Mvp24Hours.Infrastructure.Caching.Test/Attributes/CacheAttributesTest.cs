using System.Reflection;
using Mvp24Hours.Infrastructure.Caching.Attributes;

namespace Mvp24Hours.Infrastructure.Caching.Test.Attributes;

[Trait("Category", "Unit")]
public class CacheableAttributeTest
{
    [Fact]
    public void Defaults_ShouldMatchDocumentedValues()
    {
        var attribute = new CacheableAttribute();

        attribute.DurationSeconds.Should().Be(300);
        attribute.Region.Should().BeNull();
        attribute.KeyTemplate.Should().BeNull();
        attribute.Tags.Should().BeNull();
        attribute.UseSlidingExpiration.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var attribute = new CacheableAttribute
        {
            DurationSeconds = 60,
            Region = "Customers",
            KeyTemplate = "customer_{id}",
            Tags = "customers,catalog",
            UseSlidingExpiration = true
        };

        attribute.DurationSeconds.Should().Be(60);
        attribute.Region.Should().Be("Customers");
        attribute.KeyTemplate.Should().Be("customer_{id}");
        attribute.Tags.Should().Be("customers,catalog");
        attribute.UseSlidingExpiration.Should().BeTrue();
    }

    [Fact]
    public void AppliedToMethod_ShouldBeDiscoverableViaReflection()
    {
        MethodInfo method = typeof(SampleRepositoryForAttributes).GetMethod(nameof(SampleRepositoryForAttributes.GetById))!;

        CacheableAttribute? attribute = method.GetCustomAttribute<CacheableAttribute>();

        attribute.Should().NotBeNull();
        attribute!.DurationSeconds.Should().Be(120);
        attribute.Region.Should().Be("Sample");
    }

    [Fact]
    public void AttributeUsage_ShouldTargetMethodOnlyAndNotAllowMultiple()
    {
        AttributeUsageAttribute? usage = typeof(CacheableAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Method);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }
}

[Trait("Category", "Unit")]
public class CacheInvalidateAttributeTest
{
    [Fact]
    public void Defaults_ShouldMatchDocumentedValues()
    {
        var attribute = new CacheInvalidateAttribute();

        attribute.Region.Should().BeNull();
        attribute.KeyPattern.Should().BeNull();
        attribute.Tags.Should().BeNull();
        attribute.InvalidateEntityType.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var attribute = new CacheInvalidateAttribute
        {
            Region = "Products",
            KeyPattern = "product_{id}*",
            Tags = "products,catalog",
            InvalidateEntityType = false
        };

        attribute.Region.Should().Be("Products");
        attribute.KeyPattern.Should().Be("product_{id}*");
        attribute.Tags.Should().Be("products,catalog");
        attribute.InvalidateEntityType.Should().BeFalse();
    }

    [Fact]
    public void AppliedToMethod_ShouldBeDiscoverableViaReflection()
    {
        MethodInfo method = typeof(SampleRepositoryForAttributes).GetMethod(nameof(SampleRepositoryForAttributes.Modify))!;

        CacheInvalidateAttribute? attribute = method.GetCustomAttribute<CacheInvalidateAttribute>();

        attribute.Should().NotBeNull();
        attribute!.Region.Should().Be("Sample");
    }

    [Fact]
    public void AttributeUsage_ShouldTargetMethodOnlyAndAllowMultiple()
    {
        AttributeUsageAttribute? usage = typeof(CacheInvalidateAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
        usage.Inherited.Should().BeTrue();
    }
}

internal sealed class SampleRepositoryForAttributes
{
    [Cacheable(DurationSeconds = 120, Region = "Sample")]
    public string? GetById(int id) => id.ToString();

    [CacheInvalidate(Region = "Sample")]
    public void Modify(int id)
    {
    }
}
