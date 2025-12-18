//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;

namespace Mvp24Hours.Application.Contract.Events;

/// <summary>
/// Base interface for application events.
/// Application events are dispatched after successful command operations (Created, Updated, Deleted).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Application Events vs Domain Events:</strong>
/// <list type="bullet">
/// <item>Domain Events - Represent something that happened in the domain (business logic)</item>
/// <item>Application Events - Represent successful completion of application layer operations (CRUD)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Common Use Cases:</strong>
/// <list type="bullet">
/// <item>EntityCreated - Notify when a new entity is created</item>
/// <item>EntityUpdated - Notify when an entity is modified</item>
/// <item>EntityDeleted - Notify when an entity is removed</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a custom application event
/// public record OrderProcessedEvent : IApplicationEvent
/// {
///     public Guid EventId { get; init; } = Guid.NewGuid();
///     public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
///     public string? CorrelationId { get; init; }
///     public int OrderId { get; init; }
///     public string Status { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
public interface IApplicationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Optional correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Base record for application events with common properties.
/// Provides a convenient base class with automatic timestamp and event ID.
/// </summary>
/// <example>
/// <code>
/// public record CustomerCreatedEvent : ApplicationEventBase
/// {
///     public int CustomerId { get; init; }
///     public string Name { get; init; } = string.Empty;
/// }
/// </code>
/// </example>
public abstract record ApplicationEventBase : IApplicationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Generic application event for entity operations.
/// </summary>
/// <typeparam name="TEntity">The type of entity affected.</typeparam>
public interface IEntityApplicationEvent<out TEntity> : IApplicationEvent
    where TEntity : class
{
    /// <summary>
    /// The entity affected by this event.
    /// </summary>
    TEntity Entity { get; }

    /// <summary>
    /// The type of operation that occurred.
    /// </summary>
    EntityOperationType OperationType { get; }
}

/// <summary>
/// Types of entity operations that can trigger application events.
/// </summary>
public enum EntityOperationType
{
    /// <summary>
    /// A new entity was created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// An existing entity was updated.
    /// </summary>
    Updated = 2,

    /// <summary>
    /// An entity was deleted.
    /// </summary>
    Deleted = 3
}

