//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that provides detailed logging of SQL commands with parameters and timing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor logs SQL commands with their parameters, execution time, and results.
    /// It's particularly useful for debugging, performance analysis, and audit trails.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    ///   <item>Logs SQL text with parameter values</item>
    ///   <item>Tracks command execution time</item>
    ///   <item>Logs slow queries (configurable threshold)</item>
    ///   <item>Logs command failures and exceptions</item>
    ///   <item>Supports parameter masking for sensitive data</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> In production, consider the performance impact and enable
    /// only for specific scenarios or use the slow query threshold to limit logging.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with default settings (logs all queries):
    /// services.AddDbContext&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new CommandLoggingInterceptor());
    /// });
    /// 
    /// // Register with slow query threshold only:
    /// services.AddDbContext&lt;AppDbContext&gt;(options =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new CommandLoggingInterceptor(
    ///                slowQueryThreshold: TimeSpan.FromSeconds(1),
    ///                logAllQueries: false));
    /// });
    /// 
    /// // With ILogger:
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     var logger = sp.GetRequiredService&lt;ILogger&lt;AppDbContext&gt;&gt;();
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(new CommandLoggingInterceptor(logger: logger));
    /// });
    /// </code>
    /// </example>
    public class CommandLoggingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _slowQueryThreshold;
        private readonly bool _logAllQueries;
        private readonly bool _logParameters;
        private readonly string[] _sensitiveParameters;

        private static readonly AsyncLocal<Stopwatch> _stopwatch = new AsyncLocal<Stopwatch>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLoggingInterceptor"/> class.
        /// </summary>
        /// <param name="logger">Optional ILogger for structured logging. If null, no logging is performed.</param>
        /// <param name="slowQueryThreshold">Threshold for slow query warnings. Default is 1 second.</param>
        /// <param name="logAllQueries">If true, logs all queries. If false, only logs slow queries and errors.</param>
        /// <param name="logParameters">If true, logs SQL parameter values. Default is true in debug, false in release.</param>
        /// <param name="sensitiveParameters">Parameter names to mask in logs (e.g., "password", "token").</param>
        public CommandLoggingInterceptor(
            ILogger? logger = null,
            TimeSpan? slowQueryThreshold = null,
            bool logAllQueries = true,
            bool? logParameters = null,
            string[]? sensitiveParameters = null)
        {
            _logger = logger;
            _slowQueryThreshold = slowQueryThreshold ?? TimeSpan.FromSeconds(1);
            _logAllQueries = logAllQueries;
#if DEBUG
            _logParameters = logParameters ?? true;
#else
            _logParameters = logParameters ?? false;
#endif
            _sensitiveParameters = sensitiveParameters ?? new[] { "password", "pwd", "token", "secret", "key", "credential" };
        }

        /// <inheritdoc />
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            StartTiming();
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            LogCommandExecution(command, eventData.Duration);
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            LogCommandExecution(command, eventData.Duration);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            StartTiming();
            return base.NonQueryExecuting(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            LogCommandExecution(command, eventData.Duration, $"Rows affected: {result}");
            return base.NonQueryExecuted(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            LogCommandExecution(command, eventData.Duration, $"Rows affected: {result}");
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            StartTiming();
            return base.ScalarExecuting(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override object ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result)
        {
            LogCommandExecution(command, eventData.Duration, $"Result: {result}");
            return base.ScalarExecuted(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<object> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result,
            CancellationToken cancellationToken = default)
        {
            LogCommandExecution(command, eventData.Duration, $"Result: {result}");
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc />
        public override void CommandFailed(
            DbCommand command,
            CommandErrorEventData eventData)
        {
            LogCommandFailure(command, eventData.Exception);
            base.CommandFailed(command, eventData);
        }

        /// <inheritdoc />
        public override Task CommandFailedAsync(
            DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            LogCommandFailure(command, eventData.Exception);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        private void StartTiming()
        {
            _stopwatch.Value = Stopwatch.StartNew();
        }

        private void LogCommandExecution(DbCommand command, TimeSpan duration, string additionalInfo = null)
        {
            var isSlowQuery = duration >= _slowQueryThreshold;

            if (!_logAllQueries && !isSlowQuery)
            {
                return;
            }

            var sb = new StringBuilder();

            if (isSlowQuery)
            {
                sb.AppendLine($"⚠️ SLOW QUERY DETECTED (Duration: {duration.TotalMilliseconds:F2}ms, Threshold: {_slowQueryThreshold.TotalMilliseconds}ms)");
            }
            else
            {
                sb.AppendLine($"SQL Command executed in {duration.TotalMilliseconds:F2}ms");
            }

            sb.AppendLine($"Command: {command.CommandText}");

            if (_logParameters && command.Parameters.Count > 0)
            {
                sb.AppendLine("Parameters:");
                foreach (DbParameter param in command.Parameters)
                {
                    var value = IsSensitiveParameter(param.ParameterName) ? "***MASKED***" : param.Value?.ToString() ?? "NULL";
                    sb.AppendLine($"  {param.ParameterName} ({param.DbType}): {value}");
                }
            }

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                sb.AppendLine(additionalInfo);
            }

            var message = sb.ToString();

            if (isSlowQuery)
            {
                _logger?.LogWarning(message);
            }
            else
            {
                _logger?.LogDebug(message);
            }
        }

        private void LogCommandFailure(DbCommand command, Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"❌ SQL Command FAILED: {exception.Message}");
            sb.AppendLine($"Command: {command.CommandText}");

            if (_logParameters && command.Parameters.Count > 0)
            {
                sb.AppendLine("Parameters:");
                foreach (DbParameter param in command.Parameters)
                {
                    var value = IsSensitiveParameter(param.ParameterName) ? "***MASKED***" : param.Value?.ToString() ?? "NULL";
                    sb.AppendLine($"  {param.ParameterName} ({param.DbType}): {value}");
                }
            }

            sb.AppendLine($"Exception: {exception}");

            var message = sb.ToString();

            _logger?.LogError(exception, message);
        }

        private bool IsSensitiveParameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName)) return false;

            var normalizedName = parameterName.ToLowerInvariant();
            foreach (var sensitive in _sensitiveParameters)
            {
                if (normalizedName.Contains(sensitive.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

