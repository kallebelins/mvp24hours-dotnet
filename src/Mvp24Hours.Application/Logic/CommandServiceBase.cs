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

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Command-only service base class implementing the write-side of CQRS pattern.
    /// Provides data modification operations without query capabilities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class implements <see cref="ICommandService{TEntity}"/>,
    /// providing write-only access to entities. Use this for CQRS patterns where writes are
    /// separated from reads.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enforces write-only access at the class level</item>
    /// <item>Can be optimized for write operations</item>
    /// <item>Supports CQRS patterns where writes are separated from reads</item>
    /// <item>Includes FluentValidation integration</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// // Write-only order processing service
    /// public class OrderCommandService : CommandServiceBase&lt;Order, MyDbContext&gt;
    /// {
    ///     public OrderCommandService(MyDbContext unitOfWork, IValidator&lt;Order&gt; validator) 
    ///         : base(unitOfWork, validator) { }
    ///     
    ///     public IBusinessResult&lt;int&gt; PlaceOrder(Order order)
    ///     {
    ///         order.Status = OrderStatus.Placed;
    ///         return Add(order);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="ICommandService{TEntity}"/>
    /// <seealso cref="QueryServiceBase{TEntity, TUoW}"/>
    public abstract class CommandServiceBase<TEntity, TUoW> : ICommandService<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWork
    {
        #region [ Properties / Fields ]

        private readonly IRepository<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IValidator<TEntity>? _validator;

        /// <summary>
        /// Gets the unit of work instance for managing transactions.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance for data access operations.
        /// </summary>
        protected virtual IRepository<TEntity> Repository => _repository;

        /// <summary>
        /// Gets the validator instance for entity validation.
        /// </summary>
        protected virtual IValidator<TEntity>? Validator => _validator;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected CommandServiceBase(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected CommandServiceBase(TUoW unitOfWork, IValidator<TEntity>? validator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
        }

        #endregion

        #region [ ICommandService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-add");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                _repository.Add(entity);
                return _unitOfWork.SaveChanges().ToBusiness();
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-addlist");

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

            foreach (var entity in entities)
            {
                _repository.Add(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Modify(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-modify");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                _repository.Modify(entity);
                return _unitOfWork.SaveChanges().ToBusiness();
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Modify(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-modifylist");

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

            foreach (var entity in entities)
            {
                _repository.Modify(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-remove");
            _repository.Remove(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-removelist");

            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var entity in entities)
            {
                _repository.Remove(entity);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(object id)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-removebyid");
            _repository.RemoveById(id);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-commandservicebase-removebyidlist");

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            foreach (var id in ids)
            {
                _repository.RemoveById(id);
            }

            return _unitOfWork.SaveChanges().ToBusiness();
        }

        #endregion
    }
}

