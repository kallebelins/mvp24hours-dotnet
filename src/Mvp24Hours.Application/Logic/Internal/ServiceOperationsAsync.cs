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
/// Single implementation of the asynchronous query, command and specification operations shared by
/// <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/>, <see cref="QueryServiceBaseAsync{TEntity, TUoW}"/>
/// and <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type handled by the owning service.</typeparam>
/// <remarks>
/// <para>
/// The service bases own the public surface — signatures, <c>virtual</c> modifiers, the
/// <c>protected</c> extension points and which operations are exposed at all — and delegate the
/// body of each operation to this type. Logging, validation and <c>SaveChangesAsync</c> semantics
/// therefore cannot drift between the bases.
/// </para>
/// <para>
/// Read-only enforcement lives on the service base, not here: <c>QueryServiceBaseAsync</c> simply
/// does not expose the command members, even though this internal type declares them for reuse by
/// <c>ApplicationServiceBaseAsync</c>.
/// </para>
/// <para>
/// Overloads that only supply a default argument (for example <c>ListAsync(ct)</c> calling
/// <c>ListAsync(null, ct)</c>) are deliberately <strong>not</strong> represented here: they must keep
/// calling the virtual sibling on the service instance so that a derived class overriding the richer
/// overload still intercepts the call. <c>CacheableQueryServiceBaseAsync</c> relies on exactly that.
/// </para>
/// <para>
/// <c>serviceName</c> is supplied per call rather than captured, preserving the original
/// <c>GetType().Name</c> evaluation performed by each operation.
/// </para>
/// </remarks>
/// <param name="unitOfWork">The unit of work supplied to the owning service.</param>
/// <param name="validator">The optional entity validator supplied to the owning service.</param>
/// <param name="logger">The resolved logger of the owning service. Never <see langword="null"/>.</param>
internal sealed class ServiceOperationsAsync<TEntity>(IUnitOfWorkAsync unitOfWork, IValidator<TEntity>? validator, ILogger logger)
    where TEntity : class, IEntityBase
{
    #region [ Properties ]

    /// <summary>
    /// Gets the repository resolved once from the unit of work.
    /// </summary>
    internal IRepositoryAsync<TEntity> Repository { get; } = unitOfWork.GetRepository<TEntity>();

    /// <summary>
    /// Gets the optional entity validator.
    /// </summary>
    internal IValidator<TEntity>? Validator => validator;

    /// <summary>
    /// Gets the logger used by every operation.
    /// </summary>
    internal ILogger Logger => logger;

    #endregion

    #region [ IQueryServiceAsync ]

    internal Task<IBusinessResult<bool>> ListAnyAsync(string serviceName, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing ListAnyAsync for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<int>> ListCountAsync(string serviceName, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing ListCountAsync for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.ListCountAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<IList<TEntity>>> ListAsync(string serviceName, IPagingCriteria? criteria, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing ListAsync for {EntityType} with criteria", serviceName, typeof(TEntity).Name);
        return Repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<bool>> GetByAnyAsync(string serviceName, Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByAnyAsync for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<int>> GetByCountAsync(string serviceName, Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByCountAsync for {EntityType}", serviceName, typeof(TEntity).Name);
        return Repository.GetByCountAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<IList<TEntity>>> GetByAsync(string serviceName, Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByAsync for {EntityType} with criteria", serviceName, typeof(TEntity).Name);
        return Repository.GetByAsync(clause, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal Task<IBusinessResult<TEntity?>> GetByIdAsync(string serviceName, object id, IPagingCriteria? criteria, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing GetByIdAsync for {EntityType} with Id={Id}", serviceName, typeof(TEntity).Name, id);
        return Repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    #endregion

    #region [ ICommandServiceAsync ]

    internal async Task<IBusinessResult<int>> AddAsync(string serviceName, TEntity entity, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing AddAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        IList<IMessageResult> errors = entity.TryValidate(validator);
        if (!errors.AnySafe())
        {
            await Repository.AddAsync(entity, cancellationToken: cancellationToken);
            return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }
        return errors.ToBusiness<int>();
    }

    internal async Task<IBusinessResult<int>> AddAsync(string serviceName, IList<TEntity> entities, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing AddAsync for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

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

        await Task.WhenAll(entities.Select(entity => Repository.AddAsync(entity, cancellationToken: cancellationToken)));
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> ModifyAsync(string serviceName, TEntity entity, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing ModifyAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        IList<IMessageResult> errors = entity.TryValidate(validator);
        if (!errors.AnySafe())
        {
            await Repository.ModifyAsync(entity, cancellationToken: cancellationToken);
            return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }
        return errors.ToBusiness<int>();
    }

    internal async Task<IBusinessResult<int>> ModifyAsync(string serviceName, IList<TEntity> entities, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing ModifyAsync for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

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

        await Task.WhenAll(entities.Select(entity => Repository.ModifyAsync(entity, cancellationToken: cancellationToken)));
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> RemoveAsync(string serviceName, TEntity entity, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveAsync for {EntityType}", serviceName, typeof(TEntity).Name);
        await Repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> RemoveAsync(string serviceName, IList<TEntity> entities, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveAsync for {Count} {EntityType} entities", serviceName, entities?.Count ?? 0, typeof(TEntity).Name);

        if (!entities.AnySafe())
        {
            return 0.ToBusiness();
        }

        await Task.WhenAll(entities.Select(entity => Repository.RemoveAsync(entity, cancellationToken: cancellationToken)));
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> RemoveByIdAsync(string serviceName, object id, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {EntityType} with Id={Id}", serviceName, typeof(TEntity).Name, id);
        await Repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> RemoveByIdAsync(string serviceName, IList<object> ids, CancellationToken cancellationToken)
    {
        logger.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {Count} {EntityType} entities", serviceName, ids?.Count ?? 0, typeof(TEntity).Name);

        if (!ids.AnySafe())
        {
            return 0.ToBusiness();
        }

        await Task.WhenAll(ids.Select(id => Repository.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
        return await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
    }

    #endregion

    #region [ Specification Pattern ]

    internal async Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(string serviceName, TSpec specification, CancellationToken cancellationToken)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing AnyBySpecificationAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return false.ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
        {
            return (await readOnlyRepo.AnyBySpecificationAsync(specification, cancellationToken)).ToBusiness();
        }

        // Fallback: use the specification's expression directly
        return await Repository.GetByAnyAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(string serviceName, TSpec specification, CancellationToken cancellationToken)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing CountBySpecificationAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return 0.ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
        {
            return (await readOnlyRepo.CountBySpecificationAsync(specification, cancellationToken)).ToBusiness();
        }

        // Fallback: use the specification's expression directly
        return await Repository.GetByCountAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    /// <param name="serviceName">The runtime type name of the owning service, used in log messages.</param>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="pagingCriteriaFactory">
    /// Optional factory invoked when the specification is an <see cref="ISpecificationQueryEnhanced{TEntity}"/>.
    /// Services that do not translate specifications into paging criteria pass <see langword="null"/>,
    /// and the fallback query then runs without paging criteria.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(
        string serviceName,
        TSpec specification,
        Func<ISpecificationQueryEnhanced<TEntity>, IPagingCriteria?>? pagingCriteriaFactory,
        CancellationToken cancellationToken)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetBySpecificationAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((IList<TEntity>)[]).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
        {
            return (await readOnlyRepo.GetBySpecificationAsync(specification, cancellationToken)).ToBusiness();
        }

        // Fallback: use the specification's expression directly with paging if available
        IPagingCriteria? pagingCriteria = null;
        if (pagingCriteriaFactory != null && specification is ISpecificationQueryEnhanced<TEntity> enhancedSpec)
        {
            pagingCriteria = pagingCriteriaFactory(enhancedSpec);
        }

        return await Repository.GetByAsync(specification.IsSatisfiedByExpression, pagingCriteria, cancellationToken: cancellationToken).ToBusinessAsync();
    }

    internal async Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(string serviceName, TSpec specification, CancellationToken cancellationToken)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetSingleBySpecificationAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((TEntity?)null).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
        {
            return (await readOnlyRepo.GetSingleBySpecificationAsync(specification, cancellationToken)).ToBusiness();
        }

        // Fallback: get by expression and take single
        IList<TEntity> result = await Repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
        TEntity? entity = result?.SingleOrDefault();
        return entity.ToBusiness();
    }

    internal async Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(string serviceName, TSpec specification, CancellationToken cancellationToken)
        where TSpec : ISpecificationQuery<TEntity>
    {
        logger.LogDebug("[{ServiceName}] Executing GetFirstBySpecificationAsync for {EntityType}", serviceName, typeof(TEntity).Name);

        if (specification == null)
        {
            return ((TEntity?)null).ToBusiness();
        }

        // Try to use repository's specification method if available
        if (Repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
        {
            return (await readOnlyRepo.GetFirstBySpecificationAsync(specification, cancellationToken)).ToBusiness();
        }

        // Fallback: get by expression and take first
        IList<TEntity> result = await Repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
        TEntity? entity = result?.FirstOrDefault();
        return entity.ToBusiness();
    }

    #endregion
}
