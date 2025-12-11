//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Represents a stream of events for a specific aggregate.
/// An event stream is an ordered, append-only sequence of events.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Event Stream Characteristics:</strong>
/// <list type="bullet">
/// <item>Each aggregate has exactly one event stream</item>
/// <item>Events are ordered by version (sequence number)</item>
/// <item>The stream is append-only - events cannot be modified or deleted</item>
/// <item>The stream version provides optimistic concurrency control</item>
/// </list>
/// </para>
/// </remarks>
public class EventStream
{
    private readonly List<IDomainEvent> _events = new();

    /// <summary>
    /// Gets the aggregate identifier for this stream.
    /// </summary>
    public Guid AggregateId { get; }

    /// <summary>
    /// Gets the type name of the aggregate.
    /// </summary>
    public string AggregateType { get; }

    /// <summary>
    /// Gets the current version of the stream.
    /// This is the sequence number of the last event.
    /// </summary>
    public long Version { get; private set; }

    /// <summary>
    /// Gets all events in this stream.
    /// </summary>
    public IReadOnlyList<IDomainEvent> Events => _events.AsReadOnly();

    /// <summary>
    /// Gets whether the stream has any events.
    /// </summary>
    public bool IsEmpty => _events.Count == 0;

    /// <summary>
    /// Gets the number of events in the stream.
    /// </summary>
    public int Count => _events.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStream"/> class.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    public EventStream(Guid aggregateId, string aggregateType)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("Aggregate type cannot be null or empty.", nameof(aggregateType));

        AggregateId = aggregateId;
        AggregateType = aggregateType;
        Version = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStream"/> class with existing events.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="events">The existing events.</param>
    /// <param name="version">The current version.</param>
    public EventStream(Guid aggregateId, string aggregateType, IEnumerable<IDomainEvent> events, long version)
        : this(aggregateId, aggregateType)
    {
        _events.AddRange(events);
        Version = version;
    }

    /// <summary>
    /// Appends events to the stream.
    /// </summary>
    /// <param name="events">The events to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when events is null.</exception>
    internal void Append(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            _events.Add(@event);
            Version++;
        }
    }

    /// <summary>
    /// Gets events after a specific version.
    /// </summary>
    /// <param name="afterVersion">The version after which to get events.</param>
    /// <returns>Events with version greater than the specified version.</returns>
    public IReadOnlyList<IDomainEvent> GetEventsAfterVersion(long afterVersion)
    {
        if (afterVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(afterVersion), "Version cannot be negative.");

        if (afterVersion >= Version)
            return Array.Empty<IDomainEvent>();

        return _events.Skip((int)afterVersion).ToList().AsReadOnly();
    }
}

/// <summary>
/// Represents metadata about an event stream.
/// </summary>
public class EventStreamInfo
{
    /// <summary>
    /// Gets or sets the aggregate identifier.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the aggregate type.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current version.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the first event.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last event.
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of events in the stream.
    /// </summary>
    public long EventCount { get; set; }
}

