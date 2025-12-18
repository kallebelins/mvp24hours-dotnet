//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Cache;
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

namespace Mvp24Hours.Application.Logic.Cache
{
    /// <summary>
    /// Asynchronous application service with second-level caching support for queries
    /// and automatic cache invalidation for commands.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides a complete application service with:
    /// <list type="bullet">
    /// <item>Automatic caching of query results</item>
    /// <item>Automatic cache invalidation on command operations (Add/Modify/Remove)</item>
    /// <item>FluentValidation integration</item>
    /// <item>Full async/await support</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class ProductService : CacheableApplicationServiceBaseAsync&lt;Product, MyDbContext&gt;
    /// {
    ///     public ProductService(
    ///         MyDbContext unitOfWork,
    ///         IQueryCacheProvider cacheProvider,
    ///         ICacheInvalidator cacheInvalidator,
    ///         IQueryCacheKeyGenerator keyGenerator,
    ///         ILogger&lt;ProductService&gt; logger,
    ///         IValidator&lt;Product&gt;? validator = null)
    ///         : base(unitOfWork, cacheProvider, cacheInvalidator, keyGenerator, logger, validator) { }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public abstract class CacheableApplicationServiceBaseAsync<TEntity, TUoW>
        : IApplicationServiceAsync<TEntity>, IReadOnlyApplicationServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly IRepositoryAsync<TEntity> _repository;
        private readonly TUoW _unitOfWork;
        private readonly IValidator<TEntity>? _validator;
        private readonly IQueryCacheProvider _cacheProvider;
        private readonly ICacheInvalidator _cacheInvalidator;
        private readonly IQueryCacheKeyGenerator _keyGenerator;
        private readonly ILogger _logger;
        private readonly QueryCacheEntryOptions _defaultCacheOptions;

        /// <summary>
        /// Gets the unit of work instance.
        /// </summary>
        protected virtual TUoW UnitOfWork => _unitOfWork;

        /// <summary>
        /// Gets the repository instance.
        /// </summary>
        protected virtual IRepositoryAsync<TEntity> Repository => _repository;

        /// <summary>
        /// Gets the validator instance.
        /// </summary>
        protected virtual IValidator<TEntity>? Validator => _validator;

        /// <summary>
        /// Gets the cache provider.
        /// </summary>
        protected IQueryCacheProvider CacheProvider => _cacheProvider;

        /// <summary>
        /// Gets the cache invalidator.
        /// </summary>
        protected ICacheInvalidator CacheInvalidator => _cacheInvalidator;

        /// <summary>
        /// Gets or sets whether caching is enabled.
        /// </summary>
        protected virtual bool CacheEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether automatic cache invalidation is enabled on commands.
        /// </summary>
        protected virtual bool AutoInvalidateOnCommand { get; set; } = true;

        /// <summary>
        /// Gets the default cache duration for queries.
        /// </summary>
        protected virtual TimeSpan DefaultCacheDuration => TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the cache region for this entity type.
        /// </summary>
        protected virtual string CacheRegion => typeof(TEntity).Name;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheableApplicationServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        protected CacheableApplicationServiceBaseAsync(
            TUoW unitOfWork,
            IQueryCacheProvider cacheProvider,
            ICacheInvalidator cacheInvalidator,
            IQueryCacheKeyGenerator keyGenerator,
            ILogger logger,
            IValidator<TEntity>? validator = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _repository = unitOfWork.GetRepository<TEntity>();
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator;
            _defaultCacheOptions = new QueryCacheEntryOptions
            {
                Duration = DefaultCacheDuration,
                Region = CacheRegion,
                UseSlidingExpiration = false
            };
        }

        #endregion

