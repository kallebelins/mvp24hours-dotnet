//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Observability.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Observability.Helpers
{
    /// <summary>
    /// Helper class for adding observability (logging, tracing, metrics) to Infrastructure operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper provides a unified way to add observability to operations across
    /// all Infrastructure subsystems. It handles:
    /// - Structured logging with correlation IDs
    /// - OpenTelemetry Activity spans for distributed tracing
    /// - Prometheus-compatible metrics
    /// - Error tracking and reporting
    /// </para>
    /// </remarks>
    public static class ObservabilityHelper
    {
        /// <summary>
        /// Executes an operation with full observability (logging, tracing, metrics).
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="logger">The logger instance.</param>
        /// <param name="activitySource">The Activity source for tracing.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="recordMetrics">Action to record metrics (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// 1. Creates an Activity span for distributed tracing
        /// 2. Logs the start of the operation with correlation ID
        /// 3. Executes the operation
        /// 4. Records metrics (if provided)
        /// 5. Logs completion or errors
        /// </para>
        /// </remarks>
        public static async Task<TResult> ExecuteWithObservabilityAsync<TResult>(
            ILogger logger,
            ActivitySource activitySource,
            string operationName,
            Func<CancellationToken, Task<TResult>> operation,
            Action<bool, double>? recordMetrics = null,
            CancellationToken cancellationToken = default)
        {
            var correlationId = CorrelationIdPropagation.GetCorrelationId();
            var stopwatch = Stopwatch.StartNew();
            Activity? activity = null;

            try
            {
                // Create Activity span
                activity = activitySource.StartActivity(operationName);
                activity?.SetTag("correlation.id", correlationId);
                activity?.SetTag("operation.name", operationName);

                // Log start
                logger.LogDebug(
                    "Starting operation {OperationName} with CorrelationId {CorrelationId}",
                    operationName, correlationId);

                // Execute operation
                var result = await operation(cancellationToken);

                stopwatch.Stop();
                var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                // Record metrics
                recordMetrics?.Invoke(true, durationSeconds);

                // Log success
                logger.LogInformation(
                    "Operation {OperationName} completed successfully in {DurationMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);

                // Set Activity tags
                activity?.SetTag("operation.success", true);
                activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                // Record metrics
                recordMetrics?.Invoke(false, durationSeconds);

                // Log error
                logger.LogError(
                    ex,
                    "Operation {OperationName} failed after {DurationMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);

                // Set Activity tags
                activity?.SetTag("operation.success", false);
                activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);

                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        /// <summary>
        /// Executes an operation with full observability (logging, tracing, metrics) - no return value.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="activitySource">The Activity source for tracing.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="recordMetrics">Action to record metrics (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteWithObservabilityAsync(
            ILogger logger,
            ActivitySource activitySource,
            string operationName,
            Func<CancellationToken, Task> operation,
            Action<bool, double>? recordMetrics = null,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithObservabilityAsync<bool>(
                logger,
                activitySource,
                operationName,
                async ct =>
                {
                    await operation(ct);
                    return true;
                },
                recordMetrics,
                cancellationToken);
        }

        /// <summary>
        /// Executes a synchronous operation with full observability.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="logger">The logger instance.</param>
        /// <param name="activitySource">The Activity source for tracing.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="recordMetrics">Action to record metrics (optional).</param>
        /// <returns>The result of the operation.</returns>
        public static TResult ExecuteWithObservability<TResult>(
            ILogger logger,
            ActivitySource activitySource,
            string operationName,
            Func<TResult> operation,
            Action<bool, double>? recordMetrics = null)
        {
            var correlationId = CorrelationIdPropagation.GetCorrelationId();
            var stopwatch = Stopwatch.StartNew();
            Activity? activity = null;

            try
            {
                // Create Activity span
                activity = activitySource.StartActivity(operationName);
                activity?.SetTag("correlation.id", correlationId);
                activity?.SetTag("operation.name", operationName);

                // Log start
                logger.LogDebug(
                    "Starting operation {OperationName} with CorrelationId {CorrelationId}",
                    operationName, correlationId);

                // Execute operation
                var result = operation();

                stopwatch.Stop();
                var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                // Record metrics
                recordMetrics?.Invoke(true, durationSeconds);

                // Log success
                logger.LogInformation(
                    "Operation {OperationName} completed successfully in {DurationMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);

                // Set Activity tags
                activity?.SetTag("operation.success", true);
                activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                // Record metrics
                recordMetrics?.Invoke(false, durationSeconds);

                // Log error
                logger.LogError(
                    ex,
                    "Operation {OperationName} failed after {DurationMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);

                // Set Activity tags
                activity?.SetTag("operation.success", false);
                activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);

                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }
    }
}

