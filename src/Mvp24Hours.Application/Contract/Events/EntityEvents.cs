//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.Application.Contract.Events;

/// <summary>
/// Event raised when a new entity is created.
/// </summary>
/// <typeparam name="TEntity">The type of entity that was created.</typeparam>
/// <example>
/// <code>
/// // Handling customer creation
/// public class CustomerCreatedHandler : IApplicationEventHandler&lt;EntityCreatedEvent&lt;Customer&gt;&gt;
/// {
///     public Task HandleAsync(EntityCreatedEvent&lt;Customer&gt; @event, CancellationToken ct)
///     {
///         Console.WriteLine($"Customer {@event.Entity.Name} was created at {@event.OccurredAt}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record EntityCreatedEvent<TEntity> : ApplicationEventBase, IEntityApplicationEvent<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntityCreatedEvent.
    /// </summary>
    /// <param name="entity">The entity that was created.</param>
    public EntityCreatedEvent(TEntity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    /// <inheritdoc />
    public TEntity Entity { get; init; }

    /// <inheritdoc />
    public EntityOperationType OperationType => EntityOperationType.Created;
}

/// <summary>
/// Event raised when an existing entity is updated.
/// </summary>
/// <typeparam name="TEntity">The type of entity that was updated.</typeparam>
/// <example>
/// <code>
/// // Handling order updates
/// public class OrderUpdatedHandler : IApplicationEventHandler&lt;EntityUpdatedEvent&lt;Order&gt;&gt;
/// {
///     public Task HandleAsync(EntityUpdatedEvent&lt;Order&gt; @event, CancellationToken ct)
///     {
///         Console.WriteLine($"Order {@event.Entity.Id} was updated. Status: {@event.Entity.Status}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record EntityUpdatedEvent<TEntity> : ApplicationEventBase, IEntityApplicationEvent<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntityUpdatedEvent.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    public EntityUpdatedEvent(TEntity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    /// <summary>
    /// Initializes a new instance of the EntityUpdatedEvent with before/after state.
    /// </summary>
    /// <param name="entity">The entity after update.</param>
    /// <param name="originalEntity">The entity before update (optional).</param>
    public EntityUpdatedEvent(TEntity entity, TEntity? originalEntity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        OriginalEntity = originalEntity;
    }

    /// <inheritdoc />
    public TEntity Entity { get; init; }

    /// <summary>
    /// The original entity state before the update (if captured).
    /// </summary>
    public TEntity? OriginalEntity { get; init; }

    /// <inheritdoc />
    public EntityOperationType OperationType => EntityOperationType.Updated;

    /// <summary>
    /// Indicates whether the original state is available for comparison.
    /// </summary>
    public bool HasOriginalState => OriginalEntity != null;
}

/// <summary>
/// Event raised when an entity is deleted.
/// </summary>
/// <typeparam name="TEntity">The type of entity that was deleted.</typeparam>
/// <example>
/// <code>
/// // Handling product deletion
/// public class ProductDeletedHandler : IApplicationEventHandler&lt;EntityDeletedEvent&lt;Product&gt;&gt;
/// {
///     public Task HandleAsync(EntityDeletedEvent&lt;Product&gt; @event, CancellationToken ct)
///     {
///         Console.WriteLine($"Product {@event.Entity.Name} was deleted at {@event.OccurredAt}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record EntityDeletedEvent<TEntity> : ApplicationEventBase, IEntityApplicationEvent<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntityDeletedEvent.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    public EntityDeletedEvent(TEntity entity)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    /// <inheritdoc />
    public TEntity Entity { get; init; }

    /// <inheritdoc />
    public EntityOperationType OperationType => EntityOperationType.Deleted;
}

/// <summary>
/// Event raised when multiple entities are created in a batch operation.
/// </summary>
/// <typeparam name="TEntity">The type of entities that were created.</typeparam>
public record EntitiesCreatedEvent<TEntity> : ApplicationEventBase
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntitiesCreatedEvent.
    /// </summary>
    /// <param name="entities">The entities that were created.</param>
    public EntitiesCreatedEvent(IReadOnlyList<TEntity> entities)
    {
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }

    /// <summary>
    /// The entities that were created.
    /// </summary>
    public IReadOnlyList<TEntity> Entities { get; init; }

    /// <summary>
    /// The count of entities created.
    /// </summary>
    public int Count => Entities.Count;
}

/// <summary>
/// Event raised when multiple entities are updated in a batch operation.
/// </summary>
/// <typeparam name="TEntity">The type of entities that were updated.</typeparam>
public record EntitiesUpdatedEvent<TEntity> : ApplicationEventBase
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntitiesUpdatedEvent.
    /// </summary>
    /// <param name="entities">The entities that were updated.</param>
    public EntitiesUpdatedEvent(IReadOnlyList<TEntity> entities)
    {
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }

    /// <summary>
    /// The entities that were updated.
    /// </summary>
    public IReadOnlyList<TEntity> Entities { get; init; }

    /// <summary>
    /// The count of entities updated.
    /// </summary>
    public int Count => Entities.Count;
}

/// <summary>
/// Event raised when multiple entities are deleted in a batch operation.
/// </summary>
/// <typeparam name="TEntity">The type of entities that were deleted.</typeparam>
public record EntitiesDeletedEvent<TEntity> : ApplicationEventBase
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the EntitiesDeletedEvent.
    /// </summary>
    /// <param name="entities">The entities that were deleted.</param>
    public EntitiesDeletedEvent(IReadOnlyList<TEntity> entities)
    {
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }

    /// <summary>
    /// The entities that were deleted.
    /// </summary>
    public IReadOnlyList<TEntity> Entities { get; init; }

    /// <summary>
    /// The count of entities deleted.
    /// </summary>
    public int Count => Entities.Count;
}

