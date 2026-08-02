using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching.Extensions;
using Mvp24Hours.Infrastructure.Caching.Invalidation;
using Mvp24Hours.Infrastructure.Caching.KeyGenerators;
using Mvp24Hours.Infrastructure.Caching.Providers;
using Mvp24Hours.Infrastructure.Caching.Resilience;
using Mvp24Hours.Infrastructure.Caching.Serializers;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Extensions;

[Trait("Category", "Unit")]
public class CachingServiceExtensionsTest
{
    [Fact]
    public void AddMvp24HoursCaching_Legacy_ShouldConfigureExpirationOptions()
    {
        var services = new ServiceCollection();
        DateTimeOffset absolute = DateTimeOffset.UtcNow.AddHours(1);
        var relative = TimeSpan.FromMinutes(10);
        var sliding = TimeSpan.FromMinutes(2);

        IServiceCollection result = CachingServiceExtensions.AddMvp24HoursCaching(
            services,
            absolute,
            relative,
            sliding);

        result.Should().BeSameAs(services);
    }
}

[Trait("Category", "Unit")]
public class CacheProviderExtensionsTest
{
    [Fact]
    public void AddMemoryCacheProvider_NullServices_ShouldThrow()
    {
        Action act = () => CacheProviderExtensions.AddMemoryCacheProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMemoryCacheProvider_ShouldRegisterProviderAndSerializer()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider(options =>
        {
            options.DefaultKeyPrefix = "test";
            options.KeySeparator = "|";
        });
        ServiceProvider provider = services.BuildServiceProvider();

        ICacheProvider cache = provider.GetRequiredService<ICacheProvider>();
        ICacheSerializer serializer = provider.GetRequiredService<ICacheSerializer>();
        ICacheKeyGenerator keyGen = provider.GetRequiredService<ICacheKeyGenerator>();

        cache.Should().BeOfType<MemoryCacheProvider>();
        serializer.Should().BeOfType<JsonCacheSerializer>();
        keyGen.Should().BeOfType<DefaultCacheKeyGenerator>();
    }

    [Fact]
    public void AddDistributedCacheProvider_ShouldRequireDistributedCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddDistributedCacheProvider();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<DistributedCacheProvider>();
    }

    [Fact]
    public void AddMvp24HoursCaching_WithoutDistributed_ShouldFallbackToMemory()
    {
        var services = new ServiceCollection();
        CacheProviderExtensions.AddMvp24HoursCaching(
            services,
            options => options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(1));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<MemoryCacheProvider>();
    }

    [Fact]
    public void AddMvp24HoursCaching_WithDistributed_ShouldPreferDistributed()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        CacheProviderExtensions.AddMvp24HoursCaching(services);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<DistributedCacheProvider>();
    }

    [Fact]
    public void AddMemoryCacheProviderWithMessagePack_ShouldRegisterMessagePackSerializer()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProviderWithMessagePack();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<MessagePackCacheSerializer>();
        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<MemoryCacheProvider>();
    }

    [Fact]
    public void AddDistributedCacheProviderWithMessagePack_ShouldRegisterMessagePackSerializer()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddDistributedCacheProviderWithMessagePack();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<MessagePackCacheSerializer>();
        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<DistributedCacheProvider>();
    }

    [Fact]
    public void AddCacheSerializer_ShouldReplaceSerializerRegistration()
    {
        var services = new ServiceCollection();
        services.AddCacheSerializer<JsonCacheSerializer>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<JsonCacheSerializer>();
    }
}

