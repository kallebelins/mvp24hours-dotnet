using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Extensions;

[Trait("Category", "Unit")]
public class CacheConfigHelperTest
{
    // CacheConfigHelper is internal; its behavior is exercised indirectly through
    // the public CacheExtensions.SetString(IDistributedCache, string, string, DateTimeOffset)
    // overload, which is the only production code path that calls GetCacheOptions(time).
    [Fact]
    public void SetString_WithAbsoluteTime_ShouldPassExplicitAbsoluteExpirationToCache()
    {
        var cache = new Mock<IDistributedCache>();
        DistributedCacheEntryOptions? capturedOptions = null;
        cache.Setup(x => x.Set(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>()))
            .Callback<string, byte[], DistributedCacheEntryOptions>((_, _, options) => capturedOptions = options);
        DateTimeOffset explicitTime = DateTimeOffset.UtcNow.AddHours(3);

        Mvp24Hours.Extensions.CacheExtensions.SetString(cache.Object, "k", "v", explicitTime);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpiration.Should().Be(explicitTime);
    }
}

[Trait("Category", "Unit")]
public class CacheExtensionsLegacyTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public void SetString_WithMinutes_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.CacheExtensions.SetString(cache, "k", "v", 5);

        cache.GetString("k").Should().Be("v");
    }

    [Fact]
    public void SetString_WithMinutes_NullCache_ShouldNoOp()
    {
        Action act = () => Mvp24Hours.Extensions.CacheExtensions.SetString(null!, "k", "v", 5);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetString_WithMinutes_EmptyKey_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.CacheExtensions.SetString(cache, " ", "v", 5);

        cache.GetString(" ").Should().BeNull();
    }

    [Fact]
    public void SetString_WithMinutes_EmptyValue_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.CacheExtensions.SetString(cache, "k2", " ", 5);

        cache.GetString("k2").Should().BeNull();
    }

    [Fact]
    public void SetString_WithAbsoluteTime_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.CacheExtensions.SetString(cache, "k3", "v3", DateTimeOffset.UtcNow.AddMinutes(5));

        cache.GetString("k3").Should().Be("v3");
    }

    [Fact]
    public void SetString_WithAbsoluteTime_NullCache_ShouldNoOp()
    {
        Action act = () => Mvp24Hours.Extensions.CacheExtensions.SetString(null!, "k", "v", DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }
}

[Trait("Category", "Unit")]
public class CacheAsyncExtensionsLegacyTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public async Task SetStringAsync_WithMinutes_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(cache, "ak", "av", 5);

        (await cache.GetStringAsync("ak")).Should().Be("av");
    }

    [Fact]
    public async Task SetStringAsync_WithMinutes_NullCache_ShouldNoOp()
    {
        Func<Task> act = () => Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(null!, "k", "v", 5);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetStringAsync_WithMinutes_EmptyKey_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(cache, " ", "v", 5);

        (await cache.GetStringAsync(" ")).Should().BeNull();
    }

    [Fact]
    public async Task SetStringAsync_WithAbsoluteTime_ShouldStoreValue()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(cache, "ak2", "av2", DateTimeOffset.UtcNow.AddMinutes(5));

        (await cache.GetStringAsync("ak2")).Should().Be("av2");
    }

    [Fact]
    public async Task SetStringAsync_WithAbsoluteTime_NullCache_ShouldNoOp()
    {
        Func<Task> act = () => Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(null!, "k", "v", DateTimeOffset.UtcNow);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetStringAsync_WithAbsoluteTime_EmptyValue_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.CacheAsyncExtensions.SetStringAsync(cache, "ak3", " ", DateTimeOffset.UtcNow.AddMinutes(5));

        (await cache.GetStringAsync("ak3")).Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class ObjectCacheExtensionsLegacyTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public void GetObject_NullCache_ShouldReturnDefault()
    {
        Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(null!, "k").Should().BeNull();
    }

    [Fact]
    public void GetObject_EmptyKey_ShouldReturnDefault()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(cache, " ").Should().BeNull();
    }

    [Fact]
    public void GetObject_MissingKey_ShouldReturnDefault()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(cache, "missing").Should().BeNull();
    }

    [Fact]
    public void SetObject_AndGetObject_ShouldRoundTripWithNewtonsoft()
    {
        IDistributedCache cache = CreateCache();
        var entity = new CacheRepositoryEntity { Id = 1, Name = "Newtonsoft" };

        Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(cache, "obj", entity);
        CacheRepositoryEntity? result = Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(cache, "obj");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Newtonsoft");
    }

    [Fact]
    public void SetObject_NullCache_ShouldNoOp()
    {
        Action act = () => Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(null!, "k", new CacheRepositoryEntity { Id = 1 });

        act.Should().NotThrow();
    }

    [Fact]
    public void SetObject_NullValue_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        Action act = () => Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject<CacheRepositoryEntity>(cache, "k", null!);

        act.Should().NotThrow();
        cache.GetString("k").Should().BeNull();
    }

    [Fact]
    public void SetObject_WithMinutes_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(cache, "obj-m", new CacheRepositoryEntity { Id = 2, Name = "Min" }, 10);
        CacheRepositoryEntity? result = Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(cache, "obj-m");

        result!.Name.Should().Be("Min");
    }

    [Fact]
    public void SetObject_WithMinutes_NullCache_ShouldNoOp()
    {
        Action act = () => Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(null!, "k", new CacheRepositoryEntity { Id = 1 }, 5);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetObject_WithAbsoluteTime_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(cache, "obj-t", (object)new CacheRepositoryEntity { Id = 3, Name = "Time" }, DateTimeOffset.UtcNow.AddMinutes(5));
        CacheRepositoryEntity? result = Mvp24Hours.Extensions.ObjectCacheExtensions.GetObject<CacheRepositoryEntity>(cache, "obj-t");

        result!.Name.Should().Be("Time");
    }

    [Fact]
    public void SetObject_WithAbsoluteTime_NullCache_ShouldNoOp()
    {
        Action act = () => Mvp24Hours.Extensions.ObjectCacheExtensions.SetObject(null!, "k", (object)new CacheRepositoryEntity { Id = 1 }, DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }
}

