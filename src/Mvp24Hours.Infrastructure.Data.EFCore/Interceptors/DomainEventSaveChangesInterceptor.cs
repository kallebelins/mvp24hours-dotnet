//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Interceptors
{
    /// <summary>
    /// EF Core interceptor that automatically dispatches domain events after SaveChanges.
    /// This interceptor collects domain events from tracked entities before saving
    /// and dispatches them after the save operation completes successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Event Flow:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>Before SaveChanges: Collects domain events from tracked entities</description></item>
    /// <item><description>SaveChanges executes and commits to database</description></item>
    /// <item><description>After SaveChanges: Dispatches collected events</description></item>
    /// <item><description>Clears events from entities</description></item>
    /// </list>
    /// <para>
    /// <strong>Transaction Safety:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Events are only dispatched after successful save</description></item>
    /// <item><description>If save fails, no events are dispatched</description></item>
    /// <item><description>If event dispatch fails, the database changes are already committed</description></item>
    /// </list>
    /// <para>
    /// <strong>Usage:</strong>
    /// Add this interceptor to your DbContext to enable automatic event dispatching.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In DbContext configuration
    /// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    /// {
    ///     optionsBuilder.AddInterceptors(
    ///         new DomainEventSaveChangesInterceptor(eventDispatcher, logger));
    /// }
    /// 
    /// // Or via DI
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     var dispatcher = sp.GetService&lt;IDomainEventDispatcherEFCore&gt;();
    ///     options.AddInterceptors(new DomainEventSaveChangesInterceptor(dispatcher));
    /// });
    /// </code>
    /// </example>
    public class DomainEventSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IDomainEventDispatcherEFCore? _eventDispatcher;
        private readonly ILogger<DomainEventSaveChangesInterceptor>? _logger;

        // Thread-local storage for events collected before save
        [ThreadStatic]
        private static List<(IHasDomainEvents Entity, List<IDomainEvent> Events)>? _pendingEvents;

        /// <summary>
        /// Creates a new instance of the interceptor.
        /// </summary>
        /// <param name="eventDispatcher">
        /// The domain event dispatcher. If null, events will be cleared but not dispatched.
        /// </param>
        /// <param name="logger">Optional logger for recording operations.</param>
        public DomainEventSaveChangesInterceptor(
            IDomainEventDispatcherEFCore? eventDispatcher = null,
            ILogger<DomainEventSaveChangesInterceptor>? logger = null)
        {
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Called at the start of SaveChanges. Collects domain events from tracked entities.
        /// </summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CollectDomainEvents(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Called at the start of SaveChangesAsync. Collects domain events from tracked entities.
        /// </summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CollectDomainEvents(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Called after SaveChanges completes successfully. Dispatches collected domain events.
        /// </summary>
        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            DispatchEventsSync();
            return base.SavedChanges(eventData, result);
        }

        /// <summary>
        /// Called after SaveChangesAsync completes successfully. Dispatches collected domain events.
        /// </summary>
        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await DispatchEventsAsync(cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Called when SaveChanges fails. Clears collected events without dispatching.
        /// </summary>
        public override void SaveChangesFailed(
            DbContextErrorEventData eventData)
        {
            ClearPendingEvents();
            base.SaveChangesFailed(eventData);
        }

        /// <summary>
        /// Called when SaveChangesAsync fails. Clears collected events without dispatching.
        /// </summary>
        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            ClearPendingEvents();
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        #region [ Private Methods ]

        private void CollectDomainEvents(DbContext? context)
        {
            if (context == null) return;

            var entitiesWithEvents = context.ChangeTracker.Entries()
                .Where(e => e.Entity is IHasDomainEvents)
                .Select(e => (IHasDomainEvents)e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            if (entitiesWithEvents.Count == 0)
            {
                _pendingEvents = null;
                return;
            }

            // Capture events before save (in case entity state changes during save)
            _pendingEvents = entitiesWithEvents
                .Select(e => (Entity: e, Events: e.DomainEvents.ToList()))
                .ToList();

            var totalEvents = _pendingEvents.Sum(p => p.Events.Count);

            _logger?.LogDebug(
                "[DomainEventInterceptor] Collected {EventCount} domain events from {EntityCount} entities before save",
                totalEvents,
                entitiesWithEvents.Count);
        }

        private void DispatchEventsSync()
        {
            if (_pendingEvents == null || _pendingEvents.Count == 0) return;

            try
            {
                if (_eventDispatcher != null)
                {
                    var entities = _pendingEvents.Select(p => p.Entity).ToList();
                    _eventDispatcher.DispatchEventsAsync(entities, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    // Clear events even without dispatcher
                    foreach (var (entity, _) in _pendingEvents)
                    {
                        entity.ClearDomainEvents();
                    }

                    _logger?.LogWarning(
                        "[DomainEventInterceptor] Domain events were not dispatched because no IDomainEventDispatcherEFCore is registered");
                }
            }
            finally
            {
                _pendingEvents = null;
            }
        }

        private async Task DispatchEventsAsync(CancellationToken cancellationToken)
        {
            if (_pendingEvents == null || _pendingEvents.Count == 0) return;

            try
            {
                if (_eventDispatcher != null)
                {
                    var entities = _pendingEvents.Select(p => p.Entity).ToList();
                    await _eventDispatcher.DispatchEventsAsync(entities, cancellationToken);
                }
                else
                {
                    // Clear events even without dispatcher
                    foreach (var (entity, _) in _pendingEvents)
                    {
                        entity.ClearDomainEvents();
                    }

                    _logger?.LogWarning(
                        "[DomainEventInterceptor] Domain events were not dispatched because no IDomainEventDispatcherEFCore is registered");
                }
            }
            finally
            {
                _pendingEvents = null;
            }
        }

        private void ClearPendingEvents()
        {
            if (_pendingEvents == null) return;

            _logger?.LogDebug(
                "[DomainEventInterceptor] Clearing pending events due to save failure");

            _pendingEvents = null;
        }

        #endregion
    }
}

