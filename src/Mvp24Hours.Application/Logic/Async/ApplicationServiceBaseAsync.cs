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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
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
    public abstract class ApplicationServiceBaseAsync<TEntity, TUoW> : IApplicationServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
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
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBaseAsync(TUoW unitOfWork)
            : this(unitOfWork, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBaseAsync(TUoW unitOfWork, IValidator<TEntity>? validator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
        }

        #endregion

        #region [ IQueryServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-listanyasync");
            return _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-listcountasync");
            return _repository.ListCountAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-listasync");
            return _repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-getbyanyasync");
            return _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-getbycountasync");
            return _repository.GetByCountAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-getbyasync");
            return _repository.GetByAsync(clause, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-getbyidasync");
            return _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        #endregion

        #region [ ICommandServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-addasync");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-addlistasync");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-modifyasync");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-modifylistasync");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-removeasync");
            await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-removelistasync");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-removebyidasync");
            await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-applicationservicebaseasync-removebyidlistasync");

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

