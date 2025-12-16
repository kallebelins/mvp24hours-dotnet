//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Cqrs
{
    /// <summary>
    /// Adapter that dispatches domain events using a delegate function.
    /// This allows integration with external event dispatchers (like CQRS module)
    /// without direct dependencies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// This adapter enables the EF Core module to dispatch domain events
    /// using any external dispatcher implementation through a delegate.
    /// </para>
    /// <para>
    /// <strong>Integration with CQRS:</strong>
    /// Use the extension methods in <c>EFCoreCqrsIntegrationExtensions</c> to
    /// automatically wire up the CQRS dispatcher.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Manual setup with delegate
    /// services.AddScoped&lt;IDomainEventDispatcherEFCore&gt;(sp =>
    /// {
    ///     var publisher = sp.GetRequiredService&lt;IPublisher&gt;();
    ///     return new DomainEventDispatcherAdapter(async (events, ct) =>
    ///     {
    ///         foreach (var evt in events)
    ///         {
    ///             await publisher.PublishAsync(evt, ct);
    ///         }
    ///     });
    /// });
    /// </code>
    /// </example>
    public class DomainEventDispatcherAdapter : IDomainEventDispatcherEFCore
    {
        private readonly Func<IEnumerable<IDomainEvent>, CancellationToken, Task> _dispatchFunc;
        private readonly ILogger<DomainEventDispatcherAdapter>? _logger;

        /// <summary>
        /// Creates a new adapter with the specified dispatch function.
        /// </summary>
        /// <param name="dispatchFunc">
        /// Function that receives a collection of domain events and dispatches them.
        /// The function is responsible for publishing each event to appropriate handlers.
        /// </param>
        /// <param name="logger">Optional logger for recording dispatch operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when dispatchFunc is null.</exception>
        public DomainEventDispatcherAdapter(
            Func<IEnumerable<IDomainEvent>, CancellationToken, Task> dispatchFunc,
            ILogger<DomainEventDispatcherAdapter>? logger = null)
        {
            _dispatchFunc = dispatchFunc ?? throw new ArgumentNullException(nameof(dispatchFunc));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) return;

            var events = entity.DomainEvents.ToList();
            if (events.Count == 0) return;

            _logger?.LogDebug(
                "[EventDispatcher] Dispatching {EventCount} events from {EntityType}",
                events.Count,
                entity.GetType().Name);

            try
            {
                await _dispatchFunc(events, cancellationToken);
                entity.ClearDomainEvents();

                _logger?.LogDebug(
                    "[EventDispatcher] Successfully dispatched {EventCount} events from {EntityType}",
                    events.Count,
                    entity.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[EventDispatcher] Error dispatching events from {EntityType}: {Message}",
                    entity.GetType().Name,
                    ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) return;

            var entitiesList = entities.Where(e => e.DomainEvents.Count > 0).ToList();
            if (entitiesList.Count == 0) return;

            var allEvents = entitiesList.SelectMany(e => e.DomainEvents).ToList();

            _logger?.LogDebug(
                "[EventDispatcher] Dispatching {EventCount} events from {EntityCount} entities",
                allEvents.Count,
                entitiesList.Count);

            try
            {
                await _dispatchFunc(allEvents, cancellationToken);

                foreach (var entity in entitiesList)
                {
                    entity.ClearDomainEvents();
                }

                _logger?.LogDebug(
                    "[EventDispatcher] Successfully dispatched {EventCount} events from {EntityCount} entities",
                    allEvents.Count,
                    entitiesList.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "[EventDispatcher] Error dispatching events: {Message}",
                    ex.Message);
                throw;
            }
        }
    }
}

