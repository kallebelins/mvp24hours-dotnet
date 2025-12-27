//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// MongoDB interceptor that provides detailed logging of CRUD operations with timing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor logs MongoDB operations with their timing and entity details.
    /// It's particularly useful for debugging, performance analysis, and audit trails.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    ///   <item>Logs operation type (Insert, Update, Delete)</item>
    ///   <item>Tracks operation execution time</item>
    ///   <item>Logs slow operations (configurable threshold)</item>
    ///   <item>Logs operation failures and exceptions</item>
    ///   <item>Entity key logging for traceability</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> In production, consider the performance impact and enable
    /// only for specific scenarios or use the slow operation threshold to limit logging.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with default settings (logs all operations):
    /// services.AddScoped&lt;IMongoDbInterceptor, CommandLogger&gt;();
    /// 
    /// // Register with slow operation threshold only:
    /// services.AddScoped&lt;IMongoDbInterceptor&gt;(sp =>
    ///     new CommandLogger(
    ///         sp.GetService&lt;ILogger&lt;CommandLogger&gt;&gt;(),
    ///         slowOperationThreshold: TimeSpan.FromSeconds(1),
    ///         logAllOperations: false));
    /// </code>
    /// </example>
    public class CommandLogger : MongoDbInterceptorBase
    {
        private readonly ILogger _logger;
        private readonly TimeSpan _slowOperationThreshold;
        private readonly bool _logAllOperations;
        private readonly bool _logEntityDetails;

        // Track timing per entity using a unique key
        private static readonly ConcurrentDictionary<string, Stopwatch> _operationTimers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for structured logging.</param>
        /// <param name="slowOperationThreshold">Threshold for slow operation warnings. Default is 500ms.</param>
        /// <param name="logAllOperations">If true, logs all operations. If false, only logs slow operations and errors.</param>
        /// <param name="logEntityDetails">If true, logs entity key and type details. Default is true in debug, false in release.</param>
        public CommandLogger(
            ILogger<CommandLogger> logger = null,
            TimeSpan? slowOperationThreshold = null,
            bool logAllOperations = true,
            bool? logEntityDetails = null)
        {
            _logger = logger;
            _slowOperationThreshold = slowOperationThreshold ?? TimeSpan.FromMilliseconds(500);
            _logAllOperations = logAllOperations;
#if DEBUG
            _logEntityDetails = logEntityDetails ?? true;
#else
            _logEntityDetails = logEntityDetails ?? false;
#endif
        }

        /// <inheritdoc />
        public override int Order => int.MaxValue - 100; // Run near the end to capture timing

        /// <inheritdoc />
        public override Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            StartTiming(GetTimingKey("Insert", entity));
            LogOperationStart("Insert", entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            var duration = StopTiming(GetTimingKey("Insert", entity));
            LogOperationEnd("Insert", entity, duration);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            StartTiming(GetTimingKey("Update", entity));
            LogOperationStart("Update", entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnAfterUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            var duration = StopTiming(GetTimingKey("Update", entity));
            LogOperationEnd("Update", entity, duration);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            StartTiming(GetTimingKey("Delete", entity));
            LogOperationStart("Delete", entity);
            return Task.FromResult(DeleteInterceptionResult.Proceed());
        }

        /// <inheritdoc />
        public override Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
        {
            var operationType = wasSoftDeleted ? "SoftDelete" : "Delete";
            var duration = StopTiming(GetTimingKey("Delete", entity));
            LogOperationEnd(operationType, entity, duration);
            return Task.CompletedTask;
        }

        private static string GetTimingKey<T>(string operation, T entity) where T : class, IEntityBase
        {
            var key = entity.EntityKey?.ToString() ?? Guid.NewGuid().ToString();
            return $"{operation}:{typeof(T).Name}:{key}:{Thread.CurrentThread.ManagedThreadId}";
        }

        private static void StartTiming(string key)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _operationTimers.TryAdd(key, stopwatch);
        }

        private static TimeSpan StopTiming(string key)
        {
            if (_operationTimers.TryRemove(key, out var stopwatch))
            {
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }
            return TimeSpan.Zero;
        }

        private void LogOperationStart<T>(string operation, T entity) where T : class, IEntityBase
        {
            if (!_logAllOperations) return;

            var sb = new StringBuilder();
            sb.Append($"MongoDB {operation} starting - Collection: {typeof(T).Name}");

            if (_logEntityDetails)
            {
                sb.Append($", EntityKey: {entity.EntityKey}");
            }

            var message = sb.ToString();

            _logger?.LogDebug(message);
        }

        private void LogOperationEnd<T>(string operation, T entity, TimeSpan duration) where T : class, IEntityBase
        {
            var isSlowOperation = duration >= _slowOperationThreshold;

            if (!_logAllOperations && !isSlowOperation)
            {
                return;
            }

            var sb = new StringBuilder();

            if (isSlowOperation)
            {
                sb.AppendLine($"⚠️ SLOW MONGODB OPERATION DETECTED (Duration: {duration.TotalMilliseconds:F2}ms, Threshold: {_slowOperationThreshold.TotalMilliseconds}ms)");
            }

            sb.Append($"MongoDB {operation} completed in {duration.TotalMilliseconds:F2}ms - Collection: {typeof(T).Name}");

            if (_logEntityDetails)
            {
                sb.Append($", EntityKey: {entity.EntityKey}");
            }

            var message = sb.ToString();

            if (isSlowOperation)
            {
                _logger?.LogWarning(message);
            }
            else
            {
                _logger?.LogDebug(message);
            }
        }

        /// <summary>
        /// Logs an operation failure.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="operation">The operation that failed.</param>
        /// <param name="entity">The entity involved.</param>
        /// <param name="exception">The exception that occurred.</param>
        public void LogOperationFailure<T>(string operation, T entity, Exception exception) where T : class, IEntityBase
        {
            // Clean up timing
            StopTiming(GetTimingKey(operation, entity));

            var sb = new StringBuilder();
            sb.AppendLine($"❌ MongoDB {operation} FAILED: {exception.Message}");
            sb.AppendLine($"Collection: {typeof(T).Name}");

            if (_logEntityDetails)
            {
                sb.AppendLine($"EntityKey: {entity.EntityKey}");
            }

            sb.AppendLine($"Exception: {exception}");

            var message = sb.ToString();

            _logger?.LogError(exception, message);
        }
    }
}

