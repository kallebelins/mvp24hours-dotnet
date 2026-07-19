#pragma warning disable CS0618
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Resilience;

[Trait("Category", "Unit")]
public class MvpExecutionStrategyTest
{
    [Fact]
    public void MvpExecutionStrategy_Type_IsMarkedObsolete()
    {
        var obsoleteAttribute = typeof(MvpExecutionStrategy)
            .GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false)
            .Cast<ObsoleteAttribute>()
            .SingleOrDefault();

        obsoleteAttribute.Should().NotBeNull();
        obsoleteAttribute!.Message.Should().Contain("NativeDbResilienceExtensions");
    }

    [Fact]
    public void EFCoreResilienceOptions_NoResilience_DisablesLegacyStrategyFeatures()
    {
        EFCoreResilienceOptions options = EFCoreResilienceOptions.NoResilience();

        options.EnableRetryOnFailure.Should().BeFalse();
        options.EnableCircuitBreaker.Should().BeFalse();
        options.EnableDbContextPooling.Should().BeFalse();
        options.LogRetryAttempts.Should().BeFalse();
    }
}
