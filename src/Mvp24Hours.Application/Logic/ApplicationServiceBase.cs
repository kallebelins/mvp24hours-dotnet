//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogger? _logger;

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

        /// <summary>
        /// Gets the logger instance for logging operations.
        /// </summary>
        protected virtual ILogger? Logger => _logger;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBase(TUoW unitOfWork)
            : this(unitOfWork, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBase(TUoW unitOfWork, IValidator<TEntity>? validator)
            : this(unitOfWork, validator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBase{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <param name="logger">The logger for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBase(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger? logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
            _logger = logger;
        }

        #endregion

        #region [ IQueryService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> ListAny()
        {
            _logger?.LogDebug("[{ServiceName}] Executing ListAny for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.ListAny().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> ListCount()
        {
            _logger?.LogDebug("[{ServiceName}] Executing ListCount for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.ListCount().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> List()
        {
            return List(null!);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> List(IPagingCriteria? criteria)
        {
            _logger?.LogDebug("[{ServiceName}] Executing List for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
            return _repository.List(criteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetByAny for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.GetByAny(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetByCount for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.GetByCount(clause).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause)
        {
            return GetBy(clause, null!);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<IList<TEntity>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetBy for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
            return _repository.GetBy(clause, criteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity> GetById(object id)
        {
            return GetById(id, null!);
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity> GetById(object id, IPagingCriteria? criteria)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetById for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
            return _repository.GetById(id, criteria).ToBusiness();
        }

        #endregion

        #region [ ICommandService Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Add(TEntity entity)
        {
            _logger?.LogDebug("[{ServiceName}] Executing Add for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing Add for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing Modify for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing Modify for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing Remove for {EntityType}", GetType().Name, typeof(TEntity).Name);
            _repository.Remove(entity);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> Remove(IList<TEntity> entities)
        {
            _logger?.LogDebug("[{ServiceName}] Executing Remove for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing RemoveById for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
            _repository.RemoveById(id);
            return _unitOfWork.SaveChanges().ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<int> RemoveById(IList<object> ids)
        {
            _logger?.LogDebug("[{ServiceName}] Executing RemoveById for {Count} {EntityType} entities", GetType().Name, ids?.Count ?? 0, typeof(TEntity).Name);

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

        #region [ Specification Pattern Implementation ]

        /// <inheritdoc/>
        public virtual IBusinessResult<bool> AnyBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            _logger?.LogDebug("[{ServiceName}] Executing AnyBySpecification for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing CountBySpecification for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing GetBySpecification for {EntityType}", GetType().Name, typeof(TEntity).Name);

            if (specification == null)
            {
                return ((IList<TEntity>)new List<TEntity>()).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.GetBySpecification(specification).ToBusiness();
            }

            // Fallback: use the specification's expression directly with paging if available
            IPagingCriteria? pagingCriteria = null;
            if (specification is ISpecificationQueryEnhanced<TEntity> enhancedSpec)
            {
                pagingCriteria = CreatePagingCriteriaFromSpecification(enhancedSpec);
            }

            return _repository.GetBy(specification.IsSatisfiedByExpression, pagingCriteria).ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity?> GetSingleBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetSingleBySpecification for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            var result = _repository.GetBy(specification.IsSatisfiedByExpression, null!);
            var entity = result?.SingleOrDefault();
            return entity.ToBusiness();
        }

        /// <inheritdoc/>
        public virtual IBusinessResult<TEntity?> GetFirstBySpecification<TSpec>(TSpec specification)
            where TSpec : ISpecificationQuery<TEntity>
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetFirstBySpecification for {EntityType}", GetType().Name, typeof(TEntity).Name);

            if (specification == null)
            {
                return ((TEntity?)null!).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepository<TEntity> readOnlyRepo)
            {
                return readOnlyRepo.GetFirstBySpecification(specification).ToBusiness();
            }

            // Fallback: get by expression and take first
            var result = _repository.GetBy(specification.IsSatisfiedByExpression, null!);
            var entity = result?.FirstOrDefault();
            return entity.ToBusiness();
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
            return new SpecificationPagingCriteria(specification.Skip, specification.Take);
        }

        #endregion
    }

    /// <summary>
    /// Simple paging criteria implementation for specification-based queries.
    /// </summary>
    internal class SpecificationPagingCriteria : IPagingCriteria
    {
        public SpecificationPagingCriteria(int? skip, int? take)
        {
            Offset = skip ?? 0;
            Limit = take ?? 0;
        }

        public int Offset { get; }
        public int Limit { get; }
        public IReadOnlyCollection<string>? OrderBy { get; }
        public IReadOnlyCollection<string>? Navigation { get; }
    }
}

