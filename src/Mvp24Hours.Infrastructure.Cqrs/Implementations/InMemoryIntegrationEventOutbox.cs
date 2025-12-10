//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Implementations;

/// <summary>
/// In-memory implementation of the Integration Event Outbox.
/// Useful for development, testing, and simple scenarios.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores messages in memory,
/// so messages will be lost if the application restarts.
/// For production use, implement a persistent outbox using a database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI for development
/// services.AddSingleton&lt;IIntegrationEventOutbox, InMemoryIntegrationEventOutbox&gt;();
/// </code>
/// </example>
public sealed class InMemoryIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();
    private readonly ILogger<InMemoryIntegrationEventOutbox>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the InMemoryIntegrationEventOutbox.
    /// </summary>
    /// <param name="logger">Optional logger for recording operations.</param>
    public InMemoryIntegrationEventOutbox(ILogger<InMemoryIntegrationEventOutbox>? logger = null)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending,
            CorrelationId = @event.CorrelationId
        };

        _messages.TryAdd(message.Id, message);

        _logger?.LogDebug(
            "[Outbox] Added message {MessageId} of type {EventType}",
            message.Id,
            message.EventType);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var pending = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();

        _logger?.LogDebug("[Outbox] Retrieved {Count} pending messages", pending.Count);

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(pending);
    }

    /// <inheritdoc />
    public Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Published;
            message.ProcessedAt = DateTime.UtcNow;

            _logger?.LogDebug("[Outbox] Marked message {MessageId} as published", messageId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.RetryCount++;
            message.Error = error;

            // Move to dead letter if max retries exceeded
            if (message.RetryCount >= 3)
            {
                message.Status = OutboxMessageStatus.DeadLetter;
                _logger?.LogWarning(
                    "[Outbox] Message {MessageId} moved to dead letter after {RetryCount} retries. Error: {Error}",
                    messageId,
                    message.RetryCount,
                    error);
            }
            else
            {
                message.Status = OutboxMessageStatus.Failed;
                _logger?.LogWarning(
                    "[Outbox] Message {MessageId} failed (attempt {RetryCount}). Error: {Error}",
                    messageId,
                    message.RetryCount,
                    error);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var toRemove = _messages.Values
            .Where(m => m.ProcessedAt.HasValue && m.ProcessedAt.Value < olderThan)
            .Select(m => m.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _messages.TryRemove(id, out _);
        }

        _logger?.LogDebug("[Outbox] Cleaned up {Count} old messages", toRemove.Count);

        return Task.FromResult(toRemove.Count);
    }
}

