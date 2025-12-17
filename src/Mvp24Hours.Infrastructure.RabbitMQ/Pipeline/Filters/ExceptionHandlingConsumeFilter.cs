//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters
{
    /// <summary>
    /// Consume filter that provides automatic exception handling with retry and dead letter queue support.
    /// </summary>
    public class ExceptionHandlingConsumeFilter : IConsumeFilter
    {
        private readonly ILogger<ExceptionHandlingConsumeFilter>? _logger;
        private readonly ExceptionHandlingFilterOptions _options;

        /// <summary>
        /// Creates a new exception handling consume filter.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        /// <param name="options">Exception handling options.</param>
        public ExceptionHandlingConsumeFilter(
            ILogger<ExceptionHandlingConsumeFilter>? logger = null,
            IOptions<ExceptionHandlingFilterOptions>? options = null)
        {
            _logger = logger;
            _options = options?.Value ?? new ExceptionHandlingFilterOptions();
        }

        /// <inheritdoc />
        public async Task ConsumeAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            ConsumeFilterDelegate<TMessage> next,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var messageType = typeof(TMessage).Name;
            var messageId = context.MessageId;

            try
            {
                await next(context, cancellationToken);
            }
            catch (Exception ex) when (ShouldHandleException(ex))
            {
                context.SetException(ex);
                
                LogException(messageType, messageId, context.RedeliveryCount, ex);

                // Check if we should retry
                if (context.RedeliveryCount < _options.MaxRetries)
                {
                    var retryDelay = CalculateRetryDelay(context.RedeliveryCount);
                    context.SetRetry(retryDelay);
                    
                    LogRetry(messageType, messageId, context.RedeliveryCount + 1, _options.MaxRetries, retryDelay);
                }
                else
                {
                    // Max retries exceeded - send to dead letter queue
                    var reason = $"Max retries ({_options.MaxRetries}) exceeded. Last error: {ex.Message}";
                    context.SendToDeadLetter(reason);
                    
                    LogDeadLetter(messageType, messageId, reason);
                }

                // Re-throw if configured to do so
                if (_options.RethrowException)
                {
                    throw;
                }
            }
            catch (Exception ex) when (!ShouldHandleException(ex))
            {
                // Exception types that should not be handled (e.g., OperationCanceledException)
                context.SetException(ex);
                
                if (_options.SendUnhandledToDeadLetter)
                {
                    var reason = $"Unhandled exception type {ex.GetType().Name}: {ex.Message}";
                    context.SendToDeadLetter(reason);
                    LogDeadLetter(messageType, messageId, reason);
                }
                
                throw;
            }
        }

        private bool ShouldHandleException(Exception ex)
        {
            // Don't handle cancellation
            if (ex is OperationCanceledException)
                return false;

            // Check if exception type is in the ignore list
            if (_options.ExceptionsToIgnore != null)
            {
                foreach (var ignoreType in _options.ExceptionsToIgnore)
                {
                    if (ignoreType.IsInstanceOfType(ex))
                        return false;
                }
            }

            // Check if we should only handle specific exceptions
            if (_options.ExceptionsToHandle != null && _options.ExceptionsToHandle.Length > 0)
            {
                foreach (var handleType in _options.ExceptionsToHandle)
                {
                    if (handleType.IsInstanceOfType(ex))
                        return true;
                }
                return false;
            }

            return true;
        }

        private TimeSpan CalculateRetryDelay(int currentRetryCount)
        {
            if (!_options.UseExponentialBackoff)
            {
                return _options.RetryDelay;
            }

            // Exponential backoff: delay * 2^retryCount
            var baseDelay = _options.RetryDelay.TotalMilliseconds;
            var exponentialDelay = baseDelay * Math.Pow(2, currentRetryCount);
            
            // Apply max delay cap
            var maxDelay = _options.MaxRetryDelay.TotalMilliseconds;
            var finalDelay = Math.Min(exponentialDelay, maxDelay);
            
            // Add jitter if configured
            if (_options.AddJitter)
            {
                var jitter = Random.Shared.NextDouble() * _options.JitterFactor * finalDelay;
                finalDelay += jitter;
            }
            
            return TimeSpan.FromMilliseconds(finalDelay);
        }

        private void LogException(string messageType, string messageId, int redeliveryCount, Exception ex)
        {
            var message = $"Exception during message processing: Type={messageType}, MessageId={messageId}, RedeliveryCount={redeliveryCount}, Error={ex.Message}";
            
            if (_logger != null)
            {
                _logger.LogWarning(ex, message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Warning, "rabbitmq-filter-exception-handling", message);
            }
        }

        private void LogRetry(string messageType, string messageId, int retryNumber, int maxRetries, TimeSpan delay)
        {
            var message = $"Scheduling retry: Type={messageType}, MessageId={messageId}, Retry={retryNumber}/{maxRetries}, Delay={delay.TotalMilliseconds}ms";
            
            if (_logger != null)
            {
                _logger.LogInformation(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Information, "rabbitmq-filter-exception-retry", message);
            }
        }

        private void LogDeadLetter(string messageType, string messageId, string reason)
        {
            var message = $"Sending to dead letter queue: Type={messageType}, MessageId={messageId}, Reason={reason}";
            
            if (_logger != null)
            {
                _logger.LogError(message);
            }
            else
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "rabbitmq-filter-exception-dlq", message);
            }
        }
    }

    /// <summary>
    /// Options for the exception handling filter.
    /// </summary>
    public class ExceptionHandlingFilterOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retries before sending to DLQ. Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the base retry delay. Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum retry delay when using exponential backoff. Default is 30 seconds.
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries. Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to add jitter to retry delays. Default is true.
        /// </summary>
        public bool AddJitter { get; set; } = true;

        /// <summary>
        /// Gets or sets the jitter factor (0.0 to 1.0). Default is 0.1 (10%).
        /// </summary>
        public double JitterFactor { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets whether to rethrow the exception after handling. Default is false.
        /// </summary>
        public bool RethrowException { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to send unhandled exception types to DLQ. Default is true.
        /// </summary>
        public bool SendUnhandledToDeadLetter { get; set; } = true;

        /// <summary>
        /// Gets or sets the exception types to handle. If null or empty, all exceptions are handled.
        /// </summary>
        public Type[]? ExceptionsToHandle { get; set; }

        /// <summary>
        /// Gets or sets the exception types to ignore (not retry, send directly to DLQ).
        /// </summary>
        public Type[]? ExceptionsToIgnore { get; set; }
    }
}

