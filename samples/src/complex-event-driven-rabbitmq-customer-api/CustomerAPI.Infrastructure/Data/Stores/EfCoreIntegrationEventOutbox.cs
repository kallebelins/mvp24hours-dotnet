using CustomerAPI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Text.Json;

namespace CustomerAPI.Infrastructure.Data.Stores;

/// <summary>
/// EF Core-backed implementation of <see cref="IIntegrationEventOutbox"/>.
///
/// Registered as <b>Scoped</b> so it shares the same <see cref="EFDBContext"/> instance
/// as the command handler's unit-of-work. This allows <see cref="AddAsync{TEvent}"/>
/// to stage an <see cref="OutboxEntry"/> row in the current change tracker without
/// calling SaveChanges — the caller's <c>unitOfWork.SaveChangesAsync()</c> commits
/// both the domain row and the outbox row atomically.
///
/// The <see cref="Mvp24Hours.Infrastructure.Cqrs.Messaging.OutboxProcessor"/> background service
/// creates its own DI scope, so it gets a fresh Scoped instance (fresh DbContext) and can
/// independently read, mark-as-published, or mark-as-failed.
///
/// <para>
/// <b>Library gap note:</b> The library's <see cref="Mvp24Hours.Infrastructure.Cqrs.Extensions.InboxOutboxExtensions"/>
/// registers stores as Singleton (<c>TryAddSingleton</c> / <c>AddSingleton</c>) which conflicts with
/// Scoped EF Core DbContext. Therefore this project registers the stores directly as Scoped and
/// wires up the OutboxProcessor manually rather than via <c>AddMvpInboxOutbox()</c>.
/// </para>
/// </summary>
public sealed class EfCoreIntegrationEventOutbox(
    EFDBContext context,
    ILogger<EfCoreIntegrationEventOutbox>? logger = null) : IIntegrationEventOutbox
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    /// <remarks>
    /// Only stages the entry in the DbContext change tracker.
    /// The caller is responsible for calling <c>SaveChangesAsync</c>.
    /// </remarks>
    public Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), s_jsonOptions);

        var entry = new OutboxEntry
        {
            Id = @event.Id,
            EventType = @event.GetType().FullName ?? @event.GetType().Name,
            Payload = payload,
            Status = "Pending",
            RetryCount = 0,
            CorrelationId = @event.CorrelationId,
            CreatedAt = DateTime.UtcNow
        };

        context.OutboxEntries.Add(entry);

        logger?.LogDebug("[EfCoreOutbox] Staged outbox entry {Id} ({EventType})", entry.Id, entry.EventType);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var entries = await context.OutboxEntries
            .Where(e => e.Status == "Pending" || e.Status == "Failed")
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return entries.Select(e => new OutboxMessage
        {
            Id = e.Id,
            EventType = e.EventType,
            Payload = e.Payload,
            Status = e.Status == "Published" ? OutboxMessageStatus.Published
                   : e.Status == "Failed" ? OutboxMessageStatus.Failed
                   : e.Status == "DeadLetter" ? OutboxMessageStatus.DeadLetter
                   : OutboxMessageStatus.Pending,
            RetryCount = e.RetryCount,
            Error = e.Error,
            CorrelationId = e.CorrelationId,
            CreatedAt = e.CreatedAt,
            ProcessedAt = e.ProcessedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxEntries.FindAsync([messageId], cancellationToken);
        if (entry is null)
        {
            return;
        }

        entry.Status = "Published";
        entry.ProcessedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        logger?.LogDebug("[EfCoreOutbox] Marked {Id} as Published", messageId);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var entry = await context.OutboxEntries.FindAsync([messageId], cancellationToken);
        if (entry is null)
        {
            return;
        }

        entry.RetryCount++;
        entry.Error = error;
        entry.Status = "Failed";
        await context.SaveChangesAsync(cancellationToken);

        logger?.LogWarning("[EfCoreOutbox] Marked {Id} as Failed (retry {Count}): {Error}", messageId, entry.RetryCount, error);
    }

    /// <inheritdoc />
    public async Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var old = await context.OutboxEntries
            .Where(e => e.Status == "Published" && e.ProcessedAt < olderThan)
            .ToListAsync(cancellationToken);

        if (old.Count == 0)
        {
            return 0;
        }

        context.OutboxEntries.RemoveRange(old);
        await context.SaveChangesAsync(cancellationToken);
        return old.Count;
    }
}
