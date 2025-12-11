//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// In-memory implementation of <see cref="ISnapshotStore"/>.
/// Useful for testing and development environments.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores snapshots in memory
/// and will lose all data when the application restarts.
/// For production, use a persistent implementation (SQL, MongoDB, etc.).
/// </para>
/// </remarks>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<Guid, List<Snapshot>> _snapshots = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task SaveSnapshotAsync(Snapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_lock)
        {
            var snapshots = _snapshots.GetOrAdd(snapshot.AggregateId, _ => new List<Snapshot>());
            snapshots.Add(new Snapshot
            {
                Id = snapshot.Id == Guid.Empty ? Guid.NewGuid() : snapshot.Id,
                AggregateId = snapshot.AggregateId,
                AggregateType = snapshot.AggregateType,
                Version = snapshot.Version,
                Data = snapshot.Data,
                SnapshotType = snapshot.SnapshotType,
                Timestamp = snapshot.Timestamp == default ? DateTime.UtcNow : snapshot.Timestamp,
                Metadata = snapshot.Metadata
            });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Snapshot?> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        if (_snapshots.TryGetValue(aggregateId, out var snapshots))
        {
            lock (_lock)
            {
                var latest = snapshots.OrderByDescending(s => s.Version).FirstOrDefault();
                return Task.FromResult(latest);
            }
        }

        return Task.FromResult<Snapshot?>(null);
    }

    /// <inheritdoc />
    public Task<Snapshot?> GetSnapshotAtVersionAsync(Guid aggregateId, long maxVersion, CancellationToken cancellationToken = default)
    {
        if (_snapshots.TryGetValue(aggregateId, out var snapshots))
        {
            lock (_lock)
            {
                var snapshot = snapshots
                    .Where(s => s.Version <= maxVersion)
                    .OrderByDescending(s => s.Version)
                    .FirstOrDefault();
                return Task.FromResult(snapshot);
            }
        }

        return Task.FromResult<Snapshot?>(null);
    }

    /// <inheritdoc />
    public Task DeleteSnapshotsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        _snapshots.TryRemove(aggregateId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteSnapshotsBeforeVersionAsync(Guid aggregateId, long beforeVersion, CancellationToken cancellationToken = default)
    {
        if (_snapshots.TryGetValue(aggregateId, out var snapshots))
        {
            lock (_lock)
            {
                snapshots.RemoveAll(s => s.Version < beforeVersion);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all snapshots. Useful for testing.
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
    }

    /// <summary>
    /// Gets the count of all stored snapshots.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _snapshots.Values.Sum(l => l.Count);
            }
        }
    }
}

