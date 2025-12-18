//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Events;

/// <summary>
/// In-memory implementation of the application event outbox.
/// Useful for development, testing, and simple scenarios.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores events in memory,
/// so events will be lost if the application restarts.
/// For production use, implement a persistent outbox using a database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI for development
/// services.AddSingleton&lt;IApplicationEventOutbox, InMemoryApplicationEventOutbox&gt;();
/// </code>
/// </example>
public sealed class InMemoryApplicationEventOutbox : IApplicationEventOutbox
{
    private readonly ConcurrentDictionary<Guid, ApplicationEventOutboxEntry> _entries = new();
    private readonly ILogger<InMemoryApplicationEventOutbox>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxRetries;

    /// <summary>
    /// Creates a new instance of the InMemoryApplicationEventOutbox.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    /// <param name="maxRetries">Maximum retry attempts before moving to dead letter. Default is 3.</param>
    public InMemoryApplicationEventOutbox(
        ILogger<InMemoryApplicationEventOutbox>? logger = null,
        int maxRetries = 3)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent
    {
        var entry = new ApplicationEventOutboxEntry
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = ApplicationEventOutboxStatus.Pending,
            CorrelationId = @event.CorrelationId
        };

        _entries.TryAdd(entry.Id, entry);

        _logger?.LogDebug(
            "[ApplicationEventOutbox] Added entry {EntryId} of type {EventType}",
            entry.Id,
            entry.EventType);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddRangeAsync(IEnumerable<IApplicationEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            var eventType = @event.GetType();
            var entry = new ApplicationEventOutboxEntry
            {
                Id = Guid.NewGuid(),
                EventType = eventType.AssemblyQualifiedName ?? eventType.Name,
                Payload = JsonSerializer.Serialize(@event, eventType, _jsonOptions),
                CreatedAt = DateTime.UtcNow,
                Status = ApplicationEventOutboxStatus.Pending,
                CorrelationId = @event.CorrelationId
            };

            _entries.TryAdd(entry.Id, entry);
        }

        _logger?.LogDebug(
            "[ApplicationEventOutbox] Added {Count} entries",
            events.Count());

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplicationEventOutboxEntry>> GetPendingAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var pending = _entries.Values
            .Where(e => e.Status == ApplicationEventOutboxStatus.Pending || 
                       e.Status == ApplicationEventOutboxStatus.Failed)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToList();

        _logger?.LogDebug(
            "[ApplicationEventOutbox] Retrieved {Count} pending entries",
            pending.Count);

        return Task.FromResult<IReadOnlyList<ApplicationEventOutboxEntry>>(pending);
    }

    /// <inheritdoc />
    public Task MarkAsDispatchedAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(entryId, out var entry))
        {
            entry.Status = ApplicationEventOutboxStatus.Dispatched;
            entry.DispatchedAt = DateTime.UtcNow;

            _logger?.LogDebug(
                "[ApplicationEventOutbox] Marked entry {EntryId} as dispatched",
                entryId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsFailedAsync(Guid entryId, string error, CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(entryId, out var entry))
        {
            entry.RetryCount++;
            entry.Error = error;

            if (entry.RetryCount >= _maxRetries)
            {
                entry.Status = ApplicationEventOutboxStatus.DeadLetter;
                _logger?.LogWarning(
                    "[ApplicationEventOutbox] Entry {EntryId} moved to dead letter after {RetryCount} retries. Error: {Error}",
                    entryId,
                    entry.RetryCount,
                    error);
            }
            else
            {
                entry.Status = ApplicationEventOutboxStatus.Failed;
                _logger?.LogWarning(
                    "[ApplicationEventOutbox] Entry {EntryId} failed (attempt {RetryCount}). Error: {Error}",
                    entryId,
                    entry.RetryCount,
                    error);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var toRemove = _entries.Values
            .Where(e => e.DispatchedAt.HasValue && e.DispatchedAt.Value < olderThan)
            .Select(e => e.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _entries.TryRemove(id, out _);
        }

        _logger?.LogDebug(
            "[ApplicationEventOutbox] Cleaned up {Count} old entries",
            toRemove.Count);

        return Task.FromResult(toRemove.Count);
    }

    /// <summary>
    /// Gets all entries (for testing purposes).
    /// </summary>
    public IReadOnlyList<ApplicationEventOutboxEntry> GetAll()
    {
        return _entries.Values.ToList();
    }

    /// <summary>
    /// Gets entries by status (for testing purposes).
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    public IReadOnlyList<ApplicationEventOutboxEntry> GetByStatus(ApplicationEventOutboxStatus status)
    {
        return _entries.Values.Where(e => e.Status == status).ToList();
    }

    /// <summary>
    /// Clears all entries (for testing purposes).
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}

