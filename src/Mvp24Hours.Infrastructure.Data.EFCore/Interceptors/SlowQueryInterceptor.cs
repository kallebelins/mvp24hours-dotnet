//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;

/// <summary>
/// EF Core interceptor dedicated to slow query detection and logging with OpenTelemetry integration.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor monitors all database operations and identifies slow queries based on configurable thresholds.
/// It integrates with OpenTelemetry for distributed tracing and metrics collection.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
///   <item>Configurable slow query threshold</item>
///   <item>Different thresholds for reads vs writes</item>
///   <item>OpenTelemetry Activity creation for slow queries</item>
///   <item>Metrics recording via EFCoreMetrics</item>
///   <item>Structured logging with ILogger</item>
///   <item>Callback support for custom alerting</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage:
/// services.AddDbContext&lt;AppDbContext&gt;(options =>
/// {
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new SlowQueryInterceptor(
///                slowQueryThreshold: TimeSpan.FromMilliseconds(500)));
/// });
/// 
/// // With OpenTelemetry metrics:
/// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
/// {
///     var metrics = sp.GetService&lt;EFCoreMetrics&gt;();
///     var logger = sp.GetRequiredService&lt;ILogger&lt;SlowQueryInterceptor&gt;&gt;();
///     
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new SlowQueryInterceptor(
///                slowQueryThreshold: TimeSpan.FromMilliseconds(500),
///                metrics: metrics,
///                logger: logger,
///                onSlowQueryDetected: (sql, duration) => AlertSlowQuery(sql, duration)));
/// });
/// </code>
/// </example>
public class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger? _logger;
    private readonly EFCoreMetrics? _metrics;
    private readonly TimeSpan _slowQueryThreshold;
    private readonly TimeSpan _writeSlowQueryThreshold;
    private readonly bool _createActivities;
    private readonly Action<string, TimeSpan, string?>? _onSlowQueryDetected;

    /// <summary>
    /// Initializes a new instance of <see cref="SlowQueryInterceptor"/>.
    /// </summary>
    /// <param name="slowQueryThreshold">Threshold for slow queries. Default is 1 second.</param>
    /// <param name="writeSlowQueryThreshold">Threshold for write operations. If null, uses slowQueryThreshold.</param>
    /// <param name="logger">Optional ILogger for structured logging.</param>
    /// <param name="metrics">Optional EFCoreMetrics for metric collection.</param>
    /// <param name="createActivities">If true, creates OpenTelemetry activities for slow queries. Default is true.</param>
    /// <param name="onSlowQueryDetected">Optional callback invoked when a slow query is detected. Parameters: (sql, duration, dbName).</param>
    public SlowQueryInterceptor(
        TimeSpan? slowQueryThreshold = null,
        TimeSpan? writeSlowQueryThreshold = null,
        ILogger? logger = null,
        EFCoreMetrics? metrics = null,
        bool createActivities = true,
        Action<string, TimeSpan, string?>? onSlowQueryDetected = null)
    {
        _slowQueryThreshold = slowQueryThreshold ?? TimeSpan.FromSeconds(1);
        _writeSlowQueryThreshold = writeSlowQueryThreshold ?? _slowQueryThreshold;
        _logger = logger;
        _metrics = metrics;
        _createActivities = createActivities;
        _onSlowQueryDetected = onSlowQueryDetected;
    }

    #region Reader (SELECT) Operations

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        CheckSlowQuery(command, eventData.Duration, "SELECT");
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        CheckSlowQuery(command, eventData.Duration, "SELECT");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    #endregion

    #region Non-Query (INSERT/UPDATE/DELETE) Operations

    /// <inheritdoc />
    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        var operation = DetectOperation(command.CommandText);
        CheckSlowQuery(command, eventData.Duration, operation, isWrite: true);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var operation = DetectOperation(command.CommandText);
        CheckSlowQuery(command, eventData.Duration, operation, isWrite: true);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    #endregion

    #region Scalar Operations

    /// <inheritdoc />
    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        CheckSlowQuery(command, eventData.Duration, "SCALAR");
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        CheckSlowQuery(command, eventData.Duration, "SCALAR");
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    #endregion

    private void CheckSlowQuery(DbCommand command, TimeSpan duration, string operation, bool isWrite = false)
    {
        var threshold = isWrite ? _writeSlowQueryThreshold : _slowQueryThreshold;

        if (duration < threshold)
            return;

        var durationMs = duration.TotalMilliseconds;
        var thresholdMs = threshold.TotalMilliseconds;
        var commandText = command.CommandText;
        var dbName = command.Connection?.Database;

        // Record metrics
        _metrics?.RecordSlowQuery(durationMs, thresholdMs, dbName);

        // Create OpenTelemetry activity
        if (_createActivities)
        {
            using var activity = EFCoreActivitySource.StartSlowQueryActivity(commandText, durationMs, thresholdMs);
            if (activity != null)
            {
                activity.SetTag(EFCoreActivitySource.TagNames.DbOperation, operation);
                if (!string.IsNullOrEmpty(dbName))
                    activity.SetTag(EFCoreActivitySource.TagNames.DbName, dbName);
                EFCoreActivitySource.SetSuccess(activity);
            }
        }

        // Log the slow query
        LogSlowQuery(commandText, duration, threshold, operation, dbName, command.Parameters);

        // Invoke callback
        _onSlowQueryDetected?.Invoke(commandText, duration, dbName);
    }

    private void LogSlowQuery(
        string commandText,
        TimeSpan duration,
        TimeSpan threshold,
        string operation,
        string? dbName,
        DbParameterCollection parameters)
    {
        var durationMs = duration.TotalMilliseconds;
        var thresholdMs = threshold.TotalMilliseconds;

        if (_logger != null)
        {
            _logger.LogWarning(
                "⚠️ SLOW QUERY DETECTED - Operation: {Operation}, Duration: {DurationMs:F2}ms (Threshold: {ThresholdMs}ms), " +
                "Database: {Database}, SQL: {Sql}",
                operation,
                durationMs,
                thresholdMs,
                dbName ?? "unknown",
                TruncateSql(commandText));
        }
        else
        {
            var message = $"⚠️ SLOW QUERY DETECTED - Operation: {operation}, " +
                          $"Duration: {durationMs:F2}ms (Threshold: {thresholdMs}ms), " +
                          $"Database: {dbName ?? "unknown"}, " +
                          $"SQL: {TruncateSql(commandText)}";

            TelemetryHelper.Execute(TelemetryLevels.Warning, "efcore-slow-query", message);
        }
    }

    private static string DetectOperation(string commandText)
    {
        if (string.IsNullOrEmpty(commandText)) return "UNKNOWN";

        var normalized = commandText.TrimStart().ToUpperInvariant();

        if (normalized.StartsWith("INSERT")) return "INSERT";
        if (normalized.StartsWith("UPDATE")) return "UPDATE";
        if (normalized.StartsWith("DELETE")) return "DELETE";
        if (normalized.StartsWith("MERGE")) return "MERGE";
        if (normalized.StartsWith("SELECT")) return "SELECT";
        if (normalized.StartsWith("EXEC") || normalized.StartsWith("CALL")) return "PROCEDURE";

        return "UNKNOWN";
    }

    private static string TruncateSql(string sql, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(sql)) return sql;
        if (sql.Length <= maxLength) return sql;
        return sql.Substring(0, maxLength) + "...";
    }
}

