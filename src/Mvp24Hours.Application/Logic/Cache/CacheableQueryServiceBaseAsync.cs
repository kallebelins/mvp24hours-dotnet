//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Cache;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Cache
{
    /// <summary>
    /// Asynchronous query service with second-level caching support.
    /// Provides automatic caching of query results for improved performance.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be queried.</typeparam>
    /// <typeparam name="TUoW">The unit of work type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class extends the standard query service with automatic caching capabilities:
    /// <list type="bullet">
    /// <item>Automatic caching of query results</item>
    /// <item>Configurable cache duration per query type</item>
    /// <item>Cache key generation based on query parameters</item>
    /// <item>Support for cache regions and tags</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class ProductQueryService : CacheableQueryServiceBaseAsync&lt;Product, MyDbContext&gt;
    /// {
    ///     public ProductQueryService(
    ///         MyDbContext unitOfWork,
    ///         IQueryCacheProvider cacheProvider,
    ///         IQueryCacheKeyGenerator keyGenerator,
    ///         ILogger&lt;ProductQueryService&gt; logger)
    ///         : base(unitOfWork, cacheProvider, keyGenerator, logger) { }
    ///     
    ///     // All queries are automatically cached
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public abstract class CacheableQueryServiceBaseAsync<TEntity, TUoW> : QueryServiceBaseAsync<TEntity, TUoW>
        where TEntity : class, IEntityBase
        where TUoW : class, IUnitOfWorkAsync
    {
        private readonly IQueryCacheProvider _cacheProvider;
        private readonly IQueryCacheKeyGenerator _keyGenerator;
        private readonly ILogger _logger;
        private readonly QueryCacheEntryOptions _defaultCacheOptions;

        /// <summary>
        /// Gets the cache provider for storing and retrieving cached results.
        /// </summary>
        protected IQueryCacheProvider CacheProvider => _cacheProvider;

        /// <summary>
        /// Gets the key generator for creating cache keys.
        /// </summary>
        protected IQueryCacheKeyGenerator KeyGenerator => _keyGenerator;

        /// <summary>
        /// Gets or sets whether caching is enabled for this service.
        /// </summary>
        protected virtual bool CacheEnabled { get; set; } = true;

        /// <summary>
        /// Gets the default cache duration for queries.
        /// Override in derived classes to customize.
        /// </summary>
        protected virtual TimeSpan DefaultCacheDuration => TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the cache region for this entity type.
        /// </summary>
        protected virtual string CacheRegion => typeof(TEntity).Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheableQueryServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        protected CacheableQueryServiceBaseAsync(
            TUoW unitOfWork,
            IQueryCacheProvider cacheProvider,
            IQueryCacheKeyGenerator keyGenerator,
            ILogger logger)
            : base(unitOfWork)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultCacheOptions = new QueryCacheEntryOptions
            {
                Duration = DefaultCacheDuration,
                Region = CacheRegion,
                UseSlidingExpiration = false
            };
        }

        #region [ Cacheable Query Methods ]

        /// <inheritdoc/>
        public override async Task<IBusinessResult<IList<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.ListAsync(cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(ListAsync));

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-listasync-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.ListAsync(cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IBusinessResult<IList<TEntity>>> ListAsync(IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.ListAsync(criteria, cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(ListAsync), criteria);

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-listasync-paged-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.ListAsync(criteria, cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.ListAnyAsync(cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(ListAnyAsync));

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-listanyasync-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.ListAnyAsync(cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.ListCountAsync(cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(ListCountAsync));

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-listcountasync-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.ListCountAsync(cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IBusinessResult<TEntity>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.GetByIdAsync(id, cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(GetByIdAsync), id);

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-getbyidasync-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.GetByIdAsync(id, cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<IBusinessResult<TEntity>> GetByIdAsync(object id, IPagingCriteria? criteria, CancellationToken cancellationToken = default)
        {
            if (!CacheEnabled)
            {
                return await base.GetByIdAsync(id, criteria, cancellationToken);
            }

            var cacheKey = GenerateCacheKey(nameof(GetByIdAsync), id, criteria);

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogDebug("cacheable-queryservice-getbyidasync-paged-cache-miss CacheKey={CacheKey}", cacheKey);
                    var result = await base.GetByIdAsync(id, criteria, cancellationToken);
                    return result;
                },
                _defaultCacheOptions,
                cancellationToken);
        }

        #endregion

        #region [ Cacheable Query with Custom Duration ]

        /// <summary>
        /// Gets a list of entities with custom cache duration.
        /// </summary>
        protected async Task<IBusinessResult<IList<TEntity>>> ListWithCacheAsync(
            TimeSpan cacheDuration,
            CancellationToken cancellationToken = default)
        {
            var options = new QueryCacheEntryOptions
            {
                Duration = cacheDuration,
                Region = CacheRegion
            };

            var cacheKey = GenerateCacheKey(nameof(ListAsync));

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await base.ListAsync(cancellationToken),
                options,
                cancellationToken);
        }

        /// <summary>
        /// Gets entities by expression with custom cache options.
        /// </summary>
        protected async Task<IBusinessResult<IList<TEntity>>> GetByWithCacheAsync(
            Expression<Func<TEntity, bool>> clause,
            QueryCacheEntryOptions cacheOptions,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKeyFromExpression(nameof(GetByAsync), clause);

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await base.GetByAsync(clause, cancellationToken),
                cacheOptions,
                cancellationToken);
        }

        /// <summary>
        /// Gets an entity by ID with custom cache options.
        /// </summary>
        protected async Task<IBusinessResult<TEntity>> GetByIdWithCacheAsync(
            object id,
            QueryCacheEntryOptions cacheOptions,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(nameof(GetByIdAsync), id);

            return await _cacheProvider.GetOrSetAsync(
                cacheKey,
                async () => await base.GetByIdAsync(id, cancellationToken),
                cacheOptions,
                cancellationToken);
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
        protected virtual string GenerateCacheKeyFromExpression(string operation, Expression expression)
        {
            var entityType = typeof(TEntity);
            var expressionHash = expression.ToString().GetHashCode().ToString("X8");
            return $"{entityType.Name}:{operation}:{expressionHash}";
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
            await _cacheProvider.InvalidateRegionAsync(CacheRegion, cancellationToken);
        }

        /// <summary>
        /// Invalidates a specific cache entry by ID.
        /// </summary>
        protected virtual async Task InvalidateCacheByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(nameof(GetByIdAsync), id);
            _logger.LogDebug("Invalidating cache for key: {CacheKey}", cacheKey);
            await _cacheProvider.RemoveAsync(cacheKey, cancellationToken);
        }

        /// <summary>
        /// Disables caching temporarily. Use with using statement.
        /// </summary>
        protected CacheDisabler DisableCache()
        {
            return new CacheDisabler(this);
        }

        /// <summary>
        /// Helper class for temporarily disabling cache.
        /// </summary>
        protected sealed class CacheDisabler : IDisposable
        {
            private readonly CacheableQueryServiceBaseAsync<TEntity, TUoW> _service;
            private readonly bool _previousState;

            public CacheDisabler(CacheableQueryServiceBaseAsync<TEntity, TUoW> service)
            {
                _service = service;
                _previousState = service.CacheEnabled;
                service.CacheEnabled = false;
            }

            public void Dispose()
            {
                _service.CacheEnabled = _previousState;
            }
        }

        #endregion
    }
}

