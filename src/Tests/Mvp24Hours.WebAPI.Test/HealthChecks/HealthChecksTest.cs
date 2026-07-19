using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.WebAPI.HealthChecks;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.HealthChecks;

[Trait("Category", "Unit")]
public class HealthChecksTest
{
    [Fact]
    public async Task BaseHealthCheck_Should_EnrichData()
    {
        var sut = new FakeHealthCheck();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("fake", sut, null, null)
        };

        HealthCheckResult result = await sut.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("duration_ms");
        result.Data.Should().ContainKey("timestamp");
    }

    [Fact]
    public async Task BaseHealthCheck_Should_HandleException()
    {
        var sut = new ThrowingHealthCheck();
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("throwing", sut, null, null)
        };

        HealthCheckResult result = await sut.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("exceptionType");
    }

    [Fact]
    public async Task CacheHealthCheck_Should_ReportHealthy_WhenCachesWork()
    {
        var sut = new CacheHealthCheck(
            WebApiTestHelpers.CreateMemoryDistributedCache(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<CacheHealthCheck>.Instance);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("cache", sut, null, null)
        };

        HealthCheckResult result = await sut.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CacheHealthCheck_Should_ReportUnhealthy_WhenCacheNotConfigured()
    {
        var sut = new CacheHealthCheck(
            distributedCache: null,
            memoryCache: null,
            NullLogger<CacheHealthCheck>.Instance,
            new CacheHealthCheckOptions { CheckDistributedCache = true, CheckMemoryCache = true });
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("cache", sut, null, null)
        };

        HealthCheckResult result = await sut.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}

internal sealed class FakeHealthCheck : BaseHealthCheck
{
    public FakeHealthCheck() : base(NullLogger.Instance)
    {
    }

    protected override Task<HealthCheckResult> CheckHealthAsyncCore(HealthCheckContext context, CancellationToken cancellationToken)
        => Task.FromResult(HealthCheckResult.Healthy("ok", GetData()));
}

internal sealed class ThrowingHealthCheck : BaseHealthCheck
{
    public ThrowingHealthCheck() : base(NullLogger.Instance)
    {
    }

    protected override Task<HealthCheckResult> CheckHealthAsyncCore(HealthCheckContext context, CancellationToken cancellationToken)
        => throw new InvalidOperationException("boom");
}
