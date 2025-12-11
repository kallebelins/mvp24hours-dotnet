//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that have a configurable timeout.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface can specify a custom timeout duration.
/// If not specified, the default timeout from <see cref="MediatorOptions.DefaultTimeoutMilliseconds"/> is used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class LongRunningCommand : IMediatorCommand&lt;string&gt;, IHasTimeout
/// {
///     public int TimeoutMilliseconds => 30000; // 30 second timeout
/// }
/// </code>
/// </example>
public interface IHasTimeout
{
    /// <summary>
    /// Gets the timeout in milliseconds for this request.
    /// If null, the default timeout is used.
    /// </summary>
    int? TimeoutMilliseconds => null;
}

/// <summary>
/// Timeout policy configuration for requests.
/// </summary>
public interface ITimeoutPolicy
{
    /// <summary>
    /// Gets the timeout in milliseconds.
    /// </summary>
    int TimeoutMilliseconds { get; }

    /// <summary>
    /// Gets whether to throw TimeoutException or return default value on timeout.
    /// </summary>
    bool ThrowOnTimeout { get; }

    /// <summary>
    /// Gets whether to cancel the underlying operation on timeout.
    /// </summary>
    bool CancelOnTimeout { get; }
}

/// <summary>
/// Default implementation of <see cref="ITimeoutPolicy"/>.
/// </summary>
public sealed record TimeoutPolicy : ITimeoutPolicy
{
    /// <summary>
    /// Creates a new timeout policy with the specified timeout.
    /// </summary>
    public TimeoutPolicy(int timeoutMilliseconds, bool throwOnTimeout = true, bool cancelOnTimeout = true)
    {
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout must be greater than 0.");
        }

        TimeoutMilliseconds = timeoutMilliseconds;
        ThrowOnTimeout = throwOnTimeout;
        CancelOnTimeout = cancelOnTimeout;
    }

    /// <inheritdoc />
    public int TimeoutMilliseconds { get; }

    /// <inheritdoc />
    public bool ThrowOnTimeout { get; }

    /// <inheritdoc />
    public bool CancelOnTimeout { get; }

    /// <summary>
    /// Creates a policy with a 5 second timeout.
    /// </summary>
    public static TimeoutPolicy Fast => new(5000);

    /// <summary>
    /// Creates a policy with a 30 second timeout.
    /// </summary>
    public static TimeoutPolicy Default => new(30000);

    /// <summary>
    /// Creates a policy with a 120 second timeout.
    /// </summary>
    public static TimeoutPolicy Long => new(120000);
}

/// <summary>
/// Pipeline behavior that enforces a timeout on request execution.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior wraps the request execution in a timeout. If the request takes
/// longer than the configured timeout, a <see cref="TimeoutException"/> is thrown.
/// </para>
/// <para>
/// <strong>Timeout Resolution Order:</strong>
/// <code>
/// ┌──────────────────────────────────────────────────────────────────────────────┐
/// │ 1. Request implements IHasTimeout with non-null TimeoutMilliseconds         │
/// │ 2. Request type registered in timeout policies                               │
/// │ 3. MediatorOptions.DefaultTimeoutMilliseconds                                │
/// │ 4. No timeout (if all above are not set)                                    │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddMvpMediator(options =>
/// {
///     options.RegisterTimeoutBehavior = true;
///     options.DefaultTimeoutMilliseconds = 30000; // 30 seconds default
/// });
/// 
/// // Or use per-request timeout
/// public class SlowCommand : IMediatorCommand&lt;string&gt;, IHasTimeout
/// {
///     public int? TimeoutMilliseconds => 60000; // 1 minute for this command
/// }
/// </code>
/// </example>
public sealed class TimeoutBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IOptions<MediatorOptions>? _options;
    private readonly ILogger<TimeoutBehavior<TRequest, TResponse>>? _logger;

    /// <summary>
    /// Creates a new instance of the TimeoutBehavior.
    /// </summary>
    /// <param name="options">The mediator options.</param>
    /// <param name="logger">Optional logger.</param>
    public TimeoutBehavior(
        IOptions<MediatorOptions>? options = null,
        ILogger<TimeoutBehavior<TRequest, TResponse>>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var timeout = GetTimeout(request);

        // No timeout configured
        if (timeout <= 0)
        {
            return await next();
        }

        _logger?.LogDebug(
            "[Timeout] Executing {RequestName} with {Timeout}ms timeout",
            requestName,
            timeout);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, 
            timeoutCts.Token);

        try
        {
            // Execute with the linked token
            var task = next();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, linkedCts.Token));

            if (completedTask != task)
            {
                // Timeout occurred
                _logger?.LogWarning(
                    "[Timeout] Request {RequestName} timed out after {Timeout}ms",
                    requestName,
                    timeout);

                throw new RequestTimeoutException(requestName, timeout);
            }

            return await task;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger?.LogWarning(
                "[Timeout] Request {RequestName} timed out after {Timeout}ms",
                requestName,
                timeout);

            throw new RequestTimeoutException(requestName, timeout);
        }
    }

    private int GetTimeout(TRequest request)
    {
        // First, check if request has a specific timeout
        if (request is IHasTimeout hasTimeout && hasTimeout.TimeoutMilliseconds.HasValue)
        {
            return hasTimeout.TimeoutMilliseconds.Value;
        }

        // Fall back to default timeout from options
        return _options?.Value?.DefaultTimeoutMilliseconds ?? 0;
    }
}

/// <summary>
/// Exception thrown when a request times out.
/// </summary>
public sealed class RequestTimeoutException : TimeoutException
{
    /// <summary>
    /// Gets the name of the request that timed out.
    /// </summary>
    public string RequestName { get; }

    /// <summary>
    /// Gets the timeout duration in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; }

    /// <summary>
    /// Creates a new instance of RequestTimeoutException.
    /// </summary>
    /// <param name="requestName">The request name.</param>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    public RequestTimeoutException(string requestName, int timeoutMilliseconds)
        : base($"Request '{requestName}' timed out after {timeoutMilliseconds}ms.")
    {
        RequestName = requestName;
        TimeoutMilliseconds = timeoutMilliseconds;
    }

    /// <summary>
    /// Creates a new instance of RequestTimeoutException.
    /// </summary>
    /// <param name="requestName">The request name.</param>
    /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
    /// <param name="innerException">The inner exception.</param>
    public RequestTimeoutException(string requestName, int timeoutMilliseconds, Exception innerException)
        : base($"Request '{requestName}' timed out after {timeoutMilliseconds}ms.", innerException)
    {
        RequestName = requestName;
        TimeoutMilliseconds = timeoutMilliseconds;
    }
}

