//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that automatically logs request processing.
/// Records start, end, execution time, and errors.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior logs at the following levels:
/// <list type="bullet">
/// <item>Information - Request start and successful completion</item>
/// <item>Error - Request failures with exception details</item>
/// </list>
/// </para>
/// <para>
/// Each request is assigned a unique short ID for correlation in logs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(LoggingBehavior&lt;,&gt;));
/// 
/// // Log output example:
/// // [Mediator] Starting GetUserQuery (ID: a1b2c3d4) 
/// // [Mediator] Completed GetUserQuery (ID: a1b2c3d4) in 42ms
/// </code>
/// </example>
/// <remarks>
/// Creates a new instance of the LoggingBehavior.
/// </remarks>
/// <param name="logger">Logger for recording messages.</param>
/// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        string requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[Mediator] Starting {RequestName} (ID: {RequestId})",
            requestName,
            requestId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            TResponse? response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "[Mediator] Completed {RequestName} (ID: {RequestId}) in {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "[Mediator] Failed {RequestName} (ID: {RequestId}) after {ElapsedMs}ms: {ErrorMessage}",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}

