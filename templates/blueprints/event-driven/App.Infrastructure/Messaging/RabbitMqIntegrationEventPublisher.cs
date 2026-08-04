using App.Application.Integration;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace App.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ-backed integration event publisher for production-like execution.
/// </summary>
public sealed class RabbitMqIntegrationEventPublisher(
    IMvpRabbitMQClient rabbitMqClient,
    ILogger<RabbitMqIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var routingKey = typeof(TEvent).Name;

        logger.LogInformation(
            "[IntegrationEvent] Publishing to RabbitMQ EventType={EventType} EventId={EventId}",
            routingKey,
            @event.EventId);

        await rabbitMqClient.PublishAsync(@event, routingKey, @event.EventId.ToString(), cancellationToken);
    }
}