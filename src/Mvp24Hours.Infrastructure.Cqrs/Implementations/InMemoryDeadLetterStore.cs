//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Concurrent;

namespace Mvp24Hours.Infrastructure.Cqrs.Implementations;

/// <summary>
/// In-memory implementation of the Dead Letter Store.
/// Useful for development, testing, and simple scenarios.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores messages in memory,
/// so messages will be lost if the application restarts.
/// For production use, implement a persistent dead letter store using a database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI for development
/// services.AddSingleton&lt;IDeadLetterStore, InMemoryDeadLetterStore&gt;();
/// </code>
/// </example>
public sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentDictionary<Guid, DeadLetterMessage> _messages = new();
    private readonly IIntegrationEventOutbox? _outbox;
    private readonly ILogger<InMemoryDeadLetterStore>? _logger;

    /// <summary>
    /// Creates a new instance of the InMemoryDeadLetterStore.
    /// </summary>
    /// <param name="outbox">Optional outbox for requeuing messages.</param>
    /// <param name="logger">Optional logger for recording operations.</param>
    public InMemoryDeadLetterStore(
        IIntegrationEventOutbox? outbox = null,
        ILogger<InMemoryDeadLetterStore>? logger = null)
    {
        _outbox = outbox;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        if (_messages.TryAdd(message.Id, message))
        {
            _logger?.LogWarning(
                "[DLQ] Added message {MessageId} of type {EventType} to dead letter queue. Error: {Error}",
                message.OriginalMessageId,
                message.EventType,
                message.Error);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values
            .Where(m => m.Status == DeadLetterStatus.Pending)
            .OrderByDescending(m => m.FailedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterMessage>>(messages);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterMessage>> GetByEventTypeAsync(
        string eventType, 
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values
            .Where(m => m.EventType == eventType && m.Status == DeadLetterStatus.Pending)
            .OrderByDescending(m => m.FailedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterMessage>>(messages);
    }

    /// <inheritdoc />
    public Task<DeadLetterMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(id, out var message);
        return Task.FromResult(message);
    }

    /// <inheritdoc />
    public Task<bool> RequeueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(id, out var message))
        {
            _logger?.LogWarning("[DLQ] Message {MessageId} not found for requeue", id);
            return Task.FromResult(false);
        }

        // Note: In a real implementation, you would deserialize the payload
        // and add it back to the outbox for reprocessing.
        // For the in-memory implementation, we just mark it as requeued.
        message.Status = DeadLetterStatus.Requeued;

        _logger?.LogInformation(
            "[DLQ] Message {MessageId} of type {EventType} requeued for processing",
            message.OriginalMessageId,
            message.EventType);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task MarkAsResolvedAsync(Guid id, string resolution, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(id, out var message))
        {
            message.Status = DeadLetterStatus.Resolved;
            message.Resolution = resolution;
            message.ResolvedAt = DateTime.UtcNow;

            _logger?.LogInformation(
                "[DLQ] Message {MessageId} marked as resolved: {Resolution}",
                message.OriginalMessageId,
                resolution);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _messages.TryRemove(id, out var message);
        
        if (removed)
        {
            _logger?.LogDebug("[DLQ] Deleted message {MessageId}", id);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _messages.Values.Count(m => m.Status == DeadLetterStatus.Pending);
        return Task.FromResult(count);
    }
}


