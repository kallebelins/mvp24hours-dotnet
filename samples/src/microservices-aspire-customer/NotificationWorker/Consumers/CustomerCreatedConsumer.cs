using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using NotificationWorker.Events;
using NotificationWorker.Services;
using System.Text.Json;

namespace NotificationWorker.Consumers;

/// <summary>
/// Consumes <see cref="CustomerCreatedEvent"/> messages from RabbitMQ.
/// The routing key and queue name must match what CustomerAPI publishes with.
/// </summary>
public class CustomerCreatedConsumer(INotificationService notificationService, ILogger<CustomerCreatedConsumer> logger)
    : IMvpRabbitMQConsumerAsync
{
    /// <summary>
    /// Must match <c>nameof(CustomerCreatedEvent)</c> used in CustomerAPI.
    /// </summary>
    public string RoutingKey => nameof(CustomerCreatedEvent);

    public string QueueName => nameof(CustomerCreatedEvent);

    public async Task ReceivedAsync(object message, string token)
    {
        if (message is not CustomerCreatedEvent evt)
        {
            logger.LogWarning(
                "Received unexpected message type: {Type}", message?.GetType().Name ?? "null");
            return;
        }

        logger.LogInformation(
            "Received CustomerCreatedEvent for customer {CustomerId} ({Name})",
            evt.CustomerId, evt.Name);

        var payload = JsonSerializer.Serialize(evt);
        await notificationService.LogAsync(nameof(CustomerCreatedEvent), payload);
    }
}
