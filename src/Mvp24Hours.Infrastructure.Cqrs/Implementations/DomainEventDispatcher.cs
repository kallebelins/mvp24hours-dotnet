//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Implementations;

/// <summary>
/// Default implementation of <see cref="IDomainEventDispatcher"/>.
/// Dispatches domain events through the Mediator's <see cref="IPublisher"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This dispatcher publishes each domain event through the Mediator, allowing
/// multiple handlers to process the same event.
/// </para>
/// <para>
/// <strong>Event Processing Order:</strong>
/// Events are dispatched in the order they were raised by the entity.
/// All events from one entity are dispatched before moving to the next entity.
/// </para>
/// <para>
/// <strong>Error Handling:</strong>
/// If an event handler throws an exception, subsequent events are not dispatched.
/// Consider using a handler that catches exceptions if you need fire-and-forget behavior.
/// </para>
/// </remarks>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher>? _logger;

    /// <summary>
    /// Creates a new instance of the DomainEventDispatcher.
    /// </summary>
    /// <param name="publisher">The publisher for dispatching events.</param>
    /// <param name="logger">Optional logger for recording dispatch operations.</param>
    public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher>? logger = null)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var events = entity.DomainEvents.ToList();

        if (events.Count == 0)
        {
            return;
        }

        _logger?.LogDebug(
            "[DomainEvents] Dispatching {EventCount} event(s) from {EntityType}",
            events.Count,
            entity.GetType().Name);

        foreach (var domainEvent in events)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        entity.ClearDomainEvents();

        _logger?.LogDebug(
            "[DomainEvents] Successfully dispatched {EventCount} event(s) from {EntityType}",
            events.Count,
            entity.GetType().Name);
    }

    /// <inheritdoc />
    public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entitiesWithEvents = entities
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        if (entitiesWithEvents.Count == 0)
        {
            return;
        }

        var totalEvents = entitiesWithEvents.Sum(e => e.DomainEvents.Count);

        _logger?.LogDebug(
            "[DomainEvents] Dispatching {EventCount} event(s) from {EntityCount} entities",
            totalEvents,
            entitiesWithEvents.Count);

        foreach (var entity in entitiesWithEvents)
        {
            await DispatchEventsAsync(entity, cancellationToken);
        }
    }

    private async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        _logger?.LogInformation(
            "[DomainEvents] Publishing {EventType} (OccurredAt: {OccurredAt})",
            eventType.Name,
            domainEvent.OccurredAt);

        try
        {
            // Use reflection to call PublishAsync with the concrete event type
            var publishMethod = typeof(IPublisher)
                .GetMethod(nameof(IPublisher.PublishAsync))!
                .MakeGenericMethod(eventType);

            var task = (Task)publishMethod.Invoke(_publisher, new object[] { domainEvent, cancellationToken })!;
            await task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[DomainEvents] Failed to dispatch {EventType}: {Message}",
                eventType.Name,
                ex.Message);

            throw;
        }
    }
}

