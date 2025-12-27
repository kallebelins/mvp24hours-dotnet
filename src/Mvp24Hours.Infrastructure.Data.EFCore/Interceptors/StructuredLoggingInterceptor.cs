//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;

/// <summary>
/// EF Core interceptor that provides structured logging with rich context for development and debugging.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor provides detailed structured logging suitable for development environments,
/// with the ability to output logs in JSON format for log aggregation systems.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
///   <item>Structured logging with ILogger</item>
///   <item>JSON output for log aggregation (ELK, Splunk, etc.)</item>
///   <item>Parameter logging with type information</item>
///   <item>Sensitive data masking</item>
///   <item>Execution context (method, line number)</item>
///   <item>OpenTelemetry integration</item>
/// </list>
/// </para>
/// <para>
/// <strong>Warning:</strong> This interceptor can significantly impact performance and expose sensitive data.
/// Use only in development or with appropriate data masking configured.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Development environment only:
/// if (env.IsDevelopment())
/// {
///     services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
///     {
///         var logger = sp.GetRequiredService&lt;ILogger&lt;StructuredLoggingInterceptor&gt;&gt;();
///         
///         options.UseSqlServer(connectionString)
///                .AddInterceptors(new StructuredLoggingInterceptor(
///                    logger: logger,
///                    logParameters: true,
///                    outputAsJson: true,
///                    sensitiveParameters: new[] { "password", "ssn", "creditcard" }));
///     });
/// }
/// </code>
/// </example>
public class StructuredLoggingInterceptor : DbCommandInterceptor
{
    private readonly ILogger? _logger;
    private readonly EFCoreMetrics? _metrics;
    private readonly bool _logParameters;
    private readonly bool _outputAsJson;
    private readonly LogLevel _commandLogLevel;
    private readonly LogLevel _errorLogLevel;
    private readonly HashSet<string> _sensitiveParameters;
    private readonly int _maxSqlLength;
    private readonly int _maxParameterValueLength;
    private readonly bool _includeStackTrace;

    /// <summary>
    /// Initializes a new instance of <see cref="StructuredLoggingInterceptor"/>.
    /// </summary>
    /// <param name="logger">ILogger for structured logging.</param>
    /// <param name="metrics">Optional EFCoreMetrics for metric collection.</param>
    /// <param name="logParameters">If true, logs parameter values. Default is true in DEBUG.</param>
    /// <param name="outputAsJson">If true, outputs log entries as JSON. Default is false.</param>
    /// <param name="commandLogLevel">Log level for command execution. Default is Debug.</param>
    /// <param name="errorLogLevel">Log level for errors. Default is Error.</param>
    /// <param name="sensitiveParameters">Parameter names to mask in logs.</param>
    /// <param name="maxSqlLength">Maximum SQL length to log. Default is 4000.</param>
    /// <param name="maxParameterValueLength">Maximum parameter value length. Default is 200.</param>
    /// <param name="includeStackTrace">If true, includes caller stack trace. Default is false.</param>
    public StructuredLoggingInterceptor(
        ILogger? logger = null,
        EFCoreMetrics? metrics = null,
        bool? logParameters = null,
        bool outputAsJson = false,
        LogLevel commandLogLevel = LogLevel.Debug,
        LogLevel errorLogLevel = LogLevel.Error,
        IEnumerable<string>? sensitiveParameters = null,
        int maxSqlLength = 4000,
        int maxParameterValueLength = 200,
        bool includeStackTrace = false)
    {
        _logger = logger;
        _metrics = metrics;
#if DEBUG
        _logParameters = logParameters ?? true;
#else
        _logParameters = logParameters ?? false;
#endif
        _outputAsJson = outputAsJson;
        _commandLogLevel = commandLogLevel;
        _errorLogLevel = errorLogLevel;
        _sensitiveParameters = new HashSet<string>(
            sensitiveParameters ?? new[] { "password", "pwd", "token", "secret", "key", "credential", "ssn", "creditcard", "cvv" },
            StringComparer.OrdinalIgnoreCase);
        _maxSqlLength = maxSqlLength;
        _maxParameterValueLength = maxParameterValueLength;
        _includeStackTrace = includeStackTrace;
    }

