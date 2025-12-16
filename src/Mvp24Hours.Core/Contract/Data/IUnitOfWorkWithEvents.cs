//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Extends <see cref="IUnitOfWork"/> with Domain Event support.
    /// Automatically dispatches domain events after successfully persisting changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Design Pattern: Unit of Work with Domain Events</strong>
    /// </para>
    /// <para>
    /// This interface combines the Unit of Work pattern with Domain Event publishing,
    /// ensuring that domain events are only dispatched after the transaction is successfully
    /// committed. This provides:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Automatic collection of domain events from tracked entities</description></item>
    /// <item><description>Atomic commit-then-publish semantics</description></item>
    /// <item><description>Event dispatch only on successful persistence</description></item>
    /// <item><description>Consistent event ordering within a transaction</description></item>
    /// </list>
    /// <para>
    /// <strong>Event Flow:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>Entities raise domain events during state changes</description></item>
    /// <item><description>SaveChangesWithEventsAsync collects events from tracked entities</description></item>
    /// <item><description>Changes are persisted to the database</description></item>
    /// <item><description>Domain events are dispatched via the event dispatcher</description></item>
    /// <item><description>Events are cleared from entities</description></item>
    /// </list>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// If persistence fails, no events are dispatched. If event dispatch fails after
    /// successful persistence, consider using the Outbox pattern for reliability.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using IUnitOfWorkWithEvents
    /// var unitOfWork = serviceProvider.GetService&lt;IUnitOfWorkWithEvents&gt;();
    /// 
    /// var orderRepo = unitOfWork.GetRepository&lt;Order&gt;();
    /// var order = new Order();
    /// order.Place(); // Raises OrderPlacedEvent
    /// 
    /// orderRepo.Add(order);
    /// 
    /// // Saves changes and dispatches OrderPlacedEvent automatically
    /// await unitOfWork.SaveChangesWithEventsAsync();
    /// </code>
    /// </example>
    public interface IUnitOfWorkWithEvents : IUnitOfWork
    {
        /// <summary>
        /// Persists all changes and dispatches domain events from tracked entities.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// If cancellation is requested, no changes are persisted and no events are dispatched.
        /// </param>
        /// <returns>
        /// The number of state entries written to the database.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs the following operations in order:
        /// </para>
        /// <list type="number">
        /// <item><description>Collects domain events from all tracked entities implementing <see cref="IHasDomainEvents"/></description></item>
        /// <item><description>Persists all changes to the database</description></item>
        /// <item><description>Dispatches collected domain events</description></item>
        /// <item><description>Clears domain events from entities</description></item>
        /// </list>
        /// <para>
        /// <strong>Transaction Guarantees:</strong>
        /// </para>
        /// <list type="bullet">
        /// <item><description>If persistence fails, no events are dispatched</description></item>
        /// <item><description>Events are dispatched in the order they were raised</description></item>
        /// <item><description>If event dispatch fails, changes are already committed</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var order = new Order();
        /// order.Place(); // Raises OrderPlacedEvent
        /// 
        /// orderRepo.Add(order);
        /// 
        /// // This will:
        /// // 1. Save the order to the database
        /// // 2. Dispatch OrderPlacedEvent to all registered handlers
        /// int changes = unitOfWork.SaveChangesWithEvents();
        /// </code>
        /// </example>
        int SaveChangesWithEvents(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities with pending domain events that are currently tracked.
        /// </summary>
        /// <returns>
        /// A collection of entities implementing <see cref="IHasDomainEvents"/> that have pending events.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is useful for inspecting which events will be dispatched
        /// before calling <see cref="SaveChangesWithEvents"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var entitiesWithEvents = unitOfWork.GetEntitiesWithEvents();
        /// 
        /// foreach (var entity in entitiesWithEvents)
        /// {
        ///     Console.WriteLine($"Entity {entity.GetType().Name} has {entity.DomainEvents.Count} pending events");
        /// }
        /// </code>
        /// </example>
        IEnumerable<IHasDomainEvents> GetEntitiesWithEvents();
    }
}

