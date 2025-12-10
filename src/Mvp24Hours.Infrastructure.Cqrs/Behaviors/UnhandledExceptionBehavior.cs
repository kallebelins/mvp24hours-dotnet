//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Pipeline behavior that captures unhandled exceptions and adds context.
/// Useful for centralized error logging.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior catches exceptions, logs them with request context, and rethrows.
/// It does not swallow exceptions but ensures they are properly logged.
/// </para>
/// <para>
/// <strong>Note:</strong> This behavior should typically be registered early in the pipeline
/// (first to be registered = last to execute, wrapping all other behaviors).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI (register first to wrap all other behaviors)
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(UnhandledExceptionBehavior&lt;,&gt;));
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(LoggingBehavior&lt;,&gt;));
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(ValidationBehavior&lt;,&gt;));
/// </code>
/// </example>
public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Creates a new instance of the UnhandledExceptionBehavior.
    /// </summary>
    /// <param name="logger">Logger for recording errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogError(
                ex,
                "[Mediator] Unhandled exception for {RequestName}: {ExceptionType} - {Message}",
                requestName,
                ex.GetType().Name,
                ex.Message);

            throw;
        }
    }
}

