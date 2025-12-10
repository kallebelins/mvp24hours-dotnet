//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Implementations;

/// <summary>
/// RabbitMQ implementation of the Integration Event Publisher.
/// Integrates with the existing <c>IMvpRabbitMQClient</c> from Mvp24Hours.Infrastructure.RabbitMQ.
/// </summary>
/// <remarks>
/// <para>
/// This publisher uses the existing RabbitMQ client infrastructure to publish
/// integration events. It converts integration events to the format expected
/// by the RabbitMQ client.
/// </para>
/// <para>
/// <strong>Configuration:</strong>
/// Ensure that RabbitMQ is properly configured in your application using
/// <c>services.AddMvpRabbitMQ()</c> before using this publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure in DI
/// services.AddMvpRabbitMQ(options => { ... });
/// services.AddScoped&lt;IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher&gt;();
/// 
/// // Inject and use
/// public class OutboxProcessor
/// {
///     private readonly IIntegrationEventPublisher _publisher;
///     private readonly IIntegrationEventOutbox _outbox;
///     
///     public async Task ProcessAsync(CancellationToken ct)
///     {
///         var pending = await _outbox.GetPendingAsync();
///         foreach (var message in pending)
///         {
///             await _publisher.PublishFromOutboxAsync(message, ct);
///             await _outbox.MarkAsPublishedAsync(message.Id, ct);
///         }
///     }
/// }
/// </code>
/// </example>
public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqIntegrationEventPublisher>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the RabbitMqIntegrationEventPublisher.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving RabbitMQ client.</param>
    /// <param name="logger">Optional logger for recording operations.</param>
    public RabbitMqIntegrationEventPublisher(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqIntegrationEventPublisher>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent).Name;
        var correlationId = @event.CorrelationId ?? @event.Id.ToString();

        _logger?.LogInformation(
            "[RabbitMQ Publisher] Publishing integration event {EventType} with CorrelationId {CorrelationId}",
            eventType,
            correlationId);

        // Use the existing MvpRabbitMQClient infrastructure
        // The client is resolved dynamically to support both scoped and singleton registrations
        var rabbitMqClientType = Type.GetType("Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IMvpRabbitMQClient, Mvp24Hours.Infrastructure.RabbitMQ");
        
        if (rabbitMqClientType == null)
        {
            _logger?.LogWarning(
                "[RabbitMQ Publisher] IMvpRabbitMQClient not found. Ensure Mvp24Hours.Infrastructure.RabbitMQ is referenced.");
            throw new InvalidOperationException(
                "RabbitMQ client not found. Ensure Mvp24Hours.Infrastructure.RabbitMQ is referenced and configured.");
        }

        var rabbitMqClient = _serviceProvider.GetService(rabbitMqClientType);
        
        if (rabbitMqClient == null)
        {
            _logger?.LogWarning(
                "[RabbitMQ Publisher] IMvpRabbitMQClient is not registered in the service provider.");
            throw new InvalidOperationException(
                "RabbitMQ client is not registered. Call services.AddMvpRabbitMQ() to configure.");
        }

        // Create a wrapper object that can be serialized as IBusinessEvent
        var wrapper = new IntegrationEventWrapper
        {
            Id = @event.Id,
            EventType = eventType,
            CorrelationId = correlationId,
            OccurredOn = @event.OccurredOn,
            Payload = JsonSerializer.Serialize(@event, _jsonOptions)
        };

        // Use reflection to call the Publish method
        var publishMethod = rabbitMqClientType.GetMethod("Publish");
        if (publishMethod != null)
        {
            try
            {
                publishMethod.Invoke(rabbitMqClient, new object[] { wrapper, correlationId });

                _logger?.LogDebug(
                    "[RabbitMQ Publisher] Successfully published event {EventType}",
                    eventType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[RabbitMQ Publisher] Failed to publish event {EventType}: {Message}",
                    eventType,
                    ex.Message);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishFromOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger?.LogInformation(
            "[RabbitMQ Publisher] Publishing outbox message {MessageId} of type {EventType}",
            message.Id,
            message.EventType);

        // The payload is already serialized, we need to publish it as-is
        var wrapper = new IntegrationEventWrapper
        {
            Id = message.Id,
            EventType = message.EventType,
            CorrelationId = message.CorrelationId ?? message.Id.ToString(),
            OccurredOn = message.CreatedAt,
            Payload = message.Payload
        };

        var rabbitMqClientType = Type.GetType("Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IMvpRabbitMQClient, Mvp24Hours.Infrastructure.RabbitMQ");
        
        if (rabbitMqClientType == null)
        {
            _logger?.LogWarning(
                "[RabbitMQ Publisher] IMvpRabbitMQClient not found. Ensure Mvp24Hours.Infrastructure.RabbitMQ is referenced.");
            throw new InvalidOperationException(
                "RabbitMQ client not found. Ensure Mvp24Hours.Infrastructure.RabbitMQ is referenced and configured.");
        }

        var rabbitMqClient = _serviceProvider.GetService(rabbitMqClientType);
        
        if (rabbitMqClient == null)
        {
            throw new InvalidOperationException(
                "RabbitMQ client is not registered. Call services.AddMvpRabbitMQ() to configure.");
        }

        var publishMethod = rabbitMqClientType.GetMethod("Publish");
        if (publishMethod != null)
        {
            publishMethod.Invoke(rabbitMqClient, new object[] { wrapper, wrapper.CorrelationId });
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Internal wrapper for serializing integration events as messages.
/// </summary>
internal sealed class IntegrationEventWrapper
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; }
    public string Payload { get; init; } = string.Empty;
}

