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

namespace Mvp24Hours.Application.Logic
{
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
    public abstract class QueryServiceBase<TEntity, TUoW> : IQueryService<TEntity>, IReadOnlyApplicationService<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWork
    {
        #region [ Properties / Fields ]

        private readonly IRepository<TEntity> _repository;
        private readonly TUoW _unitOfWork;

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance for data access operations.
        /// </summary>
        protected virtual IRepository<TEntity> Repository => _repository;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for data access.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected QueryServiceBase(TUoW unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
        }

        #endregion

        #region [ IQueryService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> ListAny()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-listany");
            return _repository.ListAny().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> ListCount()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-listcount");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-list");
            return _repository.List(criteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getbyany");
            return _repository.GetByAny(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getbycount");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getby");
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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getbyid");
            return _repository.GetById(id, criteria).ToBusiness();
        }

        #endregion

        #region [ Specification Pattern Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> AnyBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-anybyspecification");

            if (specification == null)
            {
                return false.ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.AnyBySpecification(specification).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return _repository.GetByAny(specification.IsSatisfiedByExpression).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> CountBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-countbyspecification");

            if (specification == null)
            {
                return 0.ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.CountBySpecification(specification).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return _repository.GetByCount(specification.IsSatisfiedByExpression).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> GetBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getbyspecification");

            if (specification == null)
            {
                return ((IList<TEntity>)new List<TEntity>()).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.GetBySpecification(specification).ToBusiness();
            }

            // Fallback: use the specification's expression directly
            return _repository.GetBy(specification.IsSatisfiedByExpression, null).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity?> GetSingleBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getsinglebyspecification");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.GetSingleBySpecification(specification).ToBusiness();
            }

            // Fallback: get by expression and take single
            var result = _repository.GetBy(specification.IsSatisfiedByExpression, null);
            var entity = result?.SingleOrDefault();
            return entity.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity?> GetFirstBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-queryservicebase-getfirstbyspecification");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.GetFirstBySpecification(specification).ToBusiness();
            }

            // Fallback: get by expression and take first
            var result = _repository.GetBy(specification.IsSatisfiedByExpression, null);
            var entity = result?.FirstOrDefault();
            return entity.ToBusiness();
        }

        #endregion
    }
}

