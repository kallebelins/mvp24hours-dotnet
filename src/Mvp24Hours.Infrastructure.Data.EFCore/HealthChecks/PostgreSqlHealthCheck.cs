//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks
{
    /// <summary>
    /// Health check specifically for PostgreSQL databases.
    /// Provides PostgreSQL-specific health metrics and validations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check can verify:
    /// <list type="bullet">
    /// <item><strong>Server version</strong> - PostgreSQL version</item>
    /// <item><strong>Connection count</strong> - Active connections vs max connections</item>
    /// <item><strong>Replication status</strong> - For replica databases</item>
    /// <item><strong>Database size</strong> - Current database size</item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: This health check uses ADO.NET and is provider-agnostic.
    /// It works with Npgsql provider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddPostgreSqlHealthCheck("Host=localhost;Database=MyDb;...", options =>
    ///     {
    ///         options.CheckConnectionUsage = true;
    ///         options.ConnectionUsageThreshold = 0.8; // 80%
    ///     });
    /// </code>
    /// </example>
    public class PostgreSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly PostgreSqlHealthCheckOptions _options;
        private readonly ILogger<PostgreSqlHealthCheck> _logger;
        private readonly Func<string, DbConnection> _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="connectionFactory">Factory to create DbConnection (for Npgsql).</param>
        public PostgreSqlHealthCheck(
            string connectionString,
            PostgreSqlHealthCheckOptions? options,
            ILogger<PostgreSqlHealthCheck> logger,
            Func<string, DbConnection> connectionFactory)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _options = options ?? new PostgreSqlHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("PostgreSQL health check starting");

            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var connection = _connectionFactory(_connectionString);
                await connection.OpenAsync(cancellationToken);

                data["database"] = connection.Database;
                data["serverVersion"] = connection.ServerVersion;

                // Execute basic health query
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _options.HealthQuery;
                    command.CommandTimeout = _options.QueryTimeoutSeconds;
                    await command.ExecuteScalarAsync(cancellationToken);
                }

                // Check connection usage if enabled
                if (_options.CheckConnectionUsage)
                {
                    var (current, max) = await GetConnectionUsageAsync(connection, cancellationToken);
                    data["currentConnections"] = current;
                    data["maxConnections"] = max;
                    data["connectionUsagePercent"] = max > 0 ? Math.Round((double)current / max * 100, 2) : 0;

                    var usageRatio = max > 0 ? (double)current / max : 0;
                    if (usageRatio >= _options.ConnectionUsageThreshold)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"PostgreSQL connection usage is high: {current}/{max} ({usageRatio:P0})",
                            data: data);
                    }
                }

                // Check replication lag if enabled
                if (_options.CheckReplicationLag)
                {
                    var lagBytes = await GetReplicationLagAsync(connection, cancellationToken);
                    if (lagBytes.HasValue)
                    {
                        data["replicationLagBytes"] = lagBytes.Value;
                        
                        if (lagBytes.Value > _options.ReplicationLagThresholdBytes)
                        {
                            return HealthCheckResult.Degraded(
                                description: $"PostgreSQL replication lag is high: {lagBytes.Value} bytes",
                                data: data);
                        }
                    }
                }

                // Check database size if enabled
                if (_options.CheckDatabaseSize)
                {
                    var sizeBytes = await GetDatabaseSizeAsync(connection, cancellationToken);
                    data["databaseSizeBytes"] = sizeBytes;
                    data["databaseSizeMB"] = Math.Round(sizeBytes / 1024.0 / 1024.0, 2);

                    if (sizeBytes > _options.DatabaseSizeThresholdBytes)
                    {
                        data["warning"] = "Database size exceeds threshold";
                    }
                }

                // Check for locks if enabled
                if (_options.CheckLocks)
                {
                    var lockCount = await GetBlockedLocksCountAsync(connection, cancellationToken);
                    data["blockedLocks"] = lockCount;

                    if (lockCount >= _options.BlockedLocksThreshold)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"PostgreSQL has {lockCount} blocked locks",
                            data: data);
                    }
                }

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;

                // Evaluate response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"PostgreSQL response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"PostgreSQL response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                _logger.LogDebug("PostgreSQL health check completed successfully in {ResponseTimeMs}ms", stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Healthy(
                    description: $"PostgreSQL is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "PostgreSQL health check failed");

                return HealthCheckResult.Unhealthy(
                    description: $"PostgreSQL health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }

        private static async Task<(int Current, int Max)> GetConnectionUsageAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    (SELECT count(*) FROM pg_stat_activity) as current_connections,
                    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') as max_connections";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return (reader.GetInt32(0), reader.GetInt32(1));
            }
            return (0, 0);
        }

        private static async Task<long?> GetReplicationLagAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT CASE WHEN pg_is_in_recovery() THEN
                    COALESCE(pg_wal_lsn_diff(pg_last_wal_receive_lsn(), pg_last_wal_replay_lsn()), 0)
                ELSE NULL END";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result == DBNull.Value || result == null ? null : Convert.ToInt64(result);
        }

        private static async Task<long> GetDatabaseSizeAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_database_size(current_database())";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        private static async Task<int> GetBlockedLocksCountAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM pg_locks bl
                JOIN pg_locks hl ON bl.transactionid = hl.transactionid AND bl.pid != hl.pid
                WHERE NOT bl.granted";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
    }

    /// <summary>
    /// Configuration options for PostgreSQL health checks.
    /// </summary>
    [Serializable]
    public sealed class PostgreSqlHealthCheckOptions
    {
        /// <summary>
        /// SQL query to execute for health verification.
        /// Default: "SELECT 1".
        /// </summary>
        public string HealthQuery { get; set; } = "SELECT 1";

        /// <summary>
        /// Timeout in seconds for the health query execution.
        /// Default is 5 seconds.
        /// </summary>
        public int QueryTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// Default is 500ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 500;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// Default is 2000ms.
        /// </summary>
        public int FailureThresholdMs { get; set; } = 2000;

        /// <summary>
        /// When true, checks connection pool usage.
        /// Default is true.
        /// </summary>
        public bool CheckConnectionUsage { get; set; } = true;

        /// <summary>
        /// Connection usage ratio (0.0-1.0) that triggers degraded status.
        /// Default is 0.8 (80%).
        /// </summary>
        public double ConnectionUsageThreshold { get; set; } = 0.8;

        /// <summary>
        /// When true, checks replication lag for replica databases.
        /// Default is false.
        /// </summary>
        public bool CheckReplicationLag { get; set; }

        /// <summary>
        /// Replication lag in bytes that triggers degraded status.
        /// Default is 10MB.
        /// </summary>
        public long ReplicationLagThresholdBytes { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// When true, reports database size.
        /// Default is false.
        /// </summary>
        public bool CheckDatabaseSize { get; set; }

        /// <summary>
        /// Database size in bytes that triggers a warning (reported in data).
        /// Default is 10GB.
        /// </summary>
        public long DatabaseSizeThresholdBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB

        /// <summary>
        /// When true, checks for blocked locks.
        /// Default is false.
        /// </summary>
        public bool CheckLocks { get; set; }

        /// <summary>
        /// Number of blocked locks that triggers degraded status.
        /// Default is 10.
        /// </summary>
        public int BlockedLocksThreshold { get; set; } = 10;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "db", "database", "postgresql", "ready" };
    }
}

