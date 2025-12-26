//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Contract;
using Mvp24Hours.Infrastructure.Resilience.Helpers;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Implementations
{
    /// <summary>
    /// Generic implementation of retry policy for retrying operations.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// This implementation wraps <see cref="RetryHelper"/> to provide a reusable retry policy
    /// instance that can be configured once and used multiple times.
    /// </para>
    /// </remarks>
    public class RetryPolicy<TResult> : IRetryPolicy<TResult>
    {
        private readonly RetryOptions _options;
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy{TResult}"/> class.
        /// </summary>
        /// <param name="options">Retry options configuration.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        public RetryPolicy(RetryOptions options, ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy{TResult}"/> class with default options.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff. Default is true.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        public RetryPolicy(
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            bool useExponentialBackoff = true,
            ILogger? logger = null)
        {
            _options = new RetryOptions
            {
                MaxRetries = maxRetries,
                InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1),
                UseExponentialBackoff = useExponentialBackoff
            };
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return RetryHelper.ExecuteAsync(operation, _options, _logger, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            return RetryHelper.ExecuteAsync(
                ct => operation(context, ct),
                _options,
                _logger,
                cancellationToken);
        }
    }

    /// <summary>
    /// Generic implementation of retry policy for operations without return values.
    /// </summary>
    public class RetryPolicy : IRetryPolicy
    {
        private readonly RetryPolicy<object?> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
        /// </summary>
        /// <param name="options">Retry options configuration.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        public RetryPolicy(RetryOptions options, ILogger? logger = null)
        {
            _inner = new RetryPolicy<object?>(options, logger);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class with default options.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <param name="initialDelay">Initial delay between retries. Default is 1 second.</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff. Default is true.</param>
        /// <param name="logger">Optional logger for recording retry operations.</param>
        public RetryPolicy(
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            bool useExponentialBackoff = true,
            ILogger? logger = null)
        {
            _inner = new RetryPolicy<object?>(maxRetries, initialDelay, useExponentialBackoff, logger);
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync((_, ct) => operation(ct), null, cancellationToken);
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            return _inner.ExecuteAsync(
                async (ctx, ct) =>
                {
                    await operation(ctx, ct);
                    return (object?)null;
                },
                context,
                cancellationToken);
        }
    }
}

