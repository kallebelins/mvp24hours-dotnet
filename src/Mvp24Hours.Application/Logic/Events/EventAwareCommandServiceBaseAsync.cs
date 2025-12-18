//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Events;

/// <summary>
/// Asynchronous command service with automatic event dispatching support.
/// Automatically dispatches EntityCreated, EntityUpdated, and EntityDeleted events
/// after successful command operations.
/// </summary>
/// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> to automatically
/// dispatch application events after successful CRUD operations.
/// </para>
/// <para>
/// <strong>Events Dispatched:</strong>
/// <list type="bullet">
/// <item><see cref="EntityCreatedEvent{TEntity}"/> - After Add operations</item>
/// <item><see cref="EntityUpdatedEvent{TEntity}"/> - After Modify operations</item>
/// <item><see cref="EntityDeletedEvent{TEntity}"/> - After Remove operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// public class OrderCommandService : EventAwareCommandServiceBaseAsync&lt;Order, MyDbContext&gt;
/// {
///     public OrderCommandService(
///         MyDbContext unitOfWork,
///         IApplicationEventDispatcher eventDispatcher,
///         IValidator&lt;Order&gt;? validator = null,
///         ILogger&lt;OrderCommandService&gt;? logger = null)
///         : base(unitOfWork, eventDispatcher, validator, logger) { }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class EventAwareCommandServiceBaseAsync<TEntity, TUoW>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Fields ]

    private readonly IRepositoryAsync<TEntity> _repository;
    private readonly TUoW _unitOfWork;
    private readonly IValidator<TEntity>? _validator;
    private readonly IApplicationEventDispatcher _eventDispatcher;
    private readonly ILogger? _logger;

    #endregion

    #region [ Properties ]

    /// <summary>
    /// Gets the unit of work instance for managing transactions.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance for data access operations.
    /// </summary>
    protected virtual IRepositoryAsync<TEntity> Repository => _repository;

    /// <summary>
    /// Gets the validator instance for entity validation.
    /// </summary>
    protected virtual IValidator<TEntity>? Validator => _validator;

    /// <summary>
    /// Gets the event dispatcher for dispatching application events.
    /// </summary>
    protected virtual IApplicationEventDispatcher EventDispatcher => _eventDispatcher;

    /// <summary>
    /// Gets or sets whether to dispatch events. Default is true.
    /// </summary>
    protected virtual bool DispatchEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the correlation ID for events. Optional.
    /// </summary>
    protected virtual string? CorrelationId { get; set; }

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the EventAwareCommandServiceBaseAsync.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="eventDispatcher">The event dispatcher for dispatching events.</param>
    /// <param name="validator">Optional validator for entity validation.</param>
    /// <param name="logger">Optional logger.</param>
    protected EventAwareCommandServiceBaseAsync(
        TUoW unitOfWork,
        IApplicationEventDispatcher eventDispatcher,
        IValidator<TEntity>? validator = null,
        ILogger? logger = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _repository = unitOfWork.GetRepository<TEntity>();
        _validator = validator;
        _logger = logger;
    }

    #endregion

    #region [ Add Operations ]

    /// <summary>
    /// Adds a new entity and dispatches an EntityCreatedEvent.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-addasync");

        var errors = entity.TryValidate(_validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await _repository.AddAsync(entity, cancellationToken: cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchCreatedEventAsync(entity, cancellationToken);
        }

        return result.ToBusiness();
    }

    /// <summary>
    /// Adds multiple entities and dispatches EntitiesCreatedEvent.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-addlistasync");

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (var entity in entities)
        {
            var errors = entity.TryValidate(_validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }

        await Task.WhenAll(entities.Select(entity => _repository.AddAsync(entity, cancellationToken: cancellationToken)));
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchCreatedEventsAsync(entities, cancellationToken);
        }

        return result.ToBusiness();
    }

    #endregion

    #region [ Modify Operations ]

    /// <summary>
    /// Modifies an entity and dispatches an EntityUpdatedEvent.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-modifyasync");

        var errors = entity.TryValidate(_validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await _repository.ModifyAsync(entity, cancellationToken: cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchUpdatedEventAsync(entity, cancellationToken);
        }

        return result.ToBusiness();
    }

    /// <summary>
    /// Modifies multiple entities and dispatches EntitiesUpdatedEvent.
    /// </summary>
    /// <param name="entities">The entities to modify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-modifylistasync");

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (var entity in entities)
        {
            var errors = entity.TryValidate(_validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }

        await Task.WhenAll(entities.Select(entity => _repository.ModifyAsync(entity, cancellationToken: cancellationToken)));
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchUpdatedEventsAsync(entities, cancellationToken);
        }

        return result.ToBusiness();
    }

    #endregion

    #region [ Remove Operations ]

    /// <summary>
    /// Removes an entity and dispatches an EntityDeletedEvent.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-removeasync");

        await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchDeletedEventAsync(entity, cancellationToken);
        }

        return result.ToBusiness();
    }

    /// <summary>
    /// Removes multiple entities and dispatches EntitiesDeletedEvent.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-removelistasync");

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        await Task.WhenAll(entities.Select(entity => _repository.RemoveAsync(entity, cancellationToken: cancellationToken)));
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result > 0 && DispatchEvents)
        {
            await DispatchDeletedEventsAsync(entities, cancellationToken);
        }

        return result.ToBusiness();
    }

    /// <summary>
    /// Removes an entity by ID and dispatches an EntityDeletedEvent.
    /// Note: This method cannot dispatch the event with the full entity unless it's loaded first.
    /// </summary>
    /// <param name="id">The ID of the entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A business result with the number of affected rows.</returns>
    public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-eventaware-commandservice-removebyidasync");

        await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        // Note: Cannot dispatch EntityDeletedEvent without loading the entity first
        // If you need the event, use RemoveAsync(entity) instead

        return result.ToBusiness();
    }

    #endregion

    #region [ Event Dispatching ]

    /// <summary>
    /// Dispatches an EntityCreatedEvent for the specified entity.
    /// </summary>
    /// <param name="entity">The entity that was created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchCreatedEventAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var @event = new EntityCreatedEvent<TEntity>(entity)
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntityCreatedEvent for {EntityType}",
            typeof(TEntity).Name);
    }

    /// <summary>
    /// Dispatches an EntitiesCreatedEvent for the specified entities.
    /// </summary>
    /// <param name="entities">The entities that were created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchCreatedEventsAsync(IList<TEntity> entities, CancellationToken cancellationToken)
    {
        var @event = new EntitiesCreatedEvent<TEntity>(entities.ToList())
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntitiesCreatedEvent for {Count} {EntityType} entities",
            entities.Count,
            typeof(TEntity).Name);
    }

    /// <summary>
    /// Dispatches an EntityUpdatedEvent for the specified entity.
    /// </summary>
    /// <param name="entity">The entity that was updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchUpdatedEventAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var @event = new EntityUpdatedEvent<TEntity>(entity)
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntityUpdatedEvent for {EntityType}",
            typeof(TEntity).Name);
    }

    /// <summary>
    /// Dispatches an EntitiesUpdatedEvent for the specified entities.
    /// </summary>
    /// <param name="entities">The entities that were updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchUpdatedEventsAsync(IList<TEntity> entities, CancellationToken cancellationToken)
    {
        var @event = new EntitiesUpdatedEvent<TEntity>(entities.ToList())
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntitiesUpdatedEvent for {Count} {EntityType} entities",
            entities.Count,
            typeof(TEntity).Name);
    }

    /// <summary>
    /// Dispatches an EntityDeletedEvent for the specified entity.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchDeletedEventAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var @event = new EntityDeletedEvent<TEntity>(entity)
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntityDeletedEvent for {EntityType}",
            typeof(TEntity).Name);
    }

    /// <summary>
    /// Dispatches an EntitiesDeletedEvent for the specified entities.
    /// </summary>
    /// <param name="entities">The entities that were deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task DispatchDeletedEventsAsync(IList<TEntity> entities, CancellationToken cancellationToken)
    {
        var @event = new EntitiesDeletedEvent<TEntity>(entities.ToList())
        {
            CorrelationId = CorrelationId
        };

        await _eventDispatcher.DispatchAsync(@event, cancellationToken);

        _logger?.LogDebug(
            "[EventAwareCommandService] Dispatched EntitiesDeletedEvent for {Count} {EntityType} entities",
            entities.Count,
            typeof(TEntity).Name);
    }

    #endregion
}

