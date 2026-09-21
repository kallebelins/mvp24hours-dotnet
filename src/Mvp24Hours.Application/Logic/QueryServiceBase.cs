//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
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
/// Query-only service base class implementing the read-side of CQRS pattern.
/// Provides data projection operations without any modification capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity type to be queried by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class implements <see cref="IQueryService{TEntity}"/> and <see cref="IReadOnlyApplicationService{TEntity}"/>,
/// providing read-only access to entities. Use this for CQRS patterns where reads are
/// separated from writes.
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Enforces read-only access at the class level</item>
/// <item>Can be optimized for read operations (no tracking, read replicas)</item>
/// <item>Supports CQRS patterns where reads are separated from writes</item>
/// <item>Improves security by limiting service capabilities</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// // Read-only product catalog service
/// public class ProductCatalogQueryService : QueryServiceBase&lt;Product, MyDbContext&gt;
/// {
///     public ProductCatalogQueryService(MyDbContext unitOfWork) : base(unitOfWork) { }
///     
///     public IBusinessResult&lt;IList&lt;Product&gt;&gt; GetActiveProducts()
///     {
///         return GetBy(p => p.IsActive);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IQueryService{TEntity}"/>
/// <seealso cref="IReadOnlyApplicationService{TEntity}"/>
/// <seealso cref="CommandServiceBase{TEntity, TUoW}"/>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryServiceBase{TEntity, TUoW}"/> class.
/// </remarks>
/// <param name="unitOfWork">The unit of work for data access.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
public abstract class QueryServiceBase<TEntity, TUoW>(TUoW unitOfWork, ILogger? logger) : IQueryService<TEntity>, IReadOnlyApplicationService<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWork
{
    #region [ Properties / Fields ]

    private readonly ServiceOperations<TEntity> _operations = new(unitOfWork, null, logger ?? NullLogger.Instance);
    private readonly TUoW _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <summary>
    /// Gets the unit of work instance.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance for data access operations.
    /// </summary>
    protected virtual IRepository<TEntity> Repository => _operations.Repository;

    /// <summary>
    /// Gets the logger instance for logging operations. Never <see langword="null"/>:
    /// falls back to <see cref="NullLogger.Instance"/> when no logger is supplied.
    /// </summary>
    protected virtual ILogger Logger => _operations.Logger;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryServiceBase{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for data access.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected QueryServiceBase(TUoW unitOfWork)
        : this(unitOfWork, null)
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
    /// <remarks>
    /// This service does not translate specification paging into <see cref="IPagingCriteria"/>;
    /// the repository fallback runs unpaged. Use
    /// <see cref="ApplicationServiceBase{TEntity, TUoW}"/> when that translation is required.
    /// </remarks>
    public virtual IBusinessResult<IList<TEntity>> GetBySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetBySpecification(GetType().Name, specification, null);
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

    #endregion
}
