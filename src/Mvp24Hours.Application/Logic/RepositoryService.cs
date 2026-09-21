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
/// Base service for using repository and unit of work
/// </summary>
/// <typeparam name="TEntity">Represents an entity</typeparam>
/// <typeparam name="TUoW">Represents the unit of work</typeparam>
/// <remarks>
/// 
/// </remarks>
public class RepositoryService<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger<RepositoryService<TEntity, TUoW>>? logger = null) : IQueryService<TEntity>, ICommandService<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWork
{
    #region [ Properties / Fields ]

    private readonly IRepository<TEntity> repository = unitOfWork.GetRepository<TEntity>();
    private readonly TUoW unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IValidator<TEntity>? validator = validator;
    private readonly ILogger<RepositoryService<TEntity, TUoW>> _logger = logger ?? NullLogger<RepositoryService<TEntity, TUoW>>.Instance;

    /// <summary>
    /// Gets unit of work instance
    /// </summary>
    /// <returns>T</returns>
    protected virtual TUoW UnitOfWork => unitOfWork;

    /// <summary>
    /// Gets repository instance
    /// </summary>
    /// <returns>T</returns>
    protected virtual IRepository<TEntity> Repository => repository;

    /// <summary>
    /// Defines a validator for a particular type.
    /// </summary>
    protected virtual IValidator<TEntity>? Validator => validator;

    /// <summary>
    /// Gets the logger instance for logging operations. Never <see langword="null"/>:
    /// falls back to <see cref="NullLogger{T}.Instance"/> when no logger is supplied.
    /// </summary>
    protected virtual ILogger<RepositoryService<TEntity, TUoW>> Logger => _logger;

    #endregion

    #region [ Ctor ]
    /// <summary>
    /// 
    /// </summary>
    public RepositoryService(TUoW unitOfWork)
        : this(unitOfWork, null, null)
    {
    }
    #endregion

    #region [ Implements IQueryService ]

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.ListAny()"/>
    /// </summary>
    public virtual IBusinessResult<bool> ListAny()
    {
        _logger.LogDebug("[{ServiceName}] Executing ListAny for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .ListAny()
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.ListCount()"/>
    /// </summary>
    public virtual IBusinessResult<int> ListCount()
    {
        _logger.LogDebug("[{ServiceName}] Executing ListCount for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .ListCount()
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.List()"/>
    /// </summary>
    public virtual IBusinessResult<IList<TEntity>> List()
    {
        return List(null);
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.List(IPagingCriteria)"/>
    /// </summary>
    public virtual IBusinessResult<IList<TEntity>> List(IPagingCriteria? criteria)
    {
        _logger.LogDebug("[{ServiceName}] Executing List for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .List(criteria)
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetByAny(Expression{Func{T, bool}})"/>
    /// </summary>
    public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByAny for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByAny(clause)
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetByCount(Expression{Func{T, bool}})"/>
    /// </summary>
    public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetByCount for {EntityType}", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetByCount(clause)
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetBy(Expression{Func{T, bool}})"/>
    /// </summary>
    public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause)
    {
        return GetBy(clause, null);
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetBy(Expression{Func{T, bool}}, IPagingCriteria)"/>
    /// </summary>
    public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetBy for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetBy(clause, criteria)
            .ToBusiness();
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetById(object)"/>
    /// </summary>
    public virtual IBusinessResult<TEntity?> GetById(object id)
    {
        return GetById(id, null);
    }

    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Logic.IQueryService{T}.GetById(object, IPagingCriteria)"/>
    /// </summary>
    public virtual IBusinessResult<TEntity?> GetById(object id, IPagingCriteria? criteria)
    {
        _logger.LogDebug("[{ServiceName}] Executing GetById for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
        return UnitOfWork
            .GetRepository<TEntity>()
            .GetById(id, criteria)
            .ToBusiness();
    }

    #endregion

    #region [ Implements ICommandService ]

    public virtual IBusinessResult<int> Add(TEntity entity)
    {
        _logger.LogDebug("[{ServiceName}] Executing Add for {EntityType}", GetType().Name, typeof(TEntity).Name);
        IList<IMessageResult> errors = entity.TryValidate(Validator);
        if (!errors.AnySafe())
        {
            UnitOfWork
                .GetRepository<TEntity>()
                .Add(entity);
            return UnitOfWork.SaveChanges()
                .ToBusiness();
        }
        return errors.ToBusiness<int>();
    }

    public virtual IBusinessResult<int> Add(IList<TEntity> entities)
    {
        _logger.LogDebug("[{ServiceName}] Executing Add for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
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

        IRepository<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        foreach (TEntity entity in entities)
        {
            rep.Add(entity);
        }
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    public virtual IBusinessResult<int> Modify(TEntity entity)
    {
        _logger.LogDebug("[{ServiceName}] Executing Modify for {EntityType}", GetType().Name, typeof(TEntity).Name);
        IList<IMessageResult> errors = entity.TryValidate(Validator);
        if (!errors.AnySafe())
        {
            UnitOfWork
                .GetRepository<TEntity>()
                .Modify(entity);
            return UnitOfWork.SaveChanges()
                .ToBusiness();
        }
        return errors.ToBusiness<int>();
    }

    public virtual IBusinessResult<int> Modify(IList<TEntity> entities)
    {
        _logger.LogDebug("[{ServiceName}] Executing Modify for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        IRepository<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        foreach (TEntity entity in entities)
        {
            rep.Modify(entity);
        }
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    public virtual IBusinessResult<int> Remove(TEntity entity)
    {
        _logger.LogDebug("[{ServiceName}] Executing Remove for {EntityType}", GetType().Name, typeof(TEntity).Name);
        UnitOfWork.GetRepository<TEntity>().Remove(entity);
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
    {
        _logger.LogDebug("[{ServiceName}] Executing Remove for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);
        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        IRepository<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        foreach (TEntity entity in entities)
        {
            rep.Remove(entity);
        }
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    public virtual IBusinessResult<int> RemoveById(object id)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveById for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
        UnitOfWork.GetRepository<TEntity>().RemoveById(id);
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    public virtual IBusinessResult<int> RemoveById(IList<object> ids)
    {
        _logger.LogDebug("[{ServiceName}] Executing RemoveById for {Count} {EntityType} entities", GetType().Name, ids?.Count ?? 0, typeof(TEntity).Name);
        if (!ids.AnySafe())
        {
            return 0.ToBusiness();
        }

        IRepository<TEntity> rep = UnitOfWork.GetRepository<TEntity>();
        foreach (object id in ids)
        {
            rep.RemoveById(id);
        }
        return UnitOfWork.SaveChanges()
            .ToBusiness();
    }

    #endregion
}
