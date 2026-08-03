namespace App.Application.Integration;

/// <summary>
/// Publishes integration events. Replace with RabbitMQ/outbox in production.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
