//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Logic.Internal;

/// <summary>
/// Single implementation of the synchronous query, command and specification operations shared by
/// <see cref="ApplicationServiceBase{TEntity, TUoW}"/>, <see cref="QueryServiceBase{TEntity, TUoW}"/>
/// and <see cref="CommandServiceBase{TEntity, TUoW}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type handled by the owning service.</typeparam>
/// <remarks>
/// <para>
/// The service bases own the public surface — signatures, <c>virtual</c> modifiers, the
/// <c>protected</c> extension points and which operations are exposed at all — and delegate the
/// body of each operation to this type. Logging, validation and <c>SaveChanges</c> semantics
/// therefore cannot drift between the bases.
/// </para>
/// <para>
/// Read-only enforcement lives on the service base, not here: <c>QueryServiceBase</c> simply does
/// not expose the command members, even though this internal type declares them for reuse by
/// <c>ApplicationServiceBase</c>.
/// </para>
/// <para>
/// Overloads that only supply a default argument (for example <c>List()</c> calling
/// <c>List(null)</c>) are deliberately <strong>not</strong> represented here: they must keep calling
/// the virtual sibling on the service instance so that a derived class overriding the richer
/// overload still intercepts the call.
/// </para>
/// <para>
/// <c>serviceName</c> is supplied per call rather than captured, preserving the original
/// <c>GetType().Name</c> evaluation performed by each operation.
/// </para>
/// </remarks>
/// <param name="unitOfWork">The unit of work supplied to the owning service.</param>
/// <param name="validator">The optional entity validator supplied to the owning service.</param>
/// <param name="logger">The resolved logger of the owning service. Never <see langword="null"/>.</param>
internal sealed class ServiceOperations<TEntity>(IUnitOfWork unitOfWork, IValidator<TEntity>? validator, ILogger logger)
    where TEntity : class, IEntityBase
{
    #region [ Properties ]

    /// <summary>
    /// Gets the repository resolved once from the unit of work.
    /// </summary>
    internal IRepository<TEntity> Repository { get; } = unitOfWork.GetRepository<TEntity>();

    /// <summary>
    /// Gets the optional entity validator.
    /// </summary>
    internal IValidator<TEntity>? Validator => validator;

    /// <summary>
    /// Gets the logger used by every operation.
    /// </summary>
    internal ILogger Logger => logger;

    #endregion

    #region [ IQueryService ]

    internal IBusinessResult<bool> ListAny(string serviceName)
    {
        logger.LogDebug("[{ServiceName}] Executing ListAny for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.ListAny().ToBusiness();
    }

    internal IBusinessResult<int> ListCount(string serviceName)
    {
        logger.LogDebug("[{ServiceName}] Executing ListCount for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.ListCount().ToBusiness();
    }

    internal IBusinessResult<IList<TEntity>> List(string serviceName, IPagingCriteria? criteria)
    {
        logger.LogDebug("[{ServiceName}] Executing List for {EntityType} with criteria", serviceName, typeof(TEntity).Name);
        return Repository.List(criteria).ToBusiness();
    }

    internal IBusinessResult<bool> GetByAny(string serviceName, Expression<Func<TEntity, bool>> clause)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByAny for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.GetByAny(clause).ToBusiness();
    }

    internal IBusinessResult<int> GetByCount(string serviceName, Expression<Func<TEntity, bool>> clause)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByCount for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.GetByCount(clause).ToBusiness();
    }

    internal IBusinessResult<IList<TEntity>> GetBy(string serviceName, Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
    {
        logger.LogDebug("[{ServiceName}] Executing GetBy for {EntityType} with criteria", serviceName, typeof(TEntity).Name);
        return Repository.GetBy(clause, criteria).ToBusiness();
    }

    internal IBusinessResult<TEntity?> GetById(string serviceName, object id, IPagingCriteria? criteria)
    {
        logger.LogDebug("[{ServiceName}] Executing GetById for {EntityType} with Id={Id}", serviceName, typeof(TEntity).Name, id);
        return Repository.GetById(id, criteria).ToBusiness();
    }

    #endregion

    #region [ ICommandService ]

    internal IBusinessResult<int> Add(string serviceName, TEntity entity)
    {
        logger.LogDebug("[{ServiceName}] Executing Add for {EntityType}", serviceName, typeof(TEntity).Name);

        IList<IMessageResult> errors = entity.TryValidate(validator);
        if (!errors.AnySafe())
        {
            Repository.Add(entity);
            return unitOfWork.SaveChanges().ToBusiness();
        }
        return errors.ToBusiness<int>();
    }

    internal IBusinessResult<int> Add(string serviceName, IList<TEntity> entities)
    {
        logger.LogDebug("[{ServiceName}] Executing Add for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (TEntity entity in entities)
        {
            IList<IMessageResult> errors = entity.TryValidate(validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }

        foreach (TEntity entity in entities)
        {
            Repository.Add(entity);
        }

        return unitOfWork.SaveChanges().ToBusiness();
    }

    internal IBusinessResult<int> Modify(string serviceName, TEntity entity)
    {
        logger.LogDebug("[{ServiceName}] Executing Modify for {EntityType}", serviceName, typeof(TEntity).Name);

        IList<IMessageResult> errors = entity.TryValidate(validator);
        if (!errors.AnySafe())
        {
            Repository.Modify(entity);
            return unitOfWork.SaveChanges().ToBusiness();
        }
        return errors.ToBusiness<int>();
    }

    internal IBusinessResult<int> Modify(string serviceName, IList<TEntity> entities)
    {
        logger.LogDebug("[{ServiceName}] Executing Modify for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (TEntity entity in entities)
        {
            IList<IMessageResult> errors = entity.TryValidate(validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }
        }

        foreach (TEntity entity in entities)
        {
            Repository.Modify(entity);
        }

        return unitOfWork.SaveChanges().ToBusiness();
    }

    internal IBusinessResult<int> Remove(string serviceName, TEntity entity)
    {
        logger.LogDebug("[{ServiceName}] Executing Remove for {EntityType}", serviceName, typeof(TEntity).Name);
        Repository.Remove(entity);
        return unitOfWork.SaveChanges().ToBusiness();
    }

    internal IBusinessResult<int> Remove(string serviceName, IList<TEntity> entities)
    {
        logger.LogDebug("[{ServiceName}] Executing Remove for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (TEntity entity in entities)
        {
            Repository.Remove(entity);
        }

        return unitOfWork.SaveChanges().ToBusiness();
    }

    internal IBusinessResult<int> RemoveById(string serviceName, object id)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveById for {EntityType} with Id={Id}", serviceName, typeof(TEntity).Name, id);
        Repository.RemoveById(id);
        return unitOfWork.SaveChanges().ToBusiness();
    }

    internal IBusinessResult<int> RemoveById(string serviceName, IList<object> ids)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveById for {Count} {EntityType} entities", serviceName, ids?.Count ?? 0, typeof(TEntity).Name);

        if (!ids.AnySafe())
        {
            return 0.ToBusiness();
        }

        foreach (object id in ids)
        {
            Repository.RemoveById(id);
        }

        return unitOfWork.SaveChanges().ToBusiness();
    }

    #endregion

    #region [ Specification Pattern ]

    internal IBusinessResult<bool> AnyBySpecification<TSpec>(string serviceName, TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing AnyBySpecification for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return false.ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepository<TEntity> readOnlyRepo)
        {
            return readOnlyRepo.AnyBySpecification(specification).ToBusiness();
        }

        // Fallback: use the specification's expression directly
        return Repository.GetByAny(specification.IsSatisfiedByExpression).ToBusiness();
    }

    internal IBusinessResult<int> CountBySpecification<TSpec>(string serviceName, TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing CountBySpecification for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return 0.ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepository<TEntity> readOnlyRepo)
        {
            return readOnlyRepo.CountBySpecification(specification).ToBusiness();
        }

        // Fallback: use the specification's expression directly
        return Repository.GetByCount(specification.IsSatisfiedByExpression).ToBusiness();
    }

    /// <param name="serviceName">The runtime type name of the owning service, used in log messages.</param>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="pagingCriteriaFactory">
    /// Optional factory invoked when the specification is an <see cref="ISpecificationQueryEnhanced{TEntity}"/>.
    /// Services that do not translate specifications into paging criteria pass <see langword="null"/>,
    /// and the fallback query then runs without paging criteria.
    /// </param>
    internal IBusinessResult<IList<TEntity>> GetBySpecification<TSpec>(
        string serviceName,
        TSpec specification,
        Func<ISpecificationQueryEnhanced<TEntity>, IPagingCriteria?>? pagingCriteriaFactory)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetBySpecification for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((IList<TEntity>)[]).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepository<TEntity> readOnlyRepo)
        {
            return readOnlyRepo.GetBySpecification(specification).ToBusiness();
        }

        // Fallback: use the specification's expression directly with paging if available
        IPagingCriteria? pagingCriteria = null;
        if (pagingCriteriaFactory != null && specification is ISpecificationQueryEnhanced<TEntity> enhancedSpec)
        {
            pagingCriteria = pagingCriteriaFactory(enhancedSpec);
        }

        return Repository.GetBy(specification.IsSatisfiedByExpression, pagingCriteria).ToBusiness();
    }

    internal IBusinessResult<TEntity?> GetSingleBySpecification<TSpec>(string serviceName, TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetSingleBySpecification for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((TEntity?)null).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepository<TEntity> readOnlyRepo)
        {
            return readOnlyRepo.GetSingleBySpecification(specification).ToBusiness();
        }

        // Fallback: get by expression and take single
        IList<TEntity> result = Repository.GetBy(specification.IsSatisfiedByExpression, null);
        TEntity? entity = result?.SingleOrDefault();
        return entity.ToBusiness();
    }

    internal IBusinessResult<TEntity?> GetFirstBySpecification<TSpec>(string serviceName, TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetFirstBySpecification for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((TEntity?)null).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepository<TEntity> readOnlyRepo)
        {
            return readOnlyRepo.GetFirstBySpecification(specification).ToBusiness();
        }

        // Fallback: get by expression and take first
        IList<TEntity> result = Repository.GetBy(specification.IsSatisfiedByExpression, null);
        TEntity? entity = result?.FirstOrDefault();
        return entity.ToBusiness();
    }

    #endregion
}
