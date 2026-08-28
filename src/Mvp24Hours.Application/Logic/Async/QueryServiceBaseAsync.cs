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
/// Asynchronous query-only service base class implementing the read-side of CQRS pattern.
/// Provides async data projection operations without any modification capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity type to be queried by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class implements <see cref="IQueryServiceAsync{TEntity}"/> and <see cref="IReadOnlyApplicationServiceAsync{TEntity}"/>,
/// providing async read-only access to entities. Use this for CQRS patterns where reads are
/// separated from writes.
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Enforces read-only access at the class level</item>
/// <item>Can be optimized for read operations (no tracking, read replicas)</item>
/// <item>Supports CQRS patterns where reads are separated from writes</item>
/// <item>Full async/await support with CancellationToken</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// // Read-only async product catalog service
/// public class ProductCatalogQueryService : QueryServiceBaseAsync&lt;Product, MyDbContext&gt;
/// {
///     public ProductCatalogQueryService(MyDbContext unitOfWork) : base(unitOfWork) { }
///     
///     public Task&lt;IBusinessResult&lt;IList&lt;Product&gt;&gt;&gt; GetActiveProductsAsync(CancellationToken ct = default)
///     {
///         return GetByAsync(p => p.IsActive, ct);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IQueryServiceAsync{TEntity}"/>
/// <seealso cref="IReadOnlyApplicationServiceAsync{TEntity}"/>
/// <seealso cref="CommandServiceBaseAsync{TEntity, TUoW}"/>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryServiceBaseAsync{TEntity, TUoW}"/> class.
/// </remarks>
/// <param name="unitOfWork">The unit of work for data access.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
public abstract class QueryServiceBaseAsync<TEntity, TUoW>(TUoW unitOfWork, ILogger? logger) : IQueryServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Properties / Fields ]

    private readonly ServiceOperationsAsync<TEntity> _operations = new(unitOfWork, null, logger ?? NullLogger.Instance);
    private readonly TUoW _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <summary>
    /// Gets the unit of work instance.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance for data access operations.
    /// </summary>
    protected virtual IRepositoryAsync<TEntity> Repository => _operations.Repository;

    /// <summary>
    /// Gets the logger instance for logging operations. Never <see langword="null"/>:
    /// falls back to <see cref="NullLogger.Instance"/> when no logger is supplied.
    /// </summary>
    protected virtual ILogger Logger => _operations.Logger;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryServiceBaseAsync{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for data access.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected QueryServiceBaseAsync(TUoW unitOfWork)
        : this(unitOfWork, null)
    {
    }

    #endregion

    #region [ IQueryServiceAsync Implementation ]

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
    {
        return _operations.ListAnyAsync(GetType().Name, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
    {
        return _operations.ListCountAsync(GetType().Name, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
    {
        return ListAsync(null, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return _operations.ListAsync(GetType().Name, criteria, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return _operations.GetByAnyAsync(GetType().Name, clause, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return _operations.GetByCountAsync(GetType().Name, clause, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
    {
        return GetByAsync(clause, null, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return _operations.GetByAsync(GetType().Name, clause, criteria, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<TEntity?>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, null, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<TEntity?>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
    {
        return _operations.GetByIdAsync(GetType().Name, id, criteria, cancellationToken);
    }

    #endregion

    #region [ Specification Pattern Implementation ]

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.AnyBySpecificationAsync(GetType().Name, specification, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.CountBySpecificationAsync(GetType().Name, specification, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This service does not translate specification paging into <see cref="IPagingCriteria"/>;
    /// the repository fallback runs unpaged. Use
    /// <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> when that translation is required.
    /// </remarks>
    public virtual Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetBySpecificationAsync(GetType().Name, specification, null, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetSingleBySpecificationAsync(GetType().Name, specification, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetFirstBySpecificationAsync(GetType().Name, specification, cancellationToken);
    }

    #endregion
}
