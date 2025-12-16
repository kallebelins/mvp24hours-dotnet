//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Observability
{
    /// <summary>
    /// Configuration options for MongoDB observability features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control monitoring, logging, and metrics collection for MongoDB operations:
    /// <list type="bullet">
    ///   <item>Slow query detection and logging</item>
    ///   <item>OpenTelemetry tracing integration</item>
    ///   <item>Connection pool metrics</item>
    ///   <item>Structured logging with parameters</item>
    ///   <item>Command/query duration tracking</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMongoDbObservability(options =>
    /// {
    ///     options.EnableSlowQueryLogging = true;
    ///     options.SlowQueryThreshold = TimeSpan.FromSeconds(1);
    ///     options.EnableOpenTelemetry = true;
    ///     options.EnableConnectionPoolMetrics = true;
    ///     options.EnableStructuredLogging = true;
    ///     options.EnableDurationTracking = true;
    /// });
    /// </code>
    /// </example>
    [Serializable]
    public sealed class MongoDbObservabilityOptions
    {
        #region Slow Query Logging

        /// <summary>
        /// Gets or sets whether to enable slow query logging.
        /// Default is true.
        /// </summary>
        public bool EnableSlowQueryLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold duration for slow query detection.
        /// Queries exceeding this duration will be logged as warnings.
        /// Default is 500 milliseconds.
        /// </summary>
        public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets whether to log the full query filter for slow queries.
        /// Warning: May expose sensitive data. Default is false.
        /// </summary>
        public bool LogSlowQueryFilter { get; set; }

        /// <summary>
        /// Gets or sets whether to include explain output for slow queries.
        /// Note: This adds overhead. Default is false.
        /// </summary>
        public bool IncludeExplainForSlowQueries { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of slow queries to log per minute.
        /// Prevents log flooding. Default is 100.
        /// </summary>
        public int MaxSlowQueriesPerMinute { get; set; } = 100;

        #endregion

        #region OpenTelemetry

        /// <summary>
        /// Gets or sets whether to enable OpenTelemetry tracing integration.
        /// Default is false.
        /// </summary>
        public bool EnableOpenTelemetry { get; set; }

        /// <summary>
        /// Gets or sets the activity source name for OpenTelemetry.
        /// Default is "Mvp24Hours.MongoDb".
        /// </summary>
        public string ActivitySourceName { get; set; } = "Mvp24Hours.MongoDb";

        /// <summary>
        /// Gets or sets whether to record exception details in traces.
        /// Default is true.
        /// </summary>
        public bool RecordExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include database statements in traces.
        /// Warning: May expose sensitive data. Default is false.
        /// </summary>
        public bool IncludeStatementInTrace { get; set; }

        /// <summary>
        /// Gets or sets additional tags to include in all traces.
        /// </summary>
        public string[] AdditionalTraceTags { get; set; } = Array.Empty<string>();

        #endregion

        #region Connection Pool Metrics

        /// <summary>
        /// Gets or sets whether to enable connection pool metrics collection.
        /// Default is true.
        /// </summary>
        public bool EnableConnectionPoolMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for collecting connection pool metrics.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan ConnectionPoolMetricsInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable connection pool health alerts.
        /// Default is true.
        /// </summary>
        public bool EnableConnectionPoolAlerts { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection pool utilization threshold for alerts (0.0 to 1.0).
        /// Default is 0.8 (80% utilization).
        /// </summary>
        public double ConnectionPoolAlertThreshold { get; set; } = 0.8;

        #endregion

        #region Structured Logging

        /// <summary>
        /// Gets or sets whether to enable structured logging.
        /// Default is true in development, false in production.
        /// </summary>
        public bool EnableStructuredLogging { get; set; }
#if DEBUG
            = true;
#else
            = false;
#endif

        /// <summary>
        /// Gets or sets whether to log command parameters.
        /// Warning: May expose sensitive data. Default is false.
        /// </summary>
        public bool LogCommandParameters { get; set; }

        /// <summary>
        /// Gets or sets whether to log result counts.
        /// Default is true.
        /// </summary>
        public bool LogResultCounts { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum log message length before truncation.
        /// Default is 4096 characters.
        /// </summary>
        public int MaxLogMessageLength { get; set; } = 4096;

        /// <summary>
        /// Gets or sets sensitive field names to mask in logs.
        /// Default includes common sensitive fields.
        /// </summary>
        public string[] SensitiveFields { get; set; } = new[]
        {
            "password", "senha", "secret", "token", "apikey", "api_key",
            "creditcard", "credit_card", "cpf", "cnpj", "ssn", "pin"
        };

        #endregion

        #region Duration Tracking

        /// <summary>
        /// Gets or sets whether to enable command/query duration tracking.
        /// Default is true.
        /// </summary>
        public bool EnableDurationTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track individual operation durations.
        /// Default is true.
        /// </summary>
        public bool TrackIndividualOperations { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to collect duration percentiles (p50, p95, p99).
        /// Default is true.
        /// </summary>
        public bool CollectDurationPercentiles { get; set; } = true;

        /// <summary>
        /// Gets or sets the duration statistics aggregation window.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan DurationAggregationWindow { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the number of buckets for duration histogram.
        /// Default is 20.
        /// </summary>
        public int DurationHistogramBuckets { get; set; } = 20;

        #endregion

        #region General

        /// <summary>
        /// Gets or sets whether to enable all observability features.
        /// When true, enables slow query logging, connection pool metrics,
        /// structured logging, and duration tracking. OpenTelemetry must
        /// be enabled explicitly.
        /// </summary>
        public bool EnableAll
        {
            set
            {
                EnableSlowQueryLogging = value;
                EnableConnectionPoolMetrics = value;
                EnableStructuredLogging = value;
                EnableDurationTracking = value;
            }
        }

        /// <summary>
        /// Gets or sets the service name for metrics and traces.
        /// Default is null (uses application name).
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the environment name (e.g., "production", "staging").
        /// Default is null (uses ASPNETCORE_ENVIRONMENT).
        /// </summary>
        public string Environment { get; set; }

        #endregion
    }
}

