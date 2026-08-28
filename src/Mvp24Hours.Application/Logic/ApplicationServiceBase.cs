//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.Logic.Internal;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic;

/// <summary>
/// Abstract base class for application services that provides a unified implementation
/// of query and command operations using repository and unit of work patterns.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class provides a complete implementation of <see cref="IApplicationService{TEntity}"/>,
/// combining both query and command operations in a single service.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Unified Query + Command operations</item>
/// <item>FluentValidation integration for entity validation</item>
/// <item>Telemetry logging for all operations</item>
/// <item>Transaction management via Unit of Work</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// public class CustomerService : ApplicationServiceBase&lt;Customer, MyDbContext&gt;
/// {
///     public CustomerService(MyDbContext unitOfWork) : base(unitOfWork) { }
///     
///     // Add custom business logic here
///     public IBusinessResult&lt;Customer&gt; FindByEmail(string email)
///     {
///         return GetBy(c => c.Email == email);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IApplicationService{TEntity}"/>
/// <seealso cref="RepositoryService{TEntity, TUoW}"/>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
/// </remarks>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="validator">The validator for entity validation.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
public abstract class ApplicationServiceBase<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger? logger) : IApplicationService<TEntity>, IReadOnlyApplicationService<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWork
{
    #region [ Properties / Fields ]

    private readonly ServiceOperations<TEntity> _operations = new(unitOfWork, validator, logger ?? NullLogger.Instance);
    private readonly TUoW _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <summary>
    /// Gets the unit of work instance for managing transactions.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance for data access operations.
    /// </summary>
    protected virtual IRepository<TEntity> Repository => _operations.Repository;

    /// <summary>
    /// Gets the validator instance for entity validation.
    /// </summary>
    protected virtual IValidator<TEntity>? Validator => _operations.Validator;

    /// <summary>
    /// Gets the logger instance for logging operations. Never <see langword="null"/>:
    /// falls back to <see cref="NullLogger.Instance"/> when no logger is supplied.
    /// </summary>
    protected virtual ILogger Logger => _operations.Logger;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected ApplicationServiceBase(TUoW unitOfWork)
        : this(unitOfWork, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="validator">The validator for entity validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected ApplicationServiceBase(TUoW unitOfWork, IValidator<TEntity>? validator)
        : this(unitOfWork, validator, null)
    {
    }

    #endregion

    #region [ IQueryService Implementation ]

    /// <inheritdoc/>
    public virtual IBusinessResult<bool> ListAny()
    {
        return _operations.ListAny(GetType().Name);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> ListCount()
    {
        return _operations.ListCount(GetType().Name);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<IList<TEntity>> List()
    {
        return List(null);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<IList<TEntity>> List(IPagingCriteria? criteria)
    {
        return _operations.List(GetType().Name, criteria);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
    {
        return _operations.GetByAny(GetType().Name, clause);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
    {
        return _operations.GetByCount(GetType().Name, clause);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause)
    {
        return GetBy(clause, null);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
    {
        return _operations.GetBy(GetType().Name, clause, criteria);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<TEntity?> GetById(object id)
    {
        return GetById(id, null);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<TEntity?> GetById(object id, IPagingCriteria? criteria)
    {
        return _operations.GetById(GetType().Name, id, criteria);
    }

    #endregion

    #region [ ICommandService Implementation ]

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Add(TEntity entity)
    {
        return _operations.Add(GetType().Name, entity);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Add(IList<TEntity> entities)
    {
        return _operations.Add(GetType().Name, entities);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Modify(TEntity entity)
    {
        return _operations.Modify(GetType().Name, entity);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Modify(IList<TEntity> entities)
    {
        return _operations.Modify(GetType().Name, entities);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Remove(TEntity entity)
    {
        return _operations.Remove(GetType().Name, entity);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
    {
        return _operations.Remove(GetType().Name, entities);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> RemoveById(object id)
    {
        return _operations.RemoveById(GetType().Name, id);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> RemoveById(IList<object> ids)
    {
        return _operations.RemoveById(GetType().Name, ids);
    }

    #endregion

    #region [ Specification Pattern Implementation ]

    /// <inheritdoc/>
    public virtual IBusinessResult<bool> AnyBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.AnyBySpecification(GetType().Name, specification);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<int> CountBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.CountBySpecification(GetType().Name, specification);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<IList<TEntity>> GetBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetBySpecification(GetType().Name, specification, CreatePagingCriteriaFromSpecification);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<TEntity?> GetSingleBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetSingleBySpecification(GetType().Name, specification);
    }

    /// <inheritdoc/>
    public virtual IBusinessResult<TEntity?> GetFirstBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetFirstBySpecification(GetType().Name, specification);
    }

    /// <summary>
    /// Creates paging criteria from an enhanced specification.
    /// </summary>
    /// <param name="specification">The enhanced specification with paging info.</param>
    /// <returns>A paging criteria object, or null if no paging is configured.</returns>
    protected virtual IPagingCriteria? CreatePagingCriteriaFromSpecification(ISpecificationQueryEnhanced<TEntity> specification)
    {
        if (!specification.IsPagingEnabled && (specification.OrderBy == null || specification.OrderBy.Count == 0))
        {
            return null;
        }

        // Create a simple paging criteria from the specification
        // Note: This is a simplified implementation. Full include/ordering support
        // requires repository-level Specification support.
        return new SpecificationPagingCriteria(specification.Skip, specification.Take);
    }

    #endregion
}

/// <summary>
/// Simple paging criteria implementation for specification-based queries.
/// </summary>
internal class SpecificationPagingCriteria(int? skip, int? take) : IPagingCriteria
{
    public int Offset { get; } = skip ?? 0;
    public int Limit { get; } = take ?? 0;
    public IReadOnlyCollection<string>? OrderBy { get; }
    public IReadOnlyCollection<string>? Navigation { get; }
}

