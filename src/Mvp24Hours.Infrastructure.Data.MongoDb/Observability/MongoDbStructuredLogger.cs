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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Provides structured logging for MongoDB operations with parameter masking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Features include:
    /// <list type="bullet">
    ///   <item>Structured log output with named parameters</item>
    ///   <item>Automatic sensitive data masking</item>
    ///   <item>Configurable verbosity levels</item>
    ///   <item>Command parameter logging (development mode)</item>
    ///   <item>Result count logging</item>
    ///   <item>Duration and timing information</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with DI
    /// services.AddMongoDbObservability(options =>
    /// {
    ///     options.EnableStructuredLogging = true;
    ///     options.LogCommandParameters = true; // Development only!
    ///     options.LogResultCounts = true;
    ///     options.SensitiveFields = new[] { "password", "token", "cpf" };
    /// });
    /// 
    /// // Output example:
    /// // [MongoDB] find on customers completed in 5.23ms (returned: 10)
    /// // [MongoDB] insert on orders completed in 2.15ms (inserted: 1)
    /// </code>
    /// </example>
    public class MongoDbStructuredLogger : IDisposable
    {
        private readonly MongoDbObservabilityOptions _options;
        private readonly ILogger<MongoDbStructuredLogger> _logger;
        private readonly ConcurrentDictionary<int, CommandLogContext> _pendingCommands = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbStructuredLogger"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        /// <param name="logger">Optional ILogger for output.</param>
        public MongoDbStructuredLogger(
            IOptions<MongoDbObservabilityOptions> options,
            ILogger<MongoDbStructuredLogger> logger = null)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
            _logger = logger;
        }

        /// <summary>
        /// Configures the MongoDB cluster builder to enable structured logging.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClusterBuilder(MongoClientSettings settings)
        {
            if (!_options.EnableStructuredLogging)
                return;

            var existingConfigurator = settings.ClusterConfigurator;

            settings.ClusterConfigurator = builder =>
            {
                existingConfigurator?.Invoke(builder);

                builder.Subscribe<CommandStartedEvent>(OnCommandStarted);
                builder.Subscribe<CommandSucceededEvent>(OnCommandSucceeded);
                builder.Subscribe<CommandFailedEvent>(OnCommandFailed);
            };
        }

        private void OnCommandStarted(CommandStartedEvent e)
        {
            var context = new CommandLogContext
            {
                RequestId = e.RequestId,
                CommandName = e.CommandName,
                DatabaseName = e.DatabaseNamespace?.DatabaseName,
                CollectionName = ExtractCollectionName(e.Command, e.CommandName),
                Stopwatch = Stopwatch.StartNew(),
                Timestamp = DateTimeOffset.UtcNow
            };

            if (_options.LogCommandParameters)
            {
                context.Command = MaskSensitiveData(e.Command);
            }

            _pendingCommands.TryAdd(e.RequestId, context);

            LogCommandStart(context);
        }

        private void OnCommandSucceeded(CommandSucceededEvent e)
        {
            if (!_pendingCommands.TryRemove(e.RequestId, out var context))
                return;

            context.Stopwatch.Stop();
            context.Duration = context.Stopwatch.Elapsed;
            context.Success = true;

            if (_options.LogResultCounts)
            {
                ExtractResultCounts(e.Reply, context);
            }

            LogCommandEnd(context);
        }

        private void OnCommandFailed(CommandFailedEvent e)
        {
            if (!_pendingCommands.TryRemove(e.RequestId, out var context))
                return;

            context.Stopwatch.Stop();
            context.Duration = context.Stopwatch.Elapsed;
            context.Success = false;
            context.ErrorMessage = e.Failure?.Message;
            context.ErrorType = e.Failure?.GetType().Name;

            LogCommandEnd(context);
        }

        private void LogCommandStart(CommandLogContext context)
        {
            var log = new StructuredLog
            {
                Level = "Debug",
                Category = "MongoDB",
                EventName = "CommandStarted",
                Timestamp = context.Timestamp,
                Properties = new Dictionary<string, object>
                {
                    ["CommandName"] = context.CommandName,
                    ["Database"] = context.DatabaseName,
                    ["Collection"] = context.CollectionName
                }
            };

            if (_options.LogCommandParameters && context.Command != null)
            {
                var commandStr = TruncateString(context.Command.ToString(), _options.MaxLogMessageLength);
                log.Properties["Command"] = commandStr;
            }

            WriteLog(log);
        }

        private void LogCommandEnd(CommandLogContext context)
        {
            var log = new StructuredLog
            {
                Level = context.Success ? "Information" : "Error",
                Category = "MongoDB",
                EventName = context.Success ? "CommandSucceeded" : "CommandFailed",
                Timestamp = DateTimeOffset.UtcNow,
                Properties = new Dictionary<string, object>
                {
                    ["CommandName"] = context.CommandName,
                    ["Database"] = context.DatabaseName,
                    ["Collection"] = context.CollectionName,
                    ["DurationMs"] = context.Duration.TotalMilliseconds,
                    ["Success"] = context.Success
                }
            };

            if (!context.Success)
            {
                log.Properties["ErrorType"] = context.ErrorType;
                log.Properties["ErrorMessage"] = context.ErrorMessage;
            }

            if (_options.LogResultCounts)
            {
                if (context.DocumentsReturned.HasValue)
                    log.Properties["DocumentsReturned"] = context.DocumentsReturned.Value;
                if (context.DocumentsAffected.HasValue)
                    log.Properties["DocumentsAffected"] = context.DocumentsAffected.Value;
                if (context.DocumentsModified.HasValue)
                    log.Properties["DocumentsModified"] = context.DocumentsModified.Value;
            }

            WriteLog(log);
        }

        private void WriteLog(StructuredLog log)
        {
            if (_logger != null)
            {
                var message = BuildLogMessage(log);
                var level = log.Level switch
                {
                    "Error" => LogLevel.Error,
                    "Warning" => LogLevel.Warning,
                    "Information" => LogLevel.Information,
                    _ => LogLevel.Debug
                };

                // Create structured log with all properties
                using (_logger.BeginScope(log.Properties))
                {
                    _logger.Log(level, "[{Category}] {Message}", log.Category, message);
                }
            }
        }

        private static string BuildLogMessage(StructuredLog log)
        {
            var sb = new StringBuilder();

            if (log.Properties.TryGetValue("CommandName", out var cmd))
            {
                sb.Append(cmd);
            }

            if (log.Properties.TryGetValue("Collection", out var col) && col != null)
            {
                sb.Append(" on ").Append(col);
            }

            if (log.EventName == "CommandSucceeded" || log.EventName == "CommandFailed")
            {
                sb.Append(log.EventName == "CommandSucceeded" ? " completed" : " FAILED");

                if (log.Properties.TryGetValue("DurationMs", out var duration))
                {
                    sb.Append($" in {duration:F2}ms");
                }

                // Add result counts
                var counts = new List<string>();
                if (log.Properties.TryGetValue("DocumentsReturned", out var returned))
                    counts.Add($"returned: {returned}");
                if (log.Properties.TryGetValue("DocumentsAffected", out var affected))
                    counts.Add($"affected: {affected}");
                if (log.Properties.TryGetValue("DocumentsModified", out var modified))
                    counts.Add($"modified: {modified}");

                if (counts.Count > 0)
                {
                    sb.Append($" ({string.Join(", ", counts)})");
                }

                // Add error info
                if (log.Properties.TryGetValue("ErrorMessage", out var error))
                {
                    sb.Append($" - {error}");
                }
            }

            return sb.ToString();
        }

        private BsonDocument MaskSensitiveData(BsonDocument document)
        {
            if (document == null || _options.SensitiveFields == null || _options.SensitiveFields.Length == 0)
                return document;

            try
            {
                var clone = document.DeepClone().AsBsonDocument;
                MaskDocument(clone, _options.SensitiveFields);
                return clone;
            }
            catch
            {
                return document;
            }
        }

        private static void MaskDocument(BsonDocument document, string[] sensitiveFields)
        {
            foreach (var element in document.Elements.ToArray())
            {
                var name = element.Name.ToLowerInvariant();

                // Check if this field is sensitive
                foreach (var sensitive in sensitiveFields)
                {
                    if (name.Contains(sensitive.ToLowerInvariant()))
                    {
                        document[element.Name] = "***MASKED***";
                        break;
                    }
                }

                // Recursively check nested documents
                if (element.Value.IsBsonDocument)
                {
                    MaskDocument(element.Value.AsBsonDocument, sensitiveFields);
                }
                else if (element.Value.IsBsonArray)
                {
                    foreach (var item in element.Value.AsBsonArray)
                    {
                        if (item.IsBsonDocument)
                        {
                            MaskDocument(item.AsBsonDocument, sensitiveFields);
                        }
                    }
                }
            }
        }

        private static void ExtractResultCounts(BsonDocument reply, CommandLogContext context)
        {
            if (reply == null)
                return;

            // For find commands
            if (reply.Contains("cursor"))
            {
                var cursor = reply["cursor"].AsBsonDocument;
                if (cursor.Contains("firstBatch"))
                {
                    context.DocumentsReturned = cursor["firstBatch"].AsBsonArray.Count;
                }
            }

            // For count/distinct
            if (reply.Contains("n"))
            {
                context.DocumentsAffected = reply["n"].ToInt64();
            }

            // For updates
            if (reply.Contains("nModified"))
            {
                context.DocumentsModified = reply["nModified"].ToInt64();
            }

            // For inserts
            if (reply.Contains("insertedCount"))
            {
                context.DocumentsAffected = reply["insertedCount"].ToInt64();
            }

            // For deletes
            if (reply.Contains("deletedCount"))
            {
                context.DocumentsAffected = reply["deletedCount"].ToInt64();
            }
        }

        private static string ExtractCollectionName(BsonDocument command, string commandName)
        {
            if (command == null)
                return null;

            var collectionCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "find", "insert", "update", "delete", "aggregate", "count",
                "distinct", "findAndModify", "createIndexes", "dropIndexes"
            };

            if (collectionCommands.Contains(commandName) && command.Contains(commandName))
            {
                var value = command[commandName];
                if (value.IsString)
                    return value.AsString;
            }

            return null;
        }

        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...[TRUNCATED]";
        }

        /// <summary>
        /// Gets the number of pending commands being logged.
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

        private class CommandLogContext
        {
            public int RequestId { get; set; }
            public string CommandName { get; set; }
            public string DatabaseName { get; set; }
            public string CollectionName { get; set; }
            public BsonDocument Command { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public TimeSpan Duration { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorType { get; set; }
            public long? DocumentsReturned { get; set; }
            public long? DocumentsAffected { get; set; }
            public long? DocumentsModified { get; set; }
        }

        private class StructuredLog
        {
            public string Level { get; set; }
            public string Category { get; set; }
            public string EventName { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public Dictionary<string, object> Properties { get; set; }
        }

        #endregion
    }
}

