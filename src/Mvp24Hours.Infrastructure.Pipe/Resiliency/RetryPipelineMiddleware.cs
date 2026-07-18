//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Infrastructure.Pipe.Resiliency;

/// <summary>
/// Middleware that implements retry logic for operations.
/// Applies to operations implementing <see cref="IRetryableOperation"/> or uses default options.
/// </summary>
/// <remarks>
/// Creates a new instance of RetryPipelineMiddleware with custom options.
/// </remarks>
/// <param name="defaultOptions">Default retry options for operations without IRetryableOperation.</param>
/// <param name="logger">Optional logger instance.</param>
[Obsolete("Deprecated: Use NativePipelineResilienceExtensions with Microsoft.Extensions.Resilience instead. " +
          "This class will be removed in a future version. " +
          "See docs/en-us/modernization/generic-resilience.md for migration guide.", false)]
public class RetryPipelineMiddleware(RetryOptions defaultOptions, ILogger<RetryPipelineMiddleware>? logger = null) : IPipelineMiddleware
{
    private readonly ILogger<RetryPipelineMiddleware>? _logger = logger;
    private readonly RetryOptions _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));

    /// <summary>
    /// Creates a new instance of RetryPipelineMiddleware with default options.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public RetryPipelineMiddleware(ILogger<RetryPipelineMiddleware>? logger = null)
        : this(RetryOptions.Default, logger)
    {
    }

    /// <inheritdoc />
    public int Order => -400; // Run after logging, before circuit breaker

    /// <inheritdoc />
    public async Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
    {
        // Check if the current operation supports retry
        IRetryableOperation? retryableOperation = GetRetryableOperation(message);

        if (_defaultOptions.MaxRetryAttempts <= 0 && retryableOperation == null)
        {
            // No retry configured
            await next();
            return;
        }

        RetryOptions options = GetEffectiveOptions(retryableOperation);
        int attemptNumber = 0;
        while (true)
        {
            attemptNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await next();

                // Check if message became faulty without exception
                if (!message.IsFaulty)
                {
                    // Success
                    if (attemptNumber > 1)
                    {
                        _logger?.LogInformation(
                            "Operation succeeded after {AttemptNumber} attempts",
                            attemptNumber);
                    }
                    return;
                }

                // Message is faulty but no exception - don't retry
                _logger?.LogDebug(
                    "Operation completed with faults but no exception, not retrying. Attempt: {AttemptNumber}",
                    attemptNumber);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Don't retry on cancellation
                throw;
            }
            catch (Exception ex)
            {
                Exception? lastException = ex;

                // Check if we should retry
                bool shouldRetry = retryableOperation?.ShouldRetry(ex, attemptNumber)
                    ?? options.ShouldRetry(ex, attemptNumber);

                if (!shouldRetry)
                {
                    _logger?.LogWarning(
                        ex,
                        "Operation failed after {AttemptNumber} attempt(s), not retrying",
                        attemptNumber);
                    throw;
                }

                // Calculate delay
                TimeSpan delay = CalculateDelay(options, retryableOperation, attemptNumber);

                _logger?.LogWarning(
                    ex,
                    "Operation failed on attempt {AttemptNumber}/{MaxAttempts}, retrying in {DelayMs}ms",
                    attemptNumber,
                    GetMaxAttempts(options, retryableOperation),
                    delay.TotalMilliseconds);

                _logger?.LogDebug(
                    "RetryMiddleware: Retrying. Attempt: {AttemptNumber}, Delay: {DelayMs}ms, Error: {ErrorMessage}",
                    attemptNumber,
                    delay.TotalMilliseconds,
                    ex.Message);

                // Notify callback
                retryableOperation?.OnRetry(ex, attemptNumber, delay);
                options.OnRetry?.Invoke(ex, attemptNumber, delay);

                // Wait before retry
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException($"Retry was cancelled during delay. Last error: {ex.Message}", ex);
                }

                // Clear faulty state for retry (optional - depends on operation design)
                // Note: We do NOT clear messages here as that might lose important context
            }
        }
    }

    private static IRetryableOperation? GetRetryableOperation(IPipelineMessage message)
    {
        // Try to get the current operation from message context
        if (message.HasContent("CurrentOperation"))
        {
            object operation = message.GetContent<object>("CurrentOperation");
            if (operation is IRetryableOperation retryable)
            {
                return retryable;
            }
        }
        return null;
    }

    private static int GetMaxAttempts(RetryOptions options, IRetryableOperation? retryable)
    {
        return retryable?.MaxRetryAttempts ?? options.MaxRetryAttempts;
    }

    private RetryOptions GetEffectiveOptions(IRetryableOperation? retryable)
    {
        if (retryable == null)
        {
            return _defaultOptions;
        }

        return new RetryOptions
        {
            MaxRetryAttempts = retryable.MaxRetryAttempts,
            InitialRetryDelay = retryable.InitialRetryDelay,
            BackoffMultiplier = retryable.BackoffMultiplier,
            MaxRetryDelay = retryable.MaxRetryDelay,
            RetryableExceptions = retryable.RetryableExceptions,
            UseJitter = _defaultOptions.UseJitter,
            JitterFactor = _defaultOptions.JitterFactor
        };
    }

    private TimeSpan CalculateDelay(RetryOptions options, IRetryableOperation? retryable, int attemptNumber)
    {
        if (retryable != null)
        {
            TimeSpan baseDelay = retryable.InitialRetryDelay;
            var delay = TimeSpan.FromTicks((long)(baseDelay.Ticks * Math.Pow(retryable.BackoffMultiplier, attemptNumber - 1)));

            if (retryable.MaxRetryDelay.HasValue && delay > retryable.MaxRetryDelay.Value)
            {
                delay = retryable.MaxRetryDelay.Value;
            }

            // Apply jitter from options
            if (options.UseJitter && options.JitterFactor > 0)
            {
                double jitterRange = delay.TotalMilliseconds * options.JitterFactor;
                double jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        }

        return options.CalculateDelay(attemptNumber);
    }
}

/// <summary>
/// Exception thrown when all retry attempts have been exhausted.
/// </summary>
/// <remarks>
/// Creates a new instance of the exception.
/// </remarks>
/// <param name="attempts">The number of attempts made.</param>
/// <param name="innerException">The last exception that occurred.</param>
public class RetryExhaustedException(int attempts, Exception innerException) : Exception($"Operation failed after {attempts} retry attempts.", innerException)
{
    /// <summary>
    /// Gets the number of attempts made.
    /// </summary>
    public int Attempts { get; } = attempts;
}

