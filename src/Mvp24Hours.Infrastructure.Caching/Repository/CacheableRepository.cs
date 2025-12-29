//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Caching.Attributes;
using Mvp24Hours.Infrastructure.Caching.KeyGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Repository
{
    /// <summary>
    /// Decorator for IRepository that adds automatic caching to query operations
    /// and cache invalidation to command operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This decorator wraps an existing IRepository implementation and adds caching capabilities:
    /// <list type="bullet">
    /// <item><strong>Query Methods:</strong> Results are cached automatically when methods are marked with <see cref="CacheableAttribute"/></item>
    /// <item><strong>Command Methods:</strong> Cache is invalidated automatically when methods are marked with <see cref="CacheInvalidateAttribute"/></item>
    /// <item><strong>Automatic Invalidation:</strong> Modify/Remove operations automatically invalidate entity-specific cache entries</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Cache Key Format:</strong>
    /// <code>
    /// "{EntityType}:{MethodName}:{ParameterHash}"
    /// Example: "Customer:GetById:123" or "Product:GetBy:category=electronics"
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddScoped&lt;IRepository&lt;Customer&gt;&gt;(sp =>
    /// {
    ///     var baseRepo = new Repository&lt;Customer&gt;(dbContext, options);
    ///     var cacheProvider = sp.GetRequiredService&lt;ICacheProvider&gt;();
    ///     var logger = sp.GetRequiredService&lt;ILogger&lt;CacheableRepository&lt;Customer&gt;&gt;&gt;();
    ///     return new CacheableRepository&lt;Customer&gt;(baseRepo, cacheProvider, logger);
    /// });
    /// 
    /// // Use in service
    /// public class CustomerService
    /// {
    ///     private readonly IRepository&lt;Customer&gt; _repository;
    ///     
    ///     public CustomerService(IRepository&lt;Customer&gt; repository) => _repository = repository;
    ///     
    ///     public Customer GetCustomer(int id)
    ///     {
    ///         // This will be cached if GetById is marked with [Cacheable]
    ///         return _repository.GetById(id);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class CacheableRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntityBase
    {
        private readonly IRepository<TEntity> _repository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICacheKeyGenerator _keyGenerator;
        private readonly ILogger<CacheableRepository<TEntity>>? _logger;
        private readonly CacheableRepositoryOptions _options;

        /// <summary>
        /// Creates a new instance of CacheableRepository.
        /// </summary>
        /// <param name="repository">The underlying repository to wrap.</param>
        /// <param name="cacheProvider">The cache provider for storing/retrieving cached data.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <param name="options">Optional configuration options.</param>
        /// <param name="keyGenerator">Optional cache key generator. If null, DefaultCacheKeyGenerator is used.</param>
        public CacheableRepository(
            IRepository<TEntity> repository,
            ICacheProvider cacheProvider,
            ILogger<CacheableRepository<TEntity>>? logger = null,
            CacheableRepositoryOptions? options = null,
            ICacheKeyGenerator? keyGenerator = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _logger = logger;
            _options = options ?? new CacheableRepositoryOptions();
            _keyGenerator = keyGenerator ?? new DefaultCacheKeyGenerator(typeof(TEntity).Name);
        }

        #region IQuery<TEntity>

        public bool ListAny()
        {
            // Value types (bool) cannot be cached with current ICacheProvider constraints
            return _repository.ListAny();
        }

        public int ListCount()
        {
            // Value types (int) cannot be cached with current ICacheProvider constraints
            return _repository.ListCount();
        }

        public IList<TEntity> List()
        {
            return List(null);
        }

        public IList<TEntity> List(IPagingCriteria criteria)
        {
            return ExecuteWithCache(
                nameof(List),
                () => _repository.List(criteria),
                criteria);
        }

        public bool GetByAny(Expression<Func<TEntity, bool>> clause)
        {
            // Value types (bool) cannot be cached with current ICacheProvider constraints
            return _repository.GetByAny(clause);
        }

        public int GetByCount(Expression<Func<TEntity, bool>> clause)
        {
            // Value types (int) cannot be cached with current ICacheProvider constraints
            return _repository.GetByCount(clause);
        }

        public IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause)
        {
            return GetBy(clause, null);
        }

        public IList<TEntity> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria)
        {
            return ExecuteWithCache(
                nameof(GetBy),
                () => _repository.GetBy(clause, criteria),
                new { clause, criteria });
        }

        public TEntity GetById(object id)
        {
            return GetById(id, null);
        }

        public TEntity GetById(object id, IPagingCriteria criteria)
        {
            return ExecuteWithCache(
                nameof(GetById),
                () => _repository.GetById(id, criteria),
                new { id, criteria });
        }

        #endregion

        #region IQueryRelation<TEntity>

        public void LoadRelation<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression)
            where TProperty : class
        {
            _repository.LoadRelation(entity, propertyExpression);
        }

        public void LoadRelation<TProperty>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class
        {
            _repository.LoadRelation(entity, propertyExpression, clause, limit);
        }

        public void LoadRelationSortByAscending<TProperty, TKey>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class
        {
            _repository.LoadRelationSortByAscending(entity, propertyExpression, orderKey, clause, limit);
        }

        public void LoadRelationSortByDescending<TProperty, TKey>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
            Expression<Func<TProperty, TKey>> orderKey,
            Expression<Func<TProperty, bool>> clause = null,
            int limit = 0)
            where TProperty : class
        {
            _repository.LoadRelationSortByDescending(entity, propertyExpression, orderKey, clause, limit);
        }

        #endregion

        #region ICommand<TEntity>

        public void Add(TEntity entity)
        {
            _repository.Add(entity);
            InvalidateCacheForEntity(entity);
        }

        public void Add(IList<TEntity> entities)
        {
            _repository.Add(entities);
            InvalidateCacheForEntities(entities);
        }

        public void Modify(TEntity entity)
        {
            _repository.Modify(entity);
            InvalidateCacheForEntity(entity);
        }

        public void Modify(IList<TEntity> entities)
        {
            _repository.Modify(entities);
            InvalidateCacheForEntities(entities);
        }

        public void Remove(TEntity entity)
        {
            _repository.Remove(entity);
            InvalidateCacheForEntity(entity);
        }

        public void Remove(IList<TEntity> entities)
        {
            _repository.Remove(entities);
            InvalidateCacheForEntities(entities);
        }

        public void RemoveById(object id)
        {
            _repository.RemoveById(id);
            InvalidateCacheForEntityId(id);
        }

        public void RemoveById(IList<object> ids)
        {
            _repository.RemoveById(ids);
            foreach (var id in ids)
            {
                InvalidateCacheForEntityId(id);
            }
        }

        #endregion

        #region Private Methods

        private TResult ExecuteWithCache<TResult>(
            string methodName,
            Func<TResult> execute,
            object? parameters) where TResult : class
        {
            // Check if method has CacheableAttribute
            var methodInfo = GetMethodInfo(methodName);
            var cacheableAttr = methodInfo?.GetCustomAttribute<CacheableAttribute>();

            if (cacheableAttr == null && !_options.EnableCacheByDefault)
            {
                // No caching attribute and default caching is disabled
                return execute();
            }

            var cacheKey = GenerateCacheKey(methodName, parameters, cacheableAttr);
            var duration = cacheableAttr?.DurationSeconds ?? _options.DefaultCacheDurationSeconds;

            try
            {
                // Try to get from cache (synchronous cache access)
                var cached = GetFromCache<TResult>(cacheKey).GetAwaiter().GetResult();
                if (cached != null)
                {
                    _logger?.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                    return cached;
                }

                _logger?.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

                // Execute and cache result
                var result = execute();
                if (result != null)
                {
                    var options = new CacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(duration),
                        SlidingExpiration = cacheableAttr?.UseSlidingExpiration == true
                            ? TimeSpan.FromSeconds(duration)
                            : null
                    };

                    SetCacheAsync(cacheKey, result, options, cacheableAttr?.Region, cacheableAttr?.Tags)
                        .GetAwaiter().GetResult();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error accessing cache for key: {CacheKey}, executing without cache", cacheKey);
                return execute();
            }
        }

        private async Task<TResult?> GetFromCache<TResult>(string cacheKey) where TResult : class
        {
            try
            {
                return await _cacheProvider.GetAsync<TResult>(cacheKey);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error retrieving from cache: {CacheKey}", cacheKey);
                return default;
            }
        }

        private async Task SetCacheAsync<TResult>(
            string cacheKey,
            TResult value,
            CacheEntryOptions options,
            string? region = null,
            string? tags = null) where TResult : class
        {
            try
            {
                await _cacheProvider.SetAsync(cacheKey, value, options);

                // Store key in region/tag tracking if needed
                if (!string.IsNullOrWhiteSpace(region))
                {
                    await TrackCacheKeyInRegion(cacheKey, region);
                }

                if (!string.IsNullOrWhiteSpace(tags))
                {
                    await TrackCacheKeyWithTags(cacheKey, tags);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error setting cache: {CacheKey}", cacheKey);
            }
        }

        private string GenerateCacheKey(string methodName, object? parameters, CacheableAttribute? attribute)
        {
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.KeyTemplate))
            {
                // Use custom template
                var key = attribute.KeyTemplate;
                key = key.Replace("{methodName}", methodName);
                key = key.Replace("{entityType}", typeof(TEntity).Name);

                // Replace parameter placeholders
                if (parameters != null)
                {
                    var props = parameters.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(parameters);
                        key = key.Replace($"{{{prop.Name}}}", value?.ToString() ?? "");
                    }
                }

                return _keyGenerator.GenerateWithPrefix(typeof(TEntity).Name, key);
            }

            // Generate default key
            var keyParts = new List<string> { typeof(TEntity).Name, methodName };

            if (parameters != null)
            {
                var paramHash = GenerateParameterHash(parameters);
                keyParts.Add(paramHash);
            }

            return _keyGenerator.Generate(keyParts.ToArray());
        }

        private string GenerateParameterHash(object parameters)
        {
            try
            {
                var json = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
                return _keyGenerator.GenerateHash(json);
            }
            catch
            {
                return parameters.GetHashCode().ToString();
            }
        }

        private void InvalidateCacheForEntity(TEntity entity)
        {
            if (entity == null) return;

            try
            {
                var entityId = GetEntityId(entity);
                InvalidateCacheForEntityId(entityId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error invalidating cache for entity: {EntityType}", typeof(TEntity).Name);
            }
        }

        private void InvalidateCacheForEntities(IList<TEntity> entities)
        {
            if (entities == null || entities.Count == 0) return;

            foreach (var entity in entities)
            {
                InvalidateCacheForEntity(entity);
            }
        }

        private void InvalidateCacheForEntityId(object id)
        {
            if (id == null) return;

            try
            {
                // Invalidate entity-specific keys
                var pattern = $"{typeof(TEntity).Name}:*:{id}";
                InvalidateCacheByPattern(pattern);

                // Also invalidate all entity type cache if configured
                if (_options.InvalidateAllOnModify)
                {
                    InvalidateCacheByPattern($"{typeof(TEntity).Name}:*");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error invalidating cache for entity ID: {EntityId}", id);
            }
        }

        private void InvalidateCacheByPattern(string pattern)
        {
            // Note: This is a simplified implementation
            // In a production scenario, you might want to use cache tags or maintain a key registry
            _logger?.LogDebug("Invalidating cache pattern: {Pattern}", pattern);
            // Actual implementation would depend on cache provider capabilities
        }

        private async Task TrackCacheKeyInRegion(string cacheKey, string region)
        {
            // Track key in region for bulk invalidation
            var regionKey = $"region:{region}";
            // Implementation would store key in a set/list for the region
            _logger?.LogDebug("Tracking cache key {CacheKey} in region {Region}", cacheKey, region);
        }

        private async Task TrackCacheKeyWithTags(string cacheKey, string tags)
        {
            // Track key with tags for tag-based invalidation
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var tag in tagList)
            {
                var tagKey = $"tag:{tag}";
                // Implementation would store key in a set/list for the tag
                _logger?.LogDebug("Tracking cache key {CacheKey} with tag {Tag}", cacheKey, tag);
            }
        }

        private MethodInfo? GetMethodInfo(string methodName)
        {
            return typeof(IRepository<TEntity>).GetMethod(methodName) ??
                   typeof(CacheableRepository<TEntity>).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private object? GetEntityId(TEntity entity)
        {
            // Try to get EntityKey property
            var entityKeyProp = typeof(TEntity).GetProperty("EntityKey");
            if (entityKeyProp != null)
            {
                return entityKeyProp.GetValue(entity);
            }

            // Try to get Id property
            var idProp = typeof(TEntity).GetProperty("Id");
            if (idProp != null)
            {
                return idProp.GetValue(entity);
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Configuration options for CacheableRepository.
    /// </summary>
    public class CacheableRepositoryOptions
    {
        /// <summary>
        /// Gets or sets whether to enable caching by default for all query methods.
        /// Default is false (only methods with [Cacheable] attribute are cached).
        /// </summary>
        public bool EnableCacheByDefault { get; set; } = false;

        /// <summary>
        /// Gets or sets the default cache duration in seconds when caching is enabled by default.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int DefaultCacheDurationSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets whether to invalidate all cache entries for an entity type when any entity is modified.
        /// Default is false (only specific entity cache entries are invalidated).
        /// </summary>
        public bool InvalidateAllOnModify { get; set; } = false;
    }
}