    #region Reader (SELECT) Operations

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogCommand(command, eventData.Duration, "Reader", null);
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration, "Reader", null);
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
        LogCommand(command, eventData.Duration, "NonQuery", result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration, "NonQuery", result);
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
        LogCommand(command, eventData.Duration, "Scalar", result);
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration, "Scalar", result);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    #endregion

    #region Error Handling

    /// <inheritdoc />
    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        LogError(command, eventData.Exception, eventData.Duration);
        base.CommandFailed(command, eventData);
    }

    /// <inheritdoc />
    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        LogError(command, eventData.Exception, eventData.Duration);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    #endregion

    private void LogCommand(DbCommand command, TimeSpan duration, string commandType, object? result)
    {
        var durationMs = duration.TotalMilliseconds;
        var operation = DetectOperation(command.CommandText);

        // Record metrics
        _metrics?.RecordQuery(durationMs, operation, command.Connection?.Database);

        if (_outputAsJson)
        {
            LogAsJson(command, duration, commandType, result, operation);
        }
        else
        {
            LogAsStructured(command, duration, commandType, result, operation);
        }
    }

    private void LogAsStructured(DbCommand command, TimeSpan duration, string commandType, object? result, string operation)
    {
        if (_logger == null)
        {
            return; // No logger available, skip logging
        }

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CommandType"] = commandType,
            ["Operation"] = operation,
            ["Database"] = command.Connection?.Database,
            ["DurationMs"] = duration.TotalMilliseconds
        }))
        {
            if (_logParameters && command.Parameters.Count > 0)
            {
                var parameters = FormatParameters(command.Parameters);
                _logger.Log(
                    _commandLogLevel,
                    "SQL {Operation} executed in {DurationMs:F2}ms - {Sql} - Parameters: {Parameters}",
                    operation,
                    duration.TotalMilliseconds,
                    TruncateSql(command.CommandText),
                    parameters);
            }
            else
            {
                _logger.Log(
                    _commandLogLevel,
                    "SQL {Operation} executed in {DurationMs:F2}ms - {Sql}",
                    operation,
                    duration.TotalMilliseconds,
                    TruncateSql(command.CommandText));
            }
        }
    }

    private void LogAsJson(DbCommand command, TimeSpan duration, string commandType, object? result, string operation)
    {
        var logEntry = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = _commandLogLevel.ToString(),
            Category = "EFCore.Command",
            Message = $"SQL {operation} executed",
            Properties = new
            {
                CommandType = commandType,
                Operation = operation,
                Database = command.Connection?.Database,
                DurationMs = Math.Round(duration.TotalMilliseconds, 2),
                Sql = TruncateSql(command.CommandText),
                Parameters = _logParameters ? FormatParametersAsDict(command.Parameters) : null,
                RowsAffected = result is int rows ? rows : (int?)null,
                ConnectionId = command.Connection?.GetHashCode()
            }
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        _logger?.Log(_commandLogLevel, json);
    }

    private void LogError(DbCommand command, Exception exception, TimeSpan duration)
    {
        var operation = DetectOperation(command.CommandText);

        // Record error metrics
        _metrics?.RecordQueryError(exception.GetType().Name, command.Connection?.Database);

        _logger?.Log(
            _errorLogLevel,
            exception,
            "SQL {Operation} FAILED after {DurationMs:F2}ms - Error: {ErrorMessage} - SQL: {Sql}",
            operation,
            duration.TotalMilliseconds,
            exception.Message,
            TruncateSql(command.CommandText));
    }

    private string FormatParameters(DbParameterCollection parameters)
    {
        var parts = new List<string>();
        foreach (DbParameter param in parameters)
        {
            var value = FormatParameterValue(param);
            parts.Add($"{param.ParameterName}({param.DbType})={value}");
        }
        return string.Join(", ", parts);
    }

    private Dictionary<string, object?> FormatParametersAsDict(DbParameterCollection parameters)
    {
        var dict = new Dictionary<string, object?>();
        foreach (DbParameter param in parameters)
        {
            dict[param.ParameterName] = new
            {
                Type = param.DbType.ToString(),
                Value = FormatParameterValue(param),
                Direction = param.Direction.ToString()
            };
        }
        return dict;
    }

    private string FormatParameterValue(DbParameter param)
    {
        // Check for sensitive parameters
        if (IsSensitive(param.ParameterName))
            return "***MASKED***";

        if (param.Value == null || param.Value == DBNull.Value)
            return "NULL";

        var value = param.Value.ToString() ?? "NULL";

        // Truncate long values
        if (value.Length > _maxParameterValueLength)
            value = value.Substring(0, _maxParameterValueLength) + "...";

        return value;
    }

    private bool IsSensitive(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName)) return false;

        // Remove common prefixes (@, :, ?)
        var normalized = parameterName.TrimStart('@', ':', '?');

        foreach (var sensitive in _sensitiveParameters)
        {
            if (normalized.Contains(sensitive, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private string TruncateSql(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return sql;
        if (sql.Length <= _maxSqlLength) return sql;
        return sql.Substring(0, _maxSqlLength) + "... [TRUNCATED]";
    }

    private static string DetectOperation(string commandText)
    {
        if (string.IsNullOrEmpty(commandText)) return "UNKNOWN";

        var normalized = commandText.TrimStart().ToUpperInvariant();

        if (normalized.StartsWith("SELECT")) return "SELECT";
        if (normalized.StartsWith("INSERT")) return "INSERT";
        if (normalized.StartsWith("UPDATE")) return "UPDATE";
        if (normalized.StartsWith("DELETE")) return "DELETE";
        if (normalized.StartsWith("MERGE")) return "MERGE";
        if (normalized.StartsWith("EXEC") || normalized.StartsWith("CALL")) return "PROCEDURE";

        return "OTHER";
    }
}

