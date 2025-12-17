//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency
{
    /// <summary>
    /// Middleware that sends failed operations to a dead letter store.
    /// </summary>
    public class DeadLetterPipelineMiddleware : IPipelineMiddleware
    {
        private readonly ILogger<DeadLetterPipelineMiddleware>? _logger;
        private readonly IDeadLetterStore _deadLetterStore;
        private readonly DeadLetterOptions _options;

        /// <summary>
        /// Creates a new instance of DeadLetterPipelineMiddleware.
        /// </summary>
        /// <param name="deadLetterStore">The dead letter store to use.</param>
        /// <param name="logger">Optional logger instance.</param>
        public DeadLetterPipelineMiddleware(
            IDeadLetterStore deadLetterStore,
            ILogger<DeadLetterPipelineMiddleware>? logger = null)
            : this(deadLetterStore, DeadLetterOptions.Default, logger)
        {
        }

        /// <summary>
        /// Creates a new instance of DeadLetterPipelineMiddleware with custom options.
        /// </summary>
        /// <param name="deadLetterStore">The dead letter store to use.</param>
        /// <param name="options">Dead letter options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public DeadLetterPipelineMiddleware(
            IDeadLetterStore deadLetterStore,
            DeadLetterOptions options,
            ILogger<DeadLetterPipelineMiddleware>? logger = null)
        {
            _deadLetterStore = deadLetterStore ?? throw new ArgumentNullException(nameof(deadLetterStore));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc />
        public int Order => -100; // Run after other resilience middleware, before operation

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            Exception? caughtException = null;
            DeadLetterReason? deadLetterReason = null;
            int retryAttempts = 0;

            try
            {
                await next();

                // Check if message became faulty and should be dead-lettered
                if (message.IsFaulty && _options.DeadLetterOnFaulty)
                {
                    deadLetterReason = DeadLetterReason.Unknown;
                }
            }
            catch (RetryExhaustedException ex)
            {
                caughtException = ex.InnerException ?? ex;
                deadLetterReason = DeadLetterReason.MaxRetriesExceeded;
                retryAttempts = ex.Attempts;
            }
            catch (PipelineCircuitBreakerOpenException ex)
            {
                caughtException = ex;
                deadLetterReason = DeadLetterReason.CircuitBreakerOpen;
            }
            catch (PipelineBulkheadRejectedException ex)
            {
                caughtException = ex;
                deadLetterReason = DeadLetterReason.BulkheadRejected;
            }
            catch (FallbackFailedException ex)
            {
                caughtException = ex;
                deadLetterReason = DeadLetterReason.FallbackFailed;
            }
            catch (TimeoutException ex)
            {
                caughtException = ex;
                deadLetterReason = DeadLetterReason.Timeout;
            }
            catch (Exception ex)
            {
                caughtException = ex;

                // Check if this exception type should be dead-lettered
                if (ShouldDeadLetter(ex))
                {
                    deadLetterReason = DeadLetterReason.NonRetryableException;
                }
                else
                {
                    // Propagate exception without dead-lettering
                    throw;
                }
            }

            // Store in dead letter queue if needed
            if (deadLetterReason.HasValue)
            {
                await StoreDeadLetterAsync(message, caughtException, deadLetterReason.Value, retryAttempts, cancellationToken);

                // Propagate exception if configured
                if (caughtException != null && _options.PropagateException)
                {
                    throw caughtException;
                }
            }
        }

        private bool ShouldDeadLetter(Exception exception)
        {
            if (_options.ShouldDeadLetterPredicate != null)
            {
                return _options.ShouldDeadLetterPredicate(exception);
            }

            if (_options.DeadLetterOnExceptions == null || _options.DeadLetterOnExceptions.Length == 0)
            {
                return _options.DeadLetterOnAllExceptions;
            }

            return _options.DeadLetterOnExceptions.Any(t => t.IsInstanceOfType(exception));
        }

        private async Task StoreDeadLetterAsync(
            IPipelineMessage message,
            Exception? exception,
            DeadLetterReason reason,
            int retryAttempts,
            CancellationToken cancellationToken)
        {
            try
            {
                var operationName = GetOperationName(message);

                var deadLetter = new DeadLetterOperation
                {
                    OperationName = operationName,
                    OperationType = GetOperationType(message),
                    Message = _options.CaptureMessage ? message : null,
                    SerializedMessage = _options.SerializeMessage ? SerializeMessage(message) : null,
                    Exception = _options.CaptureException ? exception : null,
                    SerializedException = _options.SerializeException ? SerializeException(exception) : null,
                    ErrorMessage = exception?.Message,
                    Reason = reason,
                    RetryAttempts = retryAttempts,
                    CorrelationId = GetCorrelationId(message)
                };

                // Add metadata
                if (_options.MetadataProvider != null)
                {
                    var metadata = _options.MetadataProvider(message, exception);
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            deadLetter.Metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }

                await _deadLetterStore.StoreAsync(deadLetter, cancellationToken);

                _logger?.LogWarning(
                    exception,
                    "Operation '{OperationName}' sent to dead letter queue. Reason: {Reason}, ID: {DeadLetterId}",
                    operationName,
                    reason,
                    deadLetter.Id);

                TelemetryHelper.Execute(
                    TelemetryLevels.Verbose,
                    "pipe-dead-letter-stored",
                    $"operation:{operationName}, reason:{reason}, id:{deadLetter.Id}");

                // Notify callback
                _options.OnDeadLettered?.Invoke(deadLetter);
            }
            catch (Exception storeEx)
            {
                _logger?.LogError(
                    storeEx,
                    "Failed to store operation in dead letter queue");

                TelemetryHelper.Execute(
                    TelemetryLevels.Error,
                    "pipe-dead-letter-store-failed",
                    $"error:{storeEx.Message}");

                // Don't lose the original exception
                if (exception != null)
                {
                    throw new AggregateException(
                        "Failed to store operation in dead letter queue",
                        exception,
                        storeEx);
                }

                throw;
            }
        }

        private static string GetOperationName(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation != null)
                {
                    return operation.GetType().Name;
                }
            }
            return "Unknown";
        }

        private static Type? GetOperationType(IPipelineMessage message)
        {
            if (message.HasContent("CurrentOperation"))
            {
                var operation = message.GetContent<object>("CurrentOperation");
                if (operation != null)
                {
                    return operation.GetType();
                }
            }
            return null;
        }

        private static string? GetCorrelationId(IPipelineMessage message)
        {
            if (message.HasContent("CorrelationId"))
            {
                return message.GetContent<string>("CorrelationId");
            }
            return null;
        }

        private string? SerializeMessage(IPipelineMessage message)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    message.IsLocked,
                    message.IsFaulty,
                    HasContent = message.GetContentAll()?.Count > 0,
                    MessageCount = message.Messages?.Count ?? 0
                });
            }
            catch
            {
                return null;
            }
        }

        private string? SerializeException(Exception? exception)
        {
            if (exception == null)
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    Type = exception.GetType().FullName,
                    exception.Message,
                    exception.StackTrace,
                    InnerExceptionType = exception.InnerException?.GetType().FullName,
                    InnerExceptionMessage = exception.InnerException?.Message
                });
            }
            catch
            {
                return $"{exception.GetType().Name}: {exception.Message}";
            }
        }
    }

    /// <summary>
    /// Configuration options for dead letter middleware.
    /// </summary>
    public class DeadLetterOptions
    {
        /// <summary>
        /// Gets or sets whether to dead-letter operations that complete with faults.
        /// Default: false.
        /// </summary>
        public bool DeadLetterOnFaulty { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to dead-letter all unhandled exceptions.
        /// Default: true.
        /// </summary>
        public bool DeadLetterOnAllExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets specific exception types to dead-letter.
        /// When set, only these exception types will be dead-lettered.
        /// Default: null (use DeadLetterOnAllExceptions).
        /// </summary>
        public Type[]? DeadLetterOnExceptions { get; set; }

        /// <summary>
        /// Gets or sets a predicate to determine if an exception should be dead-lettered.
        /// Takes precedence over DeadLetterOnExceptions.
        /// Default: null.
        /// </summary>
        public Func<Exception, bool>? ShouldDeadLetterPredicate { get; set; }

        /// <summary>
        /// Gets or sets whether to capture the message object in the dead letter.
        /// Default: false (may cause memory issues with large messages).
        /// </summary>
        public bool CaptureMessage { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to serialize the message for persistence.
        /// Default: true.
        /// </summary>
        public bool SerializeMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture the exception object.
        /// Default: false.
        /// </summary>
        public bool CaptureException { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to serialize the exception for persistence.
        /// Default: true.
        /// </summary>
        public bool SerializeException { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to propagate the exception after dead-lettering.
        /// Default: false.
        /// </summary>
        public bool PropagateException { get; set; } = false;

        /// <summary>
        /// Gets or sets a callback invoked when an operation is dead-lettered.
        /// Default: null.
        /// </summary>
        public Action<DeadLetterOperation>? OnDeadLettered { get; set; }

        /// <summary>
        /// Gets or sets a function to provide additional metadata for dead letters.
        /// Default: null.
        /// </summary>
        public Func<IPipelineMessage, Exception?, System.Collections.Generic.Dictionary<string, string>>? MetadataProvider { get; set; }

        /// <summary>
        /// Creates default dead letter options.
        /// </summary>
        public static DeadLetterOptions Default => new();
    }
}

