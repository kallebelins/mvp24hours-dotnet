//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Extensions;

/// <summary>
/// Extension methods for integrating domain events with Unit of Work pattern.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Saves changes and dispatches domain events from the specified entities.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="entities">The entities containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// <para>
    /// This method first saves changes to the database, then dispatches all domain events.
    /// This ensures that events are only published after the data has been successfully persisted.
    /// </para>
    /// <para>
    /// <strong>Transaction Safety:</strong> If SaveChanges fails, no events are dispatched.
    /// If event dispatching fails after SaveChanges, the database changes are already committed.
    /// For full transactional consistency, consider using the Outbox pattern.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var order = new Order();
    /// order.Place();
    /// 
    /// var repository = unitOfWork.GetRepository&lt;Order&gt;();
    /// repository.Add(order);
    /// 
    /// await unitOfWork.SaveChangesWithEventsAsync(
    ///     domainEventDispatcher,
    ///     new[] { order },
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static async Task<int> SaveChangesWithEventsAsync(
        this IUnitOfWorkAsync unitOfWork,
        IDomainEventDispatcher dispatcher,
        IEnumerable<IHasDomainEvents> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // First, save all changes to the database
        var result = await unitOfWork.SaveChangesAsync(cancellationToken);

        // Then dispatch domain events
        await dispatcher.DispatchEventsAsync(entities, cancellationToken);

        return result;
    }

    /// <summary>
    /// Saves changes and dispatches domain events from a single entity.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="entity">The entity containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public static Task<int> SaveChangesWithEventsAsync(
        this IUnitOfWorkAsync unitOfWork,
        IDomainEventDispatcher dispatcher,
        IHasDomainEvents entity,
        CancellationToken cancellationToken = default)
    {
        return unitOfWork.SaveChangesWithEventsAsync(
            dispatcher,
            new[] { entity },
            cancellationToken);
    }

    /// <summary>
    /// Synchronous version: Saves changes and dispatches domain events from the specified entities.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="entities">The entities containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public static int SaveChangesWithEvents(
        this IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IEnumerable<IHasDomainEvents> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // First, save all changes to the database
        var result = unitOfWork.SaveChanges(cancellationToken);

        // Then dispatch domain events (blocking)
        dispatcher.DispatchEventsAsync(entities, cancellationToken)
            .GetAwaiter()
            .GetResult();

        return result;
    }

    /// <summary>
    /// Synchronous version: Saves changes and dispatches domain events from a single entity.
    /// </summary>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="entity">The entity containing domain events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public static int SaveChangesWithEvents(
        this IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IHasDomainEvents entity,
        CancellationToken cancellationToken = default)
    {
        return unitOfWork.SaveChangesWithEvents(
            dispatcher,
            new[] { entity },
            cancellationToken);
    }

    /// <summary>
    /// Extracts all entities that have pending domain events from a collection.
    /// </summary>
    /// <typeparam name="T">The type of entities.</typeparam>
    /// <param name="entities">The collection of entities.</param>
    /// <returns>Entities that implement IHasDomainEvents and have pending events.</returns>
    public static IEnumerable<IHasDomainEvents> WithDomainEvents<T>(this IEnumerable<T> entities)
        where T : class
    {
        return entities
            .OfType<IHasDomainEvents>()
            .Where(e => e.DomainEvents.Count > 0);
    }
}

