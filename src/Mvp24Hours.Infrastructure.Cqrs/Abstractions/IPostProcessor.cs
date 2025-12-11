//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines a post-processor that executes after the request handler completes successfully.
/// Post-processors allow inspecting or transforming the response after processing.
/// </summary>
/// <typeparam name="TRequest">The type of request that was processed.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// Post-processors are executed after all <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// behaviors complete, in the order they are registered in the DI container.
/// </para>
/// <para>
/// <strong>Important:</strong> Post-processors only execute on successful handler completion.
/// If the handler or any behavior throws an exception, post-processors are skipped.
/// Use IExceptionHandler for exception handling.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Logging successful operations with response details</item>
/// <item>Triggering side effects after successful processing</item>
/// <item>Collecting metrics on responses</item>
/// <item>Caching responses</item>
/// <item>Auditing operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Difference from IPipelineBehavior:</strong>
/// <list type="bullet">
/// <item><see cref="IPostProcessor{TRequest, TResponse}"/> - Simple post-execution logic, can't modify the response</item>
/// <item><see cref="IPipelineBehavior{TRequest, TResponse}"/> - Full pipeline control, can modify response</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class LoggingPostProcessor&lt;TRequest, TResponse&gt; : IPostProcessor&lt;TRequest, TResponse&gt;
///     where TRequest : IMediatorRequest&lt;TResponse&gt;
/// {
///     private readonly ILogger&lt;LoggingPostProcessor&lt;TRequest, TResponse&gt;&gt; _logger;
///     
///     public LoggingPostProcessor(ILogger&lt;LoggingPostProcessor&lt;TRequest, TResponse&gt;&gt; logger)
///     {
///         _logger = logger;
///     }
///     
///     public Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Request {RequestType} completed with response type {ResponseType}",
///             typeof(TRequest).Name, typeof(TResponse).Name);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Processes the request and response after the handler completes.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="response">The response from the handler.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method is called after the handler successfully completes.
    /// The response is read-only; if you need to modify it, use <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
    /// </para>
    /// <para>
    /// Exceptions thrown here will propagate to the caller but will not trigger
    /// any rollback of the handler's work.
    /// </para>
    /// </remarks>
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for post-processors that should execute on all requests.
/// Useful for cross-cutting concerns that apply universally.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface when you want a post-processor to run for all request types
/// without having to register it for each specific type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MetricsPostProcessor : IPostProcessorGlobal
/// {
///     private readonly IMetrics _metrics;
///     
///     public MetricsPostProcessor(IMetrics metrics)
///     {
///         _metrics = metrics;
///     }
///     
///     public Task ProcessAsync(object request, object? response, CancellationToken cancellationToken)
///     {
///         _metrics.IncrementRequestCount(request.GetType().Name);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPostProcessorGlobal
{
    /// <summary>
    /// Processes any request and response after the handler completes.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="response">The response from the handler (may be null for Unit responses).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(object request, object? response, CancellationToken cancellationToken);
}