[Trait("Category", "Unit")]
public class MvpCachingExtensionsTest
{
    [Fact]
    public void AddMvpCaching_NullServices_ShouldThrow()
    {
        Action act = () => MvpCachingExtensions.AddMvpCaching(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvpCaching_Default_ShouldUseMemoryAndJson()
    {
        var services = new ServiceCollection();
        services.AddMvpCaching();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<MemoryCacheProvider>();
        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<JsonCacheSerializer>();
    }

    [Fact]
    public void AddMvpCaching_UseMemoryAndMessagePack_ShouldRegister()
    {
        var services = new ServiceCollection();
        services.AddMvpCaching(options =>
        {
            options.CacheType = CacheType.Memory;
            options.Serializer = CacheSerializer.MessagePack;
            options.KeyPrefix = "app";
            options.KeySeparator = ".";
        });
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<MemoryCacheProvider>();
        provider.GetRequiredService<ICacheSerializer>().Should().BeOfType<MessagePackCacheSerializer>();
    }

    [Fact]
    public void AddMvpCaching_UseDistributed_ShouldRegisterDistributedProvider()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddMvpCaching(options => options.CacheType = CacheType.Distributed);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheProvider>().Should().BeOfType<DistributedCacheProvider>();
    }

    [Fact]
    public void AddMvpCaching_UseMultiLevel_ShouldRegisterMultiLevelCache()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
#pragma warning disable CS0618
        services.AddMvpCaching(options => options.CacheType = CacheType.MultiLevel);
#pragma warning restore CS0618
        ServiceProvider provider = services.BuildServiceProvider();

#pragma warning disable CS0618
        provider.GetRequiredService<IMultiLevelCache>().Should().BeOfType<MultiLevelCache>();
#pragma warning restore CS0618
#pragma warning disable CS0618
        provider.GetRequiredService<ICacheProvider>().Should().BeAssignableTo<IMultiLevelCache>();
#pragma warning restore CS0618
    }
}

[Trait("Category", "Unit")]
public class MultiLevelCacheExtensionsTest
{
    [Fact]
    public void AddMultiLevelCache_ShouldRegisterL1L2Coordination()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
#pragma warning disable CS0618
        services.AddMultiLevelCache();
#pragma warning restore CS0618
        ServiceProvider provider = services.BuildServiceProvider();

#pragma warning disable CS0618
        IMultiLevelCache cache = provider.GetRequiredService<IMultiLevelCache>();
#pragma warning restore CS0618
        cache.Should().NotBeNull();
    }

    [Fact]
    public void AddMultiLevelCacheWithInMemorySync_ShouldEnableSynchronizer()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
#pragma warning disable CS0618
        services.AddMultiLevelCacheWithInMemorySync();
#pragma warning restore CS0618
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheSynchronizer>().Should().NotBeNull();
#pragma warning disable CS0618
        provider.GetRequiredService<IMultiLevelCache>().Should().NotBeNull();
#pragma warning restore CS0618
    }
}

[Trait("Category", "Unit")]
public class CacheInvalidationServiceExtensionsTest
{
    [Fact]
    public void AddCacheInvalidationFeatures_ShouldRegisterAllServices()
    {
        var services = new ServiceCollection();
        services.AddMemoryCacheProvider();
        services.AddCacheInvalidationFeatures();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICacheTagManager>().Should().BeOfType<CacheTagManager>();
        provider.GetRequiredService<CacheDependencyManager>().Should().NotBeNull();
        provider.GetRequiredService<ICacheStampedePrevention>().Should().BeOfType<CacheStampedePrevention>();
        provider.GetRequiredService<ICacheInvalidationEventPublisher>()
            .Should().BeOfType<InMemoryCacheInvalidationEventPublisher>();
    }

    [Fact]
    public void AddCacheTagManager_NullServices_ShouldThrow()
    {
        Action act = () => CacheInvalidationServiceExtensions.AddCacheTagManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

[Trait("Category", "Unit")]
public class DistributedCacheStringExtensionsTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public void SetString_WithMinutes_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        cache.SetString("k", "v", 5);

        cache.GetString("k").Should().Be("v");
    }

    [Fact]
    public void SetString_WithAbsoluteExpiration_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        cache.SetString("k2", "v2", DateTimeOffset.UtcNow.AddMinutes(5));

