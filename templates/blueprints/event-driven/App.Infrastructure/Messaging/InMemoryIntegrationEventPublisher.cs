using App.Application.Integration;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Messaging;

/// <summary>
/// In-memory integration event publisher for local development and tests.
/// Replace with RabbitMQ + outbox for production (see sample below).
/// </summary>
public sealed class InMemoryIntegrationEventPublisher(ILogger<InMemoryIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        logger.LogInformation(
            "[IntegrationEvent] {EventType} EventId={EventId} Payload={@Payload}",
            typeof(TEvent).Name,
            @event.EventId,
            @event);

        return Task.CompletedTask;
    }
}
