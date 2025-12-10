//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that monitors performance and alerts about slow requests.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior logs a warning when request execution time exceeds the configured threshold.
/// Default threshold is 500ms but can be customized via constructor.
/// </para>
/// <para>
/// <strong>Use Case:</strong> Identify slow requests that may need optimization
/// or may indicate performance issues.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with default threshold (500ms)
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(PerformanceBehavior&lt;,&gt;));
/// 
/// // Or register with custom threshold via factory
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), sp =>
/// {
///     var logger = sp.GetRequiredService&lt;ILogger&lt;PerformanceBehavior&lt;TRequest, TResponse&gt;&gt;&gt;();
///     return new PerformanceBehavior&lt;TRequest, TResponse&gt;(logger, thresholdMilliseconds: 1000);
/// });
/// </code>
/// </example>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _thresholdMilliseconds;

    /// <summary>
    /// Creates a new instance of the PerformanceBehavior.
    /// </summary>
    /// <param name="logger">Logger for recording alerts.</param>
    /// <param name="thresholdMilliseconds">Threshold in milliseconds to consider a request slow (default: 500ms).</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int thresholdMilliseconds = 500)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _thresholdMilliseconds = thresholdMilliseconds;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _thresholdMilliseconds)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "[Performance] Slow request detected: {RequestName} executed in {ElapsedMs}ms (threshold: {Threshold}ms)",
                requestName,
                stopwatch.ElapsedMilliseconds,
                _thresholdMilliseconds);
        }

        return response;
    }
}

