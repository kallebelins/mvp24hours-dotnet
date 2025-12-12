//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting
{
    /// <summary>
    /// Configuration options for read/write splitting (read replicas).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read/write splitting allows distributing database load:
    /// <list type="bullet">
    /// <item><strong>Write operations</strong> - Go to the primary database</item>
    /// <item><strong>Read operations</strong> - Go to read replicas for better performance</item>
    /// </list>
    /// </para>
    /// <para>
    /// ⚠️ <strong>Important</strong>: There may be replication lag between primary and replicas.
    /// Consider this when reading data that was just written.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursReadWriteSplitting&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.PrimaryConnectionString = "Server=primary;...";
    ///     options.ReplicaConnectionStrings = new[]
    ///     {
    ///         "Server=replica1;...",
    ///         "Server=replica2;..."
    ///     };
    ///     options.LoadBalancing = ReplicaLoadBalancing.RoundRobin;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class ReadWriteOptions
    {
        #region Connection Strings

        /// <summary>
        /// Connection string for the primary (write) database.
        /// All write operations go to this database.
        /// </summary>
        public string PrimaryConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Connection strings for read replica databases.
        /// Read operations are distributed across these replicas.
        /// </summary>
        public IList<string> ReplicaConnectionStrings { get; set; } = new List<string>();

        /// <summary>
        /// Optional fallback to primary for reads when all replicas are unavailable.
        /// Default is true (reads fall back to primary).
        /// </summary>
        public bool FallbackToPrimaryOnReplicaFailure { get; set; } = true;

        #endregion

        #region Load Balancing

        /// <summary>
        /// Strategy for selecting which replica to use for read operations.
        /// Default is RoundRobin.
        /// </summary>
        public ReplicaLoadBalancing LoadBalancing { get; set; } = ReplicaLoadBalancing.RoundRobin;

        /// <summary>
        /// When using Weighted load balancing, weights for each replica.
        /// Index corresponds to ReplicaConnectionStrings index.
        /// Higher weight means more traffic.
        /// </summary>
        public IList<int> ReplicaWeights { get; set; } = new List<int>();

        #endregion

        #region Health Checking

        /// <summary>
        /// When true, performs health checks on replicas and removes unhealthy ones.
        /// Default is true.
        /// </summary>
        public bool EnableReplicaHealthChecks { get; set; } = true;

        /// <summary>
        /// Interval between health checks for replicas.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for health check queries.
        /// Default is 5 seconds.
        /// </summary>
        public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Number of consecutive failures before marking a replica as unhealthy.
        /// Default is 3.
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// Duration to wait before retrying an unhealthy replica.
        /// Default is 60 seconds.
        /// </summary>
        public TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(60);

        #endregion

        #region Behavior

        /// <summary>
        /// When true, uses read-after-write consistency for the current session.
        /// After a write, subsequent reads in the same session go to primary.
        /// Default is false.
        /// </summary>
        public bool EnableReadAfterWriteConsistency { get; set; }

        /// <summary>
        /// Duration after a write operation during which reads go to primary.
        /// Only applies when EnableReadAfterWriteConsistency is true.
        /// Default is 5 seconds.
        /// </summary>
        public TimeSpan ReadAfterWriteWindow { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When true, automatically detects read vs write operations.
        /// When false, you must explicitly specify using IReadOnlyDbContext or IWriteDbContext.
        /// Default is true.
        /// </summary>
        public bool AutoDetectOperationType { get; set; } = true;

        /// <summary>
        /// When true, logs replica selection decisions for debugging.
        /// Default is false.
        /// </summary>
        public bool LogReplicaSelection { get; set; }

        #endregion

        #region Retry and Resilience

        /// <summary>
        /// Maximum retries when a replica fails during a read operation.
        /// Default is 2 (try up to 3 replicas total).
        /// </summary>
        public int MaxReplicaRetries { get; set; } = 2;

        /// <summary>
        /// When true, retries on a different replica if one fails.
        /// Default is true.
        /// </summary>
        public bool RetryOnDifferentReplica { get; set; } = true;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates options for a simple primary + single replica setup.
        /// </summary>
        public static ReadWriteOptions SimpleSetup(string primaryConnectionString, string replicaConnectionString) => new()
        {
            PrimaryConnectionString = primaryConnectionString,
            ReplicaConnectionStrings = new List<string> { replicaConnectionString },
            LoadBalancing = ReplicaLoadBalancing.RoundRobin,
            EnableReplicaHealthChecks = true
        };

        /// <summary>
        /// Creates options for Azure SQL with geo-replicated read replicas.
        /// </summary>
        public static ReadWriteOptions AzureSqlGeoReplica(string primaryConnectionString, params string[] replicaConnectionStrings) => new()
        {
            PrimaryConnectionString = primaryConnectionString,
            ReplicaConnectionStrings = new List<string>(replicaConnectionStrings),
            LoadBalancing = ReplicaLoadBalancing.LeastLatency,
            EnableReplicaHealthChecks = true,
            EnableReadAfterWriteConsistency = true,
            ReadAfterWriteWindow = TimeSpan.FromSeconds(10) // Higher for geo-replication
        };

        /// <summary>
        /// Creates options for PostgreSQL with streaming replication.
        /// </summary>
        public static ReadWriteOptions PostgreSqlStreaming(string primaryConnectionString, params string[] replicaConnectionStrings) => new()
        {
            PrimaryConnectionString = primaryConnectionString,
            ReplicaConnectionStrings = new List<string>(replicaConnectionStrings),
            LoadBalancing = ReplicaLoadBalancing.RoundRobin,
            EnableReplicaHealthChecks = true,
            EnableReadAfterWriteConsistency = true,
            ReadAfterWriteWindow = TimeSpan.FromSeconds(2) // Lower for streaming replication
        };

        #endregion
    }

    /// <summary>
    /// Strategies for load balancing across read replicas.
    /// </summary>
    public enum ReplicaLoadBalancing
    {
        /// <summary>
        /// Selects replicas in a round-robin fashion.
        /// Equal distribution across all replicas.
        /// </summary>
        RoundRobin,

        /// <summary>
        /// Selects a random replica for each request.
        /// </summary>
        Random,

        /// <summary>
        /// Selects replicas based on configured weights.
        /// Higher weight means more traffic.
        /// </summary>
        Weighted,

        /// <summary>
        /// Selects the replica with the lowest measured latency.
        /// Requires health check latency tracking.
        /// </summary>
        LeastLatency,

        /// <summary>
        /// Selects the replica with the fewest active connections.
        /// Requires connection count tracking.
        /// </summary>
        LeastConnections,

        /// <summary>
        /// Always selects the first available replica.
        /// Others are only used as failover.
        /// </summary>
        Failover
    }
}

