using App.Application.Integration;

namespace App.Application.Events;

/// <summary>
/// Integration event published after an item is persisted.
/// </summary>
public sealed record ItemCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public int ItemId { get; init; }
    public required string ItemName { get; init; }
}
