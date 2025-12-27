//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering EF Core database health checks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides easy registration of health checks for:
    /// <list type="bullet">
    /// <item><strong>DbContext</strong> - Generic EF Core DbContext health check</item>
    /// <item><strong>SQL Server</strong> - SQL Server-specific health check</item>
    /// <item><strong>PostgreSQL</strong> - PostgreSQL-specific health check</item>
    /// <item><strong>MySQL</strong> - MySQL-specific health check</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class HealthCheckExtensions
    {
        #region DbContext Health Check

        /// <summary>
        /// Adds a health check for the specified DbContext type.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to check.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check. If null, uses DbContext type name.</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursDbContextCheck&lt;AppDbContext&gt;("database", options =>
        ///     {
        ///         options.HealthQuery = "SELECT 1";
        ///         options.CheckPendingMigrations = true;
        ///     });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursDbContextCheck<TContext>(
            this IHealthChecksBuilder builder,
            string? name = null,
            Action<DbContextHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
            where TContext : DbContext
        {
            var options = new DbContextHealthCheckOptions();
            configureOptions?.Invoke(options);

            builder.Services.Configure<DbContextHealthCheckOptions>(opt =>
            {
                opt.HealthQuery = options.HealthQuery;
                opt.QueryTimeoutSeconds = options.QueryTimeoutSeconds;
                opt.DegradedThresholdMs = options.DegradedThresholdMs;
                opt.FailureThresholdMs = options.FailureThresholdMs;
                opt.CheckPendingMigrations = options.CheckPendingMigrations;
                opt.FailOnPendingMigrations = options.FailOnPendingMigrations;
            });

            return builder.Add(new HealthCheckRegistration(
                name ?? typeof(TContext).Name,
                sp => new DbContextHealthCheck<TContext>(
                    sp.GetRequiredService<TContext>(),
                    sp.GetRequiredService<IOptions<DbContextHealthCheckOptions>>(),
                    sp.GetRequiredService<ILogger<DbContextHealthCheck<TContext>>>()),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        /// <summary>
        /// Adds a health check for the specified DbContext type with liveness configuration (minimal checks).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to check.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursDbContextLivenessCheck<TContext>(
            this IHealthChecksBuilder builder,
            string? name = null)
            where TContext : DbContext
        {
            var livenessOptions = DbContextHealthCheckOptions.Liveness();
            return builder.AddMvp24HoursDbContextCheck<TContext>(
                name ?? $"{typeof(TContext).Name}-live",
                options =>
                {
                    options.HealthQuery = livenessOptions.HealthQuery;
                    options.QueryTimeoutSeconds = livenessOptions.QueryTimeoutSeconds;
                    options.DegradedThresholdMs = livenessOptions.DegradedThresholdMs;
                    options.FailureThresholdMs = livenessOptions.FailureThresholdMs;
                    options.CheckPendingMigrations = livenessOptions.CheckPendingMigrations;
                },
                tags: livenessOptions.Tags);
        }

        /// <summary>
        /// Adds a health check for the specified DbContext type with readiness configuration (includes migrations).
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to check.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">Name of the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursDbContextReadinessCheck<TContext>(
            this IHealthChecksBuilder builder,
            string? name = null)
            where TContext : DbContext
        {
            var readinessOptions = DbContextHealthCheckOptions.Strict();
            return builder.AddMvp24HoursDbContextCheck<TContext>(
                name ?? $"{typeof(TContext).Name}-ready",
                options =>
                {
                    options.HealthQuery = readinessOptions.HealthQuery;
                    options.QueryTimeoutSeconds = readinessOptions.QueryTimeoutSeconds;
                    options.DegradedThresholdMs = readinessOptions.DegradedThresholdMs;
                    options.FailureThresholdMs = readinessOptions.FailureThresholdMs;
                    options.CheckPendingMigrations = readinessOptions.CheckPendingMigrations;
                    options.FailOnPendingMigrations = readinessOptions.FailOnPendingMigrations;
                },
                tags: readinessOptions.Tags);
        }

        #endregion

        #region SQL Server Health Check

        /// <summary>
        /// Adds a SQL Server-specific health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">SQL Server connection string.</param>
        /// <param name="name">Name of the health check. Default is "sqlserver".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursSqlServerCheck(
        ///         configuration.GetConnectionString("Default"),
        ///         "sqlserver-main",
        ///         options =>
        ///         {
        ///             options.CheckDatabaseState = true;
        ///             options.CheckBlockingSessions = true;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursSqlServerCheck(
            this IHealthChecksBuilder builder,
            string connectionString,
            string name = "sqlserver",
            Action<SqlServerHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new SqlServerHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new SqlServerHealthCheck(
                    connectionString,
                    options,
                    sp.GetRequiredService<ILogger<SqlServerHealthCheck>>()),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        /// <summary>
        /// Adds a SQL Server health check using a connection string from configuration.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionStringFactory">Factory to get connection string from service provider.</param>
        /// <param name="name">Name of the health check.</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursSqlServerCheck(
        ///         sp => sp.GetRequiredService&lt;IConfiguration&gt;().GetConnectionString("Default"),
        ///         "sqlserver-config");
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursSqlServerCheck(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, string> connectionStringFactory,
            string name = "sqlserver",
            Action<SqlServerHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new SqlServerHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new SqlServerHealthCheck(
                    connectionStringFactory(sp),
                    options,
                    sp.GetRequiredService<ILogger<SqlServerHealthCheck>>()),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region PostgreSQL Health Check

        /// <summary>
        /// Adds a PostgreSQL-specific health check.
        /// </summary>
        /// <typeparam name="TConnection">The DbConnection type (e.g., NpgsqlConnection).</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <param name="name">Name of the health check. Default is "postgresql".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// // With Npgsql provider
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursPostgreSqlCheck&lt;NpgsqlConnection&gt;(
        ///         configuration.GetConnectionString("PostgreSql"),
        ///         "postgresql-main",
        ///         options =>
        ///         {
        ///             options.CheckConnectionUsage = true;
        ///             options.CheckReplicationLag = true;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursPostgreSqlCheck<TConnection>(
            this IHealthChecksBuilder builder,
            string connectionString,
            string name = "postgresql",
            Action<PostgreSqlHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
            where TConnection : DbConnection
        {
            var options = new PostgreSqlHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new PostgreSqlHealthCheck(
                    connectionString,
                    options,
                    sp.GetRequiredService<ILogger<PostgreSqlHealthCheck>>(),
                    connStr => (DbConnection)Activator.CreateInstance(typeof(TConnection), connStr)!),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        /// <summary>
        /// Adds a PostgreSQL health check with a custom connection factory.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">PostgreSQL connection string.</param>
        /// <param name="connectionFactory">Factory to create DbConnection instances.</param>
        /// <param name="name">Name of the health check.</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursPostgreSqlCheck(
            this IHealthChecksBuilder builder,
            string connectionString,
            Func<string, DbConnection> connectionFactory,
            string name = "postgresql",
            Action<PostgreSqlHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new PostgreSqlHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new PostgreSqlHealthCheck(
                    connectionString,
                    options,
                    sp.GetRequiredService<ILogger<PostgreSqlHealthCheck>>(),
                    connectionFactory),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region MySQL Health Check

        /// <summary>
        /// Adds a MySQL-specific health check.
        /// </summary>
        /// <typeparam name="TConnection">The DbConnection type (e.g., MySqlConnection).</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">MySQL connection string.</param>
        /// <param name="name">Name of the health check. Default is "mysql".</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use. Default is Unhealthy.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// // With MySql.Data provider
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursMySqlCheck&lt;MySqlConnection&gt;(
        ///         configuration.GetConnectionString("MySql"),
        ///         "mysql-main",
        ///         options =>
        ///         {
        ///             options.CheckConnectionUsage = true;
        ///             options.CheckReplication = true;
        ///         });
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursMySqlCheck<TConnection>(
            this IHealthChecksBuilder builder,
            string connectionString,
            string name = "mysql",
            Action<MySqlHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
            where TConnection : DbConnection
        {
            var options = new MySqlHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new MySqlHealthCheck(
                    connectionString,
                    options,
                    sp.GetRequiredService<ILogger<MySqlHealthCheck>>(),
                    connStr => (DbConnection)Activator.CreateInstance(typeof(TConnection), connStr)!),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        /// <summary>
        /// Adds a MySQL health check with a custom connection factory.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="connectionString">MySQL connection string.</param>
        /// <param name="connectionFactory">Factory to create DbConnection instances.</param>
        /// <param name="name">Name of the health check.</param>
        /// <param name="configureOptions">Optional configuration for the health check.</param>
        /// <param name="failureStatus">The failure status to use.</param>
        /// <param name="tags">Tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursMySqlCheck(
            this IHealthChecksBuilder builder,
            string connectionString,
            Func<string, DbConnection> connectionFactory,
            string name = "mysql",
            Action<MySqlHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new MySqlHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new MySqlHealthCheck(
                    connectionString,
                    options,
                    sp.GetRequiredService<ILogger<MySqlHealthCheck>>(),
                    connectionFactory),
                failureStatus,
                tags ?? options.Tags,
                timeout));
        }

        #endregion

        #region Composite Health Checks

        /// <summary>
        /// Adds all standard database health checks for the specified DbContext.
        /// Includes both liveness and readiness checks.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to check.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursDbContextAllChecks&lt;AppDbContext&gt;();
        ///     
        /// // This adds:
        /// // - "AppDbContext-live" with tag "live"
        /// // - "AppDbContext-ready" with tag "ready"
        /// </code>
        /// </example>
        public static IHealthChecksBuilder AddMvp24HoursDbContextAllChecks<TContext>(
            this IHealthChecksBuilder builder)
            where TContext : DbContext
        {
            return builder
                .AddMvp24HoursDbContextLivenessCheck<TContext>()
                .AddMvp24HoursDbContextReadinessCheck<TContext>();
        }

        #endregion
    }
}

