//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.Contract.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Events;

/// <summary>
/// Default implementation of the application event dispatcher.
/// Dispatches events to all registered handlers.
/// </summary>
/// <remarks>
/// <para>
/// This dispatcher supports multiple dispatch strategies:
/// <list type="bullet">
/// <item>Sequential - Handlers execute one after another</item>
/// <item>Parallel - Handlers execute concurrently</item>
/// </list>
/// </para>
/// <para>
/// <strong>Integration with Mediator:</strong>
/// When <c>IntegrateWithMediator</c> is enabled, events are also published
/// as Mediator notifications, allowing handlers defined in the CQRS module
/// to receive application events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddApplicationEvents(options => 
/// {
///     options.Strategy = EventDispatchStrategy.Parallel;
///     options.ContinueOnError = true;
/// });
/// 
/// // Use in service
/// await _dispatcher.DispatchAsync(new EntityCreatedEvent&lt;Customer&gt;(customer));
/// </code>
/// </example>
public sealed class ApplicationEventDispatcher : IApplicationEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationEventDispatcherOptions _options;
    private readonly IApplicationEventOutbox? _outbox;
    private readonly ILogger<ApplicationEventDispatcher>? _logger;

    /// <summary>
    /// Creates a new instance of the ApplicationEventDispatcher.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    /// <param name="options">The dispatcher options.</param>
    /// <param name="outbox">Optional outbox for reliable delivery.</param>
    /// <param name="logger">Optional logger.</param>
    public ApplicationEventDispatcher(
        IServiceProvider serviceProvider,
        IOptions<ApplicationEventDispatcherOptions>? options = null,
        IApplicationEventOutbox? outbox = null,
        ILogger<ApplicationEventDispatcher>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? new ApplicationEventDispatcherOptions();
        _outbox = outbox;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent);
        
        _logger?.LogDebug(
            "[ApplicationEvents] Dispatching {EventType} (Id: {EventId})",
            eventType.Name,
            @event.EventId);

        // If using outbox pattern, store the event first
        if (_options.UseOutbox && _outbox != null)
        {
            await _outbox.AddAsync(@event, cancellationToken);
            _logger?.LogDebug(
                "[ApplicationEvents] Event {EventId} stored in outbox",
                @event.EventId);
            return; // Outbox processor will dispatch later
        }

        // Resolve handlers
        var handlerType = typeof(IApplicationEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            _logger?.LogDebug(
                "[ApplicationEvents] No handlers registered for {EventType}",
                eventType.Name);
            return;
        }

        _logger?.LogDebug(
            "[ApplicationEvents] Found {HandlerCount} handlers for {EventType}",
            handlers.Count,
            eventType.Name);

        // Dispatch based on strategy
        switch (_options.Strategy)
        {
            case EventDispatchStrategy.Sequential:
                await DispatchSequentialAsync(@event, handlers, cancellationToken);
                break;

            case EventDispatchStrategy.Parallel:
                await DispatchParallelAsync(@event, handlers, cancellationToken);
                break;

            default:
                await DispatchSequentialAsync(@event, handlers, cancellationToken);
                break;
        }

        // Integrate with Mediator if enabled
        if (_options.IntegrateWithMediator)
        {
            await DispatchToMediatorAsync(@event, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IApplicationEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventsList = events.ToList();
        if (eventsList.Count == 0)
        {
            return;
        }

        _logger?.LogDebug(
            "[ApplicationEvents] Dispatching {EventCount} events",
            eventsList.Count);

        foreach (var @event in eventsList)
        {
            // Use reflection to call the generic method
            var method = typeof(ApplicationEventDispatcher)
                .GetMethod(nameof(DispatchAsync), [typeof(IApplicationEvent), typeof(CancellationToken)]);
            
            var genericMethod = method?.MakeGenericMethod(@event.GetType());
            
            if (genericMethod != null)
            {
                var task = genericMethod.Invoke(this, [@event, cancellationToken]) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }
    }

    private async Task DispatchSequentialAsync<TEvent>(
        TEvent @event,
        IList<object?> handlers,
        CancellationToken cancellationToken)
        where TEvent : IApplicationEvent
    {
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            try
            {
                var typedHandler = (IApplicationEventHandler<TEvent>)handler;
                await typedHandler.HandleAsync(@event, cancellationToken);

                _logger?.LogDebug(
                    "[ApplicationEvents] Handler {HandlerType} completed for {EventType}",
                    handler.GetType().Name,
                    typeof(TEvent).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[ApplicationEvents] Handler {HandlerType} failed for {EventType}: {Message}",
                    handler.GetType().Name,
                    typeof(TEvent).Name,
                    ex.Message);

                if (!_options.ContinueOnError)
                {
                    throw;
                }

                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0 && !_options.ContinueOnError)
        {
            throw new AggregateException(
                "One or more event handlers failed",
                exceptions);
        }
    }

    private async Task DispatchParallelAsync<TEvent>(
        TEvent @event,
        IList<object?> handlers,
        CancellationToken cancellationToken)
        where TEvent : IApplicationEvent
    {
        var tasks = handlers
            .Where(h => h != null)
            .Select(handler => DispatchToHandlerAsync(@event, handler!, cancellationToken))
            .ToList();

        if (_options.MaxDegreeOfParallelism > 0)
        {
            // Limit parallelism
            using var semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism);
            var throttledTasks = tasks.Select(async task =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await task;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(throttledTasks);
        }
        else
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task DispatchToHandlerAsync<TEvent>(
        TEvent @event,
        object handler,
        CancellationToken cancellationToken)
        where TEvent : IApplicationEvent
    {
        try
        {
            var typedHandler = (IApplicationEventHandler<TEvent>)handler;
            await typedHandler.HandleAsync(@event, cancellationToken);

            _logger?.LogDebug(
                "[ApplicationEvents] Handler {HandlerType} completed for {EventType}",
                handler.GetType().Name,
                typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[ApplicationEvents] Handler {HandlerType} failed for {EventType}: {Message}",
                handler.GetType().Name,
                typeof(TEvent).Name,
                ex.Message);

            if (!_options.ContinueOnError)
            {
                throw;
            }
        }
    }

    private async Task DispatchToMediatorAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IApplicationEvent
    {
        try
        {
            // Try to resolve IMediatorPublisher or similar from CQRS module
            // This allows application events to also be handled by Mediator notification handlers
            var publisherType = Type.GetType("Mvp24Hours.Infrastructure.Cqrs.Abstractions.IPublisher, Mvp24Hours.Infrastructure.Cqrs");
            
            if (publisherType != null)
            {
                var publisher = _serviceProvider.GetService(publisherType);
                if (publisher != null)
                {
                    // Create a wrapper notification and publish
                    var wrapperType = typeof(ApplicationEventNotification<>).MakeGenericType(typeof(TEvent));
                    var wrapper = Activator.CreateInstance(wrapperType, @event);
                    
                    var publishMethod = publisherType.GetMethod("Publish");
                    if (publishMethod != null && wrapper != null)
                    {
                        var genericPublish = publishMethod.MakeGenericMethod(wrapperType);
                        var task = genericPublish.Invoke(publisher, [wrapper, cancellationToken]) as Task;
                        if (task != null)
                        {
                            await task;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[ApplicationEvents] Failed to dispatch to Mediator: {Message}",
                ex.Message);
        }
    }
}

/// <summary>
/// Wrapper to publish application events as Mediator notifications.
/// </summary>
/// <typeparam name="TEvent">The type of application event.</typeparam>
public class ApplicationEventNotification<TEvent>
    where TEvent : IApplicationEvent
{
    /// <summary>
    /// Creates a new notification wrapper.
    /// </summary>
    /// <param name="event">The application event.</param>
    public ApplicationEventNotification(TEvent @event)
    {
        Event = @event;
    }

    /// <summary>
    /// The wrapped application event.
    /// </summary>
    public TEvent Event { get; }
}

