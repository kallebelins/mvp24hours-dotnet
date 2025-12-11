//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines hooks that can be executed at specific points in the pipeline lifecycle.
/// Provides fine-grained extensibility without creating full behaviors.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline hooks are lightweight extension points that execute at predefined moments
/// in the request processing lifecycle. They're simpler than behaviors but less flexible.
/// </para>
/// <para>
/// <strong>Available Hooks:</strong>
/// <list type="bullet">
/// <item><see cref="OnPipelineStartAsync"/> - Before any behavior executes</item>
/// <item><see cref="OnPipelineCompleteAsync"/> - After all behaviors complete successfully</item>
/// <item><see cref="OnPipelineErrorAsync"/> - When an exception occurs in the pipeline</item>
/// </list>
/// </para>
/// </remarks>
public interface IPipelineHook
{
    /// <summary>
    /// Called at the very beginning of the pipeline, before any behavior.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="requestType">The runtime type of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the hook execution.</returns>
    Task OnPipelineStartAsync(object request, Type requestType, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the pipeline completes successfully.
    /// </summary>
    /// <param name="request">The request that was processed.</param>
    /// <param name="response">The response from the handler.</param>
    /// <param name="requestType">The runtime type of the request.</param>
    /// <param name="responseType">The runtime type of the response.</param>
    /// <param name="elapsedMilliseconds">Time taken to process the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the hook execution.</returns>
    Task OnPipelineCompleteAsync(
        object request,
        object? response,
        Type requestType,
        Type responseType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called when an exception occurs in the pipeline.
    /// </summary>
    /// <param name="request">The request that caused the exception.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="requestType">The runtime type of the request.</param>
    /// <param name="elapsedMilliseconds">Time taken before the exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the hook execution.</returns>
    Task OnPipelineErrorAsync(
        object request,
        Exception exception,
        Type requestType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken);
}

/// <summary>
/// Base implementation of <see cref="IPipelineHook"/> with empty default implementations.
/// Inherit from this class and override only the hooks you need.
/// </summary>
/// <example>
/// <code>
/// public class MetricsPipelineHook : PipelineHookBase
/// {
///     private readonly IMetrics _metrics;
///     
///     public MetricsPipelineHook(IMetrics metrics)
///     {
///         _metrics = metrics;
///     }
///     
///     public override Task OnPipelineCompleteAsync(
///         object request,
///         object? response,
///         Type requestType,
///         Type responseType,
///         long elapsedMilliseconds,
///         CancellationToken cancellationToken)
///     {
///         _metrics.RecordRequestDuration(requestType.Name, elapsedMilliseconds);
///         return Task.CompletedTask;
///     }
///     
///     public override Task OnPipelineErrorAsync(
///         object request,
///         Exception exception,
///         Type requestType,
///         long elapsedMilliseconds,
///         CancellationToken cancellationToken)
///     {
///         _metrics.IncrementErrorCount(requestType.Name, exception.GetType().Name);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public abstract class PipelineHookBase : IPipelineHook
{
    /// <inheritdoc />
    public virtual Task OnPipelineStartAsync(
        object request,
        Type requestType,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPipelineCompleteAsync(
        object request,
        object? response,
        Type requestType,
        Type responseType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPipelineErrorAsync(
        object request,
        Exception exception,
        Type requestType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Typed pipeline hook for specific request types.
/// </summary>
/// <typeparam name="TRequest">The request type this hook applies to.</typeparam>
public interface IPipelineHook<in TRequest>
{
    /// <summary>
    /// Called at the very beginning of the pipeline for this request type.
    /// </summary>
    Task OnPipelineStartAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the pipeline completes successfully for this request type.
    /// </summary>
    Task OnPipelineCompleteAsync(TRequest request, object? response, long elapsedMilliseconds, CancellationToken cancellationToken);

    /// <summary>
    /// Called when an exception occurs for this request type.
    /// </summary>
    Task OnPipelineErrorAsync(TRequest request, Exception exception, long elapsedMilliseconds, CancellationToken cancellationToken);
}

/// <summary>
/// Base implementation for typed pipeline hooks.
/// </summary>
/// <typeparam name="TRequest">The request type this hook applies to.</typeparam>
public abstract class PipelineHookBase<TRequest> : IPipelineHook<TRequest>
{
    /// <inheritdoc />
    public virtual Task OnPipelineStartAsync(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPipelineCompleteAsync(TRequest request, object? response, long elapsedMilliseconds, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPipelineErrorAsync(TRequest request, Exception exception, long elapsedMilliseconds, CancellationToken cancellationToken) => Task.CompletedTask;
}

