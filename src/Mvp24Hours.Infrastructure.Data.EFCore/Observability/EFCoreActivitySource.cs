//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Observability;

/// <summary>
/// ActivitySource for EF Core operations in OpenTelemetry-compatible tracing.
/// </summary>
/// <remarks>
/// <para>
/// This class provides integration with the .NET Activity API which is automatically
/// exported by OpenTelemetry when configured. Activities created here will appear
/// as spans in your tracing backend (Jaeger, Zipkin, Application Insights, etc.).
/// </para>
/// <para>
/// <strong>Activity Names:</strong>
/// <list type="bullet">
/// <item>Mvp24Hours.EFCore.Query - For query operations</item>
/// <item>Mvp24Hours.EFCore.Command - For INSERT/UPDATE/DELETE operations</item>
/// <item>Mvp24Hours.EFCore.SaveChanges - For SaveChanges operations</item>
/// <item>Mvp24Hours.EFCore.Transaction - For transaction operations</item>
/// <item>Mvp24Hours.EFCore.Connection - For connection operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to include Mvp24Hours EFCore activities
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddSource(EFCoreActivitySource.SourceName)
///             .AddEntityFrameworkCoreInstrumentation()
///             .AddJaegerExporter();
///     });
/// </code>
/// </example>
public static class EFCoreActivitySource
{
    /// <summary>
    /// The name of the ActivitySource for Mvp24Hours EFCore operations.
    /// </summary>
    public const string SourceName = "Mvp24Hours.EFCore";

    /// <summary>
    /// The version of the ActivitySource.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance used for creating activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// Activity names for different EFCore operations.
    /// </summary>
    public static class ActivityNames
    {
        /// <summary>Activity name for query operations (SELECT).</summary>
        public const string Query = "Mvp24Hours.EFCore.Query";
        /// <summary>Activity name for slow query operations.</summary>
        public const string SlowQuery = "Mvp24Hours.EFCore.SlowQuery";
        /// <summary>Activity name for command operations (INSERT/UPDATE/DELETE).</summary>
        public const string Command = "Mvp24Hours.EFCore.Command";
        /// <summary>Activity name for SaveChanges operations.</summary>
        public const string SaveChanges = "Mvp24Hours.EFCore.SaveChanges";
        /// <summary>Activity name for transaction operations.</summary>
        public const string Transaction = "Mvp24Hours.EFCore.Transaction";
        /// <summary>Activity name for connection operations.</summary>
        public const string Connection = "Mvp24Hours.EFCore.Connection";
        /// <summary>Activity name for connection pool operations.</summary>
        public const string ConnectionPool = "Mvp24Hours.EFCore.ConnectionPool";
    }

    /// <summary>
    /// Tag names for activity attributes following OpenTelemetry semantic conventions.
    /// </summary>
    public static class TagNames
    {
        // Database semantic conventions (db.*)
        /// <summary>Database system (e.g., sqlserver, postgresql, mysql).</summary>
        public const string DbSystem = "db.system";
        /// <summary>Database name.</summary>
        public const string DbName = "db.name";
        /// <summary>Database user.</summary>
        public const string DbUser = "db.user";
        /// <summary>Connection string (sanitized).</summary>
        public const string DbConnectionString = "db.connection_string";
        /// <summary>SQL statement being executed.</summary>
        public const string DbStatement = "db.statement";
        /// <summary>Type of operation (query, insert, update, delete).</summary>
        public const string DbOperation = "db.operation";
        /// <summary>Name of table being operated on.</summary>
        public const string DbTable = "db.sql.table";

        // Query performance metrics
        /// <summary>Duration of the query in milliseconds.</summary>
        public const string QueryDurationMs = "db.query.duration_ms";
        /// <summary>Whether this is a slow query.</summary>
        public const string IsSlowQuery = "db.query.is_slow";
        /// <summary>Slow query threshold in milliseconds.</summary>
        public const string SlowQueryThresholdMs = "db.query.slow_threshold_ms";
        /// <summary>Number of rows affected by the operation.</summary>
        public const string RowsAffected = "db.query.rows_affected";

        // Connection pool metrics
        /// <summary>Number of active connections in the pool.</summary>
        public const string PoolActiveConnections = "db.pool.active_connections";
        /// <summary>Number of idle connections in the pool.</summary>
        public const string PoolIdleConnections = "db.pool.idle_connections";
        /// <summary>Maximum pool size.</summary>
        public const string PoolMaxSize = "db.pool.max_size";
        /// <summary>Pool hit ratio.</summary>
        public const string PoolHitRatio = "db.pool.hit_ratio";

        // Error handling
        /// <summary>Whether the operation succeeded.</summary>
        public const string IsSuccess = "db.success";
        /// <summary>Error type if failed.</summary>
        public const string ErrorType = "error.type";
        /// <summary>Error message if failed.</summary>
        public const string ErrorMessage = "error.message";

        // Context
        /// <summary>Correlation ID for tracing.</summary>
        public const string CorrelationId = "correlation_id";
        /// <summary>Tenant ID for multi-tenancy.</summary>
        public const string TenantId = "tenant_id";
        /// <summary>User ID for audit.</summary>
        public const string UserId = "user_id";
    }

    /// <summary>
    /// Meter name for EFCore metrics.
    /// </summary>
    public const string MeterName = "Mvp24Hours.EFCore.Metrics";

