//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Logic;

/// <summary>
/// Asynchronous service for using repository and unit of work
/// </summary>
/// <typeparam name="TEntity">Represents an entity</typeparam>
/// <typeparam name="TUoW">Represents the unit of work</typeparam>
/// <remarks>
/// 
/// </remarks>
public class RepositoryServiceAsync<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger<RepositoryServiceAsync<TEntity, TUoW>>? logger = null) : IQueryServiceAsync<TEntity>, ICommandServiceAsync<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Properties / Fields ]

    private readonly IRepositoryAsync<TEntity> repository = unitOfWork.GetRepository<TEntity>();
    private readonly TUoW unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IValidator<TEntity>? validator = validator;
    private readonly ILogger<RepositoryServiceAsync<TEntity, TUoW>> _logger = logger ?? NullLogger<RepositoryServiceAsync<TEntity, TUoW>>.Instance;

    /// <summary>
    /// Gets unit of work instance
    /// </summary>
    /// <returns>T</returns>
    protected virtual TUoW UnitOfWork => unitOfWork;

    /// <summary>
    /// Gets repository instance
    /// </summary>
    /// <returns>T</returns>
    protected virtual IRepositoryAsync<TEntity> Repository => repository;

    /// <summary>
    /// Defines a validator for a particular type.
    /// </summary>
    protected virtual IValidator<TEntity>? Validator => validator;

    /// <summary>
    /// Gets the logger instance for logging operations. Never <see langword="null"/>:
    /// falls back to <see cref="NullLogger{T}.Instance"/> when no logger is supplied.
    /// </summary>
    protected virtual ILogger<RepositoryServiceAsync<TEntity, TUoW>> Logger => _logger;

    #endregion

    #region [ Ctor ]
    /// <summary>
    /// 
    /// </summary>
    public RepositoryServiceAsync(TUoW unitOfWork)
        : this(unitOfWork, null, null)
    {
    }
    #endregion

    #region [ Implements IQueryServiceAsync ]

    public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing ListAnyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .ListAnyAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing ListCountAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .ListCountAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken: cancellationToken);
    }

    public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing ListAsync for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .ListAsync(criteria, cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByAnyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByAnyAsync(clause, cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByCountAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByCountAsync(clause, cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return GetByAsync(clause, null, cancellationToken: cancellationToken);
    }

    public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByAsync for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByAsync(clause, criteria, cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual Task<IBusinessResult<TEntity?>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, null, cancellationToken: cancellationToken);
    }

    public virtual Task<IBusinessResult<TEntity?>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByIdAsync for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByIdAsync(id, criteria, cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    #endregion

    #region [ Implements ICommandServiceAsync ]

    public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing AddAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        IList<IMessageResult> errors = entity.TryValidate(Validator);
        if (!errors.AnySafe())
        {
            await UnitOfWork
                .GetRepository<TEntity>()
                .AddAsync(entity, cancellationToken: cancellationToken);
            return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }
        return errors.ToBusiness<int>();
    }

    public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing AddAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (TEntity entity in entities)
        {
            IList<IMessageResult> errors = entity.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }
        IRepositoryAsync<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        await Task.WhenAll(entities.Select(entity => rep.AddAsync(entity, cancellationToken: cancellationToken)));
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing ModifyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        IList<IMessageResult> errors = entity.TryValidate(Validator);
        if (!errors.AnySafe())
        {
            await UnitOfWork
                .GetRepository<TEntity>()
                .ModifyAsync(entity, cancellationToken: cancellationToken);
            return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
        }
        return errors.ToBusiness<int>();
    }

    public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing ModifyAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (TEntity entity in entities)
        {
            IList<IMessageResult> errors = entity.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }
        IRepositoryAsync<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        await Task.WhenAll(entities.Select(entity => rep.ModifyAsync(entity, cancellationToken: cancellationToken)));
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
        await UnitOfWork.GetRepository<TEntity>().RemoveAsync(entity, cancellationToken: cancellationToken);
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
    }

    public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        IRepositoryAsync<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        await Task.WhenAll(entities.Select(entity => rep.RemoveAsync(entity, cancellationToken: cancellationToken)));
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
        await UnitOfWork.GetRepository<TEntity>().RemoveByIdAsync(id, cancellationToken: cancellationToken);
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
                .ToBusinessAsync();
    }

    public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {Count} {EntityType} entities", GetType().Name, ids?.Count ?? 0, typeof(TEntity).Name);
        if (!ids.AnySafe())
        {
            return 0.ToBusiness();
        }

        IRepositoryAsync<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        await Task.WhenAll(ids.Select(id => rep.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
        return await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken)
            .ToBusinessAsync();
    }

    #endregion
}
