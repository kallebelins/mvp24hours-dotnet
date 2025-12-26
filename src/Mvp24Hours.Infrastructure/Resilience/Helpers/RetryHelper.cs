//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Helpers
{
    /// <summary>
    /// Helper class for executing operations with retry logic and exponential backoff.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper provides static methods for retrying operations with configurable
    /// retry policies. It supports exponential backoff, jitter, and custom retry conditions.
    /// </para>
    /// <para>
    /// <strong>Use cases:</strong>
    /// <list type="bullet">
    /// <item>Database operations that may fail due to transient errors</item>
    /// <item>Messaging operations that may fail due to network issues</item>
    /// <item>File operations that may fail due to temporary locks</item>
    /// <item>Any async operation that may benefit from retry logic</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple retry with exponential backoff
    /// var result = await RetryHelper.ExecuteAsync(
    ///     async () => await database.GetDataAsync(),
    ///     maxRetries: 3,
    ///     initialDelay: TimeSpan.FromSeconds(1)
    /// );
    /// 
    /// // Retry with custom options
    /// var options = new RetryOptions
    /// {
    ///     MaxRetries = 5,
    ///     InitialDelay = TimeSpan.FromSeconds(2),
    ///     UseExponentialBackoff = true
    /// };
    /// var result = await RetryHelper.ExecuteAsync(
    ///     async () => await database.GetDataAsync(),
    ///     options
    /// );
    /// </code>
    /// </example>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes an operation with retry logic and exponential backoff.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff. Default is true.</param>
        /// <param name="shouldRetryOnException">Predicate to determine if an exception should trigger a retry. If null, retries on common transient exceptions.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        public static async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            bool useExponentialBackoff = true,
            Func<Exception, bool>? shouldRetryOnException = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var options = new RetryOptions
            {
                MaxRetries = maxRetries,
                InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1),
                UseExponentialBackoff = useExponentialBackoff,
                ShouldRetryOnException = shouldRetryOnException ?? IsTransientException
            };

            return await ExecuteAsync(operation, options, logger, cancellationToken);
        }

        /// <summary>
        /// Executes an operation with retry logic using the specified options.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="options">Retry options configuration.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        public static async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            RetryOptions options,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Exception? lastException = null;
            var maxAttempts = options.MaxRetries + 1; // Initial attempt + retries

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        logger?.LogDebug(
                            "[Retry] Attempt {Attempt}/{MaxAttempts}",
                            attempt,
                            maxAttempts);
                    }

                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (attempt <= options.MaxRetries && ShouldRetry(ex, options))
                {
                    lastException = ex;

                    var delay = CalculateDelay(attempt, options);

                    logger?.LogWarning(
                        ex,
                        "[Retry] Transient failure (Attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms. Error: {Message}",
                        attempt,
                        maxAttempts,
                        delay.TotalMilliseconds,
                        ex.Message);

                    var retryInfo = new RetryAttemptInfo
                    {
                        AttemptNumber = attempt,
                        MaxAttempts = maxAttempts,
                        Delay = delay,
                        Exception = ex
                    };

                    options.OnRetry?.Invoke(retryInfo);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            logger?.LogError(
                lastException,
                "[Retry] All {MaxAttempts} retry attempts failed",
                maxAttempts);

            var exhaustedInfo = new RetryExhaustedInfo
            {
                TotalAttempts = maxAttempts,
                FinalException = lastException
            };

            options.OnRetryExhausted?.Invoke(exhaustedInfo);

            throw lastException!;
        }

        /// <summary>
        /// Executes an operation without return value with retry logic and exponential backoff.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff. Default is true.</param>
        /// <param name="shouldRetryOnException">Predicate to determine if an exception should trigger a retry. If null, retries on common transient exceptions.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            bool useExponentialBackoff = true,
            Func<Exception, bool>? shouldRetryOnException = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            await ExecuteAsync<object?>(
                async ct =>
                {
                    await operation(ct);
                    return null;
                },
                maxRetries,
                initialDelay,
                useExponentialBackoff,
                shouldRetryOnException,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Executes an operation without return value with retry logic using the specified options.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="options">Retry options configuration.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            RetryOptions options,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            await ExecuteAsync<object?>(
                async ct =>
                {
                    await operation(ct);
                    return null;
                },
                options,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Calculates the delay for a retry attempt based on the backoff strategy.
        /// </summary>
        private static TimeSpan CalculateDelay(int attempt, RetryOptions options)
        {
            var delay = options.BackoffType switch
            {
                RetryBackoffType.Constant => options.InitialDelay,
                RetryBackoffType.Linear => TimeSpan.FromMilliseconds(
                    options.InitialDelay.TotalMilliseconds * attempt),
                RetryBackoffType.Exponential => TimeSpan.FromMilliseconds(
                    options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
                RetryBackoffType.DecorrelatedJitter => CalculateJitteredDelay(attempt, options),
                _ => options.InitialDelay
            };

            // Ensure delay doesn't exceed max delay
            if (delay > options.MaxDelay)
            {
                delay = options.MaxDelay;
            }

            // Add jitter for non-decorrelated types
            if (options.BackoffType != RetryBackoffType.DecorrelatedJitter && options.JitterFactor > 0)
            {
                var random = new Random();
                var jitter = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * options.JitterFactor * random.NextDouble());
                delay = delay.Add(jitter);
            }

            return delay;
        }

        /// <summary>
        /// Calculates delay with decorrelated jitter algorithm.
        /// </summary>
        private static TimeSpan CalculateJitteredDelay(int attempt, RetryOptions options)
        {
            var random = new Random();
            var delay = TimeSpan.FromMilliseconds(
                options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1) *
                (1.0 + random.NextDouble() * options.JitterFactor));

            return delay > options.MaxDelay ? options.MaxDelay : delay;
        }

        /// <summary>
        /// Determines if an exception should trigger a retry.
        /// </summary>
        private static bool ShouldRetry(Exception exception, RetryOptions options)
        {
            if (options.ShouldRetryOnException != null)
            {
                return options.ShouldRetryOnException(exception);
            }

            return IsTransientException(exception);
        }

        /// <summary>
        /// Determines if an exception is transient and should be retried.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception is transient, false otherwise.</returns>
        public static bool IsTransientException(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }

            // Common transient exceptions
            return exception is TimeoutException
                || exception is OperationCanceledException
                || (exception.InnerException != null && IsTransientException(exception.InnerException));
        }
    }
}

