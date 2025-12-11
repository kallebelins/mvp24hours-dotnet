//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Defines a decorator for the Mediator that can intercept all operations.
/// Provides a higher-level abstraction than <see cref="IPipelineBehavior{TRequest, TResponse}"/>
/// for cross-cutting concerns that need to wrap the entire Mediator.
/// </summary>
/// <remarks>
/// <para>
/// Mediator decorators wrap the entire Mediator instance, allowing you to intercept
/// all calls including Send, Publish, and CreateStream operations.
/// </para>
/// <para>
/// <strong>Difference from IPipelineBehavior:</strong>
/// <list type="bullet">
/// <item><see cref="IMediatorDecorator"/> - Wraps the entire Mediator, intercepts all operations</item>
/// <item><see cref="IPipelineBehavior{TRequest, TResponse}"/> - Per-request pipeline behavior</item>
/// </list>
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Global logging of all mediator operations</item>
/// <item>Metrics collection across all request types</item>
/// <item>Global security checks</item>
/// <item>Request throttling or rate limiting</item>
/// <item>Circuit breaker at the mediator level</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MetricsMediatorDecorator : IMediatorDecorator
/// {
///     private readonly IMediator _inner;
///     private readonly IMetrics _metrics;
///     
///     public MetricsMediatorDecorator(IMediator inner, IMetrics metrics)
///     {
///         _inner = inner;
///         _metrics = metrics;
///     }
///     
///     public async Task&lt;TResponse&gt; SendAsync&lt;TResponse&gt;(
///         IMediatorRequest&lt;TResponse&gt; request,
///         CancellationToken cancellationToken = default)
///     {
///         using var timer = _metrics.StartTimer(request.GetType().Name);
///         try
///         {
///             return await _inner.SendAsync(request, cancellationToken);
///         }
///         catch
///         {
///             _metrics.IncrementError(request.GetType().Name);
///             throw;
///         }
///     }
///     
///     public Task PublishAsync&lt;TNotification&gt;(
///         TNotification notification,
///         CancellationToken cancellationToken = default)
///         where TNotification : IMediatorNotification
///     {
///         _metrics.IncrementNotificationCount(notification.GetType().Name);
///         return _inner.PublishAsync(notification, cancellationToken);
///     }
///     
///     public IAsyncEnumerable&lt;TResponse&gt; CreateStream&lt;TResponse&gt;(
///         IStreamRequest&lt;TResponse&gt; request,
///         CancellationToken cancellationToken = default)
///     {
///         _metrics.IncrementStreamCount(request.GetType().Name);
///         return _inner.CreateStream(request, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IMediatorDecorator : IMediator
{
    /// <summary>
    /// Gets the inner mediator being decorated.
    /// </summary>
    IMediator InnerMediator { get; }
}

/// <summary>
/// Base class for mediator decorators that provides default pass-through behavior.
/// Inherit from this class and override only the methods you need to intercept.
/// </summary>
/// <remarks>
/// <para>
/// This class implements all <see cref="IMediator"/> methods by delegating to the inner mediator.
/// Override specific methods to add custom behavior while keeping the default for others.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class LoggingMediatorDecorator : MediatorDecoratorBase
/// {
///     private readonly ILogger _logger;
///     
///     public LoggingMediatorDecorator(IMediator inner, ILogger&lt;LoggingMediatorDecorator&gt; logger) 
///         : base(inner)
///     {
///         _logger = logger;
///     }
///     
///     public override async Task&lt;TResponse&gt; SendAsync&lt;TResponse&gt;(
///         IMediatorRequest&lt;TResponse&gt; request,
///         CancellationToken cancellationToken = default)
///     {
///         _logger.LogInformation("Sending {RequestType}", request.GetType().Name);
///         var response = await base.SendAsync(request, cancellationToken);
///         _logger.LogInformation("Received response for {RequestType}", request.GetType().Name);
///         return response;
///     }
/// }
/// </code>
/// </example>
public abstract class MediatorDecoratorBase : IMediatorDecorator
{
    /// <summary>
    /// Gets the inner mediator being decorated.
    /// </summary>
    public IMediator InnerMediator { get; }

    /// <summary>
    /// Creates a new mediator decorator wrapping the specified inner mediator.
    /// </summary>
    /// <param name="inner">The mediator to decorate.</param>
    /// <exception cref="ArgumentNullException">Thrown when inner is null.</exception>
    protected MediatorDecoratorBase(IMediator inner)
    {
        InnerMediator = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Sends a request to its handler. Override to intercept Send operations.
    /// </summary>
    public virtual Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return InnerMediator.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification to all handlers. Override to intercept Publish operations.
    /// </summary>
    public virtual Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification
    {
        return InnerMediator.PublishAsync(notification, cancellationToken);
    }

    /// <summary>
    /// Creates a stream from a request. Override to intercept Stream operations.
    /// </summary>
    public virtual IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        return InnerMediator.CreateStream(request, cancellationToken);
    }
}

