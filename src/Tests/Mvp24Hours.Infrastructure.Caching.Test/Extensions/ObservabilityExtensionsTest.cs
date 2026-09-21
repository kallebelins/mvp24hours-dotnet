using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Extensions;
using Mvp24Hours.Infrastructure.Caching.Observability;

namespace Mvp24Hours.Infrastructure.Caching.Test.Extensions;

[Trait("Category", "Unit")]
public class ObservabilityExtensionsTest
{
    [Fact]
    public void AddCacheMetrics_ShouldRegisterCacheMetrics()
    {
        var services = new ServiceCollection();

        services.AddCacheMetrics();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheMetrics>().Should().BeOfType<CacheMetrics>();
    }

    [Fact]
    public void AddObservableCacheProvider_WithoutRegisteredProvider_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddObservableCacheProvider();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddObservableCacheProvider_ShouldDecorateExistingProvider()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();

        services.AddObservableCacheProvider();
        ServiceProvider provider = services.BuildServiceProvider();

        ICacheProvider cache = provider.GetRequiredService<ICacheProvider>();
        cache.Should().BeOfType<ObservableCacheProvider>();
        provider.GetRequiredService<ICacheMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddObservableCacheProvider_WithMetricsDisabled_ShouldNotRegisterMetricsAutomatically()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();

        services.AddObservableCacheProvider(options => options.EnableMetrics = false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<ObservableCacheProvider>();
        provider.GetService<ICacheMetrics>().Should().BeNull();
    }

    [Fact]
    public void AddCacheHealthCheck_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();
        services.AddHealthChecks().AddCacheHealthCheck();
        ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckServiceOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Should().ContainSingle(r => r.Name == "cache");
    }

    [Fact]
    public void AddCacheHealthCheck_WithCustomName_ShouldUseProvidedName()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();
        services.AddHealthChecks().AddCacheHealthCheck(name: "custom-cache");
        ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckServiceOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Should().ContainSingle(r => r.Name == "custom-cache");
    }

    [Fact]
    public void AddCacheObservability_ShouldRegisterMetrics()
    {
        var services = new ServiceCollection();

        services.AddCacheObservability();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheMetrics>().Should().BeOfType<CacheMetrics>();
    }
}
