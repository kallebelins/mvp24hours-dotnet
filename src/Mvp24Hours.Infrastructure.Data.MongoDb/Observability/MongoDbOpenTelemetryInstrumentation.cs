//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Provides OpenTelemetry instrumentation for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component creates distributed traces for MongoDB commands using
    /// System.Diagnostics.Activity, which integrates with OpenTelemetry exporters.
    /// Features include:
    /// <list type="bullet">
    ///   <item>Automatic span creation for all MongoDB commands</item>
    ///   <item>Database semantic conventions compliance</item>
    ///   <item>Error recording with exception details</item>
    ///   <item>Optional statement capture (with sensitive data considerations)</item>
    ///   <item>Baggage propagation for correlation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register OpenTelemetry with MongoDB instrumentation
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing =>
    ///     {
    ///         tracing.AddSource("Mvp24Hours.MongoDb");
    ///         tracing.AddOtlpExporter();
    ///     });
    /// 
    /// // Configure MongoDB to use instrumentation
    /// services.AddMongoDbObservability(options =>
    /// {
    ///     options.EnableOpenTelemetry = true;
    ///     options.ActivitySourceName = "Mvp24Hours.MongoDb";
    /// });
    /// </code>
    /// </example>
    public class MongoDbOpenTelemetryInstrumentation : IDisposable
    {
        private readonly MongoDbObservabilityOptions _options;
        private readonly ActivitySource _activitySource;
        private readonly ConcurrentDictionary<int, Activity> _pendingActivities = new();

        // OpenTelemetry semantic conventions for databases
        private const string DbSystem = "db.system";
        private const string DbName = "db.name";
        private const string DbOperation = "db.operation";
        private const string DbStatement = "db.statement";
        private const string DbMongoDbCollection = "db.mongodb.collection";
        private const string NetPeerName = "net.peer.name";
        private const string NetPeerPort = "net.peer.port";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbOpenTelemetryInstrumentation"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        public MongoDbOpenTelemetryInstrumentation(IOptions<MongoDbObservabilityOptions> options)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
            _activitySource = new ActivitySource(_options.ActivitySourceName, "1.0.0");
        }

        /// <summary>
        /// Gets the activity source for this instrumentation.
        /// </summary>
        public ActivitySource ActivitySource => _activitySource;

        /// <summary>
        /// Configures the MongoDB cluster builder to enable OpenTelemetry tracing.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClusterBuilder(MongoClientSettings settings)
        {
            if (!_options.EnableOpenTelemetry)
                return;

            var existingConfigurator = settings.ClusterConfigurator;

            settings.ClusterConfigurator = builder =>
            {
                // Call existing configurator if any
                existingConfigurator?.Invoke(builder);

                // Subscribe to command events
                builder.Subscribe<CommandStartedEvent>(OnCommandStarted);
                builder.Subscribe<CommandSucceededEvent>(OnCommandSucceeded);
                builder.Subscribe<CommandFailedEvent>(OnCommandFailed);
            };
        }

        private void OnCommandStarted(CommandStartedEvent e)
        {
            var spanName = $"MongoDB.{e.CommandName}";

            var activity = _activitySource.StartActivity(
                spanName,
                ActivityKind.Client,
                Activity.Current?.Context ?? default);

            if (activity == null)
                return;

            // Set standard database tags (OpenTelemetry semantic conventions)
            activity.SetTag(DbSystem, "mongodb");
            activity.SetTag(DbName, e.DatabaseNamespace?.DatabaseName);
            activity.SetTag(DbOperation, e.CommandName);

            // Set MongoDB-specific tags
            var collectionName = ExtractCollectionName(e.Command, e.CommandName);
            if (!string.IsNullOrEmpty(collectionName))
            {
                activity.SetTag(DbMongoDbCollection, collectionName);
            }

            // Set connection information
            if (e.ConnectionId?.ServerId?.EndPoint != null)
            {
                var endpoint = e.ConnectionId.ServerId.EndPoint.ToString();
                var parts = endpoint.Split(':');
                if (parts.Length >= 1)
                {
                    activity.SetTag(NetPeerName, parts[0]);
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var port))
                    {
                        activity.SetTag(NetPeerPort, port);
                    }
                }
            }

            // Optionally include the statement (be careful with sensitive data)
            if (_options.IncludeStatementInTrace && e.Command != null)
            {
                var statement = TruncateStatement(e.Command.ToString());
                activity.SetTag(DbStatement, statement);
            }

            // Add custom tags
            foreach (var tag in _options.AdditionalTraceTags)
            {
                var parts = tag.Split('=');
                if (parts.Length == 2)
                {
                    activity.SetTag(parts[0], parts[1]);
                }
            }

            // Add service info
            if (!string.IsNullOrEmpty(_options.ServiceName))
            {
                activity.SetTag("service.name", _options.ServiceName);
            }
            if (!string.IsNullOrEmpty(_options.Environment))
            {
                activity.SetTag("deployment.environment", _options.Environment);
            }

            _pendingActivities.TryAdd(e.RequestId, activity);
        }

        private void OnCommandSucceeded(CommandSucceededEvent e)
        {
            if (!_pendingActivities.TryRemove(e.RequestId, out var activity))
                return;

            try
            {
                // Add result metadata
                if (e.Reply != null)
                {
                    AddResultTags(activity, e.Reply);
                }

                activity.SetStatus(ActivityStatusCode.Ok);
            }
            finally
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        private void OnCommandFailed(CommandFailedEvent e)
        {
            if (!_pendingActivities.TryRemove(e.RequestId, out var activity))
                return;

            try
            {
                activity.SetStatus(ActivityStatusCode.Error, e.Failure?.Message);

                if (_options.RecordExceptions && e.Failure != null)
                {
                    activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                    {
                        { "exception.type", e.Failure.GetType().FullName },
                        { "exception.message", e.Failure.Message },
                        { "exception.stacktrace", e.Failure.StackTrace }
                    }));
                }
            }
            finally
            {
                activity.Stop();
                activity.Dispose();
            }
        }

        private static void AddResultTags(Activity activity, BsonDocument reply)
        {
            // Add document counts if available
            if (reply.Contains("cursor"))
            {
                var cursor = reply["cursor"].AsBsonDocument;
                if (cursor.Contains("firstBatch"))
                {
                    activity.SetTag("db.mongodb.documents_returned", cursor["firstBatch"].AsBsonArray.Count);
                }
            }
            else if (reply.Contains("n"))
            {
                activity.SetTag("db.mongodb.documents_affected", reply["n"].ToInt64());
            }

            // Add modified count for updates
            if (reply.Contains("nModified"))
            {
                activity.SetTag("db.mongodb.documents_modified", reply["nModified"].ToInt64());
            }

            // Add matched count
            if (reply.Contains("nMatched"))
            {
                activity.SetTag("db.mongodb.documents_matched", reply["nMatched"].ToInt64());
            }

            // Check for errors in reply
            if (reply.Contains("ok") && reply["ok"].ToDouble() != 1.0)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Command returned ok: 0");
            }
        }

        private static string ExtractCollectionName(BsonDocument command, string commandName)
        {
            if (command == null)
                return null;

            var collectionCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "find", "insert", "update", "delete", "aggregate", "count",
                "distinct", "findAndModify", "mapReduce", "createIndexes",
                "dropIndexes", "listIndexes", "findOneAndDelete",
                "findOneAndReplace", "findOneAndUpdate"
            };

            if (collectionCommands.Contains(commandName) && command.Contains(commandName))
            {
                var value = command[commandName];
                if (value.IsString)
                {
                    return value.AsString;
                }
            }

            return null;
        }

        private string TruncateStatement(string statement)
        {
            if (string.IsNullOrEmpty(statement))
                return statement;

            const int maxLength = 4096;
            return statement.Length > maxLength
                ? statement.Substring(0, maxLength) + "...[TRUNCATED]"
                : statement;
        }

        /// <summary>
        /// Creates a custom activity for a MongoDB operation.
        /// </summary>
        /// <param name="operationName">The operation name.</param>
        /// <param name="databaseName">The database name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The created activity, or null if not sampling.</returns>
        public Activity StartOperation(string operationName, string databaseName = null, string collectionName = null)
        {
            var activity = _activitySource.StartActivity(
                $"MongoDB.{operationName}",
                ActivityKind.Client);

            if (activity == null)
                return null;

            activity.SetTag(DbSystem, "mongodb");
            if (!string.IsNullOrEmpty(databaseName))
                activity.SetTag(DbName, databaseName);
            if (!string.IsNullOrEmpty(collectionName))
                activity.SetTag(DbMongoDbCollection, collectionName);
            activity.SetTag(DbOperation, operationName);

            return activity;
        }

        /// <summary>
        /// Adds tags to the current activity.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        public static void AddTags(params (string Key, object Value)[] tags)
        {
            var activity = Activity.Current;
            if (activity == null)
                return;

            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        /// <summary>
        /// Records an exception in the current activity.
        /// </summary>
        /// <param name="exception">The exception to record.</param>
        public static void RecordException(Exception exception)
        {
            var activity = Activity.Current;
            if (activity == null || exception == null)
                return;

            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.StackTrace }
            }));
        }

        /// <summary>
        /// Gets the number of pending activities.
        /// </summary>
        public int PendingActivitiesCount => _pendingActivities.Count;

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var activity in _pendingActivities.Values)
            {
                activity.Stop();
                activity.Dispose();
            }
            _pendingActivities.Clear();
            _activitySource.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

