//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Configuration
{
    /// <summary>
    /// Configuration options for EF Core connection resiliency and retry policies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive configuration for database connection resiliency including:
    /// <list type="bullet">
    /// <item><strong>Retry policies</strong> - Configure retries for transient failures</item>
    /// <item><strong>Command timeouts</strong> - Configure timeouts per query type</item>
    /// <item><strong>Connection pooling</strong> - Optimize connection management</item>
    /// <item><strong>Exception filtering</strong> - Define which exceptions trigger retries</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursDbContextWithResilience&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.ConnectionString = "Server=...";
    ///     options.EnableRetryOnFailure = true;
    ///     options.MaxRetryCount = 5;
    ///     options.MaxRetryDelaySeconds = 30;
    ///     options.CommandTimeoutSeconds = 60;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class EFCoreResilienceOptions
    {
        #region Connection Retry Options

        /// <summary>
        /// When true, enables automatic retry on transient database failures.
        /// Uses EF Core's built-in <see cref="Microsoft.EntityFrameworkCore.SqlServerRetryingExecutionStrategy"/>.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Transient failures include:
        /// <list type="bullet">
        /// <item>Network connectivity issues</item>
        /// <item>Database server restarts</item>
        /// <item>Timeouts due to resource contention</item>
        /// <item>Azure SQL DTU throttling</item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool EnableRetryOnFailure { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts before failing.
        /// Default is 6 (EF Core default). Set to 0 to disable retries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With exponential backoff, 6 retries can span approximately:
        /// 1s + 2s + 4s + 8s + 16s + 32s = 63 seconds total wait time.
        /// </para>
        /// </remarks>
        public int MaxRetryCount { get; set; } = 6;

        /// <summary>
        /// Maximum delay between retries in seconds.
        /// Default is 30 seconds. The actual delay uses exponential backoff capped at this value.
        /// </summary>
        public int MaxRetryDelaySeconds { get; set; } = 30;

        /// <summary>
        /// Additional SQL error codes that should trigger retries.
        /// Common transient errors are already included by default.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Default transient error codes include:
        /// <list type="bullet">
        /// <item>-2 (Timeout)</item>
        /// <item>20 (Server not found)</item>
        /// <item>64 (Connection was forcibly closed)</item>
        /// <item>233 (Connection initialization error)</item>
        /// <item>10053 (Connection aborted by software)</item>
        /// <item>10054 (Connection reset by peer)</item>
        /// <item>10060 (Connection timed out)</item>
        /// <item>40197 (Error processing request)</item>
        /// <item>40501 (Service busy)</item>
        /// <item>40613 (Database unavailable)</item>
        /// <item>49918 (Not enough resources)</item>
        /// <item>49919 (Not enough resources)</item>
        /// <item>49920 (Too many requests)</item>
        /// </list>
        /// </para>
        /// </remarks>
        public ICollection<int> AdditionalTransientErrorNumbers { get; set; } = new List<int>();

        /// <summary>
        /// Additional exception types that should trigger retries.
        /// Use for custom exceptions that represent transient failures.
        /// </summary>
        /// <example>
        /// <code>
        /// options.TransientExceptionTypes.Add(typeof(CustomTransientException));
        /// </code>
        /// </example>
        public ICollection<Type> TransientExceptionTypes { get; set; } = new List<Type>();

        #endregion

        #region Command Timeout Options

        /// <summary>
        /// Default command timeout in seconds for all database operations.
        /// Default is 30 seconds. Set to 0 for infinite timeout (not recommended).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value sets <see cref="Microsoft.EntityFrameworkCore.DbContext.Database"/> CommandTimeout.
        /// Consider increasing for:
        /// <list type="bullet">
        /// <item>Complex reporting queries</item>
        /// <item>Bulk operations</item>
        /// <item>Database migrations</item>
        /// </list>
        /// </para>
        /// </remarks>
        public int CommandTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Command timeout in seconds for read operations (SELECT queries).
        /// When null, uses <see cref="CommandTimeoutSeconds"/>. Set for read-specific timeout.
        /// </summary>
        public int? ReadCommandTimeoutSeconds { get; set; }

        /// <summary>
        /// Command timeout in seconds for write operations (INSERT, UPDATE, DELETE).
        /// When null, uses <see cref="CommandTimeoutSeconds"/>. Set for write-specific timeout.
        /// </summary>
        public int? WriteCommandTimeoutSeconds { get; set; }

        /// <summary>
        /// Command timeout in seconds for bulk operations.
        /// Default is 120 seconds. Bulk operations typically take longer.
        /// </summary>
        public int BulkCommandTimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Command timeout in seconds for migration operations.
        /// Default is 300 seconds (5 minutes). Migrations may involve schema changes.
        /// </summary>
        public int MigrationCommandTimeoutSeconds { get; set; } = 300;

        #endregion

        #region Connection Pooling Options

        /// <summary>
        /// When true, enables DbContext pooling for better performance.
        /// Pools DbContext instances to reduce allocation overhead.
        /// Default is true for production scenarios.
        /// </summary>
        /// <remarks>
        /// <para>
        /// DbContext pooling:
        /// <list type="bullet">
        /// <item>Reduces memory allocations</item>
        /// <item>Improves startup time for each request</item>
        /// <item>Reuses DbContext instances after reset</item>
        /// </list>
        /// </para>
        /// <para>
        /// ⚠️ <strong>Important</strong>: When pooling is enabled, avoid storing state in DbContext
        /// as it may leak between requests.
        /// </para>
        /// </remarks>
        public bool EnableDbContextPooling { get; set; } = true;

        /// <summary>
        /// Maximum number of DbContext instances to retain in the pool.
        /// Default is 1024. Adjust based on expected concurrent operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sizing guidelines:
        /// <list type="bullet">
        /// <item>Low traffic: 32-128</item>
        /// <item>Medium traffic: 128-512</item>
        /// <item>High traffic: 512-1024</item>
        /// </list>
        /// </para>
        /// </remarks>
        public int PoolSize { get; set; } = 1024;

        #endregion

        #region Circuit Breaker Options

        /// <summary>
        /// When true, enables circuit breaker pattern for database connections.
        /// Opens circuit after consecutive failures to prevent cascade failures.
        /// Default is false (opt-in feature).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Circuit breaker states:
        /// <list type="bullet">
        /// <item><strong>Closed</strong> - Normal operation, requests pass through</item>
        /// <item><strong>Open</strong> - Too many failures, requests fail immediately</item>
        /// <item><strong>Half-Open</strong> - Testing if service recovered</item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool EnableCircuitBreaker { get; set; }

        /// <summary>
        /// Number of consecutive failures before opening the circuit.
        /// Default is 5 failures.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration in seconds to keep circuit open before testing recovery.
        /// Default is 30 seconds.
        /// </summary>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;

        #endregion

        #region Logging and Diagnostics

        /// <summary>
        /// When true, logs retry attempts with detailed information.
        /// Useful for diagnosing transient failures. Default is true.
        /// </summary>
        public bool LogRetryAttempts { get; set; } = true;

        /// <summary>
        /// When true, logs connection pool statistics periodically.
        /// Default is false. Enable for performance monitoring.
        /// </summary>
        public bool LogPoolStatistics { get; set; }

        /// <summary>
        /// Interval in seconds for logging pool statistics.
        /// Default is 60 seconds (1 minute).
        /// </summary>
        public int PoolStatisticsLogIntervalSeconds { get; set; } = 60;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the effective timeout for read operations.
        /// </summary>
        public int GetReadTimeout() => ReadCommandTimeoutSeconds ?? CommandTimeoutSeconds;

        /// <summary>
        /// Gets the effective timeout for write operations.
        /// </summary>
        public int GetWriteTimeout() => WriteCommandTimeoutSeconds ?? CommandTimeoutSeconds;

        /// <summary>
        /// Creates default options optimized for production workloads.
        /// </summary>
        public static EFCoreResilienceOptions Production() => new EFCoreResilienceOptions
        {
            EnableRetryOnFailure = true,
            MaxRetryCount = 6,
            MaxRetryDelaySeconds = 30,
            CommandTimeoutSeconds = 30,
            EnableDbContextPooling = true,
            PoolSize = 1024,
            LogRetryAttempts = true,
            LogPoolStatistics = false
        };

        /// <summary>
        /// Creates default options optimized for development.
        /// </summary>
        public static EFCoreResilienceOptions Development() => new EFCoreResilienceOptions
        {
            EnableRetryOnFailure = true,
            MaxRetryCount = 3,
            MaxRetryDelaySeconds = 5,
            CommandTimeoutSeconds = 60,
            EnableDbContextPooling = false, // Easier debugging without pooling
            LogRetryAttempts = true,
            LogPoolStatistics = true,
            PoolStatisticsLogIntervalSeconds = 30
        };

        /// <summary>
        /// Creates options for Azure SQL with recommended settings.
        /// </summary>
        public static EFCoreResilienceOptions AzureSql() => new EFCoreResilienceOptions
        {
            EnableRetryOnFailure = true,
            MaxRetryCount = 10, // More retries for cloud
            MaxRetryDelaySeconds = 60,
            CommandTimeoutSeconds = 60,
            EnableDbContextPooling = true,
            PoolSize = 512,
            LogRetryAttempts = true,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerDurationSeconds = 60,
            // Azure SQL specific transient errors
            AdditionalTransientErrorNumbers = new List<int>
            {
                4060,  // Cannot open database
                4221,  // Login to read-secondary failed
                40143, // Connection could not be initialized
                40549, // Session terminated (long-running transaction)
                40550, // Session terminated (too many locks)
                40551, // Session terminated (excessive TEMPDB)
                40552, // Session terminated (excessive log space)
                40553, // Session terminated (excessive memory)
                40615, // Cannot connect due to firewall
                40627, // Operation in progress
            }
        };

        /// <summary>
        /// Creates options with no resilience features (for testing).
        /// </summary>
        public static EFCoreResilienceOptions NoResilience() => new EFCoreResilienceOptions
        {
            EnableRetryOnFailure = false,
            EnableDbContextPooling = false,
            EnableCircuitBreaker = false,
            LogRetryAttempts = false
        };

        #endregion
    }
}

