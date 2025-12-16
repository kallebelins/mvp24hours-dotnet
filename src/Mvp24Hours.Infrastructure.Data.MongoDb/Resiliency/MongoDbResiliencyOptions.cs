//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency
{
    /// <summary>
    /// Configuration options for MongoDB resiliency features including connection recovery,
    /// retry policies, circuit breaker, timeouts, and failover handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options provide enterprise-grade resiliency for MongoDB operations:
    /// <list type="bullet">
    ///   <item><b>Connection Resiliency</b>: Auto-reconnect with configurable backoff</item>
    ///   <item><b>Retry Policies</b>: Configurable retry with exponential backoff</item>
    ///   <item><b>Circuit Breaker</b>: Fail-fast when MongoDB is unavailable</item>
    ///   <item><b>Timeouts</b>: Per-operation timeout configuration</item>
    ///   <item><b>Failover</b>: Automatic failover for replica sets</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.DatabaseName = "mydb";
    ///     options.ConnectionString = "mongodb://localhost:27017";
    /// })
    /// .AddMongoDbResiliency(resiliency =>
    /// {
    ///     // Connection resiliency
    ///     resiliency.EnableAutoReconnect = true;
    ///     resiliency.MaxReconnectAttempts = 5;
    ///     
    ///     // Retry policy
    ///     resiliency.RetryCount = 3;
    ///     resiliency.RetryBaseDelayMilliseconds = 100;
    ///     resiliency.UseExponentialBackoff = true;
    ///     
    ///     // Circuit breaker
    ///     resiliency.EnableCircuitBreaker = true;
    ///     resiliency.CircuitBreakerFailureThreshold = 5;
    ///     resiliency.CircuitBreakerDurationSeconds = 30;
    ///     
    ///     // Timeouts
    ///     resiliency.DefaultOperationTimeoutSeconds = 30;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class MongoDbResiliencyOptions
    {
        #region Connection Resiliency

        /// <summary>
        /// Gets or sets whether automatic reconnection is enabled.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// When enabled, the connection manager will automatically attempt to reconnect
        /// when a connection is lost. Works in conjunction with <see cref="MaxReconnectAttempts"/>.
        /// </remarks>
        public bool EnableAutoReconnect { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of reconnection attempts.
        /// Default is 5.
        /// </summary>
        /// <remarks>
        /// After this many failed attempts, the connection is considered permanently lost
        /// and operations will fail until the service is restarted or manually recovered.
        /// </remarks>
        public int MaxReconnectAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the initial delay between reconnection attempts in milliseconds.
        /// Default is 1000 (1 second).
        /// </summary>
        public int ReconnectDelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum delay between reconnection attempts in milliseconds.
        /// Default is 30000 (30 seconds).
        /// </summary>
        /// <remarks>
        /// When using exponential backoff, this caps the maximum delay between attempts.
        /// </remarks>
        public int MaxReconnectDelayMilliseconds { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to use exponential backoff for reconnection attempts.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoffForReconnect { get; set; } = true;

        /// <summary>
        /// Gets or sets the jitter factor for reconnection delays (0.0 to 1.0).
        /// Default is 0.2 (20% jitter).
        /// </summary>
        /// <remarks>
        /// Jitter adds randomness to delay times to prevent thundering herd scenarios
        /// where multiple clients reconnect simultaneously.
        /// </remarks>
        public double ReconnectJitterFactor { get; set; } = 0.2;

        #endregion

        #region Retry Policy

        /// <summary>
        /// Gets or sets whether retry is enabled for transient failures.
        /// Default is true.
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of retry attempts for transient failures.
        /// Default is 3.
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base delay between retry attempts in milliseconds.
        /// Default is 100.
        /// </summary>
        public int RetryBaseDelayMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum delay between retry attempts in milliseconds.
        /// Default is 5000 (5 seconds).
        /// </summary>
        public int RetryMaxDelayMilliseconds { get; set; } = 5000;

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// When true, delay increases exponentially: base * 2^attempt.
        /// When false, uses constant delay between retries.
        /// </remarks>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the jitter factor for retry delays (0.0 to 1.0).
        /// Default is 0.2 (20% jitter).
        /// </summary>
        public double RetryJitterFactor { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets additional exception types that should trigger a retry.
        /// </summary>
        /// <remarks>
        /// By default, the following exceptions trigger retries:
        /// <list type="bullet">
        ///   <item>MongoConnectionException</item>
        ///   <item>MongoNotPrimaryException</item>
        ///   <item>MongoNodeIsRecoveringException</item>
        ///   <item>TimeoutException</item>
        /// </list>
        /// Use this property to add custom exception types.
        /// </remarks>
        public List<Type> AdditionalRetryableExceptions { get; set; } = new();

        /// <summary>
        /// Gets or sets exception types that should never trigger a retry.
        /// </summary>
        /// <remarks>
        /// These exceptions will always fail fast, even if they would normally be retryable.
        /// Useful for business logic exceptions that shouldn't be retried.
        /// </remarks>
        public List<Type> NonRetryableExceptions { get; set; } = new();

        #endregion

        #region Circuit Breaker

        /// <summary>
        /// Gets or sets whether the circuit breaker is enabled.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// The circuit breaker prevents operations when MongoDB is unavailable,
        /// allowing the system to fail fast and recover gracefully.
        /// </remarks>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of failures before the circuit opens.
        /// Default is 5.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time window for counting failures in seconds.
        /// Default is 60 (1 minute).
        /// </summary>
        /// <remarks>
        /// Failures older than this window are not counted toward the threshold.
        /// </remarks>
        public int CircuitBreakerSamplingDurationSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets how long the circuit stays open before transitioning to half-open, in seconds.
        /// Default is 30.
        /// </summary>
        /// <remarks>
        /// When the circuit is open, all operations fail immediately.
        /// After this duration, the circuit transitions to half-open and allows a test request.
        /// </remarks>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the minimum throughput required before the circuit can open.
        /// Default is 10.
        /// </summary>
        /// <remarks>
        /// If fewer than this many operations occur in the sampling window,
        /// the circuit will not open regardless of failure rate.
        /// </remarks>
        public int CircuitBreakerMinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Gets or sets the failure rate threshold for opening the circuit (0.0 to 1.0).
        /// Default is 0.5 (50%).
        /// </summary>
        /// <remarks>
        /// Alternative to failure count threshold. When set, the circuit opens
        /// if the failure rate exceeds this percentage.
        /// </remarks>
        public double? CircuitBreakerFailureRateThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to track circuit breaker metrics.
        /// Default is true.
        /// </summary>
        public bool TrackCircuitBreakerMetrics { get; set; } = true;

        #endregion

        #region Timeouts

        /// <summary>
        /// Gets or sets whether operation timeouts are enabled.
        /// Default is true.
        /// </summary>
        public bool EnableOperationTimeout { get; set; } = true;

        /// <summary>
        /// Gets or sets the default operation timeout in seconds.
        /// Default is 30.
        /// </summary>
        /// <remarks>
        /// This timeout applies to individual database operations.
        /// Set to 0 for no timeout (not recommended for production).
        /// </remarks>
        public int DefaultOperationTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the timeout for read operations in seconds.
        /// Default is null (uses <see cref="DefaultOperationTimeoutSeconds"/>).
        /// </summary>
        public int? ReadOperationTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout for write operations in seconds.
        /// Default is null (uses <see cref="DefaultOperationTimeoutSeconds"/>).
        /// </summary>
        public int? WriteOperationTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout for bulk operations in seconds.
        /// Default is 120 (2 minutes).
        /// </summary>
        /// <remarks>
        /// Bulk operations may take longer due to the volume of data being processed.
        /// </remarks>
        public int BulkOperationTimeoutSeconds { get; set; } = 120;

        #endregion

        #region Failover

        /// <summary>
        /// Gets or sets whether automatic failover is enabled for replica sets.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// When enabled, the driver will automatically switch to a new primary
        /// when the current primary becomes unavailable.
        /// </remarks>
        public bool EnableAutomaticFailover { get; set; } = true;

        /// <summary>
        /// Gets or sets the server selection timeout in seconds.
        /// Default is 30.
        /// </summary>
        /// <remarks>
        /// How long to wait for a suitable server (e.g., new primary) after failover.
        /// </remarks>
        public int ServerSelectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the heartbeat frequency in seconds.
        /// Default is 10.
        /// </summary>
        /// <remarks>
        /// How often the driver checks server status. Lower values detect failover faster
        /// but increase network overhead.
        /// </remarks>
        public int HeartbeatFrequencySeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to enable server monitoring.
        /// Default is true.
        /// </summary>
        public bool EnableServerMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to allow reads during a primary election.
        /// Default is true (reads from secondaries when no primary is available).
        /// </summary>
        public bool AllowReadsWithoutPrimary { get; set; } = true;

        #endregion

        #region Logging

        /// <summary>
        /// Gets or sets whether to log retry attempts.
        /// Default is true.
        /// </summary>
        public bool LogRetryAttempts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log circuit breaker state changes.
        /// Default is true.
        /// </summary>
        public bool LogCircuitBreakerStateChanges { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log connection events.
        /// Default is true.
        /// </summary>
        public bool LogConnectionEvents { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log timeout events.
        /// Default is true.
        /// </summary>
        public bool LogTimeoutEvents { get; set; } = true;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the effective timeout for read operations.
        /// </summary>
        public TimeSpan GetReadTimeout()
        {
            var seconds = ReadOperationTimeoutSeconds ?? DefaultOperationTimeoutSeconds;
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.MaxValue;
        }

        /// <summary>
        /// Gets the effective timeout for write operations.
        /// </summary>
        public TimeSpan GetWriteTimeout()
        {
            var seconds = WriteOperationTimeoutSeconds ?? DefaultOperationTimeoutSeconds;
            return seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.MaxValue;
        }

        /// <summary>
        /// Gets the timeout for bulk operations.
        /// </summary>
        public TimeSpan GetBulkOperationTimeout()
        {
            return BulkOperationTimeoutSeconds > 0
                ? TimeSpan.FromSeconds(BulkOperationTimeoutSeconds)
                : TimeSpan.MaxValue;
        }

        /// <summary>
        /// Creates a default configuration optimized for production environments.
        /// </summary>
        public static MongoDbResiliencyOptions CreateProduction()
        {
            return new MongoDbResiliencyOptions
            {
                // Connection
                EnableAutoReconnect = true,
                MaxReconnectAttempts = 10,
                ReconnectDelayMilliseconds = 1000,
                MaxReconnectDelayMilliseconds = 60000,
                UseExponentialBackoffForReconnect = true,
                ReconnectJitterFactor = 0.2,

                // Retry
                EnableRetry = true,
                RetryCount = 3,
                RetryBaseDelayMilliseconds = 200,
                RetryMaxDelayMilliseconds = 10000,
                UseExponentialBackoff = true,
                RetryJitterFactor = 0.2,

                // Circuit Breaker
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerSamplingDurationSeconds = 60,
                CircuitBreakerDurationSeconds = 30,
                CircuitBreakerMinimumThroughput = 10,

                // Timeouts
                EnableOperationTimeout = true,
                DefaultOperationTimeoutSeconds = 30,
                BulkOperationTimeoutSeconds = 300,

                // Failover
                EnableAutomaticFailover = true,
                ServerSelectionTimeoutSeconds = 30,
                HeartbeatFrequencySeconds = 10,

                // Logging
                LogRetryAttempts = true,
                LogCircuitBreakerStateChanges = true,
                LogConnectionEvents = true,
                LogTimeoutEvents = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for development environments.
        /// </summary>
        public static MongoDbResiliencyOptions CreateDevelopment()
        {
            return new MongoDbResiliencyOptions
            {
                // Connection - faster recovery for dev
                EnableAutoReconnect = true,
                MaxReconnectAttempts = 3,
                ReconnectDelayMilliseconds = 500,
                MaxReconnectDelayMilliseconds = 5000,

                // Retry - fewer retries for faster feedback
                EnableRetry = true,
                RetryCount = 2,
                RetryBaseDelayMilliseconds = 100,
                RetryMaxDelayMilliseconds = 1000,

                // Circuit Breaker - disabled for easier debugging
                EnableCircuitBreaker = false,

                // Timeouts - longer for debugging
                EnableOperationTimeout = true,
                DefaultOperationTimeoutSeconds = 60,
                BulkOperationTimeoutSeconds = 300,

                // Failover
                EnableAutomaticFailover = true,
                ServerSelectionTimeoutSeconds = 10,
                HeartbeatFrequencySeconds = 5,

                // Logging - verbose
                LogRetryAttempts = true,
                LogCircuitBreakerStateChanges = true,
                LogConnectionEvents = true,
                LogTimeoutEvents = true
            };
        }

        #endregion
    }
}

