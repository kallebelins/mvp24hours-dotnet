//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Middleware
{
    /// <summary>
    /// Middleware that logs operation execution with timing.
    /// </summary>
    public class LoggingPipelineMiddleware : IPipelineMiddleware
    {
        private readonly ILogger<LoggingPipelineMiddleware>? _logger;

        /// <summary>
        /// Creates a new instance of LoggingPipelineMiddleware.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public LoggingPipelineMiddleware(ILogger<LoggingPipelineMiddleware>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -1000; // Run early (outer middleware)

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString("N")[..8];

            _logger?.LogDebug("Pipeline operation {OperationId} starting", operationId);

            try
            {
                await next();

                stopwatch.Stop();
                _logger?.LogDebug(
                    "Pipeline operation {OperationId} completed in {ElapsedMs}ms. IsFaulty: {IsFaulty}, IsLocked: {IsLocked}",
                    operationId,
                    stopwatch.ElapsedMilliseconds,
                    message.IsFaulty,
                    message.IsLocked);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(
                    ex,
                    "Pipeline operation {OperationId} failed after {ElapsedMs}ms",
                    operationId,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// Sync version of logging middleware.
    /// </summary>
    public class LoggingPipelineMiddlewareSync : IPipelineMiddlewareSync
    {
        private readonly ILogger<LoggingPipelineMiddlewareSync>? _logger;

        public LoggingPipelineMiddlewareSync(ILogger<LoggingPipelineMiddlewareSync>? logger = null)
        {
            _logger = logger;
        }

        public int Order => -1000;

        public void Execute(IPipelineMessage message, Action next)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString("N")[..8];

            _logger?.LogDebug("Pipeline operation {OperationId} starting", operationId);

            try
            {
                next();

                stopwatch.Stop();
                _logger?.LogDebug(
                    "Pipeline operation {OperationId} completed in {ElapsedMs}ms. IsFaulty: {IsFaulty}, IsLocked: {IsLocked}",
                    operationId,
                    stopwatch.ElapsedMilliseconds,
                    message.IsFaulty,
                    message.IsLocked);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(
                    ex,
                    "Pipeline operation {OperationId} failed after {ElapsedMs}ms",
                    operationId,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

