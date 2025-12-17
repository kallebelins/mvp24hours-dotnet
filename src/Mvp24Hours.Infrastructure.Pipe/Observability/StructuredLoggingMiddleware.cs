//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Middleware that provides structured logging for pipeline operations with full context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware provides comprehensive structured logging including:
    /// <list type="bullet">
    /// <item>Correlation ID and causation ID from pipeline context</item>
    /// <item>Operation name and execution time</item>
    /// <item>User and tenant context</item>
    /// <item>Memory allocation tracking</item>
    /// <item>Exception details on failure</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StructuredLoggingMiddleware : IPipelineMiddleware
    {
        private readonly ILogger<StructuredLoggingMiddleware> _logger;
        private readonly IPipelineContextAccessor? _contextAccessor;
        private readonly StructuredLoggingOptions _options;

        /// <summary>
        /// Creates a new instance of StructuredLoggingMiddleware.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="contextAccessor">Optional pipeline context accessor.</param>
        /// <param name="options">Optional logging options.</param>
        public StructuredLoggingMiddleware(
            ILogger<StructuredLoggingMiddleware> logger,
            IPipelineContextAccessor? contextAccessor = null,
            StructuredLoggingOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextAccessor = contextAccessor;
            _options = options ?? new StructuredLoggingOptions();
        }

        /// <inheritdoc />
        public int Order => _options.MiddlewareOrder;

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            var context = _contextAccessor?.Context;
            var correlationId = context?.CorrelationId ?? message.Token ?? Guid.NewGuid().ToString("N");
            var operationId = Guid.NewGuid().ToString("N")[..8];

            // Capture initial state
            var stopwatch = Stopwatch.StartNew();
            var startMemory = _options.TrackMemory ? GC.GetTotalMemory(false) : 0;

            // Create logging scope with context
            using var scope = _logger.BeginScope(new
            {
                CorrelationId = correlationId,
                CausationId = context?.CausationId,
                OperationId = operationId,
                UserId = context?.UserId,
                TenantId = context?.TenantId,
                TraceId = context?.TraceId,
                SpanId = context?.SpanId
            });

            // Log operation start
            if (_options.LogOperationStart)
            {
                _logger.LogInformation(
                    "Pipeline operation starting. CorrelationId: {CorrelationId}, OperationId: {OperationId}, IsFaulty: {IsFaulty}, IsLocked: {IsLocked}",
                    correlationId,
                    operationId,
                    message.IsFaulty,
                    message.IsLocked);
            }

            try
            {
                await next();

                stopwatch.Stop();
                var memoryDelta = _options.TrackMemory ? GC.GetTotalMemory(false) - startMemory : 0;

                // Log operation completion
                if (_options.LogOperationEnd)
                {
                    if (message.IsFaulty)
                    {
                        _logger.LogWarning(
                            "Pipeline operation completed with faults. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms, IsFaulty: {IsFaulty}, IsLocked: {IsLocked}, MemoryDelta: {MemoryDelta}",
                            correlationId,
                            operationId,
                            stopwatch.ElapsedMilliseconds,
                            message.IsFaulty,
                            message.IsLocked,
                            memoryDelta);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Pipeline operation completed successfully. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms, IsFaulty: {IsFaulty}, IsLocked: {IsLocked}, MemoryDelta: {MemoryDelta}",
                            correlationId,
                            operationId,
                            stopwatch.ElapsedMilliseconds,
                            message.IsFaulty,
                            message.IsLocked,
                            memoryDelta);
                    }
                }

                // Log slow operation warning
                if (_options.SlowOperationThreshold.HasValue &&
                    stopwatch.Elapsed > _options.SlowOperationThreshold.Value)
                {
                    _logger.LogWarning(
                        "Slow pipeline operation detected. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms, Threshold: {ThresholdMs}ms",
                        correlationId,
                        operationId,
                        stopwatch.ElapsedMilliseconds,
                        _options.SlowOperationThreshold.Value.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "Pipeline operation cancelled. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms",
                    correlationId,
                    operationId,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Pipeline operation failed with exception. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms, ExceptionType: {ExceptionType}, ExceptionMessage: {ExceptionMessage}",
                    correlationId,
                    operationId,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().FullName,
                    ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Sync version of structured logging middleware.
    /// </summary>
    public class StructuredLoggingMiddlewareSync : IPipelineMiddlewareSync
    {
        private readonly ILogger<StructuredLoggingMiddlewareSync> _logger;
        private readonly IPipelineContextAccessor? _contextAccessor;
        private readonly StructuredLoggingOptions _options;

        /// <summary>
        /// Creates a new instance of StructuredLoggingMiddlewareSync.
        /// </summary>
        public StructuredLoggingMiddlewareSync(
            ILogger<StructuredLoggingMiddlewareSync> logger,
            IPipelineContextAccessor? contextAccessor = null,
            StructuredLoggingOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextAccessor = contextAccessor;
            _options = options ?? new StructuredLoggingOptions();
        }

        /// <inheritdoc />
        public int Order => _options.MiddlewareOrder;

        /// <inheritdoc />
        public void Execute(IPipelineMessage message, Action next)
        {
            var context = _contextAccessor?.Context;
            var correlationId = context?.CorrelationId ?? message.Token ?? Guid.NewGuid().ToString("N");
            var operationId = Guid.NewGuid().ToString("N")[..8];

            var stopwatch = Stopwatch.StartNew();
            var startMemory = _options.TrackMemory ? GC.GetTotalMemory(false) : 0;

            using var scope = _logger.BeginScope(new
            {
                CorrelationId = correlationId,
                CausationId = context?.CausationId,
                OperationId = operationId,
                UserId = context?.UserId,
                TenantId = context?.TenantId
            });

            if (_options.LogOperationStart)
            {
                _logger.LogInformation(
                    "Pipeline operation starting. CorrelationId: {CorrelationId}, OperationId: {OperationId}",
                    correlationId,
                    operationId);
            }

            try
            {
                next();

                stopwatch.Stop();
                var memoryDelta = _options.TrackMemory ? GC.GetTotalMemory(false) - startMemory : 0;

                if (_options.LogOperationEnd)
                {
                    _logger.LogInformation(
                        "Pipeline operation completed. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms, MemoryDelta: {MemoryDelta}",
                        correlationId,
                        operationId,
                        stopwatch.ElapsedMilliseconds,
                        memoryDelta);
                }

                if (_options.SlowOperationThreshold.HasValue &&
                    stopwatch.Elapsed > _options.SlowOperationThreshold.Value)
                {
                    _logger.LogWarning(
                        "Slow pipeline operation detected. CorrelationId: {CorrelationId}, Duration: {DurationMs}ms",
                        correlationId,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Pipeline operation failed. CorrelationId: {CorrelationId}, OperationId: {OperationId}, Duration: {DurationMs}ms",
                    correlationId,
                    operationId,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }

    /// <summary>
    /// Configuration options for structured logging middleware.
    /// </summary>
    public class StructuredLoggingOptions
    {
        /// <summary>
        /// Gets or sets whether to log operation start events (default: true).
        /// </summary>
        public bool LogOperationStart { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log operation end events (default: true).
        /// </summary>
        public bool LogOperationEnd { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold for slow operation warnings (default: null - disabled).
        /// </summary>
        public TimeSpan? SlowOperationThreshold { get; set; }

        /// <summary>
        /// Gets or sets whether to track memory allocation (default: false).
        /// </summary>
        public bool TrackMemory { get; set; } = false;

        /// <summary>
        /// Gets or sets the middleware execution order (default: -900).
        /// </summary>
        public int MiddlewareOrder { get; set; } = -900;
    }
}