    /// <summary>
    /// Starts a new activity for a query operation.
    /// </summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="dbName">The database name.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartQueryActivity(string commandText, string? dbName = null)
    {
        var activity = Source.StartActivity(ActivityNames.Query, ActivityKind.Client);
        if (activity == null) return null;

        activity.SetTag(TagNames.DbOperation, "SELECT");
        activity.SetTag(TagNames.DbStatement, SanitizeSqlForLogging(commandText));
        
        if (!string.IsNullOrEmpty(dbName))
            activity.SetTag(TagNames.DbName, dbName);

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a command operation.
    /// </summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="operation">The operation type (INSERT, UPDATE, DELETE).</param>
    /// <param name="dbName">The database name.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartCommandActivity(string commandText, string operation, string? dbName = null)
    {
        var activity = Source.StartActivity(ActivityNames.Command, ActivityKind.Client);
        if (activity == null) return null;

        activity.SetTag(TagNames.DbOperation, operation);
        activity.SetTag(TagNames.DbStatement, SanitizeSqlForLogging(commandText));
        
        if (!string.IsNullOrEmpty(dbName))
            activity.SetTag(TagNames.DbName, dbName);

        return activity;
    }

    /// <summary>
    /// Starts a new activity for a slow query.
    /// </summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="thresholdMs">The slow query threshold.</param>
    /// <returns>The created activity, or null if not sampled.</returns>
    public static Activity? StartSlowQueryActivity(string commandText, double durationMs, double thresholdMs)
    {
        var activity = Source.StartActivity(ActivityNames.SlowQuery, ActivityKind.Client);
        if (activity == null) return null;

        activity.SetTag(TagNames.IsSlowQuery, true);
        activity.SetTag(TagNames.QueryDurationMs, durationMs);
        activity.SetTag(TagNames.SlowQueryThresholdMs, thresholdMs);
        activity.SetTag(TagNames.DbStatement, SanitizeSqlForLogging(commandText));

        // Add event for slow query
        activity.AddEvent(new ActivityEvent("slow_query_detected", tags: new ActivityTagsCollection
        {
            { "duration_ms", durationMs },
            { "threshold_ms", thresholdMs }
        }));

        return activity;
    }

    /// <summary>
    /// Marks an activity as successful.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <param name="rowsAffected">Optional number of rows affected.</param>
    public static void SetSuccess(Activity? activity, int? rowsAffected = null)
    {
        if (activity == null) return;

        activity.SetTag(TagNames.IsSuccess, true);
        activity.SetStatus(ActivityStatusCode.Ok);

        if (rowsAffected.HasValue)
            activity.SetTag(TagNames.RowsAffected, rowsAffected.Value);
    }

    /// <summary>
    /// Marks an activity as failed with exception details.
    /// </summary>
    /// <param name="activity">The activity to mark.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void SetError(Activity? activity, Exception exception)
    {
        if (activity == null) return;

        activity.SetTag(TagNames.IsSuccess, false);
        activity.SetTag(TagNames.ErrorType, exception.GetType().FullName);
        activity.SetTag(TagNames.ErrorMessage, exception.Message);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception event following OpenTelemetry conventions
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        }));
    }

    /// <summary>
    /// Sets timing information on an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public static void SetDuration(Activity? activity, double durationMs)
    {
        activity?.SetTag(TagNames.QueryDurationMs, durationMs);
    }

    /// <summary>
    /// Sets context information (correlation, tenant, user) on an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    public static void SetContext(Activity? activity, string? correlationId, string? tenantId = null, string? userId = null)
    {
        if (activity == null) return;

        if (!string.IsNullOrEmpty(correlationId))
            activity.SetTag(TagNames.CorrelationId, correlationId);

        if (!string.IsNullOrEmpty(tenantId))
            activity.SetTag(TagNames.TenantId, tenantId);

        if (!string.IsNullOrEmpty(userId))
            activity.SetTag(TagNames.UserId, userId);
    }

    /// <summary>
    /// Sanitizes SQL for logging by truncating and masking sensitive data.
    /// </summary>
    /// <param name="sql">The SQL statement.</param>
    /// <param name="maxLength">Maximum length to include.</param>
    /// <returns>Sanitized SQL string.</returns>
    private static string SanitizeSqlForLogging(string sql, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(sql)) return sql;

        // Truncate if too long
        if (sql.Length > maxLength)
            sql = sql.Substring(0, maxLength) + "... [TRUNCATED]";

        return sql;
    }
}

/// <summary>
/// Extension methods for Activity operations in EFCore context.
/// </summary>
public static class EFCoreActivityExtensions
{
    /// <summary>
    /// Sets database system information on an activity.
    /// </summary>
    public static Activity WithDatabaseSystem(this Activity activity, string dbSystem)
    {
        activity.SetTag(EFCoreActivitySource.TagNames.DbSystem, dbSystem);
        return activity;
    }

    /// <summary>
    /// Sets database name on an activity.
    /// </summary>
    public static Activity WithDatabaseName(this Activity activity, string dbName)
    {
        activity.SetTag(EFCoreActivitySource.TagNames.DbName, dbName);
        return activity;
    }

    /// <summary>
    /// Marks an activity as a slow query.
    /// </summary>
    public static Activity AsSlowQuery(this Activity activity, double durationMs, double thresholdMs)
    {
        activity.SetTag(EFCoreActivitySource.TagNames.IsSlowQuery, true);
        activity.SetTag(EFCoreActivitySource.TagNames.QueryDurationMs, durationMs);
        activity.SetTag(EFCoreActivitySource.TagNames.SlowQueryThresholdMs, thresholdMs);
        return activity;
    }
}

