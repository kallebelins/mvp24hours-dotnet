using CustomerAPI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Infrastructure.Data.Stores;

/// <summary>
/// EF Core-backed implementation of <see cref="IInboxStore"/>.
///
/// Registered as <b>Scoped</b>. The RabbitMQ consumer creates its own DI scope when it processes
/// a message, so each delivery gets a fresh <see cref="EFDBContext"/> and the inbox check is
/// always a real DB query (no in-process cache).
///
/// Idempotency guarantee: if a message with the same <c>MessageId</c> was already processed,
/// <see cref="ExistsAsync"/> returns <c>true</c> and the consumer skips processing.
/// </summary>
public sealed class EfCoreInboxStore(
    EFDBContext context,
    ILogger<EfCoreInboxStore>? logger = null) : IInboxStore
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await context.InboxEntries.AnyAsync(e => e.MessageId == messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, string messageType, CancellationToken cancellationToken = default)
    {
        if (await ExistsAsync(messageId, cancellationToken))
        {
            return;
        }

        context.InboxEntries.Add(new InboxEntry
        {
            MessageId = messageId,
            MessageType = messageType,
            ProcessedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);

        logger?.LogDebug("[EfCoreInbox] Marked message {MessageId} ({MessageType}) as processed", messageId, messageType);
    }

    /// <inheritdoc />
    public async Task<InboxMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await context.InboxEntries.FindAsync([messageId], cancellationToken);
        if (entry is null)
        {
            return null;
        }

        return new InboxMessage
        {
            Id = entry.MessageId,
            MessageType = entry.MessageType,
            ProcessedAt = entry.ProcessedAt
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InboxMessage>> GetByTimeRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var entries = await context.InboxEntries
            .Where(e => e.ProcessedAt >= from && e.ProcessedAt <= to)
            .OrderBy(e => e.ProcessedAt)
            .ToListAsync(cancellationToken);

        return entries.Select(e => new InboxMessage
        {
            Id = e.MessageId,
            MessageType = e.MessageType,
            ProcessedAt = e.ProcessedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var old = await context.InboxEntries
            .Where(e => e.ProcessedAt < olderThan)
            .ToListAsync(cancellationToken);

        if (old.Count == 0)
        {
            return 0;
        }

        context.InboxEntries.RemoveRange(old);
        await context.SaveChangesAsync(cancellationToken);
        return old.Count;
    }
}
