using CustomerAPI.Application.Events;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Text.Json;

namespace CustomerAPI.Infrastructure.Messaging.Consumers;

/// <summary>
/// RabbitMQ consumer for <see cref="CustomerCreatedIntegrationEvent"/>.
///
/// Flow:
///   1. Deserialize the wrapper published by <see cref="Mvp24Hours.Infrastructure.Cqrs.Implementations.RabbitMqIntegrationEventPublisher"/>
///   2. Use <see cref="IInboxProcessor"/> to check idempotency (EF-backed inbox table)
///   3. Inside the handler: write <see cref="NotificationLog"/> to DB
///
/// Dead-letter: if <see cref="ReceivedAsync"/> throws, the RabbitMQ client's
/// MaxRedeliveredCount setting governs redelivery; after exhaustion the message is
/// routed to the dead-letter exchange (configured in ServiceBuilderExtensions).
/// </summary>
public sealed class CustomerCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<CustomerCreatedConsumer> logger) : IMvpRabbitMQConsumerAsync
{
    // Must match the RoutingKey used by the publisher.
    // RabbitMqIntegrationEventPublisher publishes with CorrelationId as routing key via IMvpRabbitMQClient.Publish.
    // The wrapper type name "IntegrationEventWrapper" is used as the routing/queue key.
    public string RoutingKey => "IntegrationEventWrapper";
    public string QueueName => "IntegrationEventWrapper";

    public async Task ReceivedAsync(object message, string token)
    {
        if (message is null)
        {
            logger.LogWarning("[Consumer] Received null message, skipping.");
            return;
        }

        // The RabbitMqIntegrationEventPublisher wraps the event in IntegrationEventWrapper
        // which is serialized as JSON and received here as an object (already deserialized).
        // We need to re-serialize and then deserialize as the wrapper to extract the payload.
        string rawJson = JsonSerializer.Serialize(message);

        IntegrationEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Consumer] Failed to deserialize message envelope.");
            return;
        }

        if (envelope is null || string.IsNullOrEmpty(envelope.Payload))
        {
            logger.LogWarning("[Consumer] Envelope is null or has empty payload, skipping.");
            return;
        }

        if (!envelope.EventType?.Contains(nameof(CustomerCreatedIntegrationEvent), StringComparison.OrdinalIgnoreCase) ?? true)
        {
            logger.LogDebug("[Consumer] Event type {EventType} is not CustomerCreatedIntegrationEvent, skipping.", envelope.EventType);
            return;
        }

        CustomerCreatedIntegrationEvent? integrationEvent;
        try
        {
            integrationEvent = JsonSerializer.Deserialize<CustomerCreatedIntegrationEvent>(
                envelope.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Consumer] Failed to deserialize CustomerCreatedIntegrationEvent payload.");
            throw;
        }

        if (integrationEvent is null)
        {
            logger.LogWarning("[Consumer] CustomerCreatedIntegrationEvent deserialized as null.");
            return;
        }

        logger.LogInformation("[Consumer] Received CustomerCreatedIntegrationEvent {EventId} for customer {CustomerId} (CorrelationId: {CorrelationId})",
            integrationEvent.Id, integrationEvent.CustomerId, integrationEvent.CorrelationId);

        // Each message delivery gets its own DI scope for proper Scoped service resolution
        using var scope = scopeFactory.CreateScope();
        var inboxProcessor = scope.ServiceProvider.GetRequiredService<IInboxProcessor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<EFDBContext>();

        var processed = await inboxProcessor.ProcessAsync(
            integrationEvent,
            handler: async (evt, ct) =>
            {
                // Side effect: write audit/notification log
                var log = new NotificationLog
                {
                    EventType = evt.EventType,
                    CorrelationId = evt.CorrelationId,
                    CausationId = evt.CausationId,
                    Payload = envelope.Payload,
                    ProcessedAt = DateTime.UtcNow,
                    Notes = $"CustomerCreated: Id={evt.CustomerId}, Name={evt.CustomerName}"
                };

                dbContext.NotificationLogs.Add(log);
                await dbContext.SaveChangesAsync(ct);

                logger.LogInformation("[Consumer] Written NotificationLog for customer {CustomerId}", evt.CustomerId);
            });

        if (!processed)
        {
            logger.LogInformation("[Consumer] Message {EventId} was a duplicate — skipped (inbox idempotency).", integrationEvent.Id);
        }
    }
}

/// <summary>
/// Minimal shape of the IntegrationEventWrapper published by RabbitMqIntegrationEventPublisher.
/// Mirrors the internal <c>IntegrationEventWrapper</c> class in that assembly.
/// </summary>
file sealed class IntegrationEventEnvelope
{
    public Guid Id { get; init; }
    public string? EventType { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime OccurredOn { get; init; }
    public string? Payload { get; init; }
}
