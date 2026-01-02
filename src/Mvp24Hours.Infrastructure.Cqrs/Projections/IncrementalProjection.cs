//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Interface for incremental projections that process events one at a time.
/// </summary>
/// <remarks>
/// <para>
/// Incremental projections:
/// <list type="bullet">
/// <item>Process each event as it arrives</item>
/// <item>Maintain their own position</item>
/// <item>Support catch-up and live processing</item>
/// <item>Are the most common type of projection</item>
/// </list>
/// </para>
/// </remarks>
public interface IIncrementalProjection : IProjection
{
    /// <summary>
    /// Processes a single event.
    /// </summary>
    /// <param name="event">The event to process.</param>
    /// <param name="context">The projection context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessEventAsync(IMediatorDomainEvent @event, ProjectionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event types this projection handles.
    /// </summary>
    IReadOnlyList<Type> HandledEventTypes { get; }
}

/// <summary>
/// Base class for incremental projections.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public abstract class IncrementalProjection<TReadModel> : ProjectionBase, IIncrementalProjection
    where TReadModel : class
{
    /// <summary>
    /// Gets the read model repository.
    /// </summary>
    protected IReadModelRepository<TReadModel> Repository { get; }

    private readonly List<Type> _handledEventTypes = new();

    /// <inheritdoc />
    public IReadOnlyList<Type> HandledEventTypes => _handledEventTypes.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the incremental projection.
    /// </summary>
    /// <param name="repository">The read model repository.</param>
    protected IncrementalProjection(IReadModelRepository<TReadModel> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        RegisterEventHandlers();
    }

    /// <summary>
    /// Override to register event handlers using the <see cref="Handles{TEvent}"/> method.
    /// </summary>
    protected abstract void RegisterEventHandlers();

    /// <summary>
    /// Registers an event type that this projection handles.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    protected void Handles<TEvent>() where TEvent : IMediatorDomainEvent
    {
        _handledEventTypes.Add(typeof(TEvent));
    }

    /// <inheritdoc />
    public abstract Task ProcessEventAsync(
        IMediatorDomainEvent @event,
        ProjectionContext context,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await base.ResetAsync(cancellationToken);
        await Repository.DeleteAllAsync(cancellationToken);
    }
}

/// <summary>
/// Convenience base class for projections that handle multiple event types.
/// Uses the Apply pattern for event handling.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
/// <example>
/// <code>
/// public class OrderSummaryProjection : ApplyProjection&lt;OrderSummary&gt;
/// {
///     public override string Name => "OrderSummary";
/// 
///     public OrderSummaryProjection(IReadModelRepository&lt;OrderSummary&gt; repository) 
///         : base(repository)
///     {
///     }
/// 
///     protected override void RegisterEventHandlers()
///     {
///         Handles&lt;OrderCreatedEvent&gt;();
///         Handles&lt;OrderShippedEvent&gt;();
///         Handles&lt;OrderCancelledEvent&gt;();
///     }
/// 
///     public async Task Apply(OrderCreatedEvent @event, ProjectionContext context, CancellationToken ct)
///     {
///         await Repository.InsertAsync(new OrderSummary
///         {
///             Id = @event.OrderId,
///             Status = "Created",
///             CreatedAt = @event.OccurredAt
///         }, ct);
///     }
/// 
///     public async Task Apply(OrderShippedEvent @event, ProjectionContext context, CancellationToken ct)
///     {
///         var order = await Repository.GetByIdAsync(@event.OrderId, ct);
///         if (order != null)
///         {
///             order.Status = "Shipped";
///             order.ShippedAt = @event.OccurredAt;
///             await Repository.UpdateAsync(order, ct);
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class ApplyProjection<TReadModel> : IncrementalProjection<TReadModel>
    where TReadModel : class
{
    private readonly Dictionary<Type, Func<IMediatorDomainEvent, ProjectionContext, CancellationToken, Task>> _handlers = new();

    /// <summary>
    /// Initializes a new instance of the apply projection.
    /// </summary>
    /// <param name="repository">The read model repository.</param>
    protected ApplyProjection(IReadModelRepository<TReadModel> repository) : base(repository)
    {
        DiscoverApplyMethods();
    }

    /// <inheritdoc />
    public override async Task ProcessEventAsync(
        IMediatorDomainEvent @event,
        ProjectionContext context,
        CancellationToken cancellationToken = default)
    {
        var eventType = @event.GetType();
        
        if (_handlers.TryGetValue(eventType, out var handler))
        {
            await handler(@event, context, cancellationToken);
        }
    }

    private void DiscoverApplyMethods()
    {
        var methods = GetType()
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "Apply" && m.GetParameters().Length == 3);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 3) continue;

            var eventType = parameters[0].ParameterType;
            if (!typeof(IMediatorDomainEvent).IsAssignableFrom(eventType)) continue;
            if (parameters[1].ParameterType != typeof(ProjectionContext)) continue;
            if (parameters[2].ParameterType != typeof(CancellationToken)) continue;

            var handler = CreateHandler(method, eventType);
            _handlers[eventType] = handler;
        }
    }

    private Func<IMediatorDomainEvent, ProjectionContext, CancellationToken, Task> CreateHandler(
        System.Reflection.MethodInfo method,
        Type eventType)
    {
        return async (@event, context, ct) =>
        {
            var result = method.Invoke(this, new object[] { @event, context, ct });
            if (result is Task task)
            {
                await task;
            }
        };
    }
}

