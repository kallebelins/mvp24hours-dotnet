//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs
{
    /// <summary>
    /// No-operation implementation of <see cref="IDomainEventDispatcherMongoDb"/>.
    /// Used when domain event dispatching is not needed but the interface is required.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// This implementation provides a safe default when no actual event dispatcher
    /// is configured. It simply clears events from entities without dispatching them.
    /// </para>
    /// <para>
    /// <strong>Usage Scenarios:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Development/testing without full CQRS infrastructure</description></item>
    /// <item><description>Applications that don't use domain events</description></item>
    /// <item><description>Gradual migration to domain events</description></item>
    /// </list>
    /// <para>
    /// <strong>Warning:</strong>
    /// Events are silently discarded. If you need event dispatching, use
    /// <see cref="DomainEventDispatcherAdapter"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register no-op dispatcher (events will be silently discarded)
    /// services.AddScoped&lt;IDomainEventDispatcherMongoDb, NoOpDomainEventDispatcher&gt;();
    /// </code>
    /// </example>
    public class NoOpDomainEventDispatcher : IDomainEventDispatcherMongoDb
    {
        private readonly ILogger<NoOpDomainEventDispatcher>? _logger;

        /// <summary>
        /// Creates a new instance of the no-op dispatcher.
        /// </summary>
        /// <param name="logger">Optional logger for recording discarded events.</param>
        public NoOpDomainEventDispatcher(ILogger<NoOpDomainEventDispatcher>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) return Task.CompletedTask;

            var eventCount = entity.DomainEvents.Count;
            if (eventCount > 0)
            {
                _logger?.LogDebug(
                    "[NoOpDispatcher] Discarding {EventCount} events from {EntityType} (no dispatcher configured)",
                    eventCount,
                    entity.GetType().Name);

                entity.ClearDomainEvents();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) return Task.CompletedTask;

            var entitiesList = entities.Where(e => e.DomainEvents.Count > 0).ToList();
            if (entitiesList.Count == 0) return Task.CompletedTask;

            var totalEvents = entitiesList.Sum(e => e.DomainEvents.Count);

            _logger?.LogDebug(
                "[NoOpDispatcher] Discarding {EventCount} events from {EntityCount} entities (no dispatcher configured)",
                totalEvents,
                entitiesList.Count);

            foreach (var entity in entitiesList)
            {
                entity.ClearDomainEvents();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void DispatchEvents(IHasDomainEvents entity, CancellationToken cancellationToken = default)
        {
            DispatchEventsAsync(entity, cancellationToken).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void DispatchEvents(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
        {
            DispatchEventsAsync(entities, cancellationToken).GetAwaiter().GetResult();
        }
    }
}

