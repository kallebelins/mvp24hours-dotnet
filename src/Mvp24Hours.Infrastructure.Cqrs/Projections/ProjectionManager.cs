//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Projections;

/// <summary>
/// Interface for managing projections lifecycle.
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Starts all registered projections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all running projections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds a specific projection from the beginning.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RebuildAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds all projections from the beginning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RebuildAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all projections.
    /// </summary>
    /// <returns>Projection information.</returns>
    IReadOnlyList<ProjectionInfo> GetProjectionInfos();

    /// <summary>
    /// Gets information about a specific projection.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <returns>Projection information.</returns>
    ProjectionInfo? GetProjectionInfo(string projectionName);

    /// <summary>
    /// Processes a single event for all applicable projections.
    /// </summary>
    /// <param name="storedEvent">The stored event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessEventAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manager for coordinating projection handlers and their lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Projection Manager Responsibilities:</strong>
/// <list type="bullet">
/// <item>Register and manage projection handlers</item>
/// <item>Subscribe to event store for new events</item>
/// <item>Dispatch events to appropriate handlers</item>
/// <item>Track projection positions for catch-up</item>
/// <item>Support rebuild operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure projections
/// services.AddProjectionManager(options =>
/// {
///     options.AddProjection&lt;OrderSummaryProjection&gt;();
///     options.AddProjection&lt;CustomerStatsProjection&gt;();
/// });
/// 
/// // Start projections (typically in hosted service)
/// await projectionManager.StartAsync();
/// 
/// // Rebuild a specific projection
/// await projectionManager.RebuildAsync("OrderSummary");
/// </code>
/// </example>
public class ProjectionManager : IProjectionManager
{
    private readonly IEventStoreWithSubscription _eventStore;
    private readonly IEventSerializer _eventSerializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectionManager> _logger;
    private readonly IProjectionPositionStore _positionStore;
    private readonly Dictionary<string, ProjectionRegistration> _registrations = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;

    /// <summary>
    /// Initializes a new instance of the projection manager.
    /// </summary>
    public ProjectionManager(
        IEventStoreWithSubscription eventStore,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider,
        IProjectionPositionStore positionStore,
        ILogger<ProjectionManager> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _eventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _positionStore = positionStore ?? throw new ArgumentNullException(nameof(positionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a projection.
    /// </summary>
    /// <param name="name">The projection name.</param>
    /// <param name="handlerTypes">The handler types for this projection.</param>
    public void RegisterProjection(string name, params Type[] handlerTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Projection name is required.", nameof(name));

        _registrations[name] = new ProjectionRegistration
        {
            Name = name,
            HandlerTypes = handlerTypes.ToList()
        };
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting projection manager with {Count} projections", _registrations.Count);

        // Load initial positions for all projections
        foreach (var registration in _registrations.Values)
        {
            registration.Position = await _positionStore.GetPositionAsync(registration.Name, cancellationToken);
            registration.Status = ProjectionStatus.CatchingUp;
            
            _logger.LogInformation(
                "Projection {Name} starting from position {Position}",
                registration.Name,
                registration.Position);
        }

        // Start background processing
        _processingTask = ProcessEventsAsync(_cts.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping projection manager");

        _cts.Cancel();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        foreach (var registration in _registrations.Values)
        {
            registration.Status = ProjectionStatus.Stopped;
        }
    }

    /// <inheritdoc />
    public async Task RebuildAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        if (!_registrations.TryGetValue(projectionName, out var registration))
        {
            throw new InvalidOperationException($"Projection '{projectionName}' not found.");
        }

        _logger.LogInformation("Rebuilding projection {Name}", projectionName);

        registration.Status = ProjectionStatus.Rebuilding;

        // Reset position
        await _positionStore.SavePositionAsync(projectionName, 0, cancellationToken);
        registration.Position = 0;

        // Reset any resettable projections
        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in registration.HandlerTypes)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler is IResettableProjection resettable)
            {
                await resettable.ResetAsync(cancellationToken);
            }
        }

        // Process all events from the beginning
        await CatchUpAsync(registration, isRebuild: true, cancellationToken);

        registration.Status = ProjectionStatus.Live;
        _logger.LogInformation("Projection {Name} rebuild complete at position {Position}", projectionName, registration.Position);
    }

    /// <inheritdoc />
    public async Task RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rebuilding all {Count} projections", _registrations.Count);

