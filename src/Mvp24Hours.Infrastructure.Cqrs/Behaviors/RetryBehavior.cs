//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

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
/// <remarks>
/// Creates a new instance of the RetryBehavior.
/// </remarks>
/// <param name="logger">Optional logger for recording retry operations.</param>
public sealed class RetryBehavior<TRequest, TResponse>(ILogger<RetryBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>>? _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply retry if the request implements IRetryable
        if (request is not IRetryable retryable)
        {
            return await next();
        }

        string requestName = typeof(TRequest).Name;
        int maxAttempts = retryable.MaxRetryAttempts;
        TimeSpan baseDelay = retryable.RetryDelay;
        bool useExponentialBackoff = retryable.UseExponentialBackoff;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts + 1; attempt++)
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

                TimeSpan delay = useExponentialBackoff
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
        string message = exception.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("timeout") && (message.Contains("sql") || message.Contains("database"));
    }

    /// <summary>
    /// Determines if the exception is a network error.
    /// </summary>
    public static bool IsNetworkError(this Exception exception)
    {
        string message = exception.Message?.ToLowerInvariant() ?? string.Empty;
        return exception is System.Net.Http.HttpRequestException
            || message.Contains("network")
            || message.Contains("connection");
    }
}

