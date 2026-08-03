using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;
using Mvp24Hours.Infrastructure.Pipe.Observability;

namespace Mvp24Hours.Application.Pipe.Test.Observability;

[Trait("Category", "Unit")]
public class PipelineObservabilityExtensionsTest
{
    [Fact]
    public void AddPipelineMetrics_Should_RegisterMetricsService()
    {
        var services = new ServiceCollection();

        services.AddPipelineMetrics(maxDurationSamples: 500);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IPipelineMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineStructuredLogging_Should_RegisterMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPipelineStructuredLogging(options =>
        {
            options.LogOperationStart = true;
            options.TrackMemory = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetServices<IPipelineMiddleware>().Should().NotBeEmpty();
        provider.GetServices<IPipelineMiddlewareSync>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddPipelineStructuredLogging_Should_SupportThresholdOverload()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPipelineStructuredLogging(TimeSpan.FromSeconds(2), trackMemory: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<StructuredLoggingOptions>()!.SlowOperationThreshold.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddPipelineVisualizer_Should_RegisterVisualizer()
    {
        var services = new ServiceCollection();

        services.AddPipelineVisualizer();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IPipelineVisualizer>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineHealthCheck_Should_RegisterHealthServices()
    {
        var services = new ServiceCollection();

        services.AddPipelineHealthCheck(options => options.MinimumSuccessRate = 0.9);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<PipelineHealthCheckOptions>().Should().NotBeNull();
        provider.GetService<IPipelineMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineHealthCheck_OnBuilder_Should_RegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        IHealthChecksBuilder builder = services.AddHealthChecks();

        builder.AddPipelineHealthCheck("pipe-health", options => options.CriticalSuccessRate = 0.7, tags: ["pipe"]);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<PipelineHealthCheckOptions>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineObservers_Should_RegisterObserverManager()
    {
        var services = new ServiceCollection();

        services.AddPipelineObservers();
        services.AddPipelineMetricsObserver();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IPipelineObserverManager>().Should().NotBeNull();
        provider.GetServices<IPipelineObserver>().Should().ContainSingle(o => o is MetricsCollectorObserver);
    }

    [Fact]
    public void AddPipelineObservability_Should_RegisterCompleteStack()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPipelineObservability(options =>
        {
            options.EnableMetrics = true;
            options.EnableStructuredLogging = true;
            options.EnableVisualizer = true;
            options.EnableHealthMonitor = true;
            options.EnableObservers = true;
            options.SlowOperationThreshold = TimeSpan.FromSeconds(1);
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IPipelineContextAccessor>().Should().NotBeNull();
        provider.GetService<IPipelineMetrics>().Should().NotBeNull();
        provider.GetService<IPipelineVisualizer>().Should().NotBeNull();
        provider.GetService<IPipelineHealthMonitor>().Should().NotBeNull();
        provider.GetService<IPipelineObserverManager>().Should().NotBeNull();
    }
}
