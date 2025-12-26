//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Observability;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Observability.Logging
{
    /// <summary>
    /// Extension methods for structured logging in Infrastructure module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide consistent structured logging across all Infrastructure
    /// subsystems, with automatic correlation ID propagation and performance tracking.
    /// </para>
    /// </remarks>
    public static class StructuredLoggingExtensions
    {
        /// <summary>
        /// Logs the start of an operation with structured data.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="memberName">The calling member name (automatically populated).</param>
        /// <param name="filePath">The source file path (automatically populated).</param>
        /// <param name="lineNumber">The source line number (automatically populated).</param>
        /// <returns>A disposable scope that logs the end of the operation.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a logging scope with correlation ID and operation name.
        /// When the returned scope is disposed, it logs the completion with duration.
        /// </para>
        /// </remarks>
        public static IDisposable BeginOperation(
            this ILogger logger,
            string operationName,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var correlationId = CorrelationIdPropagation.GetCorrelationId();
            var stopwatch = Stopwatch.StartNew();

            using var scope = logger.BeginScope("Operation: {OperationName}, CorrelationId: {CorrelationId}",
                operationName, correlationId);

            logger.LogDebug(
                "Starting operation {OperationName} in {MemberName} at {FilePath}:{LineNumber}",
                operationName, memberName, filePath, lineNumber);

            return new OperationScope(logger, operationName, stopwatch);
        }

        /// <summary>
        /// Logs an operation with structured data and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="memberName">The calling member name (automatically populated).</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>
        /// <para>
        /// This method logs the start and end of an operation, including duration and
        /// any exceptions that occur.
        /// </para>
        /// </remarks>
        public static T LogOperation<T>(
            this ILogger logger,
            string operationName,
            Func<T> operation,
            [CallerMemberName] string memberName = "")
        {
            using var scope = logger.BeginOperation(operationName, memberName);
            try
            {
                var result = operation();
                logger.LogDebug("Operation {OperationName} completed successfully", operationName);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {OperationName} failed", operationName);
                throw;
            }
        }

        /// <summary>
        /// Logs an async operation with structured data and returns a result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="memberName">The calling member name (automatically populated).</param>
        /// <returns>A task that completes with the result of the operation.</returns>
        public static async Task<T> LogOperationAsync<T>(
            this ILogger logger,
            string operationName,
            Func<Task<T>> operation,
            [CallerMemberName] string memberName = "")
        {
            using var scope = logger.BeginOperation(operationName, memberName);
            try
            {
                var result = await operation();
                logger.LogDebug("Operation {OperationName} completed successfully", operationName);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {OperationName} failed", operationName);
                throw;
            }
        }

        /// <summary>
        /// Logs an operation with structured data (no return value).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="memberName">The calling member name (automatically populated).</param>
        public static void LogOperation(
            this ILogger logger,
            string operationName,
            Action operation,
            [CallerMemberName] string memberName = "")
        {
            using var scope = logger.BeginOperation(operationName, memberName);
            try
            {
                operation();
                logger.LogDebug("Operation {OperationName} completed successfully", operationName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {OperationName} failed", operationName);
                throw;
            }
        }

        /// <summary>
        /// Logs an async operation with structured data (no return value).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="memberName">The calling member name (automatically populated).</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public static async Task LogOperationAsync(
            this ILogger logger,
            string operationName,
            Func<Task> operation,
            [CallerMemberName] string memberName = "")
        {
            using var scope = logger.BeginOperation(operationName, memberName);
            try
            {
                await operation();
                logger.LogDebug("Operation {OperationName} completed successfully", operationName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {OperationName} failed", operationName);
                throw;
            }
        }

        /// <summary>
        /// Disposable scope that logs operation completion with duration.
        /// </summary>
        private class OperationScope : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed;

            public OperationScope(ILogger logger, string operationName, Stopwatch stopwatch)
            {
                _logger = logger;
                _operationName = operationName;
                _stopwatch = stopwatch;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _stopwatch.Stop();
                _logger.LogDebug(
                    "Operation {OperationName} completed in {DurationMs}ms",
                    _operationName,
                    _stopwatch.ElapsedMilliseconds);

                _disposed = true;
            }
        }
    }
}

