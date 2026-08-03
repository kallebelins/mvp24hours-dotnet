using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Resilience;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Resilience;

[Trait("Category", "Unit")]
public class CacheResilienceOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new CacheResilienceOptions();

        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableRetry.Should().BeTrue();
        options.MaxRetries.Should().Be(3);
        options.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.UseExponentialBackoff.Should().BeTrue();
        options.MaxRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.EnableGracefulDegradation.Should().BeTrue();
        options.LogFailures.Should().BeTrue();
        options.CircuitBreaker.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
public class ResilientCacheProviderTest
{
    [Fact]
    public async Task GetAsync_InnerThrowsWithGracefulDegradation_ShouldReturnNull()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.GetAsync<TestEntity>("key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("cache down"));
        var options = new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false,
            EnableGracefulDegradation = true
        };
        string? fallbackKey = null;
        options.OnFallback = (key, _) => fallbackKey = key;
        var provider = new ResilientCacheProvider(inner.Object, options);

        TestEntity? result = await provider.GetAsync<TestEntity>("key");

        result.Should().BeNull();
        fallbackKey.Should().Be("key");
    }

    [Fact]
    public async Task SetAsync_InnerThrowsWithGracefulDegradation_ShouldNotThrow()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.SetAsync("key", It.IsAny<TestEntity>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("write failed"));
        var options = new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false,
            EnableGracefulDegradation = true
        };
        var provider = new ResilientCacheProvider(inner.Object, options);

        Func<Task> act = async () =>
            await provider.SetAsync("key", new TestEntity { Id = 1, Name = "X" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_InnerThrowsWithGracefulDegradation_ShouldReturnFalse()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.ExistsAsync("key", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Sockets.SocketException());
        var options = new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false,
            EnableGracefulDegradation = true
        };
        var provider = new ResilientCacheProvider(inner.Object, options);

        bool exists = await provider.ExistsAsync("key");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WithResilienceDisabled_ShouldDelegateToInner()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetAsync("ok", new TestEntity { Id = 1, Name = "Ok" });
        var options = new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false
        };
        var provider = new ResilientCacheProvider(inner, options);

        TestEntity? result = await provider.GetAsync<TestEntity>("ok");

        result!.Name.Should().Be("Ok");
    }

    [Fact]
    public void Constructor_NullInnerProvider_ShouldThrow()
    {
        Action act = () => new ResilientCacheProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_EmptyKey_ShouldThrow()
    {
        var inner = new Mock<ICacheProvider>();
        var provider = new ResilientCacheProvider(inner.Object, new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false
        });

        Func<Task> act = () => provider.GetAsync<TestEntity>(" ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetManyAsync_ShouldAggregateResults()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetAsync("a", new TestEntity { Id = 1, Name = "A" });
        await inner.SetAsync("b", new TestEntity { Id = 2, Name = "B" });
        var provider = new ResilientCacheProvider(inner, new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false
        });

        Dictionary<string, TestEntity> result = await provider.GetManyAsync<TestEntity>(["a", "b", "missing"]);

        result.Should().HaveCount(2);
        result["a"].Name.Should().Be("A");
    }

    [Fact]
    public async Task SetManyAsync_AndRemoveManyAsync_ShouldProcessAllKeys()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        var provider = new ResilientCacheProvider(inner, new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false
        });
        var values = new Dictionary<string, TestEntity>
        {
            ["k1"] = new TestEntity { Id = 1, Name = "One" },
            ["k2"] = new TestEntity { Id = 2, Name = "Two" }
        };

        await provider.SetManyAsync(values);
        (await provider.GetAsync<TestEntity>("k1"))!.Name.Should().Be("One");

        await provider.RemoveManyAsync(["k1", "k2", ""]);
        (await provider.ExistsAsync("k1")).Should().BeFalse();
    }

    [Fact]
    public async Task GetStringAsync_WithRetryEnabled_ShouldReturnValue()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetStringAsync("retry-key", "ok");
        var provider = new ResilientCacheProvider(inner, new CacheResilienceOptions
        {
            EnableCircuitBreaker = true,
            EnableRetry = true,
            MaxRetries = 1
        });

        string? value = await provider.GetStringAsync("retry-key");

        value.Should().Be("ok");
    }

    [Fact]
    public async Task RefreshAsync_InnerThrowsWithGracefulDegradation_ShouldNotThrow()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("refresh failed"));
        var provider = new ResilientCacheProvider(inner.Object, new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false,
            EnableGracefulDegradation = true
        });

        Func<Task> act = () => provider.RefreshAsync("key");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_NullValue_ShouldThrow()
    {
        var inner = new Mock<ICacheProvider>();
        var provider = new ResilientCacheProvider(inner.Object, new CacheResilienceOptions
        {
            EnableCircuitBreaker = false,
            EnableRetry = false
        });

        Func<Task> act = () => provider.SetAsync<TestEntity>("key", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
