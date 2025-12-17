//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
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
    public abstract class CommandServiceBaseAsync<TEntity, TUoW> : ICommandServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IRepositoryAsync<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IValidator<TEntity>? _validator;

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

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected CommandServiceBaseAsync(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected CommandServiceBaseAsync(TUoW unitOfWork, IValidator<TEntity>? validator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
        }

        #endregion

        #region [ ICommandServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-addasync");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                await _repository.AddAsync(entity, cancellationToken: cancellationToken);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-addlistasync");

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
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-modifyasync");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                await _repository.ModifyAsync(entity, cancellationToken: cancellationToken);
                return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-modifylistasync");

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
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-removeasync");
            await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-removelistasync");

            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(entities.Select(entity => _repository.RemoveAsync(entity, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-removebyidasync");
            await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebaseasync-removebyidlistasync");

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(ids.Select(id => _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        #endregion
    }
}

