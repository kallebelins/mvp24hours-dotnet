//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks
{
    /// <summary>
    /// Generic health check for any EF Core DbContext.
    /// Verifies database connectivity and optionally performs schema validation.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to check.</typeparam>
    /// <remarks>
    /// <para>
    /// This health check performs the following validations:
    /// <list type="bullet">
    /// <item><strong>Connectivity</strong> - Verifies database connection using CanConnect</item>
    /// <item><strong>Query execution</strong> - Optionally executes a custom health query</item>
    /// <item><strong>Response time</strong> - Measures and reports query latency</item>
    /// <item><strong>Pending migrations</strong> - Optionally checks for pending migrations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddDbContextCheck&lt;AppDbContext&gt;("database", options =>
    ///     {
    ///         options.HealthQuery = "SELECT 1";
    ///         options.CheckPendingMigrations = true;
    ///         options.DegradedThresholdMs = 500;
    ///         options.FailureThresholdMs = 2000;
    ///     });
    /// </code>
    /// </example>
    public class DbContextHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly TContext _dbContext;
        private readonly DbContextHealthCheckOptions _options;
        private readonly ILogger<DbContextHealthCheck<TContext>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextHealthCheck{TContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext instance to check.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public DbContextHealthCheck(
            TContext dbContext,
            IOptions<DbContextHealthCheckOptions> options,
            ILogger<DbContextHealthCheck<TContext>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _options = options?.Value ?? new DbContextHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "dbcontexthealthcheck-checkhealthasync-start");

            var data = new Dictionary<string, object>
            {
                ["database"] = _dbContext.Database.GetDbConnection().Database,
                ["dataSource"] = SanitizeConnectionString(_dbContext.Database.GetDbConnection().ConnectionString),
                ["providerName"] = _dbContext.Database.ProviderName ?? "Unknown"
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Step 1: Check basic connectivity
                if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
                {
                    _logger.LogError("Database connection check failed for {DbContext}", typeof(TContext).Name);
                    return HealthCheckResult.Unhealthy(
                        description: $"Cannot connect to database for {typeof(TContext).Name}",
                        data: data);
                }

                // Step 2: Execute custom health query if provided
                if (!string.IsNullOrWhiteSpace(_options.HealthQuery))
                {
                    await ExecuteHealthQueryAsync(cancellationToken);
                }

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;

                // Step 3: Check pending migrations if enabled
                if (_options.CheckPendingMigrations)
                {
                    var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                    var pendingList = new List<string>(pendingMigrations);
                    data["pendingMigrations"] = pendingList.Count;

                    if (pendingList.Count > 0)
                    {
                        data["pendingMigrationsList"] = pendingList;
                        
                        if (_options.FailOnPendingMigrations)
                        {
                            _logger.LogWarning(
                                "Database {DbContext} has {Count} pending migrations",
                                typeof(TContext).Name,
                                pendingList.Count);

                            return HealthCheckResult.Degraded(
                                description: $"Database has {pendingList.Count} pending migrations",
                                data: data);
                        }
                    }
                }

                // Step 4: Evaluate response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    _logger.LogWarning(
                        "Database {DbContext} response time {ElapsedMs}ms exceeded failure threshold {ThresholdMs}ms",
                        typeof(TContext).Name,
                        stopwatch.ElapsedMilliseconds,
                        _options.FailureThresholdMs);

                    return HealthCheckResult.Unhealthy(
                        description: $"Database response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    _logger.LogWarning(
                        "Database {DbContext} response time {ElapsedMs}ms exceeded degraded threshold {ThresholdMs}ms",
                        typeof(TContext).Name,
                        stopwatch.ElapsedMilliseconds,
                        _options.DegradedThresholdMs);

                    return HealthCheckResult.Degraded(
                        description: $"Database response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "dbcontexthealthcheck-checkhealthasync-healthy");

                return HealthCheckResult.Healthy(
                    description: $"Database connection is healthy (response time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "Database health check failed for {DbContext}", typeof(TContext).Name);

                TelemetryHelper.Execute(TelemetryLevels.Error, "dbcontexthealthcheck-checkhealthasync-error", ex.Message);

                return HealthCheckResult.Unhealthy(
                    description: $"Database health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }

        private async Task ExecuteHealthQueryAsync(CancellationToken cancellationToken)
        {
            var connection = _dbContext.Database.GetDbConnection();
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = _options.HealthQuery;
            command.CommandTimeout = _options.QueryTimeoutSeconds;

            await command.ExecuteScalarAsync(cancellationToken);
        }

        private static string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Unknown";

            // Remove sensitive information like passwords
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd|User Id|Uid|User)=[^;]+",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
}

