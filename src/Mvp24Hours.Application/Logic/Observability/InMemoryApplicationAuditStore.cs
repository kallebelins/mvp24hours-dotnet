//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Concurrent;
using Mvp24Hours.Application.Contract.Observability;

namespace Mvp24Hours.Application.Logic.Observability;

/// <summary>
/// In-memory implementation of <see cref="IApplicationAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores entries in memory only.
/// Data will be lost when the application restarts.
/// Use a persistent implementation (EFCore, MongoDB, etc.) for production.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
/// <item>Thread-safe storage using ConcurrentDictionary</item>
/// <item>Automatic cleanup of old entries (configurable)</item>
/// <item>Query by correlation ID, user, and entity</item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new in-memory audit store.
/// </remarks>
/// <param name="maxEntries">Maximum number of entries to keep (default: 10000).</param>
/// <param name="retentionDays">Number of days to retain entries (default: 7).</param>
public sealed class InMemoryApplicationAuditStore(int maxEntries = 10000, int retentionDays = 7) : IApplicationAuditStore
{
    private readonly ConcurrentDictionary<string, ApplicationAuditEntry> _entries = new();
    private readonly int _maxEntries = maxEntries;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(retentionDays);

    /// <inheritdoc />
    public Task SaveAsync(ApplicationAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Clean up old entries if necessary
        if (_entries.Count >= _maxEntries)
        {
            CleanupOldEntries();
        }

        _entries.TryAdd(entry.Id, entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IList<ApplicationAuditEntry>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => e.CorrelationId == correlationId)
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IList<ApplicationAuditEntry>>(results);
    }

    /// <inheritdoc />
    public Task<IList<ApplicationAuditEntry>> GetByUserIdAsync(
        string userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => e.UserId == userId && e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IList<ApplicationAuditEntry>>(results);
    }

    /// <inheritdoc />
    public Task<IList<ApplicationAuditEntry>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => e.EntityType == entityType &&
                       (e.EntityIds?.Contains(entityId) ?? false))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IList<ApplicationAuditEntry>>(results);
    }

    /// <summary>
    /// Gets all entries (for debugging/testing).
    /// </summary>
    /// <returns>All audit entries.</returns>
    public IReadOnlyList<ApplicationAuditEntry> GetAll()
    {
        return [.. _entries.Values.OrderByDescending(e => e.Timestamp)];
    }

    /// <summary>
    /// Clears all entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Gets the current number of entries.
    /// </summary>
    public int Count => _entries.Count;

    private void CleanupOldEntries()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - _retentionPeriod;
        var oldEntries = _entries.Where(e => e.Value.Timestamp < cutoff).ToList();

        foreach (KeyValuePair<string, ApplicationAuditEntry> entry in oldEntries)
        {
            _entries.TryRemove(entry.Key, out _);
        }

        // If still over limit, remove oldest entries
        if (_entries.Count >= _maxEntries)
        {
            var toRemove = _entries
                .OrderBy(e => e.Value.Timestamp)
                .Take(_entries.Count - _maxEntries + 1000)
                .ToList();

            foreach (KeyValuePair<string, ApplicationAuditEntry> entry in toRemove)
            {
                _entries.TryRemove(entry.Key, out _);
            }
        }
    }
}

