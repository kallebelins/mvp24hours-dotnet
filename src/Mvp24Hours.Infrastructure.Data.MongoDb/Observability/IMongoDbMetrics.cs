//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Interface for MongoDB metrics collection and reporting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides metrics for:
    /// <list type="bullet">
    ///   <item>Command/query duration tracking</item>
    ///   <item>Connection pool statistics</item>
    ///   <item>Slow query detection</item>
    ///   <item>Operation counts and error rates</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbMetrics
    {
        /// <summary>
        /// Records the duration of a MongoDB command.
        /// </summary>
        /// <param name="commandName">The command name (e.g., "find", "insert", "update").</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="duration">The command duration.</param>
        /// <param name="success">Whether the command succeeded.</param>
        void RecordCommandDuration(string commandName, string collectionName, TimeSpan duration, bool success);

        /// <summary>
        /// Records a slow query event.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="duration">The query duration.</param>
        /// <param name="documentsExamined">Number of documents examined.</param>
        /// <param name="documentsReturned">Number of documents returned.</param>
        void RecordSlowQuery(string commandName, string collectionName, TimeSpan duration, long documentsExamined, long documentsReturned);

        /// <summary>
        /// Records connection pool statistics.
        /// </summary>
        /// <param name="stats">The connection pool statistics.</param>
        void RecordConnectionPoolStats(ConnectionPoolStats stats);

        /// <summary>
        /// Records a connection checkout duration.
        /// </summary>
        /// <param name="duration">The checkout duration.</param>
        void RecordConnectionCheckoutDuration(TimeSpan duration);

        /// <summary>
        /// Records an operation error.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="errorType">The error type or exception type name.</param>
        void RecordError(string commandName, string collectionName, string errorType);

        /// <summary>
        /// Gets the current metrics snapshot.
        /// </summary>
        /// <returns>The metrics snapshot.</returns>
        MongoDbMetricsSnapshot GetSnapshot();

        /// <summary>
        /// Gets duration statistics for a specific command type.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <returns>Duration statistics.</returns>
        DurationStatistics GetDurationStatistics(string commandName);

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Connection pool statistics.
    /// </summary>
    public class ConnectionPoolStats
    {
        /// <summary>
        /// Gets or sets the server endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the current number of connections in the pool.
        /// </summary>
        public int CurrentSize { get; set; }

        /// <summary>
        /// Gets or sets the number of available (idle) connections.
        /// </summary>
        public int AvailableCount { get; set; }

        /// <summary>
        /// Gets or sets the number of connections currently in use.
        /// </summary>
        public int InUseCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum pool size.
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum pool size.
        /// </summary>
        public int MinSize { get; set; }

        /// <summary>
        /// Gets or sets the number of pending connection requests.
        /// </summary>
        public int WaitQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of connections created.
        /// </summary>
        public long TotalConnectionsCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of connections closed.
        /// </summary>
        public long TotalConnectionsClosed { get; set; }

        /// <summary>
        /// Gets or sets the total checkout failures.
        /// </summary>
        public long TotalCheckoutFailures { get; set; }

        /// <summary>
        /// Gets the utilization ratio (InUse / MaxSize).
        /// </summary>
        public double Utilization => MaxSize > 0 ? (double)InUseCount / MaxSize : 0;

        /// <summary>
        /// Gets or sets the timestamp of this snapshot.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Snapshot of MongoDB metrics at a point in time.
    /// </summary>
    public class MongoDbMetricsSnapshot
    {
        /// <summary>
        /// Gets or sets the snapshot timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the total commands executed.
        /// </summary>
        public long TotalCommands { get; set; }

        /// <summary>
        /// Gets or sets the total successful commands.
        /// </summary>
        public long SuccessfulCommands { get; set; }

        /// <summary>
        /// Gets or sets the total failed commands.
        /// </summary>
        public long FailedCommands { get; set; }

        /// <summary>
        /// Gets or sets the total slow queries detected.
        /// </summary>
        public long SlowQueries { get; set; }

        /// <summary>
        /// Gets or sets command counts by type.
        /// </summary>
        public Dictionary<string, long> CommandCounts { get; set; } = new();

        /// <summary>
        /// Gets or sets error counts by type.
        /// </summary>
        public Dictionary<string, long> ErrorCounts { get; set; } = new();

        /// <summary>
        /// Gets or sets the latest connection pool stats.
        /// </summary>
        public ConnectionPoolStats ConnectionPool { get; set; }

        /// <summary>
        /// Gets or sets average command duration by type.
        /// </summary>
        public Dictionary<string, double> AverageDurationMs { get; set; } = new();

        /// <summary>
        /// Gets or sets p95 command duration by type.
        /// </summary>
        public Dictionary<string, double> P95DurationMs { get; set; } = new();

        /// <summary>
        /// Gets or sets p99 command duration by type.
        /// </summary>
        public Dictionary<string, double> P99DurationMs { get; set; } = new();

        /// <summary>
        /// Gets the error rate (failed / total).
        /// </summary>
        public double ErrorRate => TotalCommands > 0 ? (double)FailedCommands / TotalCommands : 0;

        /// <summary>
        /// Gets the success rate (successful / total).
        /// </summary>
        public double SuccessRate => TotalCommands > 0 ? (double)SuccessfulCommands / TotalCommands : 0;
    }

    /// <summary>
    /// Duration statistics for MongoDB operations.
    /// </summary>
    public class DurationStatistics
    {
        /// <summary>
        /// Gets or sets the command name.
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Gets or sets the sample count.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the minimum duration in milliseconds.
        /// </summary>
        public double MinMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum duration in milliseconds.
        /// </summary>
        public double MaxMs { get; set; }

        /// <summary>
        /// Gets or sets the average duration in milliseconds.
        /// </summary>
        public double AverageMs { get; set; }

        /// <summary>
        /// Gets or sets the median (p50) duration in milliseconds.
        /// </summary>
        public double MedianMs { get; set; }

        /// <summary>
        /// Gets or sets the 95th percentile duration in milliseconds.
        /// </summary>
        public double P95Ms { get; set; }

        /// <summary>
        /// Gets or sets the 99th percentile duration in milliseconds.
        /// </summary>
        public double P99Ms { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation in milliseconds.
        /// </summary>
        public double StandardDeviationMs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of this calculation.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}

