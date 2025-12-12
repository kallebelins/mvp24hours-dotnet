//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Interface for event store - the persistence layer for event sourcing.
/// The event store is responsible for appending and retrieving domain events
/// in an ordered, immutable sequence per aggregate.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Event Sourcing Pattern:</strong>
/// Instead of storing the current state of an aggregate, we store all the events
/// that have occurred. The current state is derived by replaying these events.
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item>Events are immutable and append-only</item>
/// <item>Each aggregate has its own event stream identified by aggregate ID</item>
/// <item>Events are versioned for optimistic concurrency control</item>
/// <item>The event store provides a complete audit trail</item>
/// </list>
/// </para>
/// <para>
/// <strong>Concurrency Control:</strong>
/// The expectedVersion parameter enables optimistic locking. If the current version
/// in the store doesn't match the expected version, a <see cref="ConcurrencyException"/>
/// should be thrown.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Appending events
/// var events = new[] { new OrderCreatedEvent { OrderId = aggregateId } };
/// await eventStore.AppendEventsAsync(aggregateId, events, expectedVersion: 0);
/// 
/// // Loading events to reconstruct aggregate
/// var events = await eventStore.GetEventsAsync(aggregateId);
/// aggregate.LoadFromHistory(events);
/// </code>
/// </example>
public interface IEventStore
{
    /// <summary>
    /// Appends a sequence of events to the event stream for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="expectedVersion">
    /// The expected current version of the aggregate stream.
    /// Use 0 for new aggregates. If the actual version differs, a
    /// <see cref="ConcurrencyException"/> should be thrown.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ConcurrencyException">
    /// Thrown when the expected version doesn't match the actual version in the store.
    /// </exception>
    /// <example>
    /// <code>
    /// // Create new aggregate stream
    /// await eventStore.AppendEventsAsync(
    ///     orderId,
    ///     new[] { new OrderCreatedEvent(...) },
    ///     expectedVersion: 0);
    /// 
    /// // Append to existing stream
    /// var currentVersion = await eventStore.GetCurrentVersionAsync(orderId);
    /// await eventStore.AppendEventsAsync(
    ///     orderId,
    ///     new[] { new OrderShippedEvent(...) },
    ///     expectedVersion: currentVersion);
    /// </code>
    /// </example>
    Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<CoreDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events for an aggregate starting from a specific version.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="fromVersion">
    /// The version to start reading from (exclusive).
    /// Use 0 to get all events. Default is 0.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An ordered, read-only list of events starting after the specified version.
    /// Returns an empty list if no events exist for the aggregate.
    /// </returns>
    /// <example>
    /// <code>
    /// // Get all events
    /// var allEvents = await eventStore.GetEventsAsync(orderId);
    /// 
    /// // Get events after version 5 (for snapshot optimization)
    /// var eventsAfterSnapshot = await eventStore.GetEventsAsync(orderId, fromVersion: 5);
    /// </code>
    /// </example>
    Task<IReadOnlyList<CoreDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        long fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version (number of events) for an aggregate stream.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The current version (highest event sequence number).
    /// Returns 0 if no events exist for the aggregate.
    /// </returns>
    /// <example>
    /// <code>
    /// var version = await eventStore.GetCurrentVersionAsync(orderId);
    /// if (version == 0)
    /// {
    ///     Console.WriteLine("Aggregate not found");
    /// }
    /// </code>
    /// </example>
    Task<long> GetCurrentVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate stream exists.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the aggregate has at least one event; otherwise, false.</returns>
    Task<bool> ExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended event store interface with support for stream subscriptions.
/// </summary>
/// <remarks>
/// Stream subscriptions allow real-time processing of events as they are appended
/// to the event store. This is useful for projections, integrations, and reactive systems.
/// </remarks>
public interface IEventStoreWithSubscription : IEventStore
{
/// <summary>
/// Subscribes to all events from a specific position.
/// </summary>
/// <param name="fromPosition">
/// The global position to start reading from.
/// Use 0 to read from the beginning.
/// </param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>An async enumerable of stored events.</returns>
IAsyncEnumerable<StoredEvent> SubscribeFromPositionAsync(
        long fromPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to events for a specific aggregate type.
    /// </summary>
    /// <param name="aggregateType">The type of aggregate to filter by.</param>
    /// <param name="fromPosition">The global position to start from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of stored events.</returns>
    IAsyncEnumerable<StoredEvent> SubscribeByAggregateTypeAsync(
        string aggregateType,
        long fromPosition,
        CancellationToken cancellationToken = default);
}

