using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Caching.Base;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test;

[Trait("Category", "Unit")]
public class RepositoryCacheBaseTest
{
    [Fact]
    public void Constructor_NullCache_ShouldThrow()
    {
        Action act = () => _ = new RepositoryCacheBase(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

[Trait("Category", "Unit")]
public class RepositoryCacheTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public void SetAndGet_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache);
        var entity = new CacheRepositoryEntity { Id = 1, Name = "One" };

        repository.Set("key1", entity);
        CacheRepositoryEntity? result = repository.Get("key1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("One");
    }

    [Fact]
    public void Get_MissingKey_ShouldReturnNull()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache);

        CacheRepositoryEntity? result = repository.Get("missing");

        result.Should().BeNull();
    }

    [Fact]
    public void SetStringAndGetString_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache);

        repository.SetString("skey", "svalue");
        string? result = repository.GetString("skey");

        result.Should().Be("svalue");
    }

    [Fact]
    public void GetString_MissingKey_ShouldReturnNull()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache);

        string? result = repository.GetString("missing");

        result.Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldDeleteEntry()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache);
        repository.SetString("rkey", "rvalue");

        repository.Remove("rkey");

        repository.GetString("rkey").Should().BeNull();
    }

    [Fact]
    public void Get_WhenCacheThrows_ShouldLogAndRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.Get(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache.Object, NullLogger<RepositoryCache<CacheRepositoryEntity>>.Instance);

        Action act = () => repository.Get("k");

        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public void GetString_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.Get(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache.Object);

        Action act = () => repository.GetString("k");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Set_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()))
            .Throws(new InvalidOperationException("boom"));
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache.Object);

        Action act = () => repository.Set("k", new CacheRepositoryEntity { Id = 1 });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetString_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()))
            .Throws(new InvalidOperationException("boom"));
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache.Object);

        Action act = () => repository.SetString("k", "v");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Remove_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.Remove(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));
        var repository = new RepositoryCache<CacheRepositoryEntity>(cache.Object);

        Action act = () => repository.Remove("k");

        act.Should().Throw<InvalidOperationException>();
    }
}

[Trait("Category", "Unit")]
public class RepositoryCacheAsyncTest
{
    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache);
        var entity = new CacheRepositoryEntity { Id = 1, Name = "One" };

        await repository.SetAsync("key1", entity);
        CacheRepositoryEntity? result = await repository.GetAsync("key1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("One");
    }

    [Fact]
    public async Task GetAsync_MissingKey_ShouldReturnNull()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache);

        CacheRepositoryEntity? result = await repository.GetAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetStringAndGetStringAsync_ShouldRoundTrip()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache);

        await repository.SetStringAsync("skey", "svalue");
        string? result = await repository.GetStringAsync("skey");

        result.Should().Be("svalue");
    }

    [Fact]
    public async Task GetStringAsync_MissingKey_ShouldReturnNull()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache);

        string? result = await repository.GetStringAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteEntry()
    {
        IDistributedCache cache = CreateCache();
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache);
        await repository.SetStringAsync("rkey", "rvalue");

        await repository.RemoveAsync("rkey");

        (await repository.GetStringAsync("rkey")).Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache.Object, NullLogger<RepositoryCacheAsync<CacheRepositoryEntity>>.Instance);

        Func<Task> act = () => repository.GetAsync("k");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task GetStringAsync_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache.Object);

        Func<Task> act = () => repository.GetStringAsync("k");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetAsync_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache.Object);

        Func<Task> act = () => repository.SetAsync("k", new CacheRepositoryEntity { Id = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetStringAsync_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache.Object);

        Func<Task> act = () => repository.SetStringAsync("k", "v");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RemoveAsync_WhenCacheThrows_ShouldRethrow()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var repository = new RepositoryCacheAsync<CacheRepositoryEntity>(cache.Object);

        Func<Task> act = () => repository.RemoveAsync("k");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