        #region [ IQueryServiceAsync Implementation - Cached ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-listanyasync");
                return await _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey(nameof(ListAnyAsync));
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.ListAnyAsync(cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-listcountasync");
                return await _repository.ListCountAsync(cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey(nameof(ListCountAsync));
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.ListCountAsync(cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
        {
            return ListAsync(null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-listasync");
                return await _repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey(nameof(ListAsync), criteria);
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.ListAsync(criteria, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getbyanyasync");
                return await _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKeyFromExpression(nameof(GetByAnyAsync), clause);
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByAnyAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getbycountasync");
                return await _repository.GetByCountAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKeyFromExpression(nameof(GetByCountAsync), clause);
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByCountAsync(clause, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default)
        {
            return GetByAsync(clause, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TEntity>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getbyasync");
                return await _repository.GetByAsync(clause, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKeyFromExpression(nameof(GetByAsync), clause, criteria);
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByAsync(clause, criteria, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, null, cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getbyidasync");
                return await _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey(nameof(GetByIdAsync), id, criteria);
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByIdAsync(id, criteria, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        #endregion

        #region [ Specification Pattern Implementation - Cached ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<bool>> AnyBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-anybyspecificationasync");

            if (specification == null)
            {
                return false.ToBusiness();
            }

            if (!CacheEnabled)
            {
                return await _repository.GetByAnyAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey($"AnyBySpec_{specification.GetType().Name}", specification.GetHashCode());
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByAnyAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> CountBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-countbyspecificationasync");

            if (specification == null)
            {
                return 0.ToBusiness();
            }

            if (!CacheEnabled)
            {
                return await _repository.GetByCountAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey($"CountBySpec_{specification.GetType().Name}", specification.GetHashCode());
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByCountAsync(specification.IsSatisfiedByExpression, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<IList<TEntity>>> GetBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getbyspecificationasync");

            if (specification == null)
            {
                return ((IList<TEntity>)new List<TEntity>()).ToBusiness();
            }

            if (!CacheEnabled)
            {
                return await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken).ToBusinessAsync();
            }

            var cacheKey = GenerateCacheKey($"GetBySpec_{specification.GetType().Name}", specification.GetHashCode());
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken).ToBusinessAsync(),
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity?>> GetSingleBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getsinglebyspecificationasync");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            if (!CacheEnabled)
            {
                var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
                return result?.SingleOrDefault().ToBusiness();
            }

            var cacheKey = GenerateCacheKey($"GetSingleBySpec_{specification.GetType().Name}", specification.GetHashCode());
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
                    return result?.SingleOrDefault().ToBusiness();
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<TEntity?>> GetFirstBySpecificationAsync<TSpec>(TSpec specification, CancellationToken cancellationToken = default)
            where TSpec : ISpecificationQuery<TEntity>
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-getfirstbyspecificationasync");

            if (specification == null)
            {
                return ((TEntity?)null).ToBusiness();
            }

            if (!CacheEnabled)
            {
                var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
                return result?.FirstOrDefault().ToBusiness();
            }

            var cacheKey = GenerateCacheKey($"GetFirstBySpec_{specification.GetType().Name}", specification.GetHashCode());
            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var result = await _repository.GetByAsync(specification.IsSatisfiedByExpression, null, cancellationToken: cancellationToken);
                    return result?.FirstOrDefault().ToBusiness();
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        #endregion

        #region [ ICommandServiceAsync Implementation - With Cache Invalidation ]

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-addasync");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                await _repository.AddAsync(entity, cancellationToken: cancellationToken);
                var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

                // Invalidate cache on successful add
                if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
                {
                    await InvalidateCacheAsync(cancellationToken);
                }

                return result;
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> AddAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-addlistasync");

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
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful add
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-modifyasync");

            var errors = entity.TryValidate(_validator);
            if (!errors.AnySafe())
            {
                await _repository.ModifyAsync(entity, cancellationToken: cancellationToken);
                var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

                // Invalidate cache on successful modify
                if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
                {
                    await InvalidateCacheAsync(cancellationToken);
                }

                return result;
            }
            return errors.ToBusiness<int>();
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> ModifyAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-modifylistasync");

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
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful modify
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-removeasync");

            await _repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful remove
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveAsync(IList<TEntity> entities, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-removelistasync");

            if (!entities.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(entities.Select(entity => _repository.RemoveAsync(entity, cancellationToken: cancellationToken)));
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful remove
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-removebyidasync");

            await _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful remove
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await _cacheInvalidator.InvalidateByIdAsync<TEntity>(id, cancellationToken);
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "cacheable-appservice-removebyidlistasync");

            if (!ids.AnySafe())
            {
                return 0.ToBusiness();
            }

            await Task.WhenAll(ids.Select(id => _repository.RemoveByIdAsync(id, cancellationToken: cancellationToken)));
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken).ToBusinessAsync();

            // Invalidate cache on successful remove
            if (AutoInvalidateOnCommand && result.HasData() && result.GetDataValue() > 0)
            {
                await InvalidateCacheAsync(cancellationToken);
            }

            return result;
        }

        #endregion

        #region [ Cache Key Generation ]

        /// <summary>
        /// Generates a cache key for the given operation and parameters.
        /// </summary>
        protected virtual string GenerateCacheKey(string operation, params object?[] parameters)
        {
            var entityType = typeof(TEntity);
            var keyParts = new List<string>
            {
                entityType.Name,
                operation
            };

            foreach (var param in parameters)
            {
                if (param != null)
                {
                    keyParts.Add(SerializeParameter(param));
                }
            }

            return string.Join(":", keyParts);
        }

        /// <summary>
        /// Generates a cache key from an expression.
        /// </summary>
        protected virtual string GenerateCacheKeyFromExpression(string operation, Expression expression, IPagingCriteria? criteria = null)
        {
            var entityType = typeof(TEntity);
            var expressionHash = expression.ToString().GetHashCode().ToString("X8");
            var key = $"{entityType.Name}:{operation}:{expressionHash}";
            
            if (criteria != null)
            {
                key += $":p{criteria.Offset}_s{criteria.Limit}";
            }
            
            return key;
        }

        /// <summary>
        /// Serializes a parameter to a string for cache key generation.
        /// </summary>
        private static string SerializeParameter(object param)
        {
            return param switch
            {
                IPagingCriteria criteria => $"p{criteria.Offset}_s{criteria.Limit}",
                ISpecificationQuery<TEntity> spec => $"spec_{spec.GetType().Name}_{spec.GetHashCode():X8}",
                _ => param.ToString() ?? "null"
            };
        }

        #endregion

        #region [ Cache Management ]

        /// <summary>
        /// Invalidates all cached entries for this entity type.
        /// </summary>
        protected virtual async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Invalidating cache for entity type: {EntityType}", typeof(TEntity).Name);
            await _cacheInvalidator.InvalidateEntityAsync<TEntity>(cancellationToken);
        }

        /// <summary>
        /// Invalidates a specific cache entry by ID.
        /// </summary>
        protected virtual async Task InvalidateCacheByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Invalidating cache for entity: {EntityType}, Id: {EntityId}", typeof(TEntity).Name, id);
            await _cacheInvalidator.InvalidateByIdAsync<TEntity>(id, cancellationToken);
        }

        #endregion
    }
}

