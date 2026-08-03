using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Observability;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Observability;

[Trait("Category", "Unit")]
public class MongoDbConnectionPoolMetricsTest
{
    [Fact]
    public void GetCurrentStats_WhenEmpty_ShouldReturnAggregateWithZeroCounts()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions()));

        ConnectionPoolStats stats = metrics.GetCurrentStats();

        stats.Endpoint.Should().Be("aggregate");
        stats.CurrentSize.Should().Be(0);
        stats.InUseCount.Should().Be(0);
        stats.AvailableCount.Should().Be(0);
    }

    [Fact]
    public void GetStats_ForUnknownEndpoint_ShouldReturnNull()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions()));

        ConnectionPoolStats? stats = metrics.GetStats("unknown:27017");

        stats.Should().BeNull();
    }

    [Fact]
    public void GetAllStats_WhenEmpty_ShouldReturnEmptyList()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions()));

        IReadOnlyList<ConnectionPoolStats> stats = metrics.GetAllStats();

        stats.Should().BeEmpty();
    }

    [Fact]
    public void ConfigureClusterBuilder_WhenDisabled_ShouldNotThrow()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions
        {
            EnableConnectionPoolMetrics = false
        }));
        var settings = new MongoClientSettings();

        Action act = () => metrics.ConfigureClusterBuilder(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void StartAndStopPeriodicCollection_ShouldNotThrow()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions
        {
            EnableConnectionPoolMetrics = true,
            ConnectionPoolMetricsInterval = TimeSpan.FromMilliseconds(50)
        }));

        metrics.StartPeriodicCollection();
        metrics.StartPeriodicCollection();
        Thread.Sleep(75);
        metrics.StopPeriodicCollection();

        Action act = () => metrics.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        var metrics = new MongoDbConnectionPoolMetrics(Options.Create(new MongoDbObservabilityOptions()));

        metrics.Dispose();
        Action act = () => metrics.Dispose();

        act.Should().NotThrow();
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbConnectionPoolMetricsIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ConfigureClusterBuilder_WithRealClient_ShouldCollectStatsAfterPing()
    {
        var options = Options.Create(new MongoDbObservabilityOptions
        {
            EnableConnectionPoolMetrics = true,
            EnableConnectionPoolAlerts = true,
            ConnectionPoolAlertThreshold = 0.01
        });
        var metrics = new MongoDbConnectionPoolMetrics(options, NullLogger<MongoDbConnectionPoolMetrics>.Instance);
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
        metrics.ConfigureClusterBuilder(settings);
        metrics.StartPeriodicCollection();

        try
        {
            var client = new MongoClient(settings);
            BsonDocument pingResult = await client
                .GetDatabase(fixture.DatabaseName)
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            pingResult["ok"].AsDouble.Should().Be(1);
            ConnectionPoolStats stats = metrics.GetCurrentStats();
            stats.Should().NotBeNull();
            metrics.GetAllStats().Should().NotBeNull();
        }
        finally
        {
            metrics.Dispose();
        }
    }
}
