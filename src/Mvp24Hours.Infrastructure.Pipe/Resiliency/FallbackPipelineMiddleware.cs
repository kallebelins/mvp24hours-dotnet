//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Middleware that implements fallback logic for operations.
    /// Applies to operations implementing <see cref="IFallbackOperation"/> or uses configured fallback action.
    /// </summary>
    public class FallbackPipelineMiddleware : IPipelineMiddleware
    {
        private readonly ILogger<FallbackPipelineMiddleware>? _logger;
        private readonly FallbackOptions _defaultOptions;

        /// <summary>
        /// Creates a new instance of FallbackPipelineMiddleware with default options.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public FallbackPipelineMiddleware(ILogger<FallbackPipelineMiddleware>? logger = null)
            : this(FallbackOptions.Default, logger)
        {
        }

        /// <summary>
        /// Creates a new instance of FallbackPipelineMiddleware with custom options.
        /// </summary>
        /// <param name="defaultOptions">Default fallback options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public FallbackPipelineMiddleware(FallbackOptions defaultOptions, ILogger<FallbackPipelineMiddleware>? logger = null)
        {
            _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -200; // Run after circuit breaker

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            var fallbackOperation = GetFallbackOperation(message);
            Exception? caughtException = null;
            bool operationFailed = false;

            try
            {
                await next();

                // Check if message became faulty
                if (message.IsFaulty)
                {
                    operationFailed = true;
                    bool shouldFallback = fallbackOperation?.FallbackOnFaulty ?? _defaultOptions.FallbackOnFaulty;

                    if (!shouldFallback)
                    {
                        _logger?.LogDebug("Operation completed with faults but FallbackOnFaulty is disabled");
                        return;
                    }
                }
                else
                {
                    // Success - no fallback needed
                    return;
                }
            }
            catch (Exception ex)
            {
                operationFailed = true;
                caughtException = ex;

                // Check if this exception should trigger fallback
                bool shouldFallback = fallbackOperation?.ShouldFallback(ex) ?? _defaultOptions.ShouldFallback(ex);

                if (!shouldFallback)
                {
                    _logger?.LogDebug(ex, "Exception does not trigger fallback, propagating");
                    throw;
                }
            }

            // Execute fallback if operation failed
            if (operationFailed)
            {
                await ExecuteFallbackAsync(message, fallbackOperation, caughtException, cancellationToken);
            }
        }

        private async Task ExecuteFallbackAsync(
            IPipelineMessage message,
            IFallbackOperation? fallbackOperation,
            Exception? exception,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation(
                exception,
                "Executing fallback for failed operation");

            _logger?.LogDebug(
                "FallbackMiddleware: Starting fallback. Exception: {ExceptionType}",
                exception?.GetType().Name ?? "null");

            // Notify fallback starting
            fallbackOperation?.OnFallbackStarting(exception);
            _defaultOptions.OnFallbackStarting?.Invoke(exception);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Execute fallback
                if (fallbackOperation != null)
                {
                    await fallbackOperation.ExecuteFallbackAsync(message, exception);
                }
                else if (_defaultOptions.FallbackAction != null)
                {
                    await _defaultOptions.FallbackAction(message, exception);
                }
                else
                {
                    _logger?.LogWarning(
                        "No fallback action configured for failed operation. Exception: {ExceptionMessage}",
                        exception?.Message);

                    // If no fallback is configured and we caught an exception, rethrow it
                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }

                // Notify fallback completed
                fallbackOperation?.OnFallbackCompleted();
                _defaultOptions.OnFallbackCompleted?.Invoke();

                _logger?.LogInformation("Fallback executed successfully");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning("Fallback was cancelled");
                throw;
            }
            catch (Exception fallbackEx) when (fallbackEx != exception)
            {
                _logger?.LogError(
                    fallbackEx,
                    "Fallback execution failed");

                // Notify fallback failed
                fallbackOperation?.OnFallbackFailed(fallbackEx);
                _defaultOptions.OnFallbackFailed?.Invoke(fallbackEx);

                // Wrap both exceptions
                throw new FallbackFailedException(exception, fallbackEx);
            }
        }

        private static IFallbackOperation? GetFallbackOperation(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation is IFallbackOperation fallbackOp)
                {
                    return fallbackOp;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Synchronous version of fallback middleware.
    /// </summary>
    public class FallbackPipelineMiddlewareSync : IPipelineMiddlewareSync
    {
        private readonly ILogger<FallbackPipelineMiddlewareSync>? _logger;
        private readonly FallbackOptions _defaultOptions;

        /// <summary>
        /// Creates a new instance with default options.
        /// </summary>
        public FallbackPipelineMiddlewareSync(ILogger<FallbackPipelineMiddlewareSync>? logger = null)
            : this(FallbackOptions.Default, logger)
        {
        }

        /// <summary>
        /// Creates a new instance with custom options.
        /// </summary>
        public FallbackPipelineMiddlewareSync(FallbackOptions defaultOptions, ILogger<FallbackPipelineMiddlewareSync>? logger = null)
        {
            _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -200;

        /// <inheritdoc />
        public void Execute(IPipelineMessage message, Action next)
        {
            var fallbackOperationSync = GetFallbackOperationSync(message);
            Exception? caughtException = null;
            bool operationFailed = false;

            try
            {
                next();

                if (message.IsFaulty)
                {
                    operationFailed = true;
                    bool shouldFallback = fallbackOperationSync?.FallbackOnFaulty ?? _defaultOptions.FallbackOnFaulty;

                    if (!shouldFallback)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                operationFailed = true;
                caughtException = ex;

                bool shouldFallback = fallbackOperationSync?.ShouldFallback(ex) ?? _defaultOptions.ShouldFallback(ex);

                if (!shouldFallback)
                {
                    throw;
                }
            }

            if (operationFailed)
            {
                ExecuteFallbackSync(message, fallbackOperationSync, caughtException);
            }
        }

        private void ExecuteFallbackSync(
            IPipelineMessage message,
            IFallbackOperationSync? fallbackOperationSync,
            Exception? exception)
        {
            _logger?.LogInformation(exception, "Executing sync fallback for failed operation");

            try
            {
                if (fallbackOperationSync != null)
                {
                    fallbackOperationSync.ExecuteFallback(message, exception);
                }
                else if (_defaultOptions.FallbackActionSync != null)
                {
                    _defaultOptions.FallbackActionSync(message, exception);
                }
                else
                {
                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }

                _logger?.LogInformation("Sync fallback executed successfully");
            }
            catch (Exception fallbackEx) when (fallbackEx != exception)
            {
                _logger?.LogError(fallbackEx, "Sync fallback execution failed");
                throw new FallbackFailedException(exception, fallbackEx);
            }
        }

        private static IFallbackOperationSync? GetFallbackOperationSync(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation is IFallbackOperationSync fallbackOp)
                {
                    return fallbackOp;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Exception thrown when a fallback operation fails.
    /// </summary>
    public class FallbackFailedException : AggregateException
    {
        /// <summary>
        /// Gets the original exception that triggered the fallback.
        /// </summary>
        public Exception? OriginalException { get; }

        /// <summary>
        /// Gets the exception from the fallback execution.
        /// </summary>
        public Exception FallbackException { get; }

        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="originalException">The original exception.</param>
        /// <param name="fallbackException">The fallback exception.</param>
        public FallbackFailedException(Exception? originalException, Exception fallbackException)
            : base(
                $"Fallback operation failed. Original error: {originalException?.Message ?? "N/A"}. Fallback error: {fallbackException.Message}",
                originalException != null ? new[] { originalException, fallbackException } : new[] { fallbackException })
        {
            OriginalException = originalException;
            FallbackException = fallbackException;
        }
    }
}

