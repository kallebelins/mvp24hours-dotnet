//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks
{
    /// <summary>
    /// Health check specifically for SQL Server databases.
    /// Provides SQL Server-specific health metrics and validations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In addition to basic connectivity, this health check can verify:
    /// <list type="bullet">
    /// <item><strong>Server version</strong> - SQL Server edition and version</item>
    /// <item><strong>Database state</strong> - Online, Offline, Recovering, etc.</item>
    /// <item><strong>Connection pool</strong> - Pool statistics if available</item>
    /// <item><strong>Blocking sessions</strong> - Detects potential deadlocks</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddSqlServerHealthCheck("Server=localhost;Database=MyDb;...", options =>
    ///     {
    ///         options.CheckDatabaseState = true;
    ///         options.CheckBlockingSessions = true;
    ///         options.BlockingSessionThreshold = 5;
    ///     });
    /// </code>
    /// </example>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly SqlServerHealthCheckOptions _options;
        private readonly ILogger<SqlServerHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerHealthCheck"/> class.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public SqlServerHealthCheck(
            string connectionString,
            SqlServerHealthCheckOptions? options,
            ILogger<SqlServerHealthCheck> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _options = options ?? new SqlServerHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "sqlserverhealthcheck-checkhealthasync-start");

            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                data["database"] = connection.Database;
                data["serverVersion"] = connection.ServerVersion;
                data["workstationId"] = connection.WorkstationId;

                // Execute basic health query
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = _options.HealthQuery;
                    command.CommandTimeout = _options.QueryTimeoutSeconds;
                    await command.ExecuteScalarAsync(cancellationToken);
                }

                // Check database state if enabled
                if (_options.CheckDatabaseState)
                {
                    var state = await GetDatabaseStateAsync(connection, cancellationToken);
                    data["databaseState"] = state;

                    if (state != "ONLINE")
                    {
                        return HealthCheckResult.Unhealthy(
                            description: $"Database is not online. Current state: {state}",
                            data: data);
                    }
                }

                // Check blocking sessions if enabled
                if (_options.CheckBlockingSessions)
                {
                    var blockingCount = await GetBlockingSessionCountAsync(connection, cancellationToken);
                    data["blockingSessions"] = blockingCount;

                    if (blockingCount >= _options.BlockingSessionThreshold)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"Database has {blockingCount} blocking sessions",
                            data: data);
                    }
                }

                // Check long running queries if enabled
                if (_options.CheckLongRunningQueries)
                {
                    var longRunningCount = await GetLongRunningQueryCountAsync(connection, cancellationToken);
                    data["longRunningQueries"] = longRunningCount;

                    if (longRunningCount >= _options.LongRunningQueryThreshold)
                    {
                        data["longRunningQueryDetails"] = await GetLongRunningQueryDetailsAsync(connection, cancellationToken);
                        return HealthCheckResult.Degraded(
                            description: $"Database has {longRunningCount} long-running queries (>{_options.LongRunningQuerySeconds}s)",
                            data: data);
                    }
                }

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;

                // Evaluate response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"SQL Server response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"SQL Server response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "sqlserverhealthcheck-checkhealthasync-healthy");

                return HealthCheckResult.Healthy(
                    description: $"SQL Server is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (SqlException ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;
                data["sqlErrorNumber"] = ex.Number;
                data["sqlErrorState"] = ex.State;

                _logger.LogError(ex, "SQL Server health check failed. Error Number: {ErrorNumber}", ex.Number);

                return HealthCheckResult.Unhealthy(
                    description: $"SQL Server health check failed: {ex.Message} (Error: {ex.Number})",
                    exception: ex,
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "SQL Server health check failed with unexpected error");

                return HealthCheckResult.Unhealthy(
                    description: $"SQL Server health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }

        private static async Task<string> GetDatabaseStateAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT state_desc 
                FROM sys.databases 
                WHERE name = DB_NAME()";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString() ?? "UNKNOWN";
        }

        private static async Task<int> GetBlockingSessionCountAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT COUNT(DISTINCT blocking_session_id) 
                FROM sys.dm_exec_requests 
                WHERE blocking_session_id <> 0";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        private async Task<int> GetLongRunningQueryCountAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            var query = $@"
                SELECT COUNT(*) 
                FROM sys.dm_exec_requests 
                WHERE total_elapsed_time > {_options.LongRunningQuerySeconds * 1000}
                AND session_id <> @@SPID";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        private async Task<List<object>> GetLongRunningQueryDetailsAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            var query = $@"
                SELECT TOP 5
                    session_id,
                    status,
                    command,
                    total_elapsed_time / 1000 as elapsed_seconds,
                    wait_type,
                    SUBSTRING(st.text, (r.statement_start_offset/2)+1,
                        ((CASE r.statement_end_offset
                            WHEN -1 THEN DATALENGTH(st.text)
                            ELSE r.statement_end_offset
                        END - r.statement_start_offset)/2) + 1) AS query_text
                FROM sys.dm_exec_requests r
                CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
                WHERE total_elapsed_time > {_options.LongRunningQuerySeconds * 1000}
                AND session_id <> @@SPID
                ORDER BY total_elapsed_time DESC";

            var details = new List<object>();
            
            using var command = connection.CreateCommand();
            command.CommandText = query;
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                details.Add(new
                {
                    SessionId = reader.GetInt16(0),
                    Status = reader.GetString(1),
                    Command = reader.GetString(2),
                    ElapsedSeconds = reader.GetInt64(3),
                    WaitType = reader.IsDBNull(4) ? null : reader.GetString(4),
                    QueryText = reader.IsDBNull(5) ? null : reader.GetString(5)?.Substring(0, Math.Min(200, reader.GetString(5)?.Length ?? 0))
                });
            }

            return details;
        }
    }

    /// <summary>
    /// Configuration options for SQL Server health checks.
    /// </summary>
    [Serializable]
    public sealed class SqlServerHealthCheckOptions
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
        /// When true, checks the database state (ONLINE, OFFLINE, etc.).
        /// Default is true.
        /// </summary>
        public bool CheckDatabaseState { get; set; } = true;

        /// <summary>
        /// When true, checks for blocking sessions.
        /// Default is false.
        /// </summary>
        public bool CheckBlockingSessions { get; set; }

        /// <summary>
        /// Number of blocking sessions that triggers degraded status.
        /// Default is 5.
        /// </summary>
        public int BlockingSessionThreshold { get; set; } = 5;

        /// <summary>
        /// When true, checks for long-running queries.
        /// Default is false.
        /// </summary>
        public bool CheckLongRunningQueries { get; set; }

        /// <summary>
        /// Duration in seconds to consider a query as "long running".
        /// Default is 30 seconds.
        /// </summary>
        public int LongRunningQuerySeconds { get; set; } = 30;

        /// <summary>
        /// Number of long-running queries that triggers degraded status.
        /// Default is 3.
        /// </summary>
        public int LongRunningQueryThreshold { get; set; } = 3;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "db", "database", "sqlserver", "ready" };
    }
}

