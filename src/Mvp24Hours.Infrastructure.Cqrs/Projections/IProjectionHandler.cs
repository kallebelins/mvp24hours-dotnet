//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Marker interface for projection handlers.
/// </summary>
public interface IProjectionHandler
{
    /// <summary>
    /// Gets the event types this handler can process.
    /// </summary>
    IReadOnlyList<Type> HandledEventTypes { get; }
}

/// <summary>
/// Handler for projecting a specific event type to a read model.
/// </summary>
/// <typeparam name="TEvent">The event type to project.</typeparam>
/// <remarks>
/// <para>
/// Projection handlers are responsible for updating read models in response
/// to domain events. Unlike domain event handlers, projection handlers:
/// <list type="bullet">
/// <item>Focus on updating denormalized views</item>
/// <item>Are typically idempotent (can process the same event multiple times)</item>
/// <item>Work with global event position for ordering</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderSummaryProjectionHandler : IProjectionHandler&lt;OrderPlacedEvent&gt;
/// {
///     private readonly IReadModelRepository&lt;OrderSummary&gt; _repository;
/// 
///     public OrderSummaryProjectionHandler(IReadModelRepository&lt;OrderSummary&gt; repository)
///     {
///         _repository = repository;
///     }
/// 
///     public async Task HandleAsync(OrderPlacedEvent @event, ProjectionContext context, CancellationToken cancellationToken)
///     {
///         var summary = new OrderSummary
///         {
///             Id = @event.OrderId,
///             CustomerEmail = @event.CustomerEmail,
///             TotalAmount = @event.TotalAmount,
///             Status = "Placed",
///             CreatedAt = @event.OccurredAt
///         };
/// 
///         await _repository.UpsertAsync(summary, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IProjectionHandler<in TEvent> : IProjectionHandler
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the projection of an event.
    /// </summary>
    /// <param name="event">The event to project.</param>
    /// <param name="context">The projection context with metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent @event, ProjectionContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    IReadOnlyList<Type> IProjectionHandler.HandledEventTypes => new[] { typeof(TEvent) };
}

/// <summary>
/// Handler that can process multiple event types.
/// </summary>
public interface IMultiEventProjectionHandler : IProjectionHandler
{
    /// <summary>
    /// Handles the projection of any supported event.
    /// </summary>
    /// <param name="event">The event to project.</param>
    /// <param name="context">The projection context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(IDomainEvent @event, ProjectionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this handler can process the given event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>True if the handler can process this event type.</returns>
    bool CanHandle(Type eventType);
}

/// <summary>
/// Context information passed to projection handlers.
/// Contains metadata about the event being projected.
/// </summary>
public class ProjectionContext
{
    /// <summary>
    /// Gets the global position of the event in the event store.
    /// </summary>
    public long GlobalPosition { get; init; }

    /// <summary>
    /// Gets the aggregate ID that the event belongs to.
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets the version of the event within its aggregate stream.
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// Gets the timestamp when the event was stored.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the causation ID.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets the name of the projection processing this event.
    /// </summary>
    public string ProjectionName { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this is a rebuild operation.
    /// </summary>
    public bool IsRebuilding { get; init; }

    /// <summary>
    /// Creates a projection context from a stored event.
    /// </summary>
    /// <param name="storedEvent">The stored event.</param>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="isRebuilding">Whether this is a rebuild.</param>
    /// <returns>The projection context.</returns>
    public static ProjectionContext FromStoredEvent(
        StoredEvent storedEvent,
        string projectionName,
        bool isRebuilding = false)
    {
        return new ProjectionContext
        {
            GlobalPosition = storedEvent.GlobalPosition,
            AggregateId = storedEvent.AggregateId,
            Version = storedEvent.Version,
            Timestamp = storedEvent.Timestamp,
            CorrelationId = storedEvent.CorrelationId,
            CausationId = storedEvent.CausationId,
            ProjectionName = projectionName,
            IsRebuilding = isRebuilding
        };
    }
}

/// <summary>
/// Base class for projection handlers that simplifies implementation.
/// </summary>
/// <typeparam name="TEvent">The event type to project.</typeparam>
public abstract class ProjectionHandlerBase<TEvent> : IProjectionHandler<TEvent>
    where TEvent : IDomainEvent
{
    /// <inheritdoc />
    public abstract Task HandleAsync(TEvent @event, ProjectionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for handlers that project to a specific read model type.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public abstract class ReadModelProjectionHandler<TEvent, TReadModel> : ProjectionHandlerBase<TEvent>
    where TEvent : IDomainEvent
    where TReadModel : class
{
    /// <summary>
    /// Gets the read model repository.
    /// </summary>
    protected IReadModelRepository<TReadModel> Repository { get; }

    /// <summary>
    /// Initializes a new instance of the projection handler.
    /// </summary>
    /// <param name="repository">The read model repository.</param>
    protected ReadModelProjectionHandler(IReadModelRepository<TReadModel> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
}

/// <summary>
/// Handler for projecting multiple events to update a single read model.
/// Useful for aggregating data from multiple event types.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public abstract class AggregatingProjectionHandler<TReadModel> : IMultiEventProjectionHandler
    where TReadModel : class
{
    private readonly List<Type> _handledTypes = new();

    /// <summary>
    /// Gets the read model repository.
    /// </summary>
    protected IReadModelRepository<TReadModel> Repository { get; }

    /// <inheritdoc />
    public IReadOnlyList<Type> HandledEventTypes => _handledTypes.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the aggregating handler.
    /// </summary>
    /// <param name="repository">The read model repository.</param>
    protected AggregatingProjectionHandler(IReadModelRepository<TReadModel> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Registers an event type that this handler can process.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    protected void Handles<TEvent>() where TEvent : IDomainEvent
    {
        _handledTypes.Add(typeof(TEvent));
    }

    /// <inheritdoc />
    public bool CanHandle(Type eventType)
    {
        return _handledTypes.Any(t => t.IsAssignableFrom(eventType));
    }

    /// <inheritdoc />
    public abstract Task HandleAsync(IDomainEvent @event, ProjectionContext context, CancellationToken cancellationToken = default);
}


