//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Events;

/// <summary>
/// Interface for dispatching application events to their handlers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Dispatch Strategies:</strong>
/// <list type="bullet">
/// <item>Sequential - Handlers execute one after another</item>
/// <item>Parallel - Handlers execute concurrently</item>
/// <item>Immediate - Events dispatched synchronously</item>
/// <item>Deferred - Events queued and dispatched later</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dispatch an event
/// var @event = new EntityCreatedEvent&lt;Customer&gt;(customer);
/// await _dispatcher.DispatchAsync(@event);
/// 
/// // Dispatch multiple events
/// await _dispatcher.DispatchAsync(new IApplicationEvent[]
/// {
///     new EntityCreatedEvent&lt;Order&gt;(order),
///     new EntityCreatedEvent&lt;OrderItem&gt;(item1),
///     new EntityCreatedEvent&lt;OrderItem&gt;(item2)
/// });
/// </code>
/// </example>
public interface IApplicationEventDispatcher
{
    /// <summary>
    /// Dispatches a single application event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to dispatch.</typeparam>
    /// <param name="event">The event to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent;

    /// <summary>
    /// Dispatches multiple application events to their handlers.
    /// </summary>
    /// <param name="events">The events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchAsync(IEnumerable<IApplicationEvent> events, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring the application event dispatcher.
/// </summary>
public class ApplicationEventDispatcherOptions
{
    /// <summary>
    /// Gets or sets the dispatch strategy for multiple handlers.
    /// Default is <see cref="EventDispatchStrategy.Parallel"/>.
    /// </summary>
    public EventDispatchStrategy Strategy { get; set; } = EventDispatchStrategy.Parallel;

    /// <summary>
    /// Gets or sets whether to continue dispatching to other handlers when one fails.
    /// Default is true.
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use the outbox pattern for reliable delivery.
    /// When enabled, events are first stored in an outbox before being dispatched.
    /// Default is false.
    /// </summary>
    public bool UseOutbox { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to integrate with the Mediator for dispatching.
    /// When enabled, events are also published as Mediator notifications.
    /// Default is false.
    /// </summary>
    public bool IntegrateWithMediator { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for parallel dispatch.
    /// Default is -1 (unlimited).
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = -1;
}

/// <summary>
/// Dispatch strategies for application events.
/// </summary>
public enum EventDispatchStrategy
{
    /// <summary>
    /// Handlers are executed sequentially, one after another.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Handlers are executed in parallel.
    /// </summary>
    Parallel = 1,

    /// <summary>
    /// Events are queued and dispatched by a background process.
    /// </summary>
    Deferred = 2
}

