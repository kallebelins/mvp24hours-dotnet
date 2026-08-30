using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbBsonExtensionsUnitTest
{
    [Fact]
    public void ApplyConfigurationsFromAssembly_ShouldInstantiateAndConfigureEachClassMap()
    {
        RecordingBsonClassMap.ConfigureCallCount = 0;
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Mvp24HoursContext result = context.ApplyConfigurationsFromAssembly(typeof(MongoDbBsonExtensionsUnitTest).Assembly);

        result.Should().BeSameAs(context);
        RecordingBsonClassMap.ConfigureCallCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WithAssemblyContainingNoClassMaps_ShouldReturnContextUnchanged()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        // System.Private.CoreLib has no IBsonClassMap implementations, so the reflection
        // scan should simply find zero types and return the context as-is without throwing.
        Mvp24HoursContext result = context.ApplyConfigurationsFromAssembly(typeof(object).Assembly);

        result.Should().BeSameAs(context);
    }
}

/// <summary>
/// Public, parameterless-constructible <see cref="IBsonClassMap"/> implementation used to verify
/// that <see cref="MongoDbBsonExtensions.ApplyConfigurationsFromAssembly"/> discovers, activates,
/// and invokes <see cref="Configure"/> on every exported type implementing the interface.
/// </summary>
public sealed class RecordingBsonClassMap : IBsonClassMap
{
    public static int ConfigureCallCount;

    public void Configure()
    {
        ConfigureCallCount++;
    }
}
