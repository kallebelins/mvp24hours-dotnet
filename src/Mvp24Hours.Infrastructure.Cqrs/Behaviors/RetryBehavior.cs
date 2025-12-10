//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that should be retried on transient failures.
/// </summary>
/// <remarks>
/// <para>
/// Apply this interface to requests that may fail due to transient issues
/// (network errors, database timeouts, etc.) and should be automatically retried.
/// </para>
/// <para>
/// <strong>Important:</strong> Only use on idempotent operations. Retrying
/// non-idempotent operations (like creating records without deduplication)
/// can lead to duplicate data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GetExternalDataQuery : IMediatorQuery&lt;ExternalData&gt;, IRetryable
/// {
///     public string ExternalId { get; init; } = string.Empty;
///     
///     // Optional: Customize retry settings
///     public int MaxRetryAttempts => 3;
///     public TimeSpan RetryDelay => TimeSpan.FromSeconds(1);
///     public bool UseExponentialBackoff => true;
/// }
/// </code>
/// </example>
public interface IRetryable
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// Default is 3.
    /// </summary>
    int MaxRetryAttempts => 3;

    /// <summary>
    /// Gets the delay between retry attempts.
    /// Default is 1 second.
    /// </summary>
    TimeSpan RetryDelay => TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets whether to use exponential backoff (delay doubles with each retry).
    /// Default is true.
    /// </summary>
    bool UseExponentialBackoff => true;

    /// <summary>
    /// Determines if the exception is transient and should trigger a retry.
    /// Override to customize which exceptions are retryable.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the exception is transient, false otherwise.</returns>
    bool IsTransientException(Exception exception)
    {
        // Default: retry on common transient exceptions
        return exception is TimeoutException
            || exception is OperationCanceledException
            || (exception.InnerException != null && IsTransientException(exception.InnerException));
    }
}

/// <summary>
/// Pipeline behavior that retries requests on transient failures.
/// Only applies to requests that implement <see cref="IRetryable"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior implements retry logic with optional exponential backoff.
/// It's a lightweight alternative to using Polly for simple retry scenarios.
/// </para>
/// <para>
/// <strong>For more advanced scenarios</strong> (circuit breaker, bulkhead, etc.),
/// consider integrating with Polly directly in your handlers or using the
/// Polly extensions for .NET.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(RetryBehavior&lt;,&gt;));
/// </code>
/// </example>
public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the RetryBehavior.
    /// </summary>
    /// <param name="logger">Optional logger for recording retry operations.</param>
    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply retry if the request implements IRetryable
        if (request is not IRetryable retryable)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var maxAttempts = retryable.MaxRetryAttempts;
        var baseDelay = retryable.RetryDelay;
        var useExponentialBackoff = retryable.UseExponentialBackoff;

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts + 1; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    _logger?.LogDebug(
                        "[Retry] Attempt {Attempt}/{MaxAttempts} for {RequestName}",
                        attempt,
                        maxAttempts + 1,
                        requestName);
                }

                return await next();
            }
            catch (Exception ex) when (attempt <= maxAttempts && retryable.IsTransientException(ex))
            {
                lastException = ex;

                var delay = useExponentialBackoff
                    ? TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                    : baseDelay;

                _logger?.LogWarning(
                    ex,
                    "[Retry] Transient failure for {RequestName} (Attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms. Error: {Message}",
                    requestName,
                    attempt,
                    maxAttempts + 1,
                    delay.TotalMilliseconds,
                    ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger?.LogError(
            lastException,
            "[Retry] All {MaxAttempts} retry attempts failed for {RequestName}",
            maxAttempts,
            requestName);

        throw lastException!;
    }
}

/// <summary>
/// Extension methods for configuring retry with Polly integration.
/// </summary>
/// <remarks>
/// This is a placeholder for future Polly integration.
/// For now, use <see cref="IRetryable"/> with the built-in retry behavior.
/// </remarks>
public static class RetryPolicyExtensions
{
    /// <summary>
    /// Determines if the exception is a database timeout.
    /// </summary>
    public static bool IsDatabaseTimeout(this Exception exception)
    {
        var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("timeout") && (message.Contains("sql") || message.Contains("database"));
    }

    /// <summary>
    /// Determines if the exception is a network error.
    /// </summary>
    public static bool IsNetworkError(this Exception exception)
    {
        var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
        return exception is System.Net.Http.HttpRequestException
            || message.Contains("network")
            || message.Contains("connection");
    }
}

