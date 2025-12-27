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
    /// Health check specifically for MySQL databases.
    /// Provides MySQL-specific health metrics and validations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check can verify:
    /// <list type="bullet">
    /// <item><strong>Server version</strong> - MySQL/MariaDB version</item>
    /// <item><strong>Connection count</strong> - Active connections vs max connections</item>
    /// <item><strong>Slow queries</strong> - Count of slow queries</item>
    /// <item><strong>Table locks</strong> - Current table lock waits</item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: This health check uses ADO.NET and is provider-agnostic.
    /// It works with MySql.Data or Pomelo.EntityFrameworkCore.MySql providers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddMySqlHealthCheck("Server=localhost;Database=MyDb;...", options =>
    ///     {
    ///         options.CheckConnectionUsage = true;
    ///         options.CheckSlowQueries = true;
    ///     });
    /// </code>
    /// </example>
    public class MySqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly MySqlHealthCheckOptions _options;
        private readonly ILogger<MySqlHealthCheck> _logger;
        private readonly Func<string, DbConnection> _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">MySQL connection string.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="connectionFactory">Factory to create DbConnection (for MySqlConnection).</param>
        public MySqlHealthCheck(
            string connectionString,
            MySqlHealthCheckOptions? options,
            ILogger<MySqlHealthCheck> logger,
            Func<string, DbConnection> connectionFactory)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _options = options ?? new MySqlHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("MySQL health check starting");

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

                // Get server status variables
                var status = await GetServerStatusAsync(connection, cancellationToken);

                // Check connection usage if enabled
                if (_options.CheckConnectionUsage && status.ContainsKey("Threads_connected") && status.ContainsKey("max_connections"))
                {
                    var current = Convert.ToInt32(status["Threads_connected"]);
                    var max = Convert.ToInt32(status["max_connections"]);
                    
                    data["currentConnections"] = current;
                    data["maxConnections"] = max;
                    data["connectionUsagePercent"] = max > 0 ? Math.Round((double)current / max * 100, 2) : 0;

                    var usageRatio = max > 0 ? (double)current / max : 0;
                    if (usageRatio >= _options.ConnectionUsageThreshold)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"MySQL connection usage is high: {current}/{max} ({usageRatio:P0})",
                            data: data);
                    }
                }

                // Check slow queries if enabled
                if (_options.CheckSlowQueries && status.ContainsKey("Slow_queries"))
                {
                    var slowQueries = Convert.ToInt64(status["Slow_queries"]);
                    data["slowQueries"] = slowQueries;

                    // Note: This is cumulative, so we just report it
                    // For proper monitoring, you'd need to track the delta
                }

                // Check table lock waits if enabled
                if (_options.CheckTableLocks && status.ContainsKey("Table_locks_waited"))
                {
                    var lockWaits = Convert.ToInt64(status["Table_locks_waited"]);
                    data["tableLockWaits"] = lockWaits;
                }

                // Check questions/queries per second
                if (status.ContainsKey("Questions") && status.ContainsKey("Uptime"))
                {
                    var questions = Convert.ToInt64(status["Questions"]);
                    var uptime = Convert.ToInt64(status["Uptime"]);
                    if (uptime > 0)
                    {
                        data["queriesPerSecond"] = Math.Round((double)questions / uptime, 2);
                    }
                }

                // Check InnoDB buffer pool if enabled
                if (_options.CheckBufferPool)
                {
                    var bufferPoolStats = await GetBufferPoolStatsAsync(connection, cancellationToken);
                    if (bufferPoolStats.Count > 0)
                    {
                        data["innodbBufferPoolStats"] = bufferPoolStats;
                    }
                }

                // Check replication status if enabled
                if (_options.CheckReplication)
                {
                    var replicationStatus = await GetReplicationStatusAsync(connection, cancellationToken);
                    if (replicationStatus != null)
                    {
                        data["replicationStatus"] = replicationStatus;

                        if (replicationStatus.TryGetValue("Seconds_Behind_Master", out var lag) && 
                            lag != null && 
                            Convert.ToInt32(lag) > _options.ReplicationLagThresholdSeconds)
                        {
                            return HealthCheckResult.Degraded(
                                description: $"MySQL replication lag: {lag} seconds behind master",
                                data: data);
                        }
                    }
                }

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;

                // Evaluate response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"MySQL response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"MySQL response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                _logger.LogDebug("MySQL health check completed successfully in {ResponseTimeMs}ms", stopwatch.ElapsedMilliseconds);

                return HealthCheckResult.Healthy(
                    description: $"MySQL is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "MySQL health check failed");

                return HealthCheckResult.Unhealthy(
                    description: $"MySQL health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }

        private static async Task<Dictionary<string, object>> GetServerStatusAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var status = new Dictionary<string, object>();

            // Get status variables
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SHOW GLOBAL STATUS WHERE Variable_name IN ('Threads_connected', 'Slow_queries', 'Table_locks_waited', 'Questions', 'Uptime')";
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    status[reader.GetString(0)] = reader.GetString(1);
                }
            }

            // Get variables
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SHOW GLOBAL VARIABLES WHERE Variable_name = 'max_connections'";
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    status[reader.GetString(0)] = reader.GetString(1);
                }
            }

            return status;
        }

        private static async Task<Dictionary<string, object>> GetBufferPoolStatsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var stats = new Dictionary<string, object>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SHOW GLOBAL STATUS WHERE Variable_name IN (
                    'Innodb_buffer_pool_read_requests',
                    'Innodb_buffer_pool_reads',
                    'Innodb_buffer_pool_pages_total',
                    'Innodb_buffer_pool_pages_free'
                )";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                stats[reader.GetString(0)] = reader.GetString(1);
            }

            // Calculate hit ratio if we have the necessary stats
            if (stats.TryGetValue("Innodb_buffer_pool_read_requests", out var requests) &&
                stats.TryGetValue("Innodb_buffer_pool_reads", out var reads))
            {
                var requestsLong = Convert.ToInt64(requests);
                var readsLong = Convert.ToInt64(reads);
                if (requestsLong > 0)
                {
                    var hitRatio = Math.Round((1.0 - (double)readsLong / requestsLong) * 100, 2);
                    stats["buffer_pool_hit_ratio_percent"] = hitRatio;
                }
            }

            return stats;
        }

        private static async Task<Dictionary<string, object>?> GetReplicationStatusAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SHOW SLAVE STATUS";

            try
            {
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var status = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        if (name is "Slave_IO_Running" or "Slave_SQL_Running" or "Seconds_Behind_Master" or "Last_Error")
                        {
                            status[name] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                        }
                    }
                    return status;
                }
            }
            catch
            {
                // Not a replica, or no permission
            }

            return null;
        }
    }

    /// <summary>
    /// Configuration options for MySQL health checks.
    /// </summary>
    [Serializable]
    public sealed class MySqlHealthCheckOptions
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
        /// When true, reports slow query count.
        /// Default is false.
        /// </summary>
        public bool CheckSlowQueries { get; set; }

        /// <summary>
        /// When true, reports table lock wait count.
        /// Default is false.
        /// </summary>
        public bool CheckTableLocks { get; set; }

        /// <summary>
        /// When true, reports InnoDB buffer pool statistics.
        /// Default is false.
        /// </summary>
        public bool CheckBufferPool { get; set; }

        /// <summary>
        /// When true, checks replication status for replica databases.
        /// Default is false.
        /// </summary>
        public bool CheckReplication { get; set; }

        /// <summary>
        /// Replication lag in seconds that triggers degraded status.
        /// Default is 30 seconds.
        /// </summary>
        public int ReplicationLagThresholdSeconds { get; set; } = 30;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "db", "database", "mysql", "ready" };
    }
}

