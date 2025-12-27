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
        private readonly ILogger? _logger;

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

        /// <summary>
        /// Gets the logger instance for logging operations.
        /// </summary>
        protected virtual ILogger? Logger => _logger;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work for transaction management.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <param name="logger">The logger for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
        protected ApplicationServiceBaseAsync(TUoW unitOfWork, IValidator<TEntity>? validator, ILogger? logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _validator = validator;
            _logger = logger;
        }

        #endregion

        #region [ IQueryServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing ListAnyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing ListCountAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
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
            _logger?.LogDebug("[{ServiceName}] Executing ListAsync for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
            return _repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetByAnyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
            return _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetByCountAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
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
            _logger?.LogDebug("[{ServiceName}] Executing GetByAsync for {EntityType} with criteria", GetType().Name, typeof(TEntity).Name);
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
            _logger?.LogDebug("[{ServiceName}] Executing GetByIdAsync for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
            return _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        #endregion

        #region [ ICommandServiceAsync Implementation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing AddAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing AddAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing ModifyAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing ModifyAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing RemoveAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);
            await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing RemoveAsync for {Count} {EntityType} entities", GetType().Name, entities?.Count ?? 0, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {EntityType} with Id={Id}", GetType().Name, typeof(TEntity).Name, id);
            await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("[{ServiceName}] Executing RemoveByIdAsync for {Count} {EntityType} entities", GetType().Name, ids?.Count ?? 0, typeof(TEntity).Name);

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(ids.Select(id => _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
            return await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();
        }

        #endregion

        #region [ Specification Pattern Implementation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            _logger?.LogDebug("[{ServiceName}] Executing AnyBySpecificationAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing CountBySpecificationAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing GetBySpecificationAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

            if (specification == null)
            {
                return ((IList<TEntity>)new List<TEntity>()).ToBusiness();
            }

            // Try to use repository's specification method if available
            if (_repository is IReadOnlyRepositoryAsync<TEntity> readOnlyRepo)
            {
                return (await readOnlyRepo.GetBySpecificationAsync(specification, cancellationToken)).ToBusiness();
            }

            // Fallback: use the specification's expression directly with paging if available
            IPagingCriteria? pagingCriteria = null;
            if (specification is ISpecificationQueryEnhanced<TEntity> enhancedSpec)
            {
                pagingCriteria = CreatePagingCriteriaFromSpecification(enhancedSpec);
            }

            return await _repository.GetByAsync(specification.IsSatisfiedByExpression, pagingCriteria, cancellationToken: cancellationToken).ToBusinessAsync();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            _logger?.LogDebug("[{ServiceName}] Executing GetSingleBySpecificationAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
            _logger?.LogDebug("[{ServiceName}] Executing GetFirstBySpecificationAsync for {EntityType}", GetType().Name, typeof(TEntity).Name);

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
    internal class SpecificationPagingCriteriaAsync : IPagingCriteria
    {
        public SpecificationPagingCriteriaAsync(int? skip, int? take)
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

