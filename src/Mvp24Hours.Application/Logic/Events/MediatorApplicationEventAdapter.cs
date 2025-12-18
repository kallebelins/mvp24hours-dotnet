//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Events;

/// <summary>
/// Adapter that bridges application events with Mediator notifications.
/// When an application event is dispatched, this adapter publishes it as a Mediator notification.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This adapter allows handlers defined using the CQRS module's notification system
/// to also receive application events, providing flexibility in how events are handled.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// Register the adapter as an event handler for the events you want to bridge:
/// <code>
/// services.AddScoped&lt;IApplicationEventHandler&lt;EntityCreatedEvent&lt;Customer&gt;&gt;, 
///     MediatorApplicationEventAdapter&lt;EntityCreatedEvent&lt;Customer&gt;&gt;&gt;();
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TEvent">The type of application event to bridge.</typeparam>
public class MediatorApplicationEventAdapter<TEvent> : IApplicationEventHandler<TEvent>
    where TEvent : IApplicationEvent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediatorApplicationEventAdapter<TEvent>>? _logger;

    /// <summary>
    /// Creates a new instance of the MediatorApplicationEventAdapter.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving the Mediator publisher.</param>
    /// <param name="logger">Optional logger.</param>
    public MediatorApplicationEventAdapter(
        IServiceProvider serviceProvider,
        ILogger<MediatorApplicationEventAdapter<TEvent>>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to resolve IPublisher from CQRS module
            var publisherType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.Abstractions.IPublisher, Mvp24Hours.Infrastructure.Cqrs");
            
            if (publisherType == null)
            {
                _logger?.LogDebug(
                    "[MediatorAdapter] CQRS module not available, skipping Mediator publish for {EventType}",
                    typeof(TEvent).Name);
                return;
            }

            var publisher = _serviceProvider.GetService(publisherType);
            if (publisher == null)
            {
                _logger?.LogDebug(
                    "[MediatorAdapter] IPublisher not registered, skipping Mediator publish for {EventType}",
                    typeof(TEvent).Name);
                return;
            }

            // Create a notification wrapper
            var notification = new ApplicationEventMediatorNotification<TEvent>(@event);

            // Get the Publish method
            var publishMethod = publisherType.GetMethod("Publish");
            if (publishMethod == null)
            {
                _logger?.LogWarning(
                    "[MediatorAdapter] Publish method not found on IPublisher");
                return;
            }

            // Make the generic method
            var notificationType = typeof(ApplicationEventMediatorNotification<TEvent>);
            var genericPublish = publishMethod.MakeGenericMethod(notificationType);

            // Invoke
            var task = genericPublish.Invoke(publisher, [notification, cancellationToken]) as Task;
            if (task != null)
            {
                await task;
            }

            _logger?.LogDebug(
                "[MediatorAdapter] Published {EventType} to Mediator",
                typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[MediatorAdapter] Failed to publish {EventType} to Mediator: {Message}",
                typeof(TEvent).Name,
                ex.Message);
        }
    }
}

/// <summary>
/// Wrapper notification for publishing application events through the Mediator.
/// Implements IMediatorNotification from the CQRS module.
/// </summary>
/// <typeparam name="TEvent">The type of application event.</typeparam>
/// <remarks>
/// This class is designed to implement IMediatorNotification dynamically,
/// allowing it to work even when the CQRS module is not referenced directly.
/// </remarks>
public class ApplicationEventMediatorNotification<TEvent>
    where TEvent : IApplicationEvent
{
    /// <summary>
    /// Creates a new notification wrapper.
    /// </summary>
    /// <param name="event">The application event.</param>
    public ApplicationEventMediatorNotification(TEvent @event)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
    }

    /// <summary>
    /// The wrapped application event.
    /// </summary>
    public TEvent Event { get; }

    /// <summary>
    /// The event ID from the wrapped event.
    /// </summary>
    public Guid EventId => Event.EventId;

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime OccurredAt => Event.OccurredAt;

    /// <summary>
    /// The correlation ID from the wrapped event.
    /// </summary>
    public string? CorrelationId => Event.CorrelationId;
}

/// <summary>
/// Factory for creating Mediator adapter registrations.
/// </summary>
public static class MediatorApplicationEventAdapterFactory
{
    /// <summary>
    /// Creates a handler type for the specified event type.
    /// </summary>
    /// <param name="eventType">The application event type.</param>
    /// <returns>The adapter handler type.</returns>
    public static Type CreateAdapterType(Type eventType)
    {
        if (!typeof(IApplicationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Type {eventType.Name} does not implement IApplicationEvent",
                nameof(eventType));
        }

        return typeof(MediatorApplicationEventAdapter<>).MakeGenericType(eventType);
    }
}

