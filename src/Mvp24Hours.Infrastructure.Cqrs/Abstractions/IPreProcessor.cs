//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines a pre-processor that executes before the request handler.
/// Pre-processors allow modifying or enriching the request before it reaches the handler.
/// </summary>
/// <typeparam name="TRequest">The type of request to pre-process.</typeparam>
/// <remarks>
/// <para>
/// Pre-processors are executed in the order they are registered in the DI container,
/// before any <see cref="IPipelineBehavior{TRequest, TResponse}"/> behaviors.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Enriching requests with additional data (e.g., current user, timestamp)</item>
/// <item>Normalizing or sanitizing request data</item>
/// <item>Setting up context before processing</item>
/// <item>Lightweight transformations that don't need access to the response</item>
/// </list>
/// </para>
/// <para>
/// <strong>Difference from IPipelineBehavior:</strong>
/// <list type="bullet">
/// <item><see cref="IPreProcessor{TRequest}"/> - Simple pre-execution logic, no access to response</item>
/// <item><see cref="IPipelineBehavior{TRequest, TResponse}"/> - Full pipeline control with before/after capabilities</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TimestampPreProcessor&lt;TRequest&gt; : IPreProcessor&lt;TRequest&gt;
///     where TRequest : IMediatorRequest&lt;object&gt;
/// {
///     public Task ProcessAsync(TRequest request, CancellationToken cancellationToken)
///     {
///         if (request is IHasTimestamp timestampedRequest)
///         {
///             timestampedRequest.Timestamp = DateTime.UtcNow;
///         }
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPreProcessor<in TRequest>
{
    /// <summary>
    /// Processes the request before it reaches the handler.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// The request can be modified in place by this method.
    /// If you throw an exception here, the handler will not be executed.
    /// </para>
    /// </remarks>
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for pre-processors that should execute on all requests.
/// Useful for cross-cutting concerns that apply universally.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when you want a pre-processor to run for all request types
/// without having to register it for each specific type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GlobalLoggingPreProcessor : IPreProcessorGlobal
/// {
///     private readonly ILogger _logger;
///     
///     public GlobalLoggingPreProcessor(ILogger&lt;GlobalLoggingPreProcessor&gt; logger)
///     {
///         _logger = logger;
///     }
///     
///     public Task ProcessAsync(object request, CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Processing request: {RequestType}", request.GetType().Name);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPreProcessorGlobal
{
    /// <summary>
    /// Processes any request before it reaches the handler.
    /// </summary>
    /// <param name="request">The request object to process.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(object request, CancellationToken cancellationToken);
}

