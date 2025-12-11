//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Represents a persisted event in the event store.
/// Contains the event data along with metadata for identification, ordering, and tracing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Schema Design:</strong>
/// This entity is designed to be stored in a relational database with the following indexes:
/// <list type="bullet">
/// <item>Primary key on <see cref="Id"/></item>
/// <item>Unique constraint on (<see cref="AggregateId"/>, <see cref="Version"/>)</item>
/// <item>Index on <see cref="AggregateId"/> for stream queries</item>
/// <item>Index on <see cref="GlobalPosition"/> for subscriptions</item>
/// </list>
/// </para>
/// </remarks>
public class StoredEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this stored event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the global position in the event store.
    /// This is used for subscriptions and ordering across all streams.
    /// </summary>
    public long GlobalPosition { get; set; }

    /// <summary>
    /// Gets or sets the aggregate identifier that this event belongs to.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the aggregate.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version (sequence number) within the aggregate stream.
    /// Starts at 1 for the first event.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the event.
    /// Used for deserialization.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event data (typically JSON).
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional metadata (correlation ID, causation ID, user info, etc.).
    /// Stored as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was stored.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID (the ID of the event that caused this event).
    /// </summary>
    public string? CausationId { get; set; }
}

/// <summary>
/// Metadata associated with a stored event.
/// </summary>
public class EventMetadata
{
    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who triggered the event.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional custom properties.
    /// </summary>
    public Dictionary<string, object?> AdditionalProperties { get; set; } = new();
}