        foreach (var projectionName in _registrations.Keys)
        {
            await RebuildAsync(projectionName, cancellationToken);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ProjectionInfo> GetProjectionInfos()
    {
        return _registrations.Values.Select(r => new ProjectionInfo
        {
            Name = r.Name,
            Position = r.Position,
            Status = r.Status,
            LastErrorMessage = r.LastError?.Message,
            LastUpdatedAt = r.LastUpdatedAt,
            HandledEventTypes = r.HandlerTypes.SelectMany(GetHandledEventTypes).Select(t => t.Name).Distinct().ToList()
        }).ToList();
    }

    /// <inheritdoc />
    public ProjectionInfo? GetProjectionInfo(string projectionName)
    {
        if (!_registrations.TryGetValue(projectionName, out var registration))
        {
            return null;
        }

        return new ProjectionInfo
        {
            Name = registration.Name,
            Position = registration.Position,
            Status = registration.Status,
            LastErrorMessage = registration.LastError?.Message,
            LastUpdatedAt = registration.LastUpdatedAt,
            HandledEventTypes = registration.HandlerTypes
                .SelectMany(GetHandledEventTypes)
                .Select(t => t.Name)
                .Distinct()
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task ProcessEventAsync(StoredEvent storedEvent, CancellationToken cancellationToken = default)
    {
        var @event = _eventSerializer.Deserialize(storedEvent.EventType, storedEvent.EventData);

        foreach (var registration in _registrations.Values)
        {
            if (storedEvent.GlobalPosition <= registration.Position)
            {
                continue; // Already processed
            }

            try
            {
                await ProcessEventForProjectionAsync(
                    registration,
                    storedEvent,
                    @event,
                    isRebuild: false,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {Position} for projection {Name}",
                    storedEvent.GlobalPosition, registration.Name);
                registration.LastError = ex;
                registration.Status = ProjectionStatus.Faulted;
            }
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // First, catch up all projections
            foreach (var registration in _registrations.Values)
            {
                await CatchUpAsync(registration, isRebuild: false, cancellationToken);
                registration.Status = ProjectionStatus.Live;
            }

            // Then subscribe to new events
            var minPosition = _registrations.Values.Min(r => r.Position);

            await foreach (var storedEvent in _eventStore.SubscribeFromPositionAsync(minPosition, cancellationToken))
            {
                await ProcessEventAsync(storedEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Projection processing stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in projection processing");
            throw;
        }
    }

    private async Task CatchUpAsync(ProjectionRegistration registration, bool isRebuild, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Catching up projection {Name} from position {Position}",
            registration.Name,
            registration.Position);

        await foreach (var storedEvent in _eventStore.SubscribeFromPositionAsync(registration.Position, cancellationToken))
        {
            var @event = _eventSerializer.Deserialize(storedEvent.EventType, storedEvent.EventData);

            await ProcessEventForProjectionAsync(
                registration,
                storedEvent,
                @event,
                isRebuild,
                cancellationToken);

            // Check if we've caught up (simple check - in production would use event store position)
            if (await IsUpToDateAsync(storedEvent.GlobalPosition))
            {
                break;
            }
        }
    }

    private async Task ProcessEventForProjectionAsync(
        ProjectionRegistration registration,
        StoredEvent storedEvent,
        CoreDomainEvent @event,
        bool isRebuild,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = ProjectionContext.FromStoredEvent(storedEvent, registration.Name, isRebuild);
        var eventType = @event.GetType();

        foreach (var handlerType in registration.HandlerTypes)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler == null) continue;

            // Check if handler can process this event type
            if (CanHandle(handler, eventType))
            {
                await InvokeHandlerAsync(handler, @event, context, cancellationToken);
            }
        }

        // Update position
        registration.Position = storedEvent.GlobalPosition;
        registration.LastUpdatedAt = DateTime.UtcNow;
        await _positionStore.SavePositionAsync(registration.Name, registration.Position, cancellationToken);
    }

    private static bool CanHandle(object handler, Type eventType)
    {
        if (handler is IMultiEventProjectionHandler multiHandler)
        {
            return multiHandler.CanHandle(eventType);
        }

        var handlerType = handler.GetType();
        var interfaces = handlerType.GetInterfaces();
        
        return interfaces.Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>) &&
            i.GetGenericArguments()[0].IsAssignableFrom(eventType));
    }

    private static async Task InvokeHandlerAsync(
        object handler,
        CoreDomainEvent @event,
        ProjectionContext context,
        CancellationToken cancellationToken)
    {
        if (handler is IMultiEventProjectionHandler multiHandler)
        {
            await multiHandler.HandleAsync(@event, context, cancellationToken);
            return;
        }

        var eventType = @event.GetType();
        var handlerType = handler.GetType();
        var handleMethod = handlerType.GetMethod("HandleAsync", new[] { eventType, typeof(ProjectionContext), typeof(CancellationToken) });

        if (handleMethod != null)
        {
            var task = (Task?)handleMethod.Invoke(handler, new object[] { @event, context, cancellationToken });
            if (task != null)
            {
                await task;
            }
        }
    }

    private Task<bool> IsUpToDateAsync(long currentPosition)
    {
        // In a real implementation, this would check the event store's current position
        // For now, we'll check if there are no more events waiting
        return Task.FromResult(false);
    }

    private static IEnumerable<Type> GetHandledEventTypes(Type handlerType)
    {
        return handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionHandler<>))
            .Select(i => i.GetGenericArguments()[0]);
    }

    private class ProjectionRegistration
    {
        public string Name { get; set; } = string.Empty;
        public List<Type> HandlerTypes { get; set; } = new();
        public long Position { get; set; }
        public ProjectionStatus Status { get; set; } = ProjectionStatus.NotStarted;
        public Exception? LastError { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}

/// <summary>
/// Interface for storing projection positions.
/// </summary>
public interface IProjectionPositionStore
{
    /// <summary>
    /// Gets the current position for a projection.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current position.</returns>
    Task<long> GetPositionAsync(string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the position for a projection.
    /// </summary>
    /// <param name="projectionName">The projection name.</param>
    /// <param name="position">The position to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SavePositionAsync(string projectionName, long position, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of projection position store.
/// </summary>
public class InMemoryProjectionPositionStore : IProjectionPositionStore
{
    private readonly Dictionary<string, long> _positions = new();

    /// <inheritdoc />
    public Task<long> GetPositionAsync(string projectionName, CancellationToken cancellationToken = default)
    {
        _positions.TryGetValue(projectionName, out var position);
        return Task.FromResult(position);
    }

    /// <inheritdoc />
    public Task SavePositionAsync(string projectionName, long position, CancellationToken cancellationToken = default)
    {
        _positions[projectionName] = position;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all positions. Useful for testing.
    /// </summary>
    public void Clear()
    {
        _positions.Clear();
    }
}