/// <summary>
/// Interface for batch projections that process multiple events at once.
/// </summary>
/// <remarks>
/// <para>
/// Batch projections:
/// <list type="bullet">
/// <item>Process events in batches for better performance</item>
/// <item>Useful for aggregations and complex calculations</item>
/// <item>Can optimize database operations</item>
/// </list>
/// </para>
/// </remarks>
public interface IBatchProjection : IProjection
{
    /// <summary>
    /// Gets the batch size.
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Processes a batch of events.
    /// </summary>
    /// <param name="events">The events to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessBatchAsync(
        IReadOnlyList<(IMediatorDomainEvent Event, ProjectionContext Context)> events,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for batch projections.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public abstract class BatchProjection<TReadModel> : ProjectionBase, IBatchProjection
    where TReadModel : class
{
    /// <summary>
    /// Gets the read model repository.
    /// </summary>
    protected IReadModelRepository<TReadModel> Repository { get; }

    /// <inheritdoc />
    public virtual int BatchSize => 100;

    /// <summary>
    /// Initializes a new instance of the batch projection.
    /// </summary>
    /// <param name="repository">The read model repository.</param>
    protected BatchProjection(IReadModelRepository<TReadModel> repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public abstract Task ProcessBatchAsync(
        IReadOnlyList<(IMediatorDomainEvent Event, ProjectionContext Context)> events,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await base.ResetAsync(cancellationToken);
        await Repository.DeleteAllAsync(cancellationToken);
    }
}

/// <summary>
/// Checkpoint manager for projection positions with persistence.
/// </summary>
public interface IProjectionCheckpointManager
{
    /// <summary>
    /// Gets the last checkpoint position for a projection.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The checkpoint position.</returns>
    Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a checkpoint for a projection.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveCheckpointAsync(
        ProjectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a projection checkpoint.
/// </summary>
public class ProjectionCheckpoint
{
    /// <summary>
    /// Gets or sets the projection name.
    /// </summary>
    public string ProjectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the global event position.
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of this checkpoint.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// In-memory implementation of checkpoint manager.
/// </summary>
public class InMemoryProjectionCheckpointManager : IProjectionCheckpointManager
{
    private readonly Dictionary<string, ProjectionCheckpoint> _checkpoints = new();

    /// <inheritdoc />
    public Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        _checkpoints.TryGetValue(projectionName, out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    /// <inheritdoc />
    public Task SaveCheckpointAsync(
        ProjectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        _checkpoints[checkpoint.ProjectionName] = checkpoint;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all checkpoints. Useful for testing.
    /// </summary>
    public void Clear()
    {
        _checkpoints.Clear();
    }
}


