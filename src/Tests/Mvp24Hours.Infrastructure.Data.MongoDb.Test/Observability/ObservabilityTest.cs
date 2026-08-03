using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Observability;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

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
        IOptions<MongoDbObservabilityOptions> options = Options.Create(new MongoDbObservabilityOptions
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
    public void MongoDbSlowQueryLogger_ShouldTrackCommandLifecycleAndRateLimit()
    {
        var mockLogger = new Mock<ILogger<MongoDbSlowQueryLogger>>();
        var metrics = new Mock<IMongoDbMetrics>();
        using var slowLogger = new MongoDbSlowQueryLogger(
            Options.Create(new MongoDbObservabilityOptions
            {
                EnableSlowQueryLogging = true,
                SlowQueryThreshold = TimeSpan.Zero,
                MaxSlowQueriesPerMinute = 1,
                LogSlowQueryFilter = true,
                SensitiveFields = ["password"]
            }),
            mockLogger.Object,
            metrics.Object);

        var settings = new MongoClientSettings();
        slowLogger.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().NotBeNull();

        int requestId = 7;
        var command = new BsonDocument
        {
            { "find", "customers" },
            { "filter", new BsonDocument("password", "secret") }
        };

        MongoDbEventTestHelper.InvokeEvent(slowLogger, "OnCommandStarted",
            MongoDbEventTestHelper.CreateCommandStartedEvent(requestId, "find", "testdb", command));
        slowLogger.PendingCommandsCount.Should().Be(1);

        var reply = new BsonDocument
        {
            { "cursor", new BsonDocument("firstBatch", new BsonArray()) },
            { "n", 3 }
        };
        MongoDbEventTestHelper.InvokeEvent(slowLogger, "OnCommandSucceeded",
            MongoDbEventTestHelper.CreateCommandSucceededEvent(requestId, "find", reply, TimeSpan.FromMilliseconds(500)));

        slowLogger.PendingCommandsCount.Should().Be(0);
        slowLogger.CurrentSlowQueryCount.Should().Be(1);
        metrics.Verify(m => m.RecordSlowQuery("find", "customers", It.IsAny<TimeSpan>(), 0, 0), Times.Once);

        MongoDbEventTestHelper.InvokeEvent(slowLogger, "OnCommandStarted",
            MongoDbEventTestHelper.CreateCommandStartedEvent(8, "find", "testdb", command));
        MongoDbEventTestHelper.InvokeEvent(slowLogger, "OnCommandSucceeded",
            MongoDbEventTestHelper.CreateCommandSucceededEvent(8, "find", reply, TimeSpan.FromMilliseconds(600)));
        slowLogger.CurrentSlowQueryCount.Should().Be(1);
    }

    [Fact]
    public void MongoDbSlowQueryLogger_Dispose_ShouldClearPendingCommands()
    {
        using var slowLogger = new MongoDbSlowQueryLogger(
            Options.Create(new MongoDbObservabilityOptions { EnableSlowQueryLogging = true }));

        slowLogger.Dispose();
        slowLogger.PendingCommandsCount.Should().Be(0);
    }

    [Fact]
    public void MongoDbSlowQueryLogger_ConfigureClusterBuilder_Disabled_ShouldNotConfigure()
    {
        using var slowLogger = new MongoDbSlowQueryLogger(
            Options.Create(new MongoDbObservabilityOptions { EnableSlowQueryLogging = false }));

        var settings = new MongoClientSettings();
        slowLogger.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().BeNull();
    }

    [Fact]
    public void MongoDbOpenTelemetryInstrumentation_ShouldHandleClusterEvents()
    {
        using var instrumentation = new MongoDbOpenTelemetryInstrumentation(
            Options.Create(new MongoDbObservabilityOptions
            {
                EnableOpenTelemetry = true,
                ActivitySourceName = "Mvp24Hours.MongoDb.Events",
                IncludeStatementInTrace = true,
                ServiceName = "test-service",
                Environment = "test",
                AdditionalTraceTags = ["team=platform"],
                RecordExceptions = true
            }));

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mvp24Hours.MongoDb.Events",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var settings = new MongoClientSettings();
        instrumentation.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().NotBeNull();

        int requestId = 21;
        var command = new BsonDocument { { "find", "customers" }, { "filter", new BsonDocument() } };
        MongoDbEventTestHelper.InvokeEvent(instrumentation, "OnCommandStarted",
            MongoDbEventTestHelper.CreateCommandStartedEvent(requestId, "find", "testdb", command));

        instrumentation.PendingActivitiesCount.Should().Be(1);

        var reply = new BsonDocument
        {
            { "cursor", new BsonDocument("firstBatch", new BsonArray { new BsonDocument("id", 1) }) },
            { "nModified", 2L },
            { "nMatched", 2L }
        };
        MongoDbEventTestHelper.InvokeEvent(instrumentation, "OnCommandSucceeded",
            MongoDbEventTestHelper.CreateCommandSucceededEvent(requestId, "find", reply, TimeSpan.FromMilliseconds(5)));

        instrumentation.PendingActivitiesCount.Should().Be(0);
    }

    [Fact]
    public void MongoDbOpenTelemetryInstrumentation_TruncateStatement_ShouldTruncateLongStatements()
    {
        using var instrumentation = new MongoDbOpenTelemetryInstrumentation(
            Options.Create(new MongoDbObservabilityOptions
            {
                EnableOpenTelemetry = true,
                ActivitySourceName = "Mvp24Hours.MongoDb.Trunc",
                IncludeStatementInTrace = true
            }));

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mvp24Hours.MongoDb.Trunc",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var settings = new MongoClientSettings();
        instrumentation.ConfigureClusterBuilder(settings);

        string longValue = new('x', 5000);
        var command = new BsonDocument { { "find", "customers" }, { "payload", longValue } };
        MongoDbEventTestHelper.InvokeEvent(instrumentation, "OnCommandStarted",
            MongoDbEventTestHelper.CreateCommandStartedEvent(30, "find", "testdb", command));
        MongoDbEventTestHelper.InvokeEvent(instrumentation, "OnCommandSucceeded",
            MongoDbEventTestHelper.CreateCommandSucceededEvent(30, "find", new BsonDocument { { "n", 1 } }, TimeSpan.FromMilliseconds(1)));
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

    [Fact]
    public void MongoDbStructuredLogger_ShouldTrackCommandLifecycleWithMockClusterEvents()
    {
        var mockLogger = new Mock<ILogger<MongoDbStructuredLogger>>();
        using var structuredLogger = new MongoDbStructuredLogger(
            Options.Create(new MongoDbObservabilityOptions
            {
                EnableStructuredLogging = true,
                LogCommandParameters = true,
                LogResultCounts = true,
                SensitiveFields = ["password"]
            }),
            mockLogger.Object);

        var settings = new MongoClientSettings();
        structuredLogger.ConfigureClusterBuilder(settings);
        settings.ClusterConfigurator.Should().NotBeNull();

        int requestId = 42;
        var command = new BsonDocument
        {
            { "find", "customers" },
            { "filter", new BsonDocument("password", "secret-value") }
        };
        CommandStartedEvent startedEvent = MongoDbStructuredLoggerTestHelper.CreateCommandStartedEvent(requestId, "find", "mvp24hours_test", command);
        MongoDbStructuredLoggerTestHelper.InvokeStructuredLoggerEvent(structuredLogger, "OnCommandStarted", startedEvent);
        structuredLogger.PendingCommandsCount.Should().Be(1);

        var reply = new BsonDocument
        {
            { "cursor", new BsonDocument("firstBatch", new BsonArray { new BsonDocument("name", "Alice") }) }
        };
        CommandSucceededEvent succeededEvent = MongoDbStructuredLoggerTestHelper.CreateCommandSucceededEvent(requestId, "find", reply, TimeSpan.FromMilliseconds(12));
        MongoDbStructuredLoggerTestHelper.InvokeStructuredLoggerEvent(structuredLogger, "OnCommandSucceeded", succeededEvent);
        structuredLogger.PendingCommandsCount.Should().Be(0);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("find", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbStructuredLoggerIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ConfigureClusterBuilder_WithRealClient_ShouldLogPingCommand()
    {
        var mockLogger = new Mock<ILogger<MongoDbStructuredLogger>>();
        using var structuredLogger = new MongoDbStructuredLogger(
            Options.Create(new MongoDbObservabilityOptions
            {
                EnableStructuredLogging = true,
                LogResultCounts = true
            }),
            mockLogger.Object);

        var settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
        structuredLogger.ConfigureClusterBuilder(settings);

        var client = new MongoClient(settings);
        BsonDocument pingResult = await client
            .GetDatabase(fixture.DatabaseName)
            .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        pingResult["ok"].AsDouble.Should().Be(1);
        structuredLogger.PendingCommandsCount.Should().Be(0);

        mockLogger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

internal static class MongoDbStructuredLoggerTestHelper
{
    internal static void InvokeStructuredLoggerEvent(MongoDbStructuredLogger logger, string methodName, object eventArg)
    {
        MethodInfo? method = typeof(MongoDbStructuredLogger).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(logger, [eventArg]);
    }

    internal static CommandStartedEvent CreateCommandStartedEvent(
        int requestId,
        string commandName,
        string databaseName,
        BsonDocument command)
    {
        var clusterId = new ClusterId(1);
        var connectionId = new ConnectionId(new ServerId(clusterId, new DnsEndPoint("localhost", 27017)), 1);
        int operationId = 99;

        return (CommandStartedEvent)RuntimeHelpers.GetUninitializedObject(typeof(CommandStartedEvent))
            .Also(e =>
            {
                SetField(e, "_commandName", commandName);
                SetField(e, "_databaseNamespace", new DatabaseNamespace(databaseName));
                SetField(e, "_command", command);
                SetField(e, "_requestId", requestId);
                SetField(e, "_operationId", (long?)operationId);
                SetField(e, "_connectionId", connectionId);
            });
    }

    internal static CommandSucceededEvent CreateCommandSucceededEvent(
        int requestId,
        string commandName,
        BsonDocument reply,
        TimeSpan duration)
    {
        var clusterId = new ClusterId(1);
        var connectionId = new ConnectionId(new ServerId(clusterId, new DnsEndPoint("localhost", 27017)), 1);
        int operationId = 99;

        return (CommandSucceededEvent)RuntimeHelpers.GetUninitializedObject(typeof(CommandSucceededEvent))
            .Also(e =>
            {
                SetField(e, "_commandName", commandName);
                SetField(e, "_reply", reply);
                SetField(e, "_duration", duration);
                SetField(e, "_requestId", requestId);
                SetField(e, "_operationId", (long?)operationId);
                SetField(e, "_connectionId", connectionId);
            });
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        field!.SetValue(target, value);
    }

    private static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}

internal static class MongoDbEventTestHelper
{
    internal static void InvokeEvent(object target, string methodName, object eventArg)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(target, [eventArg]);
    }

    internal static CommandStartedEvent CreateCommandStartedEvent(
        int requestId,
        string commandName,
        string databaseName,
        BsonDocument command)
    {
        return MongoDbStructuredLoggerTestHelper.CreateCommandStartedEvent(requestId, commandName, databaseName, command);
    }

    internal static CommandSucceededEvent CreateCommandSucceededEvent(
        int requestId,
        string commandName,
        BsonDocument reply,
        TimeSpan duration)
    {
        return MongoDbStructuredLoggerTestHelper.CreateCommandSucceededEvent(requestId, commandName, reply, duration);
    }

    internal static CommandFailedEvent CreateCommandFailedEvent(
        int requestId,
        string commandName,
        Exception failure,
        TimeSpan duration)
    {
        var clusterId = new ClusterId(1);
        var connectionId = new ConnectionId(new ServerId(clusterId, new DnsEndPoint("localhost", 27017)), 1);

        return (CommandFailedEvent)RuntimeHelpers.GetUninitializedObject(typeof(CommandFailedEvent))
            .Also(e =>
            {
                SetField(e, "_commandName", commandName);
                SetField(e, "_failure", failure);
                SetField(e, "_duration", duration);
                SetField(e, "_requestId", requestId);
                SetField(e, "_operationId", (long?)99);
                SetField(e, "_connectionId", connectionId);
            });
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull($"Field '{fieldName}' was not found on {target.GetType().Name}.");
        field!.SetValue(target, value);
    }

    private static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
