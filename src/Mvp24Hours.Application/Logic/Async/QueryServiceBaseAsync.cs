//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
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
    public abstract class QueryServiceBaseAsync<TEntity, TUoW> : IQueryServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IRepositoryAsync<TEntity> _repository;
        private readonly TUoW _unitOfWork;

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance for data access operations.
        /// </summary>
        protected virtual IRepositoryAsync<TEntity> Repository => _repository;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for data access.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected QueryServiceBaseAsync(TUoW unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
        }

        #endregion

        #region [ IQueryServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-listanyasync");
            return _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-listcountasync");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-listasync");
            return _repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getbyanyasync");
            return _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getbycountasync");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getbyasync");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getbyidasync");
            return _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        #endregion

        #region [ Specification Pattern Implementation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-anybyspecificationasync");

            if (specification == null)
            {
                return false.ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.AnyBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return await _repository.GetByAnyAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-countbyspecificationasync");

            if (specification == null)
            {
                return 0.ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.CountBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return await _repository.GetByCountAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getbyspecificationasync");

            if (specification == null)
            {
                return ((IList<TEntity>)new List<TEntity>()).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.GetBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getsinglebyspecificationasync");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.GetSingleBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: get by expression and take single
            var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
            var entity = result?.SingleOrDefault();
            return entity.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebaseasync-getfirstbyspecificationasync");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.GetFirstBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: get by expression and take first
            var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
            var entity = result?.FirstOrDefault();
            return entity.ToBusiness();
        }

        #endregion
    }
}

