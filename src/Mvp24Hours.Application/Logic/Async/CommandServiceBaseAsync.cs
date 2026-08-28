//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.Logic.Internal;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic;

/// <summary>
/// Asynchronous command-only service base class implementing the write-side of CQRS pattern.
/// Provides async data modification operations without query capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
/// <typeparam name="TUoW">The unit of work type.</typeparam>
/// <remarks>
/// <para>
/// This class implements <see cref="ICommandServiceAsync{TEntity}"/>,
/// providing async write-only access to entities. Use this for CQRS patterns where writes are
/// separated from reads.
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Enforces write-only access at the class level</item>
/// <item>Can be optimized for write operations</item>
/// <item>Supports CQRS patterns where writes are separated from reads</item>
/// <item>Includes FluentValidation integration</item>
/// <item>Full async/await support with CancellationToken</item>
/// </list>
/// </para>
/// <para>
/// <strong>Example usage:</strong>
/// <code>
/// // Write-only async order processing service
/// public class OrderCommandService : CommandServiceBaseAsync&lt;Order, MyDbContext&gt;
/// {
///     public OrderCommandService(MyDbContext unitOfWork, IValidator&lt;Order&gt; validator) 
///         : base(unitOfWork, validator) { }
///     
///     public Task&lt;IBusinessResult&lt;int&gt;&gt; PlaceOrderAsync(Order order, CancellationToken ct = default)
///     {
///         order.Status = OrderStatus.Placed;
///         return AddAsync(order, ct);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <seealso cref="ICommandServiceAsync{TEntity}"/>
/// <seealso cref="QueryServiceBaseAsync{TEntity, TUoW}"/>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> class.
/// </remarks>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="validator">The validator for entity validation.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
public abstract class CommandServiceBaseAsync<TEntity, TUoW>(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger? logger) : ICommandServiceAsync<TEntity>
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
    /// Initializes a new instance of the <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected CommandServiceBaseAsync(TUoW unitOfWork)
        : this(unitOfWork, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="validator">The validator for entity validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    protected CommandServiceBaseAsync(TUoW unitOfWork, IValidator<TEntity>? validator)
        : this(unitOfWork, validator, null)
    {
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
}
