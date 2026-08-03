using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Observability;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbObservabilityExtensionsTest
{
    [Fact]
    public void AddMongoDbObservability_WithoutConfigure_ShouldRegisterObservabilityServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbObservability();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMongoDbMetrics>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbSlowQueryLogger>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbOpenTelemetryInstrumentation>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbConnectionPoolMetrics>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbStructuredLogger>().Should().NotBeNull();
        provider.GetRequiredService<MongoDbDurationTracker>().Should().NotBeNull();
    }

    [Fact]
    public void AddMongoDbObservability_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbObservability(options =>
        {
            options.EnableSlowQueryLogging = false;
            options.SlowQueryThreshold = TimeSpan.FromSeconds(2);
            options.EnableOpenTelemetry = true;
        });

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableSlowQueryLogging.Should().BeFalse();
        options.SlowQueryThreshold.Should().Be(TimeSpan.FromSeconds(2));
        options.EnableOpenTelemetry.Should().BeTrue();
    }

    [Fact]
    public void AddMongoDbFullObservability_WithThreshold_ShouldEnableAllFeatures()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbFullObservability(TimeSpan.FromMilliseconds(750), enableOpenTelemetry: true);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableSlowQueryLogging.Should().BeTrue();
        options.EnableConnectionPoolMetrics.Should().BeTrue();
        options.EnableStructuredLogging.Should().BeTrue();
        options.EnableDurationTracking.Should().BeTrue();
        options.EnableOpenTelemetry.Should().BeTrue();
        options.SlowQueryThreshold.Should().Be(TimeSpan.FromMilliseconds(750));
    }

    [Fact]
    public void AddMongoDbFullObservability_WithoutThreshold_ShouldEnableAllFeatures()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbFullObservability();

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableSlowQueryLogging.Should().BeTrue();
        options.EnableConnectionPoolMetrics.Should().BeTrue();
        options.EnableStructuredLogging.Should().BeTrue();
        options.EnableDurationTracking.Should().BeTrue();
        options.EnableOpenTelemetry.Should().BeFalse();
    }

    [Fact]
    public void ConfigureObservability_ShouldConfigureClientSettings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMongoDbObservability(options =>
        {
            options.EnableSlowQueryLogging = true;
            options.EnableOpenTelemetry = true;
            options.EnableConnectionPoolMetrics = true;
            options.EnableStructuredLogging = true;
            options.EnableDurationTracking = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        var settings = MongoClientSettings.FromConnectionString("mongodb://127.0.0.1:27017");

        MongoClientSettings result = settings.ConfigureObservability(provider);

        result.Should().BeSameAs(settings);
    }

    [Fact]
    public void AddMongoDbSlowQueryLogging_ShouldConfigureSlowQueryOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbSlowQueryLogging(TimeSpan.FromSeconds(3), logFilter: true);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableSlowQueryLogging.Should().BeTrue();
        options.SlowQueryThreshold.Should().Be(TimeSpan.FromSeconds(3));
        options.LogSlowQueryFilter.Should().BeTrue();
    }

    [Fact]
    public void AddMongoDbOpenTelemetry_ShouldConfigureTracingOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbOpenTelemetry("Custom.MongoDb", includeStatement: true);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableOpenTelemetry.Should().BeTrue();
        options.ActivitySourceName.Should().Be("Custom.MongoDb");
        options.IncludeStatementInTrace.Should().BeTrue();
    }

    [Fact]
    public void AddMongoDbConnectionPoolMetrics_ShouldConfigurePoolMetricsOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbConnectionPoolMetrics(TimeSpan.FromSeconds(30), alertThreshold: 0.9);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableConnectionPoolMetrics.Should().BeTrue();
        options.EnableConnectionPoolAlerts.Should().BeTrue();
        options.ConnectionPoolAlertThreshold.Should().Be(0.9);
        options.ConnectionPoolMetricsInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddMongoDbStructuredLogging_ShouldConfigureStructuredLoggingOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbStructuredLogging(
            logParameters: true,
            logResultCounts: false,
            sensitiveFields: ["Password", "Token"]);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableStructuredLogging.Should().BeTrue();
        options.LogCommandParameters.Should().BeTrue();
        options.LogResultCounts.Should().BeFalse();
        options.SensitiveFields.Should().BeEquivalentTo(["Password", "Token"]);
    }

    [Fact]
    public void AddMongoDbDurationTracking_ShouldConfigureDurationTrackingOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMongoDbDurationTracking(TimeSpan.FromMinutes(5), collectPercentiles: false);

        MongoDbObservabilityOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MongoDbObservabilityOptions>>().Value;

        options.EnableDurationTracking.Should().BeTrue();
        options.CollectDurationPercentiles.Should().BeFalse();
        options.DurationAggregationWindow.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetMongoDbMetrics_ShouldResolveMetricsFromProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMongoDbObservability();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetMongoDbMetrics().Should().NotBeNull();
    }

    [Fact]
    public void GetMongoDbDurationTracker_ShouldResolveTrackerFromProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMongoDbObservability();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetMongoDbDurationTracker().Should().NotBeNull();
    }
}