        cache.GetString("k2").Should().Be("v2");
    }

    [Fact]
    public void SetString_EmptyKey_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        cache.SetString(" ", "v", 5);

        cache.GetString(" ").Should().BeNull();
    }

    [Fact]
    public async Task SetStringAsync_WithMinutes_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        await cache.SetStringAsync("ak", "av", 5);

        (await cache.GetStringAsync("ak")).Should().Be("av");
    }

    [Fact]
    public async Task SetStringAsync_WithAbsoluteExpiration_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        await cache.SetStringAsync("ak2", "av2", DateTimeOffset.UtcNow.AddMinutes(5));

        (await cache.GetStringAsync("ak2")).Should().Be("av2");
    }

    [Fact]
    public void SetObject_AndGetObject_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();
        var entity = new TestEntity { Id = 3, Name = "Obj" };

        cache.SetObject("obj", entity);
        TestEntity? result = cache.GetObject<TestEntity>("obj");

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
        result.Name.Should().Be("Obj");
    }

    [Fact]
    public void SetObject_WithMinutes_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        cache.SetObject("obj-m", new TestEntity { Id = 4, Name = "Min" }, 10);
        TestEntity? result = cache.GetObject<TestEntity>("obj-m");

        result!.Name.Should().Be("Min");
    }

    [Fact]
    public void SetObject_WithAbsoluteTime_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        cache.SetObject("obj-t", new TestEntity { Id = 5, Name = "Time" }, DateTimeOffset.UtcNow.AddMinutes(5));
        TestEntity? result = cache.GetObject<TestEntity>("obj-t");

        result!.Name.Should().Be("Time");
    }

    [Fact]
    public async Task SetObjectAsync_AndGetObjectAsync_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        await cache.SetObjectAsync("aobj", new TestEntity { Id = 6, Name = "Async" });
        TestEntity? result = await cache.GetObjectAsync<TestEntity>("aobj");

        result!.Name.Should().Be("Async");
    }

    [Fact]
    public async Task SetObjectAsync_WithMinutes_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        await cache.SetObjectAsync("aobj-m", new TestEntity { Id = 7, Name = "AsyncMin" }, 5);
        TestEntity? result = await cache.GetObjectAsync<TestEntity>("aobj-m");

        result!.Name.Should().Be("AsyncMin");
    }

    [Fact]
    public async Task SetObjectAsync_WithAbsoluteTime_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        await cache.SetObjectAsync(
            "aobj-t",
            new TestEntity { Id = 8, Name = "AsyncTime" },
            DateTimeOffset.UtcNow.AddMinutes(5));
        TestEntity? result = await cache.GetObjectAsync<TestEntity>("aobj-t");

        result!.Name.Should().Be("AsyncTime");
    }

    [Fact]
    public void GetObject_MissingKey_ShouldReturnNull()
    {
        IDistributedCache cache = CreateCache();

        cache.GetObject<TestEntity>("missing").Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class CacheResilienceGetOrSetExtensionsTest
{
    [Fact]
    public async Task GetOrSetAsync_Miss_ShouldLoadAndStore()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        int calls = 0;

        TestEntity? result = await CacheResilienceExtensions.GetOrSetAsync(
            cache,
            "res-key",
            _ =>
            {
                calls++;
                return Task.FromResult(new TestEntity { Id = 1, Name = "Loaded" });
            });

        result!.Name.Should().Be("Loaded");
        calls.Should().Be(1);
        (await cache.GetAsync<TestEntity>("res-key"))!.Name.Should().Be("Loaded");
    }

    [Fact]
    public async Task GetOrDefaultAsync_Miss_ShouldReturnDefault()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var fallback = new TestEntity { Id = 0, Name = "Default" };

        TestEntity result = await CacheResilienceExtensions.GetOrDefaultAsync(cache, "missing", fallback);

        result.Should().BeSameAs(fallback);
    }

    [Fact]
    public async Task GetWithFallbackAsync_SourceFails_ShouldReturnDefault()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var fallback = new TestEntity { Id = -1, Name = "Fallback" };

        TestEntity result = await CacheResilienceExtensions.GetWithFallbackAsync(
            cache,
            "fb-key",
            _ => throw new InvalidOperationException("source down"),
            fallback);

        result.Should().BeSameAs(fallback);
    }

    [Fact]
    public void WithResilience_NullProvider_ShouldThrow()
    {
        Action act = () => CacheResilienceExtensions.WithResilience(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
