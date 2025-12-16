//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Default implementation of MongoDB metrics collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides in-memory metrics collection with:
    /// <list type="bullet">
    ///   <item>Command duration tracking with percentiles</item>
    ///   <item>Slow query counting</item>
    ///   <item>Error tracking by type</item>
    ///   <item>Connection pool statistics</item>
    ///   <item>Thread-safe operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metrics = serviceProvider.GetRequiredService&lt;IMongoDbMetrics&gt;();
    /// 
    /// // Get snapshot
    /// var snapshot = metrics.GetSnapshot();
    /// Console.WriteLine($"Total commands: {snapshot.TotalCommands}");
    /// Console.WriteLine($"Error rate: {snapshot.ErrorRate:P2}");
    /// 
    /// // Get command-specific stats
    /// var findStats = metrics.GetDurationStatistics("find");
    /// Console.WriteLine($"Find P95: {findStats.P95Ms}ms");
    /// </code>
    /// </example>
    public class MongoDbMetrics : IMongoDbMetrics
    {
        private readonly MongoDbObservabilityOptions _options;

        // Counters
        private long _totalCommands;
        private long _successfulCommands;
        private long _failedCommands;
        private long _slowQueries;

        // Duration tracking per command type
        private readonly ConcurrentDictionary<string, DurationTracker> _durationTrackers = new();

        // Error tracking
        private readonly ConcurrentDictionary<string, long> _errorCounts = new();

        // Command counts
        private readonly ConcurrentDictionary<string, long> _commandCounts = new();

        // Connection pool stats (latest)
        private ConnectionPoolStats _latestPoolStats;

        // Checkout durations
        private readonly DurationTracker _checkoutDurations = new(1000);

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbMetrics"/> class.
        /// </summary>
        /// <param name="options">The observability options.</param>
        public MongoDbMetrics(IOptions<MongoDbObservabilityOptions> options = null)
        {
            _options = options?.Value ?? new MongoDbObservabilityOptions();
        }

        /// <inheritdoc />
        public void RecordCommandDuration(string commandName, string collectionName, TimeSpan duration, bool success)
        {
            Interlocked.Increment(ref _totalCommands);

            if (success)
                Interlocked.Increment(ref _successfulCommands);
            else
                Interlocked.Increment(ref _failedCommands);

            // Track command count
            _commandCounts.AddOrUpdate(commandName, 1, (_, count) => count + 1);

            // Track duration
            var tracker = _durationTrackers.GetOrAdd(commandName, _ => new DurationTracker(_options.DurationHistogramBuckets * 1000));
            tracker.Record(duration.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void RecordSlowQuery(string commandName, string collectionName, TimeSpan duration, long documentsExamined, long documentsReturned)
        {
            Interlocked.Increment(ref _slowQueries);
        }

        /// <inheritdoc />
        public void RecordConnectionPoolStats(ConnectionPoolStats stats)
        {
            _latestPoolStats = stats;
        }

        /// <inheritdoc />
        public void RecordConnectionCheckoutDuration(TimeSpan duration)
        {
            _checkoutDurations.Record(duration.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void RecordError(string commandName, string collectionName, string errorType)
        {
            var key = $"{commandName}:{errorType}";
            _errorCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
        }

        /// <inheritdoc />
        public MongoDbMetricsSnapshot GetSnapshot()
        {
            var snapshot = new MongoDbMetricsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                TotalCommands = _totalCommands,
                SuccessfulCommands = _successfulCommands,
                FailedCommands = _failedCommands,
                SlowQueries = _slowQueries,
                CommandCounts = _commandCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ErrorCounts = _errorCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ConnectionPool = _latestPoolStats
            };

            // Calculate averages and percentiles
            foreach (var (command, tracker) in _durationTrackers)
            {
                var stats = tracker.GetStatistics();
                snapshot.AverageDurationMs[command] = stats.Average;
                snapshot.P95DurationMs[command] = stats.P95;
                snapshot.P99DurationMs[command] = stats.P99;
            }

            return snapshot;
        }

        /// <inheritdoc />
        public DurationStatistics GetDurationStatistics(string commandName)
        {
            if (!_durationTrackers.TryGetValue(commandName, out var tracker))
            {
                return new DurationStatistics { CommandName = commandName };
            }

            var stats = tracker.GetStatistics();
            return new DurationStatistics
            {
                CommandName = commandName,
                Count = stats.Count,
                MinMs = stats.Min,
                MaxMs = stats.Max,
                AverageMs = stats.Average,
                MedianMs = stats.Median,
                P95Ms = stats.P95,
                P99Ms = stats.P99,
                StandardDeviationMs = stats.StdDev,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        /// <inheritdoc />
        public void Reset()
        {
            _totalCommands = 0;
            _successfulCommands = 0;
            _failedCommands = 0;
            _slowQueries = 0;
            _durationTrackers.Clear();
            _errorCounts.Clear();
            _commandCounts.Clear();
            _latestPoolStats = null;
        }

        /// <summary>
        /// Gets the total number of commands recorded.
        /// </summary>
        public long TotalCommands => _totalCommands;

        /// <summary>
        /// Gets the number of slow queries recorded.
        /// </summary>
        public long SlowQueryCount => _slowQueries;

        /// <summary>
        /// Gets checkout duration statistics.
        /// </summary>
        /// <returns>The checkout duration statistics.</returns>
        public (double Average, double P95, double P99) GetCheckoutStats()
        {
            var stats = _checkoutDurations.GetStatistics();
            return (stats.Average, stats.P95, stats.P99);
        }

        #region Internal Classes

        private class DurationTracker
        {
            private readonly ConcurrentQueue<double> _samples = new();
            private readonly int _maxSamples;

            public DurationTracker(int maxSamples)
            {
                _maxSamples = maxSamples;
            }

            public void Record(double durationMs)
            {
                _samples.Enqueue(durationMs);

                // Trim old samples
                while (_samples.Count > _maxSamples && _samples.TryDequeue(out _)) { }
            }

            public TrackerStats GetStatistics()
            {
                var samples = _samples.ToArray();
                if (samples.Length == 0)
                {
                    return new TrackerStats();
                }

                var sorted = samples.OrderBy(x => x).ToArray();
                var count = sorted.Length;

                return new TrackerStats
                {
                    Count = count,
                    Min = sorted.First(),
                    Max = sorted.Last(),
                    Average = sorted.Average(),
                    Median = GetPercentile(sorted, 50),
                    P95 = GetPercentile(sorted, 95),
                    P99 = GetPercentile(sorted, 99),
                    StdDev = CalculateStdDev(sorted)
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

            private static double CalculateStdDev(double[] data)
            {
                if (data.Length < 2)
                    return 0;

                var mean = data.Average();
                var sumSquaredDiffs = data.Sum(d => Math.Pow(d - mean, 2));
                return Math.Sqrt(sumSquaredDiffs / (data.Length - 1));
            }
        }

        private struct TrackerStats
        {
            public long Count;
            public double Min;
            public double Max;
            public double Average;
            public double Median;
            public double P95;
            public double P99;
            public double StdDev;
        }

        #endregion
    }
}

