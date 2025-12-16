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

namespace Mvp24Hours.Infrastructure.Data.EFCore.Cqrs
{
    /// <summary>
    /// No-operation implementation of <see cref="IDomainEventDispatcherEFCore"/>.
    /// Clears domain events without dispatching them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong>
    /// Use this implementation when you want to use <see cref="UnitOfWorkWithEventsAsync"/>
    /// but don't have the CQRS module configured, or when you want to disable event
    /// dispatching in certain environments (e.g., testing).
    /// </para>
    /// <para>
    /// <strong>Warning:</strong>
    /// Events are cleared but not processed. If you need event handling,
    /// use the CQRS adapter instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register for environments without CQRS
    /// services.AddScoped&lt;IDomainEventDispatcherEFCore, NoOpDomainEventDispatcher&gt;();
    /// </code>
    /// </example>
    public class NoOpDomainEventDispatcher : IDomainEventDispatcherEFCore
    {
        private readonly ILogger<NoOpDomainEventDispatcher>? _logger;

        /// <summary>
        /// Creates a new instance of the no-op dispatcher.
        /// </summary>
        /// <param name="logger">Optional logger for recording skipped events.</param>
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
                    "[NoOpDispatcher] Clearing {EventCount} domain events from {EntityType} (events not dispatched)",
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

            var entitiesList = entities.ToList();
            var totalEvents = entitiesList.Sum(e => e.DomainEvents.Count);

            if (totalEvents > 0)
            {
                _logger?.LogDebug(
                    "[NoOpDispatcher] Clearing {EventCount} domain events from {EntityCount} entities (events not dispatched)",
                    totalEvents,
                    entitiesList.Count);

                foreach (var entity in entitiesList)
                {
                    entity.ClearDomainEvents();
                }
            }

            return Task.CompletedTask;
        }
    }
}

