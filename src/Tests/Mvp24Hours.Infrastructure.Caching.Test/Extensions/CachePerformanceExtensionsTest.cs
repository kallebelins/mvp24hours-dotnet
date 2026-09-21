using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Extensions;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using Mvp24Hours.Infrastructure.Caching.Warming;

namespace Mvp24Hours.Infrastructure.Caching.Test.Extensions;

[Trait("Category", "Unit")]
public class CachePerformanceExtensionsTest
{
    [Fact]
    public void AddCacheCompression_NullServices_ShouldThrow()
    {
        Action act = () => CachePerformanceExtensions.AddCacheCompression(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCacheCompression_ShouldRegisterCompressorAndWrapSerializer()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
        services.AddCacheCompression();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheCompressor>().Should().NotBeNull();
        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<CompressedCacheSerializer>();
    }

    [Fact]
    public void AddCacheCompression_WithoutExistingSerializer_ShouldFallBackToJson()
    {
        var services = new ServiceCollection();
        services.AddCacheCompression();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<CompressedCacheSerializer>();
    }

    [Fact]
    public void AddCachePrefetching_NullServices_ShouldThrow()
    {
        Action act = () => CachePerformanceExtensions.AddCachePrefetching(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCachePrefetching_ShouldRegisterPrefetcher()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();
        services.AddCachePrefetching();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICachePrefetcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddCacheWarming_NullServices_ShouldThrow()
    {
        Action act = () => CachePerformanceExtensions.AddCacheWarming(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCacheWarming_WithAutoWarmupEnabled_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddCacheWarming(enableAutoWarmup: true);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheWarmer>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().ContainSingle(s => s is CacheWarmupHostedService);
    }

    [Fact]
    public void AddCacheWarming_WithAutoWarmupDisabled_ShouldNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddCacheWarming(enableAutoWarmup: false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheWarmer>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().NotContain(s => s is CacheWarmupHostedService);
    }

    [Fact]
    public void AddCacheWarmupOperation_NullServices_ShouldThrow()
    {
        Action act = () => CachePerformanceExtensions.AddCacheWarmupOperation<NoOpWarmupOperation>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCacheWarmupOperation_ShouldRegisterOperation()
    {
        var services = new ServiceCollection();
        services.AddCacheWarmupOperation<NoOpWarmupOperation>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<ICacheWarmupOperation>().Should().ContainSingle(op => op is NoOpWarmupOperation);
    }

    [Fact]
    public void ConfigureCacheCompression_NullServices_ShouldThrow()
    {
        Action act = () => CachePerformanceExtensions.ConfigureCacheCompression(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConfigureCacheCompression_ShouldEnableCompressionOptionsAndRegisterCompressor()
    {
        var services = new ServiceCollection();
        services.ConfigureCacheCompression(compressionThresholdBytes: 2048);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheCompressor>().Should().NotBeNull();
        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<CompressedCacheSerializer>();
    }

    private sealed class NoOpWarmupOperation : ICacheWarmupOperation
    {
        public string Name => "NoOp";

        public int Priority => 0;

        public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
