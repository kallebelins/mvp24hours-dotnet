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
}
