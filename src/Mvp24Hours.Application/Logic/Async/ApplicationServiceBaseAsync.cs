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
/// Asynchronous abstract base class for application services that provides a unified implementation
/// of query and command operations using repository and unit of work patterns.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class provides a complete async implementation of <see cref="IApplicationServiceAsync{TEntity}"/>,
/// combining both query and command operations in a single service.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Unified async Query + Command operations</item>
/// <item>FluentValidation integration for entity validation</item>
/// <item>Telemetry logging for all operations</item>
/// <item>Transaction management via Unit of Work</item>
/// <item>Full CancellationToken support</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// public class CustomerService : ApplicationServiceBaseAsync&lt;Customer, MyDbContext&gt;
/// {
///     public CustomerService(MyDbContext unitOfWork) : base(unitOfWork) { }
///     
///     // Add custom business logic here
///     public Task&lt;IBusinessResult&lt;Customer&gt;&gt; FindByEmailAsync(string email, CancellationToken ct = default)
///     {
///         return GetByAsync(c => c.Email == email, ct);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="IApplicationServiceAsync{TEntity}"/>
/// <seealso cref="RepositoryServiceAsync{TEntity, TUoW}"/>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
/// </remarks>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="validator">The validator for entity validation.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
public abstract class ApplicationServiceBaseAsync<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger? logger) : IApplicationServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
    where TEntity : class, IEntityBase
    where TUoW : class, IUnitOfWorkAsync
{
    #region [ Properties / Fields ]

    private readonly ServiceOperationsAsync<TEntity> _operations = new(unitOfWork, validator, logger ?? NullLogger.Instance);
    private readonly TUoW _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <summary>
    /// Gets the unit of work instance for managing transactions.
    /// </summary>
    protected virtual TUoW UnitOfWork => _unitOfWork;

    /// <summary>
    /// Gets the repository instance for data access operations.
    /// </summary>
    protected virtual IRepositoryAsync<TEntity> Repository => _operations.Repository;

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
    /// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected ApplicationServiceBaseAsync(TUoW unitOfWork)
        : this(unitOfWork, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="validator">The validator for entity validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected ApplicationServiceBaseAsync(TUoW unitOfWork, IValidator<TEntity>? validator)
        : this(unitOfWork, validator, null)
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

    #region [ ICommandServiceAsync Implementation ]

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return _operations.AddAsync(GetType().Name, entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return _operations.AddAsync(GetType().Name, entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return _operations.ModifyAsync(GetType().Name, entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return _operations.ModifyAsync(GetType().Name, entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return _operations.RemoveAsync(GetType().Name, entity, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return _operations.RemoveAsync(GetType().Name, entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return _operations.RemoveByIdAsync(GetType().Name, id, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
    {
        return _operations.RemoveByIdAsync(GetType().Name, ids, cancellationToken);
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
    public virtual Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
        where TSpec : ISpecificationQuery<TEntity>
    {
        return _operations.GetBySpecificationAsync(GetType().Name, specification, CreatePagingCriteriaFromSpecification, cancellationToken);
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
        return new SpecificationPagingCriteriaAsync(specification.Skip, specification.Take);
    }

    #endregion
}

/// <summary>
/// Simple paging criteria implementation for specification-based async queries.
/// </summary>
internal class SpecificationPagingCriteriaAsync(int? skip, int? take) : IPagingCriteria
{
    public int Offset { get; } = skip ?? 0;
    public int Limit { get; } = take ?? 0;
    public IReadOnlyCollection<string>? OrderBy { get; }
    public IReadOnlyCollection<string>? Navigation { get; }
}

