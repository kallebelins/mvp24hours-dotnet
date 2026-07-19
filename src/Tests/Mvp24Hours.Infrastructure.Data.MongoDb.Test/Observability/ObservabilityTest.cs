using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Observability;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Observability;

[Trait("Category", "Unit")]
public class ObservabilityTest
{
    [Fact]
    public void MongoDbMetrics_ShouldRecordCommandsAndErrors()
    {
        var metrics = new MongoDbMetrics(Options.Create(new MongoDbObservabilityOptions()));

        metrics.RecordCommandDuration("find", "customers", TimeSpan.FromMilliseconds(10), success: true);
        metrics.RecordCommandDuration("find", "customers", TimeSpan.FromMilliseconds(20), success: true);
        metrics.RecordCommandDuration("insert", "orders", TimeSpan.FromMilliseconds(50), success: false);
        metrics.RecordSlowQuery("find", "customers", TimeSpan.FromSeconds(2), 100, 5);
        metrics.RecordError("insert", "orders", "MongoWriteException");
        metrics.RecordConnectionCheckoutDuration(TimeSpan.FromMilliseconds(5));
        metrics.RecordConnectionPoolStats(new ConnectionPoolStats { CurrentSize = 3, AvailableCount = 2 });

        MongoDbMetricsSnapshot snapshot = metrics.GetSnapshot();
        snapshot.TotalCommands.Should().Be(3);
        snapshot.SuccessfulCommands.Should().Be(2);
        snapshot.FailedCommands.Should().Be(1);
        snapshot.SlowQueries.Should().Be(1);
        snapshot.CommandCounts["find"].Should().Be(2);
        snapshot.ConnectionPool.Should().NotBeNull();

        DurationStatistics findStats = metrics.GetDurationStatistics("find");
        findStats.Count.Should().Be(2);
        findStats.AverageMs.Should().BeApproximately(15, 0.1);

        metrics.GetCheckoutStats().Average.Should().BeGreaterThan(0);
        metrics.Reset();
        metrics.GetSnapshot().TotalCommands.Should().Be(0);
    }

    [Fact]
    public void MongoDbDurationTracker_ShouldAggregateStatistics()
    {
        var tracker = new MongoDbDurationTracker(Options.Create(new MongoDbObservabilityOptions
        {
            EnableDurationTracking = true,
            TrackIndividualOperations = true
        }));

        tracker.RecordDuration("find", "customers", TimeSpan.FromMilliseconds(5), success: true);
        tracker.RecordDuration("find", "customers", TimeSpan.FromMilliseconds(15), success: true);
        tracker.RecordDuration("find", "customers", TimeSpan.FromMilliseconds(25), success: false);

        DurationStatistics? stats = tracker.GetStatistics("find");
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(3);
        stats.MinMs.Should().Be(5);
        stats.MaxMs.Should().Be(25);

        IReadOnlyList<DurationStatistics> all = tracker.GetAllStatistics();
        all.Should().ContainSingle(s => s.CommandName == "find");

        IReadOnlyDictionary<string, CommandSummary> summary = tracker.GetSummary();
        summary["find"].TotalCount.Should().Be(3);
        summary["find"].FailureCount.Should().Be(1);

        tracker.Reset();
        tracker.GetStatistics("find").Should().BeNull();
    }

    [Fact]
    public void MongoDbOpenTelemetryInstrumentation_ShouldCreateActivity()
    {
        using var instrumentation = new MongoDbOpenTelemetryInstrumentation(
            Options.Create(new MongoDbObservabilityOptions { ActivitySourceName = "Mvp24Hours.MongoDb.Test" }));

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mvp24Hours.MongoDb.Test",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        Activity? activity = instrumentation.StartOperation("find", "testdb", "customers");
        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == "db.system" && t.Value == "mongodb");

        using (activity)
        {
            MongoDbOpenTelemetryInstrumentation.AddTags(("custom.tag", "value"));
            activity.GetTagItem("custom.tag").Should().Be("value");

            var ex = new InvalidOperationException("trace-error");
            MongoDbOpenTelemetryInstrumentation.RecordException(ex);
            activity.Status.Should().Be(ActivityStatusCode.Error);
        }

        instrumentation.PendingActivitiesCount.Should().Be(0);
    }

    [Fact]
    public void MongoDbOpenTelemetryInstrumentation_ConfigureClusterBuilder_ShouldRespectDisabledFlag()
    {
        using var instrumentation = new MongoDbOpenTelemetryInstrumentation(
            Options.Create(new MongoDbObservabilityOptions { EnableOpenTelemetry = false }));

        var settings = new MongoClientSettings();
        instrumentation.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().BeNull();
    }

    [Fact]
    public void MongoDbSlowQueryLogger_ShouldRespectRateLimit()
    {
        var options = Options.Create(new MongoDbObservabilityOptions
        {
            EnableSlowQueryLogging = true,
            SlowQueryThreshold = TimeSpan.Zero,
            MaxSlowQueriesPerMinute = 2,
            LogSlowQueryFilter = true,
            SensitiveFields = ["password"]
        });

        var metrics = new Mock<IMongoDbMetrics>();
        using var slowLogger = new MongoDbSlowQueryLogger(options, metrics: metrics.Object);

        slowLogger.CurrentSlowQueryCount.Should().Be(0);
        slowLogger.PendingCommandsCount.Should().Be(0);
    }

    [Fact]
    public void MongoDbStructuredLogger_ShouldRespectDisabledFlag()
    {
        using var structuredLogger = new MongoDbStructuredLogger(
            Options.Create(new MongoDbObservabilityOptions { EnableStructuredLogging = false }));

        var settings = new MongoClientSettings();
        structuredLogger.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().BeNull();
        structuredLogger.PendingCommandsCount.Should().Be(0);
    }

    [Fact]
    public void MongoDbObservabilityOptions_ShouldHaveDefaults()
    {
        var options = new MongoDbObservabilityOptions();

        options.EnableSlowQueryLogging.Should().BeTrue();
        options.SlowQueryThreshold.Should().Be(TimeSpan.FromMilliseconds(500));
        options.EnableOpenTelemetry.Should().BeFalse();
        options.EnableDurationTracking.Should().BeTrue();
#if DEBUG
        options.EnableStructuredLogging.Should().BeTrue();
#else
        options.EnableStructuredLogging.Should().BeFalse();
#endif
        options.MaxSlowQueriesPerMinute.Should().Be(100);
    }
}
