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
using System.Linq.Expressions;

namespace Mvp24Hours.Application.Logic
{
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
    public abstract class ApplicationServiceBase<TEntity, TUoW> : IApplicationService<TEntity>, IReadOnlyApplicationService<TEntity>
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
        /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBase(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBase(TUoW unitOfWork, IValidator<TEntity>? validator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
        }

        #endregion

        #region [ IQueryService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-listany");
            return _repository.ListAny().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-listcount");
            return _repository.ListCount().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> List()
        {
            return List(null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> List(IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-list");
            return _repository.List(criteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-getbyany");
            return _repository.GetByAny(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-getbycount");
            return _repository.GetByCount(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause)
        {
            return GetBy(clause, null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-getby");
            return _repository.GetBy(clause, criteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity> GetById(object id)
        {
            return GetById(id, null);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity> GetById(object id, IPagingCriteria? criteria)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-getbyid");
            return _repository.GetById(id, criteria).ToBusiness();
        }

        #endregion

        #region [ ICommandService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(TEntity entity)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-add");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-addlist");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-modify");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-modifylist");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-remove");
            _repository.Remove(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-removelist");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-removebyid");
            _repository.RemoveById(id);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebase-removebyidlist");

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

