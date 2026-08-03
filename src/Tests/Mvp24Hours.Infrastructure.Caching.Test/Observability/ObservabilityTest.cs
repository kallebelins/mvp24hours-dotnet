using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Observability;
using Mvp24Hours.Infrastructure.Caching.Test.Support;

namespace Mvp24Hours.Infrastructure.Caching.Test.Observability;

[Trait("Category", "Unit")]
public class CacheHealthCheckTest
{
    [Fact]
    public async Task CheckHealthAsync_HealthyProvider_ShouldReturnHealthy()
    {
        MemoryCacheProvider cache = CacheTestHelpers.CreateMemoryProvider();
        var healthCheck = new CacheHealthCheck(cache);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_FailingProvider_ShouldReturnUnhealthy()
    {
        var cache = new Mock<ICacheProvider>();
        cache.Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cache unavailable"));
        var healthCheck = new CacheHealthCheck(cache.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_KeyStillExistsAfterRemove_ShouldReturnDegraded()
    {
        var cache = new Mock<ICacheProvider>();
        cache.Setup(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("health_check_test_value");
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var healthCheck = new CacheHealthCheck(cache.Object);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void CacheHealthCheckOptions_DefaultValues_ShouldBeExpected()
    {
        var options = new CacheHealthCheckOptions();

        options.MaxOperationDurationMs.Should().Be(1000);
        options.TestKeyPrefix.Should().Be("health_check_");
        options.IncludeDetailedDiagnostics.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
public class CacheMetricsTest
{
    [Fact]
    public void RecordGet_ShouldTrackHitsAndMisses()
    {
        var metrics = new CacheMetrics();

        metrics.RecordGet("k1", 10, isHit: true, provider: "Memory");
        metrics.RecordGet("k2", 5, isHit: false, provider: "Memory");

        metrics.GetTotalHits("Memory").Should().Be(1);
        metrics.GetTotalMisses("Memory").Should().Be(1);
        metrics.GetHitRatio("Memory").Should().Be(0.5);
        metrics.GetTotalOperations("Memory").Should().Be(2);
    }

    [Fact]
    public void RecordSetAndRemove_ShouldIncrementOperations()
    {
        var metrics = new CacheMetrics();

        metrics.RecordSet("k", 3, 100, "Distributed");
        metrics.RecordRemove("k", 2, "Distributed");

        metrics.GetTotalOperations("Distributed").Should().Be(2);
    }

    [Fact]
    public void GetHitRatio_NoOperations_ShouldReturnNull()
    {
        var metrics = new CacheMetrics();

        metrics.GetHitRatio().Should().BeNull();
    }

    [Fact]
    public void RecordEvictionAndInvalidation_ShouldNotThrow()
    {
        var metrics = new CacheMetrics();

        Action act = () =>
        {
            metrics.RecordEviction("Memory");
            metrics.RecordInvalidation("entity:*", "Memory");
            metrics.RecordError("get", new InvalidOperationException(), "Memory");
        };

        act.Should().NotThrow();
    }
}

[Trait("Category", "Unit")]
public class CacheActivitySourceTest
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        CacheActivitySource.SourceName.Should().Be("Mvp24Hours.Cache");
        CacheActivitySource.MeterName.Should().Be("Mvp24Hours.Cache");
        CacheActivitySource.ActivityNames.Get.Should().Be("Mvp24Hours.Cache.Get");
        CacheActivitySource.TagNames.Key.Should().Be("cache.key");
    }

    [Fact]
    public void RecordOperation_ShouldNotThrow()
    {
        Action act = () => CacheActivitySource.RecordOperation("get", "Memory", 12.5, true, true, 128);

        act.Should().NotThrow();
    }

    [Fact]
    public void StartCacheActivity_ShouldCreateActivityWhenListenerPresent()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CacheActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = CacheActivitySource.StartCacheActivity(
            CacheActivitySource.ActivityNames.Get,
            "get",
            "activity-key",
            "Memory");

        activity.Should().NotBeNull();
        CacheActivitySource.SetSuccess(activity, true);
        CacheActivitySource.EnrichActivity(activity, 5);
    }
}

[Trait("Category", "Unit")]
public class ObservableCacheProviderTest
{
    [Fact]
    public async Task GetAsync_Hit_ShouldRecordMetrics()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetAsync("obs-key", new TestEntity { Id = 1, Name = "Observed" });
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        TestEntity? result = await provider.GetAsync<TestEntity>("obs-key");

        result.Should().NotBeNull();
        metrics.Verify(x => x.RecordGet("obs-key", It.IsAny<double>(), true, "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Miss_ShouldRecordMiss()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.GetAsync<TestEntity>("missing");

        metrics.Verify(x => x.RecordGet("missing", It.IsAny<double>(), false, "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldRecordSetMetrics()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.SetAsync("set-key", new TestEntity { Id = 1, Name = "Set" });

        metrics.Verify(x => x.RecordSet("set-key", It.IsAny<double>(), null, "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRecordRemoveMetrics()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetStringAsync("remove-key", "value");
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.RemoveAsync("remove-key");

        metrics.Verify(x => x.RecordRemove("remove-key", It.IsAny<double>(), "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task GetAsync_InnerThrows_ShouldRecordErrorAndRethrow()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.GetAsync<TestEntity>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("inner failed"));
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner.Object, metrics.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAsync<TestEntity>("err"));
        metrics.Verify(x => x.RecordError("get", It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetStringAsync_HitAndMiss_ShouldRecordMetrics()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetStringAsync("str-key", "value");
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.GetStringAsync("str-key");
        await provider.GetStringAsync("missing-str");

        metrics.Verify(x => x.RecordGet("str-key", It.IsAny<double>(), true, "MemoryCacheProvider"), Times.Once);
        metrics.Verify(x => x.RecordGet("missing-str", It.IsAny<double>(), false, "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task SetStringAsync_ShouldRecordSetMetricsWithSize()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.SetStringAsync("set-str", "payload");

        metrics.Verify(x => x.RecordSet("set-str", It.IsAny<double>(), It.IsAny<long>(), "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_AndRefreshAsync_ShouldComplete()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetStringAsync("exists-key", "value");
        var provider = new ObservableCacheProvider(inner);

        (await provider.ExistsAsync("exists-key")).Should().BeTrue();
        await provider.RefreshAsync("exists-key");
    }

    [Fact]
    public async Task ExistsAsync_InnerThrows_ShouldRecordErrorAndRethrow()
    {
        var inner = new Mock<ICacheProvider>();
        inner.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("exists failed"));
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner.Object, metrics.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ExistsAsync("err"));
        metrics.Verify(x => x.RecordError("exists", It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithStringValue_ShouldTrackValueSize()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        var metrics = new Mock<ICacheMetrics>();
        var provider = new ObservableCacheProvider(inner, metrics.Object);

        await provider.SetAsync("string-set", "hello");

        metrics.Verify(x => x.RecordSet("string-set", It.IsAny<double>(), It.IsAny<long>(), "MemoryCacheProvider"), Times.Once);
    }

    [Fact]
    public async Task RemoveManyAsync_ShouldDelegateToInner()
    {
        MemoryCacheProvider inner = CacheTestHelpers.CreateMemoryProvider();
        await inner.SetStringAsync("k1", "v1");
        await inner.SetStringAsync("k2", "v2");
        var provider = new ObservableCacheProvider(inner);

        await provider.RemoveManyAsync(["k1", "k2"]);

        (await inner.ExistsAsync("k1")).Should().BeFalse();
        (await inner.ExistsAsync("k2")).Should().BeFalse();
    }
}
