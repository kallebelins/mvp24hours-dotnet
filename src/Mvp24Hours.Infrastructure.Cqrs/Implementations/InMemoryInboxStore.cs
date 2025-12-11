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
/// In-memory implementation of the Inbox Store.
/// Useful for development, testing, and simple scenarios.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores messages in memory,
/// so messages will be lost if the application restarts.
/// For production use, implement a persistent inbox using a database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI for development
/// services.AddSingleton&lt;IInboxStore, InMemoryInboxStore&gt;();
/// </code>
/// </example>
public sealed class InMemoryInboxStore : IInboxStore
{
    private readonly ConcurrentDictionary<Guid, InboxMessage> _messages = new();
    private readonly ILogger<InMemoryInboxStore>? _logger;

    /// <summary>
    /// Creates a new instance of the InMemoryInboxStore.
    /// </summary>
    /// <param name="logger">Optional logger for recording operations.</param>
    public InMemoryInboxStore(ILogger<InMemoryInboxStore>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var exists = _messages.ContainsKey(messageId);
        
        if (exists)
        {
            _logger?.LogDebug("[Inbox] Message {MessageId} already processed (duplicate)", messageId);
        }
        
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default)
    {
        var message = new InboxMessage
        {
            Id = messageId,
            MessageType = messageType,
            ProcessedAt = DateTime.UtcNow
        };

        if (_messages.TryAdd(messageId, message))
        {
            _logger?.LogDebug(
                "[Inbox] Marked message {MessageId} of type {MessageType} as processed",
                messageId,
                messageType);
        }
        else
        {
            _logger?.LogDebug(
                "[Inbox] Message {MessageId} was already marked as processed",
                messageId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<InboxMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<InboxMessage>> GetByTimeRangeAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values
            .Where(m => m.ProcessedAt >= from && m.ProcessedAt <= to)
            .OrderBy(m => m.ProcessedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<InboxMessage>>(messages);
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var toRemove = _messages.Values
            .Where(m => m.ProcessedAt < olderThan)
            .Select(m => m.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _messages.TryRemove(id, out _);
        }

        _logger?.LogDebug("[Inbox] Cleaned up {Count} old messages", toRemove.Count);

        return Task.FromResult(toRemove.Count);
    }

    /// <summary>
    /// Gets the total count of messages in the inbox.
    /// </summary>
    /// <returns>The number of messages stored.</returns>
    public int GetCount() => _messages.Count;
}