[Trait("Category", "Unit")]
public class ObjectCacheAsyncExtensionsLegacyTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public async Task GetObjectAsync_NullCache_ShouldReturnDefault()
    {
        (await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.GetObjectAsync<CacheRepositoryEntity>(null!, "k"))
            .Should().BeNull();
    }

    [Fact]
    public async Task GetObjectAsync_EmptyKey_ShouldReturnDefault()
    {
        IDistributedCache cache = CreateCache();

        (await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.GetObjectAsync<CacheRepositoryEntity>(cache, " "))
            .Should().BeNull();
    }

    [Fact]
    public async Task SetObjectAsync_AndGetObjectAsync_ShouldRoundTripWithNewtonsoft()
    {
        IDistributedCache cache = CreateCache();
        var entity = new CacheRepositoryEntity { Id = 1, Name = "AsyncNewtonsoft" };

        await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync(cache, "aobj", entity);
        CacheRepositoryEntity? result = await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.GetObjectAsync<CacheRepositoryEntity>(cache, "aobj");

        result.Should().NotBeNull();
        result!.Name.Should().Be("AsyncNewtonsoft");
    }

    [Fact]
    public async Task SetObjectAsync_NullCache_ShouldNoOp()
    {
        Func<Task> act = () => Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync(null!, "k", new CacheRepositoryEntity { Id = 1 });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetObjectAsync_NullValue_ShouldNoOp()
    {
        IDistributedCache cache = CreateCache();

        Func<Task> act = () => Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync<CacheRepositoryEntity>(cache, "k", null!);

        await act.Should().NotThrowAsync();
        (await cache.GetStringAsync("k")).Should().BeNull();
    }

    [Fact]
    public async Task SetObjectAsync_WithMinutes_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync(cache, "aobj-m", new CacheRepositoryEntity { Id = 2, Name = "AsyncMin" }, 10);
        CacheRepositoryEntity? result = await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.GetObjectAsync<CacheRepositoryEntity>(cache, "aobj-m");

        result!.Name.Should().Be("AsyncMin");
    }

    [Fact]
    public async Task SetObjectAsync_WithAbsoluteTime_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();

        await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync(cache, "aobj-t", (object)new CacheRepositoryEntity { Id = 3, Name = "AsyncTime" }, DateTimeOffset.UtcNow.AddMinutes(5));
        CacheRepositoryEntity? result = await Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.GetObjectAsync<CacheRepositoryEntity>(cache, "aobj-t");

        result!.Name.Should().Be("AsyncTime");
    }

    [Fact]
    public async Task SetObjectAsync_WithAbsoluteTime_NullCache_ShouldNoOp()
    {
        Func<Task> act = () => Mvp24Hours.Extensions.ObjectCacheAsyncExtensions.SetObjectAsync(null!, "k", (object)new CacheRepositoryEntity { Id = 1 }, DateTimeOffset.UtcNow);

        await act.Should().NotThrowAsync();
    }
}
