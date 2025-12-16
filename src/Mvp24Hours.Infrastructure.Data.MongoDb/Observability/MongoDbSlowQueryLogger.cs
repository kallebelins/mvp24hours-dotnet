//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Provides slow query detection and logging for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component monitors MongoDB commands and logs queries that exceed a configurable threshold.
    /// Features include:
    /// <list type="bullet">
    ///   <item>Configurable slow query threshold</item>
    ///   <item>Rate limiting to prevent log flooding</item>
    ///   <item>Optional explain output for slow queries</item>
    ///   <item>Sensitive data masking</item>
    ///   <item>Integration with ILogger and TelemetryHelper</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddSingleton&lt;MongoDbSlowQueryLogger&gt;();
    /// 
    /// // Configure MongoDB client to use the logger
    /// var logger = serviceProvider.GetRequiredService&lt;MongoDbSlowQueryLogger&gt;();
    /// var settings = MongoClientSettings.FromConnectionString(connectionString);
    /// logger.ConfigureClusterBuilder(settings);
    /// var client = new MongoClient(settings);
    /// 
    /// // Or use the extension method
    /// services.AddMongoDbObservability(options =>
    /// {
    ///     options.EnableSlowQueryLogging = true;
    ///     options.SlowQueryThreshold = TimeSpan.FromSeconds(1);
    /// });
    /// </code>
    /// </example>
    public class MongoDbSlowQueryLogger : IDisposable
    {
        private readonly ILogger<MongoDbSlowQueryLogger> _logger;
        private readonly MongoDbObservabilityOptions _options;
        private readonly IMongoDbMetrics _metrics;

        // Track command start times
        private readonly ConcurrentDictionary<int, CommandInfo> _pendingCommands = new();

        // Rate limiting
        private int _slowQueryCount;
        private DateTime _rateLimitResetTime = DateTime.UtcNow.AddMinutes(1);
        private readonly object _rateLimitLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbSlowQueryLogger"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        /// <param name="logger">Optional logger for structured logging.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        public MongoDbSlowQueryLogger(
            IOptions<MongoDbObservabilityOptions> options,
            ILogger<MongoDbSlowQueryLogger> logger = null,
            IMongoDbMetrics metrics = null)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Configures the MongoDB cluster builder to subscribe to command events.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClusterBuilder(MongoClientSettings settings)
        {
            if (!_options.EnableSlowQueryLogging)
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
            var info = new CommandInfo
            {
                CommandName = e.CommandName,
                DatabaseName = e.DatabaseNamespace?.DatabaseName,
                CollectionName = ExtractCollectionName(e.Command, e.CommandName),
                Stopwatch = Stopwatch.StartNew(),
                Command = _options.LogSlowQueryFilter ? e.Command : null
            };

            _pendingCommands.TryAdd(e.RequestId, info);
        }

        private void OnCommandSucceeded(CommandSucceededEvent e)
        {
            if (!_pendingCommands.TryRemove(e.RequestId, out var info))
                return;

            info.Stopwatch.Stop();
            var duration = info.Stopwatch.Elapsed;

            // Record metrics
            _metrics?.RecordCommandDuration(info.CommandName, info.CollectionName, duration, true);

            // Check for slow query
            if (duration >= _options.SlowQueryThreshold)
            {
                LogSlowQuery(info, duration, e.Reply);
            }
        }

        private void OnCommandFailed(CommandFailedEvent e)
        {
            if (!_pendingCommands.TryRemove(e.RequestId, out var info))
                return;

            info.Stopwatch.Stop();
            var duration = info.Stopwatch.Elapsed;

            // Record metrics
            _metrics?.RecordCommandDuration(info.CommandName, info.CollectionName, duration, false);
            _metrics?.RecordError(info.CommandName, info.CollectionName, e.Failure?.GetType().Name ?? "Unknown");

            // Log the failure
            LogCommandFailure(info, duration, e.Failure);
        }

        private void LogSlowQuery(CommandInfo info, TimeSpan duration, BsonDocument reply)
        {
            // Rate limiting check
            if (!ShouldLogSlowQuery())
                return;

            // Extract documents examined/returned if available
            long documentsExamined = 0;
            long documentsReturned = 0;

            if (reply != null)
            {
                if (reply.Contains("cursor"))
                {
                    var cursor = reply["cursor"].AsBsonDocument;
                    if (cursor.Contains("firstBatch"))
                    {
                        documentsReturned = cursor["firstBatch"].AsBsonArray.Count;
                    }
                }
                else if (reply.Contains("n"))
                {
                    documentsReturned = reply["n"].ToInt64();
                }
            }

            // Record slow query metrics
            _metrics?.RecordSlowQuery(info.CommandName, info.CollectionName, duration, documentsExamined, documentsReturned);

            // Build log message
            var details = new SlowQueryDetails
            {
                CommandName = info.CommandName,
                DatabaseName = info.DatabaseName,
                CollectionName = info.CollectionName,
                DurationMs = duration.TotalMilliseconds,
                ThresholdMs = _options.SlowQueryThreshold.TotalMilliseconds,
                DocumentsReturned = documentsReturned,
                Timestamp = DateTimeOffset.UtcNow
            };

            if (_options.LogSlowQueryFilter && info.Command != null)
            {
                details.Filter = MaskSensitiveData(info.Command.ToString());
            }

            // Log using ILogger if available
            if (_logger != null)
            {
                _logger.LogWarning(
                    "⚠️ SLOW MONGODB QUERY DETECTED - Command: {CommandName}, Collection: {Collection}, " +
                    "Duration: {DurationMs:F2}ms (Threshold: {ThresholdMs}ms), Documents: {DocumentsReturned}",
                    details.CommandName,
                    details.CollectionName ?? "N/A",
                    details.DurationMs,
                    details.ThresholdMs,
                    details.DocumentsReturned);

                if (!string.IsNullOrEmpty(details.Filter))
                {
                    _logger.LogWarning("Slow query filter: {Filter}", details.Filter);
                }
            }
            else
            {
                TelemetryHelper.Execute(
                    TelemetryLevels.Warning,
                    "mongodb-slow-query",
                    details);
            }
        }

        private void LogCommandFailure(CommandInfo info, TimeSpan duration, Exception exception)
        {
            var details = new CommandFailureDetails
            {
                CommandName = info.CommandName,
                DatabaseName = info.DatabaseName,
                CollectionName = info.CollectionName,
                DurationMs = duration.TotalMilliseconds,
                ErrorMessage = exception?.Message,
                ErrorType = exception?.GetType().Name,
                Timestamp = DateTimeOffset.UtcNow
            };

            if (_logger != null)
            {
                _logger.LogError(
                    exception,
                    "❌ MONGODB COMMAND FAILED - Command: {CommandName}, Collection: {Collection}, " +
                    "Duration: {DurationMs:F2}ms, Error: {ErrorMessage}",
                    details.CommandName,
                    details.CollectionName ?? "N/A",
                    details.DurationMs,
                    details.ErrorMessage);
            }
            else
            {
                TelemetryHelper.Execute(
                    TelemetryLevels.Error,
                    "mongodb-command-failed",
                    details);
            }
        }

        private bool ShouldLogSlowQuery()
        {
            lock (_rateLimitLock)
            {
                var now = DateTime.UtcNow;

                // Reset counter if window has passed
                if (now >= _rateLimitResetTime)
                {
                    _slowQueryCount = 0;
                    _rateLimitResetTime = now.AddMinutes(1);
                }

                // Check if we're under the limit
                if (_slowQueryCount >= _options.MaxSlowQueriesPerMinute)
                {
                    return false;
                }

                _slowQueryCount++;
                return true;
            }
        }

        private static string ExtractCollectionName(BsonDocument command, string commandName)
        {
            if (command == null)
                return null;

            // Most commands have the collection name as the first value
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

        private string MaskSensitiveData(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = input;
            foreach (var field in _options.SensitiveFields)
            {
                // Simple masking - replace values after sensitive field names
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    $@"(""{field}""\s*:\s*)""[^""]*""",
                    $@"$1""***MASKED***""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Gets the current slow query count for the current minute window.
        /// </summary>
        public int CurrentSlowQueryCount => _slowQueryCount;

        /// <summary>
        /// Gets the number of pending commands being tracked.
        /// </summary>
        public int PendingCommandsCount => _pendingCommands.Count;

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            _pendingCommands.Clear();
            GC.SuppressFinalize(this);
        }

        #region Internal Classes

        private class CommandInfo
        {
            public string CommandName { get; set; }
            public string DatabaseName { get; set; }
            public string CollectionName { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public BsonDocument Command { get; set; }
        }

        /// <summary>
        /// Details about a slow query for logging.
        /// </summary>
        public class SlowQueryDetails
        {
            public string CommandName { get; set; }
            public string DatabaseName { get; set; }
            public string CollectionName { get; set; }
            public double DurationMs { get; set; }
            public double ThresholdMs { get; set; }
            public long DocumentsReturned { get; set; }
            public string Filter { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }

        /// <summary>
        /// Details about a command failure for logging.
        /// </summary>
        public class CommandFailureDetails
        {
            public string CommandName { get; set; }
            public string DatabaseName { get; set; }
            public string CollectionName { get; set; }
            public double DurationMs { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorType { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }

        #endregion
    }
}

