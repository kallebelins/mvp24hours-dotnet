//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting
{
    /// <summary>
    /// Interface for selecting read replicas based on load balancing strategy.
    /// </summary>
    public interface IReplicaSelector
    {
        /// <summary>
        /// Selects the next replica connection string to use for read operations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The selected replica connection string, or null if no healthy replica is available.</returns>
        Task<string?> SelectReplicaAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the primary (write) connection string.
        /// </summary>
        /// <returns>The primary connection string.</returns>
        string GetPrimaryConnectionString();

        /// <summary>
        /// Marks a replica as failed (unhealthy).
        /// </summary>
        /// <param name="connectionString">The connection string of the failed replica.</param>
        void MarkReplicaFailed(string connectionString);

        /// <summary>
        /// Marks a replica as recovered (healthy).
        /// </summary>
        /// <param name="connectionString">The connection string of the recovered replica.</param>
        void MarkReplicaRecovered(string connectionString);

        /// <summary>
        /// Gets the health status of all replicas.
        /// </summary>
        /// <returns>Dictionary of connection string to health status.</returns>
        IReadOnlyDictionary<string, ReplicaHealth> GetReplicaHealthStatus();
    }

    /// <summary>
    /// Represents the health status of a replica.
    /// </summary>
    public sealed class ReplicaHealth
    {
        /// <summary>
        /// Gets whether the replica is currently healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets the number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Gets the last recorded latency in milliseconds.
        /// </summary>
        public double? LatencyMs { get; init; }

        /// <summary>
        /// Gets the estimated number of active connections.
        /// </summary>
        public int? ActiveConnections { get; init; }

        /// <summary>
        /// Gets the timestamp of the last health check.
        /// </summary>
        public System.DateTime LastCheckTime { get; init; }

        /// <summary>
        /// Gets the timestamp of the last failure.
        /// </summary>
        public System.DateTime? LastFailureTime { get; init; }

        /// <summary>
        /// Creates a healthy replica status.
        /// </summary>
        public static ReplicaHealth Healthy(double? latencyMs = null) => new()
        {
            IsHealthy = true,
            ConsecutiveFailures = 0,
            LatencyMs = latencyMs,
            LastCheckTime = System.DateTime.UtcNow
        };

        /// <summary>
        /// Creates an unhealthy replica status.
        /// </summary>
        public static ReplicaHealth Unhealthy(int consecutiveFailures) => new()
        {
            IsHealthy = false,
            ConsecutiveFailures = consecutiveFailures,
            LastCheckTime = System.DateTime.UtcNow,
            LastFailureTime = System.DateTime.UtcNow
        };
    }
}

