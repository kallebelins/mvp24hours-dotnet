//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Tracks command and query durations for MongoDB operations with percentile calculations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides detailed duration tracking including:
    /// <list type="bullet">
    ///   <item>Per-command type duration statistics</item>
    ///   <item>Percentile calculations (p50, p95, p99)</item>
    ///   <item>Sliding window aggregation</item>
    ///   <item>Min/max/average calculations</item>
    ///   <item>Standard deviation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tracker = serviceProvider.GetRequiredService&lt;MongoDbDurationTracker&gt;();
    /// 
    /// // Get statistics for find commands
    /// var findStats = tracker.GetStatistics("find");
    /// Console.WriteLine($"Find P95: {findStats.P95Ms}ms");
    /// 
    /// // Get all statistics
    /// var allStats = tracker.GetAllStatistics();
    /// foreach (var stat in allStats)
    /// {
    ///     Console.WriteLine($"{stat.CommandName}: Avg={stat.AverageMs}ms, P99={stat.P99Ms}ms");
    /// }
    /// </code>
    /// </example>
    public class MongoDbDurationTracker : IDisposable
    {
        private readonly MongoDbObservabilityOptions _options;
        private readonly ConcurrentDictionary<int, (string CommandName, string Collection, Stopwatch Watch)> _pending = new();
        private readonly ConcurrentDictionary<string, DurationBucket> _commandBuckets = new();
        private readonly object _cleanupLock = new();
        private DateTime _lastCleanup = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbDurationTracker"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        public MongoDbDurationTracker(IOptions<MongoDbObservabilityOptions> options)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
        }

        /// <summary>
        /// Configures the MongoDB cluster builder to track command durations.
        /// </summary>
        /// <param name="settings">The MongoDB client settings.</param>
        public void ConfigureClusterBuilder(MongoClientSettings settings)
        {
            if (!_options.EnableDurationTracking)
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
            if (!_options.TrackIndividualOperations)
                return;

            var collection = ExtractCollectionName(e.Command, e.CommandName);
            _pending.TryAdd(e.RequestId, (e.CommandName, collection, Stopwatch.StartNew()));
        }

        private void OnCommandSucceeded(CommandSucceededEvent e)
        {
            if (!_pending.TryRemove(e.RequestId, out var info))
                return;

            info.Watch.Stop();
            RecordDuration(info.CommandName, info.Collection, info.Watch.Elapsed, true);
        }

        private void OnCommandFailed(CommandFailedEvent e)
        {
            if (!_pending.TryRemove(e.RequestId, out var info))
                return;

            info.Watch.Stop();
            RecordDuration(info.CommandName, info.Collection, info.Watch.Elapsed, false);
        }

        /// <summary>
        /// Records a duration for a command.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="success">Whether the command succeeded.</param>
        public void RecordDuration(string commandName, string collectionName, TimeSpan duration, bool success)
        {
            var bucket = _commandBuckets.GetOrAdd(commandName, _ => new DurationBucket(
                _options.DurationHistogramBuckets,
                _options.DurationAggregationWindow));

            bucket.Record(duration, success, collectionName);

            // Periodic cleanup
            TryCleanup();
        }

        /// <summary>
        /// Gets duration statistics for a specific command type.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <returns>The duration statistics, or null if no data.</returns>
        public DurationStatistics GetStatistics(string commandName)
        {
            if (!_commandBuckets.TryGetValue(commandName, out var bucket))
                return null;

            return bucket.GetStatistics(commandName);
        }

        /// <summary>
        /// Gets duration statistics for all tracked command types.
        /// </summary>
        /// <returns>A list of duration statistics.</returns>
        public IReadOnlyList<DurationStatistics> GetAllStatistics()
        {
            return _commandBuckets
                .Select(kvp => kvp.Value.GetStatistics(kvp.Key))
                .Where(s => s != null && s.Count > 0)
                .ToList();
        }

        /// <summary>
        /// Gets a summary of all tracked commands.
        /// </summary>
        /// <returns>A summary dictionary.</returns>
        public IReadOnlyDictionary<string, CommandSummary> GetSummary()
        {
            return _commandBuckets.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetSummary());
        }

        /// <summary>
        /// Resets all tracking data.
        /// </summary>
        public void Reset()
        {
            _commandBuckets.Clear();
            _pending.Clear();
        }

        private void TryCleanup()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanup < _options.DurationAggregationWindow)
                return;

            lock (_cleanupLock)
            {
                if (now - _lastCleanup < _options.DurationAggregationWindow)
                    return;

                _lastCleanup = now;

                foreach (var bucket in _commandBuckets.Values)
                {
                    bucket.Cleanup(_options.DurationAggregationWindow);
                }
            }
        }

        private static string ExtractCollectionName(MongoDB.Bson.BsonDocument command, string commandName)
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

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            _pending.Clear();
            _commandBuckets.Clear();
            GC.SuppressFinalize(this);
        }

        #region Internal Classes

        private class DurationBucket
        {
            private readonly int _maxSamples;
            private readonly TimeSpan _window;
            private readonly ConcurrentQueue<DurationSample> _samples = new();
            private long _totalCount;
            private long _successCount;
            private long _failureCount;
            private readonly ConcurrentDictionary<string, long> _collectionCounts = new();

            public DurationBucket(int maxSamples, TimeSpan window)
            {
                _maxSamples = maxSamples * 1000; // Store more samples for better percentiles
                _window = window;
            }

            public void Record(TimeSpan duration, bool success, string collection)
            {
                var sample = new DurationSample
                {
                    DurationMs = duration.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow,
                    Success = success
                };

                _samples.Enqueue(sample);
                System.Threading.Interlocked.Increment(ref _totalCount);

                if (success)
                    System.Threading.Interlocked.Increment(ref _successCount);
                else
                    System.Threading.Interlocked.Increment(ref _failureCount);

                if (!string.IsNullOrEmpty(collection))
                {
                    _collectionCounts.AddOrUpdate(collection, 1, (_, count) => count + 1);
                }

                // Trim if too many samples
                while (_samples.Count > _maxSamples && _samples.TryDequeue(out _)) { }
            }

            public void Cleanup(TimeSpan window)
            {
                var cutoff = DateTime.UtcNow - window;
                while (_samples.TryPeek(out var sample) && sample.Timestamp < cutoff)
                {
                    _samples.TryDequeue(out _);
                }
            }

            public DurationStatistics GetStatistics(string commandName)
            {
                var samples = _samples.ToArray();
                if (samples.Length == 0)
                    return new DurationStatistics { CommandName = commandName };

                var durations = samples.Select(s => s.DurationMs).OrderBy(d => d).ToArray();

                return new DurationStatistics
                {
                    CommandName = commandName,
                    Count = samples.Length,
                    MinMs = durations.First(),
                    MaxMs = durations.Last(),
                    AverageMs = durations.Average(),
                    MedianMs = GetPercentile(durations, 50),
                    P95Ms = GetPercentile(durations, 95),
                    P99Ms = GetPercentile(durations, 99),
                    StandardDeviationMs = CalculateStandardDeviation(durations),
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            public CommandSummary GetSummary()
            {
                var samples = _samples.ToArray();
                var durations = samples.Select(s => s.DurationMs).ToArray();

                return new CommandSummary
                {
                    TotalCount = _totalCount,
                    SuccessCount = _successCount,
                    FailureCount = _failureCount,
                    AverageMs = durations.Length > 0 ? durations.Average() : 0,
                    CollectionCounts = _collectionCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            }

            private static double GetPercentile(double[] sortedData, int percentile)
            {
                if (sortedData.Length == 0)
                    return 0;

                var index = (percentile / 100.0) * (sortedData.Length - 1);
                var lower = (int)Math.Floor(index);
                var upper = (int)Math.Ceiling(index);

                if (lower == upper)
                    return sortedData[lower];

                var fraction = index - lower;
                return sortedData[lower] + (sortedData[upper] - sortedData[lower]) * fraction;
            }

            private static double CalculateStandardDeviation(double[] data)
            {
                if (data.Length < 2)
                    return 0;

                var mean = data.Average();
                var sumSquaredDiffs = data.Sum(d => Math.Pow(d - mean, 2));
                return Math.Sqrt(sumSquaredDiffs / (data.Length - 1));
            }
        }

        private struct DurationSample
        {
            public double DurationMs;
            public DateTime Timestamp;
            public bool Success;
        }

        #endregion
    }

    /// <summary>
    /// Summary of command execution.
    /// </summary>
    public class CommandSummary
    {
        /// <summary>
        /// Gets or sets the total execution count.
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the successful execution count.
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the failed execution count.
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the average duration in milliseconds.
        /// </summary>
        public double AverageMs { get; set; }

        /// <summary>
        /// Gets or sets execution counts per collection.
        /// </summary>
        public Dictionary<string, long> CollectionCounts { get; set; } = new();

        /// <summary>
        /// Gets the success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;
    }
}

