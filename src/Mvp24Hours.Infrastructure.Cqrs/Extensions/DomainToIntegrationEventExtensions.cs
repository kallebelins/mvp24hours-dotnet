//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Extensions;

/// <summary>
/// Extension methods for converting Domain Events to Integration Events.
/// </summary>
public static class DomainToIntegrationEventExtensions
{
    /// <summary>
    /// Converts domain events to integration events and adds them to the outbox.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that has domain events.</typeparam>
    /// <param name="entity">The entity with domain events.</param>
    /// <param name="serviceProvider">Service provider for resolving converters.</param>
    /// <param name="outbox">The integration event outbox.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// // In your unit of work or save changes method
    /// foreach (var entity in changeTracker.Entries&lt;IHasDomainEvents&gt;())
    /// {
    ///     await entity.Entity.ConvertAndQueueIntegrationEventsAsync(
    ///         serviceProvider,
    ///         outbox);
    /// }
    /// </code>
    /// </example>
    public static async Task ConvertAndQueueIntegrationEventsAsync<TEntity>(
        this TEntity entity,
        IServiceProvider serviceProvider,
        IIntegrationEventOutbox outbox,
        CancellationToken cancellationToken = default)
        where TEntity : IHasDomainEvents
    {
        if (entity == null || !entity.DomainEvents.Any())
        {
            return;
        }

        var logger = serviceProvider.GetService<ILogger<TEntity>>();

        foreach (var domainEvent in entity.DomainEvents)
        {
            var integrationEvent = ConvertDomainEventToIntegrationEvent(domainEvent, serviceProvider);

            if (integrationEvent != null)
            {
                await outbox.AddAsync(integrationEvent, cancellationToken);

                logger?.LogDebug(
                    "Converted domain event {DomainEventType} to integration event {IntegrationEventType}",
                    domainEvent.GetType().Name,
                    integrationEvent.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Converts a single domain event to an integration event using registered converters.
    /// </summary>
    /// <param name="domainEvent">The domain event to convert.</param>
    /// <param name="serviceProvider">Service provider for resolving converters.</param>
    /// <returns>The integration event, or null if no converter is registered.</returns>
    public static IIntegrationEvent? ConvertDomainEventToIntegrationEvent(
        IDomainEvent domainEvent,
        IServiceProvider serviceProvider)
    {
        var domainEventType = domainEvent.GetType();
        
        // Find a converter for this domain event type
        // Look for IDomainToIntegrationEventConverter<TDomainEvent, TIntegrationEvent>
        var converterInterfaceType = typeof(IDomainToIntegrationEventConverter<,>);
        
        foreach (var service in serviceProvider.GetServices<object>())
        {
            if (service == null) continue;

            var serviceType = service.GetType();
            var interfaces = serviceType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                if (!@interface.IsGenericType) continue;
                if (@interface.GetGenericTypeDefinition() != converterInterfaceType) continue;

                var genericArgs = @interface.GetGenericArguments();
                if (genericArgs.Length != 2) continue;

                var converterDomainEventType = genericArgs[0];
                
                if (!converterDomainEventType.IsAssignableFrom(domainEventType)) continue;

                // Found a matching converter, invoke the Convert method
                var convertMethod = @interface.GetMethod("Convert");
                if (convertMethod != null)
                {
                    var result = convertMethod.Invoke(service, new object[] { domainEvent });
                    return result as IIntegrationEvent;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Domain event handler that automatically converts and queues integration events.
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event.</typeparam>
/// <typeparam name="TIntegrationEvent">The type of integration event.</typeparam>
/// <remarks>
/// <para>
/// Register this handler along with a converter to automatically queue
/// integration events when domain events are dispatched.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the automatic handler
/// services.AddTransient&lt;
///     IDomainEventHandler&lt;OrderCreatedDomainEvent&gt;,
///     AutoIntegrationEventHandler&lt;OrderCreatedDomainEvent, OrderCreatedIntegrationEvent&gt;&gt;();
/// 
/// // Register the converter
/// services.AddTransient&lt;
///     IDomainToIntegrationEventConverter&lt;OrderCreatedDomainEvent, OrderCreatedIntegrationEvent&gt;,
///     OrderCreatedDomainToIntegrationConverter&gt;();
/// </code>
/// </example>
public sealed class AutoIntegrationEventHandler<TDomainEvent, TIntegrationEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : class, IIntegrationEvent
{
    private readonly IDomainToIntegrationEventConverter<TDomainEvent, TIntegrationEvent>? _converter;
    private readonly IIntegrationEventOutbox _outbox;
    private readonly ILogger<AutoIntegrationEventHandler<TDomainEvent, TIntegrationEvent>>? _logger;

    /// <summary>
    /// Creates a new instance of the AutoIntegrationEventHandler.
    /// </summary>
    public AutoIntegrationEventHandler(
        IIntegrationEventOutbox outbox,
        IDomainToIntegrationEventConverter<TDomainEvent, TIntegrationEvent>? converter = null,
        ILogger<AutoIntegrationEventHandler<TDomainEvent, TIntegrationEvent>>? logger = null)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _converter = converter;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        if (_converter == null)
        {
            _logger?.LogDebug(
                "No converter registered for {DomainEventType} to {IntegrationEventType}",
                typeof(TDomainEvent).Name,
                typeof(TIntegrationEvent).Name);
            return;
        }

        var integrationEvent = _converter.Convert(notification);

        if (integrationEvent != null)
        {
            await _outbox.AddAsync(integrationEvent, cancellationToken);

            _logger?.LogInformation(
                "Queued integration event {IntegrationEventType} from domain event {DomainEventType}",
                typeof(TIntegrationEvent).Name,
                typeof(TDomainEvent).Name);
        }
    }
}

