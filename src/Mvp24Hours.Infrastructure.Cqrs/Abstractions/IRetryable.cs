//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

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
