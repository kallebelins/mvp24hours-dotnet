//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Application.Extensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class CacheServiceCollectionExtensionsTest
{
    private sealed class CustomKeyGenerator : IQueryCacheKeyGenerator
    {
        public string GenerateKey<TQuery>(TQuery query) where TQuery : ICacheableQuery => "custom-key";
        public string GenerateKey(MethodInfo method, object?[] parameters, Type entityType) => "custom-key";
        public string GenerateKeyFromTemplate(string template, IDictionary<string, object?> parameters) => "custom-key";
        public string GenerateRegionKey<TEntity>() => "custom-region";
        public string GenerateRegionKey(Type entityType) => "custom-region";
        public string GenerateInvalidationPattern(Type entityType, string? operation = null) => "custom-pattern";
    }

    private sealed class CustomCacheProvider : IQueryCacheProvider
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, QueryCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => factory();
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class CustomCacheInvalidator : ICacheInvalidator
    {
        public Task InvalidateEntityAsync<TEntity>(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateEntityAsync(Type entityType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateKeysAsync(string[] keys, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void AddMvpApplicationQueryCache_Parameterless_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddLogging();

        services.AddMvpApplicationQueryCache();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IQueryCacheKeyGenerator>().Should().NotBeNull();
        provider.GetRequiredService<IQueryCacheProvider>().Should().NotBeNull();
        provider.GetRequiredService<ICacheInvalidator>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpApplicationQueryCache_WithConfigureOptions_ShouldApplyOptions()
    {
        var services = new ServiceCollection();

        services.AddMvpApplicationQueryCache(options =>
        {
            options.KeyPrefix = "test:";
        });
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value.KeyPrefix.Should().Be("test:");
    }

    [Fact]
    public void AddMvpApplicationQueryCache_WithNullServices_ShouldThrow()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddMvpApplicationQueryCache(_ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddMvpApplicationQueryCache_WithNullConfigureOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvpApplicationQueryCache((Action<QueryCacheOptions>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configureOptions");
    }

    [Fact]
    public void AddMvpApplicationQueryCacheHybrid_ShouldEnableL1CacheAndRegisterMemoryCache()
    {
        var services = new ServiceCollection();

        services.AddMvpApplicationQueryCacheHybrid();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value.EnableL1Cache.Should().BeTrue();
        provider.GetRequiredService<IMemoryCache>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpApplicationQueryCacheHybrid_WithAdditionalConfigure_ShouldApplyBothConfigurations()
    {
        var services = new ServiceCollection();

        services.AddMvpApplicationQueryCacheHybrid(options => options.KeyPrefix = "hybrid:");
        ServiceProvider provider = services.BuildServiceProvider();

        QueryCacheOptions opts = provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value;
        opts.EnableL1Cache.Should().BeTrue();
        opts.KeyPrefix.Should().Be("hybrid:");
    }

    [Fact]
    public void AddMvpApplicationQueryCacheDistributed_ShouldDisableL1Cache()
    {
        var services = new ServiceCollection();

        services.AddMvpApplicationQueryCacheDistributed();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value.EnableL1Cache.Should().BeFalse();
    }

    [Fact]
    public void AddMvpApplicationQueryCacheDistributed_WithAdditionalConfigure_ShouldApplyBothConfigurations()
    {
        var services = new ServiceCollection();

        services.AddMvpApplicationQueryCacheDistributed(options => options.KeyPrefix = "dist:");
        ServiceProvider provider = services.BuildServiceProvider();

        QueryCacheOptions opts = provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value;
        opts.EnableL1Cache.Should().BeFalse();
        opts.KeyPrefix.Should().Be("dist:");
    }

    [Fact]
    public void UseCacheKeyGenerator_ShouldReplaceDefaultGenerator()
    {
        var services = new ServiceCollection();
        services.AddMvpApplicationQueryCache();

        services.UseCacheKeyGenerator<CustomKeyGenerator>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IQueryCacheKeyGenerator>().Should().BeOfType<CustomKeyGenerator>();
    }

    [Fact]
    public void UseCacheProvider_ShouldReplaceDefaultProvider()
    {
        var services = new ServiceCollection();
        services.AddMvpApplicationQueryCache();

        services.UseCacheProvider<CustomCacheProvider>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IQueryCacheProvider>().Should().BeOfType<CustomCacheProvider>();
    }

    [Fact]
    public void UseCacheInvalidator_ShouldReplaceDefaultInvalidator()
    {
        var services = new ServiceCollection();
        services.AddMvpApplicationQueryCache();

        services.UseCacheInvalidator<CustomCacheInvalidator>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheInvalidator>().Should().BeOfType<CustomCacheInvalidator>();
    }
}
