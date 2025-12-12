//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks
{
    /// <summary>
    /// Configuration options for DbContext health checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control how the database health check behaves:
    /// <list type="bullet">
    /// <item><strong>HealthQuery</strong> - Custom SQL to verify database health</item>
    /// <item><strong>Thresholds</strong> - Response time limits for degraded/unhealthy states</item>
    /// <item><strong>Migrations</strong> - Whether to check for pending migrations</item>
    /// </list>
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class DbContextHealthCheckOptions
    {
        /// <summary>
        /// Custom SQL query to execute for health verification.
        /// If null, only connectivity is checked. Default: "SELECT 1".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Examples by database:
        /// <list type="bullet">
        /// <item>SQL Server: <c>SELECT 1</c></item>
        /// <item>PostgreSQL: <c>SELECT 1</c></item>
        /// <item>MySQL: <c>SELECT 1</c></item>
        /// <item>Oracle: <c>SELECT 1 FROM DUAL</c></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? HealthQuery { get; set; } = "SELECT 1";

        /// <summary>
        /// Timeout in seconds for the health query execution.
        /// Default is 5 seconds. Health checks should be fast.
        /// </summary>
        public int QueryTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// If response time exceeds this, health check returns Degraded.
        /// Default is 500ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 500;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// If response time exceeds this, health check returns Unhealthy.
        /// Default is 2000ms (2 seconds).
        /// </summary>
        public int FailureThresholdMs { get; set; } = 2000;

        /// <summary>
        /// When true, checks for pending database migrations.
        /// Default is false.
        /// </summary>
        public bool CheckPendingMigrations { get; set; }

        /// <summary>
        /// When true, returns Degraded status if there are pending migrations.
        /// Only applies when <see cref="CheckPendingMigrations"/> is true.
        /// Default is false (just reports in data).
        /// </summary>
        public bool FailOnPendingMigrations { get; set; }

        /// <summary>
        /// Tags to associate with this health check for filtering.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "db", "database", "efcore" };

        /// <summary>
        /// Name of the health check. If null, uses DbContext type name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The failure status to use when the health check fails completely.
        /// Default is Unhealthy.
        /// </summary>
        public Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus FailureStatus { get; set; } 
            = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;

        #region Factory Methods

        /// <summary>
        /// Creates options optimized for SQL Server.
        /// </summary>
        public static DbContextHealthCheckOptions SqlServer() => new()
        {
            HealthQuery = "SELECT 1",
            QueryTimeoutSeconds = 5,
            DegradedThresholdMs = 500,
            FailureThresholdMs = 2000,
            Tags = new[] { "db", "database", "efcore", "sqlserver" }
        };

        /// <summary>
        /// Creates options optimized for PostgreSQL.
        /// </summary>
        public static DbContextHealthCheckOptions PostgreSql() => new()
        {
            HealthQuery = "SELECT 1",
            QueryTimeoutSeconds = 5,
            DegradedThresholdMs = 500,
            FailureThresholdMs = 2000,
            Tags = new[] { "db", "database", "efcore", "postgresql" }
        };

        /// <summary>
        /// Creates options optimized for MySQL.
        /// </summary>
        public static DbContextHealthCheckOptions MySql() => new()
        {
            HealthQuery = "SELECT 1",
            QueryTimeoutSeconds = 5,
            DegradedThresholdMs = 500,
            FailureThresholdMs = 2000,
            Tags = new[] { "db", "database", "efcore", "mysql" }
        };

        /// <summary>
        /// Creates options for strict health checking (includes migration check).
        /// </summary>
        public static DbContextHealthCheckOptions Strict() => new()
        {
            HealthQuery = "SELECT 1",
            QueryTimeoutSeconds = 3,
            DegradedThresholdMs = 300,
            FailureThresholdMs = 1000,
            CheckPendingMigrations = true,
            FailOnPendingMigrations = true,
            Tags = new[] { "db", "database", "efcore", "ready" }
        };

        /// <summary>
        /// Creates minimal options for basic liveness check.
        /// </summary>
        public static DbContextHealthCheckOptions Liveness() => new()
        {
            HealthQuery = null, // Just check CanConnect
            QueryTimeoutSeconds = 3,
            DegradedThresholdMs = 1000,
            FailureThresholdMs = 5000,
            CheckPendingMigrations = false,
            Tags = new[] { "db", "live" }
        };

        #endregion
    }
}

