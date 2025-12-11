//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Represents a stored snapshot of an aggregate's state.
/// Snapshots are used to optimize aggregate reconstruction by storing
/// the state at a specific version, reducing the number of events to replay.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Snapshot Strategy:</strong>
/// <list type="bullet">
/// <item>Take snapshots after N events (e.g., every 100 events)</item>
/// <item>Take snapshots after significant state changes</item>
/// <item>Take snapshots on demand for high-traffic aggregates</item>
/// </list>
/// </para>
/// <para>
/// <strong>Reconstruction with Snapshots:</strong>
/// <code>
/// 1. Load latest snapshot (if exists)
/// 2. Restore aggregate from snapshot
/// 3. Load events after snapshot version
/// 4. Apply remaining events
/// </code>
/// </para>
/// </remarks>
public class Snapshot
{
    /// <summary>
    /// Gets or sets the unique identifier of this snapshot.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the aggregate identifier.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the aggregate type name.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version at which this snapshot was taken.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the serialized snapshot data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type name of the snapshot data.
    /// </summary>
    public string SnapshotType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this snapshot was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Interface for storing and retrieving snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot for an aggregate.
    /// </summary>
    /// <param name="snapshot">The snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest snapshot, or null if none exists.</returns>
    Task<Snapshot?> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a snapshot at or before a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="maxVersion">The maximum version to consider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot at or before the version, or null if none exists.</returns>
    Task<Snapshot?> GetSnapshotAtVersionAsync(Guid aggregateId, long maxVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all snapshots for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes snapshots older than a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="beforeVersion">Delete snapshots with version less than this.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSnapshotsBeforeVersionAsync(Guid aggregateId, long beforeVersion, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a strategy for when to take snapshots.
/// </summary>
public interface ISnapshotStrategy
{
    /// <summary>
    /// Determines if a snapshot should be taken for the aggregate.
    /// </summary>
    /// <typeparam name="TAggregate">The type of aggregate.</typeparam>
    /// <param name="aggregate">The aggregate to evaluate.</param>
    /// <param name="lastSnapshotVersion">The version of the last snapshot (0 if none).</param>
    /// <returns>True if a snapshot should be taken.</returns>
    bool ShouldTakeSnapshot<TAggregate>(TAggregate aggregate, long lastSnapshotVersion)
        where TAggregate : IAggregate;
}

/// <summary>
/// Snapshot strategy based on event count threshold.
/// Takes a snapshot every N events since the last snapshot.
/// </summary>
public class EventCountSnapshotStrategy : ISnapshotStrategy
{
    private readonly int _threshold;

    /// <summary>
    /// Initializes a new instance with default threshold of 100 events.
    /// </summary>
    public EventCountSnapshotStrategy() : this(100) { }

    /// <summary>
    /// Initializes a new instance with specified threshold.
    /// </summary>
    /// <param name="threshold">Number of events between snapshots.</param>
    public EventCountSnapshotStrategy(int threshold)
    {
        if (threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be positive.");

        _threshold = threshold;
    }

    /// <inheritdoc />
    public bool ShouldTakeSnapshot<TAggregate>(TAggregate aggregate, long lastSnapshotVersion)
        where TAggregate : IAggregate
    {
        return aggregate.Version - lastSnapshotVersion >= _threshold;
    }
}

/// <summary>
/// Composite snapshot strategy that combines multiple strategies with OR logic.
/// </summary>
public class CompositeSnapshotStrategy : ISnapshotStrategy
{
    private readonly IEnumerable<ISnapshotStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance with multiple strategies.
    /// </summary>
    /// <param name="strategies">The strategies to combine.</param>
    public CompositeSnapshotStrategy(IEnumerable<ISnapshotStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    /// <inheritdoc />
    public bool ShouldTakeSnapshot<TAggregate>(TAggregate aggregate, long lastSnapshotVersion)
        where TAggregate : IAggregate
    {
        return _strategies.Any(s => s.ShouldTakeSnapshot(aggregate, lastSnapshotVersion));
    }
}

/// <summary>
/// Strategy that never takes snapshots. Useful for small aggregates.
/// </summary>
public class NeverSnapshotStrategy : ISnapshotStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NeverSnapshotStrategy Instance = new();

    private NeverSnapshotStrategy() { }

    /// <inheritdoc />
    public bool ShouldTakeSnapshot<TAggregate>(TAggregate aggregate, long lastSnapshotVersion)
        where TAggregate : IAggregate => false;
}

/// <summary>
/// Strategy that always takes snapshots after every change.
/// Useful for critical aggregates requiring fast reconstruction.
/// </summary>
public class AlwaysSnapshotStrategy : ISnapshotStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly AlwaysSnapshotStrategy Instance = new();

    private AlwaysSnapshotStrategy() { }

    /// <inheritdoc />
    public bool ShouldTakeSnapshot<TAggregate>(TAggregate aggregate, long lastSnapshotVersion)
        where TAggregate : IAggregate => aggregate.Version > lastSnapshotVersion;
}

