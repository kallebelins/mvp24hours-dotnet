namespace App.Application.Integration;

/// <summary>
/// Marker for integration events published across bounded contexts.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
