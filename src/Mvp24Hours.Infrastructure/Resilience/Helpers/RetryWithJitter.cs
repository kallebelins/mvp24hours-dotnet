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
    /// Helper class for executing operations with retry logic and jitter to avoid thundering herd problems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper extends <see cref="RetryHelper"/> with enhanced jitter support to prevent
    /// thundering herd problems. When multiple clients retry simultaneously, jitter randomizes
    /// the retry delays to spread out the load.
    /// </para>
    /// <para>
    /// <strong>Thundering Herd Problem:</strong>
    /// When many clients retry at the same time after a service failure, they can overwhelm
    /// the recovering service. Jitter adds randomness to retry delays, spreading out retries
    /// over time and reducing the peak load.
    /// </para>
    /// <para>
    /// <strong>Jitter Strategies:</strong>
    /// <list type="bullet">
    /// <item><strong>Full Jitter:</strong> Random delay between 0 and calculated delay</item>
    /// <item><strong>Equal Jitter:</strong> Half of delay is fixed, half is random</item>
    /// <item><strong>Decorrelated Jitter:</strong> Exponential backoff with decorrelated jitter</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Retry with full jitter (prevents thundering herd)
    /// var result = await RetryWithJitter.ExecuteAsync(
    ///     async () => await database.GetDataAsync(),
    ///     maxRetries: 3,
    ///     initialDelay: TimeSpan.FromSeconds(1),
    ///     jitterStrategy: JitterStrategy.Full
    /// );
    /// </code>
    /// </example>
    public static class RetryWithJitter
    {
        /// <summary>
        /// Executes an operation with retry logic and jitter to prevent thundering herd problems.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="jitterStrategy">Jitter strategy to use. Default is Full.</param>
        /// <param name="jitterFactor">Jitter factor (0.0 to 1.0). Default is 0.1.</param>
        /// <param name="shouldRetryOnException">Predicate to determine if an exception should trigger a retry. If null, retries on common transient exceptions.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        public static async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            JitterStrategy jitterStrategy = JitterStrategy.Full,
            double jitterFactor = 0.1,
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
                UseExponentialBackoff = true,
                JitterFactor = jitterFactor,
                BackoffType = jitterStrategy == JitterStrategy.DecorrelatedJitter
                    ? RetryBackoffType.DecorrelatedJitter
                    : RetryBackoffType.Exponential,
                ShouldRetryOnException = shouldRetryOnException ?? RetryHelper.IsTransientException
            };

            // Override delay calculation for jitter strategies
            return await ExecuteWithJitterStrategy(operation, options, jitterStrategy, logger, cancellationToken);
        }

        /// <summary>
        /// Executes an operation without return value with retry logic and jitter.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="jitterStrategy">Jitter strategy to use. Default is Full.</param>
        /// <param name="jitterFactor">Jitter factor (0.0 to 1.0). Default is 0.1.</param>
        /// <param name="shouldRetryOnException">Predicate to determine if an exception should trigger a retry. If null, retries on common transient exceptions.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            JitterStrategy jitterStrategy = JitterStrategy.Full,
            double jitterFactor = 0.1,
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
                jitterStrategy,
                jitterFactor,
                shouldRetryOnException,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Executes an operation with a specific jitter strategy.
        /// </summary>
        private static async Task<TResult> ExecuteWithJitterStrategy<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            RetryOptions options,
            JitterStrategy jitterStrategy,
            ILogger? logger,
            CancellationToken cancellationToken)
        {
            Exception? lastException = null;
            var maxAttempts = options.MaxRetries + 1;
            var random = new Random();

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        logger?.LogDebug(
                            "[RetryWithJitter] Attempt {Attempt}/{MaxAttempts}",
                            attempt,
                            maxAttempts);
                    }

                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (attempt <= options.MaxRetries && ShouldRetry(ex, options))
                {
                    lastException = ex;

                    var baseDelay = CalculateBaseDelay(attempt, options);
                    var delay = ApplyJitter(baseDelay, jitterStrategy, options, random);

                    logger?.LogWarning(
                        ex,
                        "[RetryWithJitter] Transient failure (Attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms (base: {BaseDelay}ms). Error: {Message}",
                        attempt,
                        maxAttempts,
                        delay.TotalMilliseconds,
                        baseDelay.TotalMilliseconds,
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
                "[RetryWithJitter] All {MaxAttempts} retry attempts failed",
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
        /// Calculates the base delay for a retry attempt.
        /// </summary>
        private static TimeSpan CalculateBaseDelay(int attempt, RetryOptions options)
        {
            return options.UseExponentialBackoff
                ? TimeSpan.FromMilliseconds(options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                : options.InitialDelay;
        }

        /// <summary>
        /// Applies jitter to a delay based on the jitter strategy.
        /// </summary>
        private static TimeSpan ApplyJitter(
            TimeSpan baseDelay,
            JitterStrategy jitterStrategy,
            RetryOptions options,
            Random random)
        {
            var delay = jitterStrategy switch
            {
                JitterStrategy.Full => TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * random.NextDouble()),
                JitterStrategy.Equal => TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds / 2 + baseDelay.TotalMilliseconds / 2 * random.NextDouble()),
                JitterStrategy.DecorrelatedJitter => TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * (1.0 + random.NextDouble() * options.JitterFactor)),
                _ => baseDelay
            };

            // Ensure delay doesn't exceed max delay
            if (delay > options.MaxDelay)
            {
                delay = options.MaxDelay;
            }

            return delay;
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

            return RetryHelper.IsTransientException(exception);
        }
    }

    /// <summary>
    /// Defines jitter strategies for retry delays.
    /// </summary>
    public enum JitterStrategy
    {
        /// <summary>
        /// No jitter - uses calculated delay as-is.
        /// </summary>
        None,

        /// <summary>
        /// Full jitter - random delay between 0 and calculated delay.
        /// Best for preventing thundering herd when many clients retry simultaneously.
        /// </summary>
        Full,

        /// <summary>
        /// Equal jitter - half of delay is fixed, half is random.
        /// Balances between predictability and distribution.
        /// </summary>
        Equal,

        /// <summary>
        /// Decorrelated jitter - exponential backoff with decorrelated jitter.
        /// Provides good distribution while maintaining exponential growth.
        /// </summary>
        DecorrelatedJitter
    }
}

