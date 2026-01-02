//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Cache;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Cache
{
    /// <summary>
    /// Default implementation of <see cref="ICacheInvalidator"/> for automatic cache invalidation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides automatic cache invalidation when command operations
    /// modify data, ensuring cache consistency across the application.
    /// </para>
    /// </remarks>
    public class CacheInvalidator : ICacheInvalidator
    {
        private readonly IQueryCacheProvider _cacheProvider;
        private readonly IQueryCacheKeyGenerator _keyGenerator;
        private readonly ILogger<CacheInvalidator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheInvalidator"/> class.
        /// </summary>
        public CacheInvalidator(
            IQueryCacheProvider cacheProvider,
            IQueryCacheKeyGenerator keyGenerator,
            ILogger<CacheInvalidator> logger)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task InvalidateEntityAsync<TEntity>(CancellationToken cancellationToken = default)
        {
            await InvalidateEntityAsync(typeof(TEntity), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task InvalidateEntityAsync(Type entityType, CancellationToken cancellationToken = default)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            _logger.LogDebug("application-cache-invalidateentity-start EntityType={EntityType}", entityType.Name);

            try
            {
                // Invalidate by region (entity type name)
                var region = _keyGenerator.GenerateRegionKey(entityType);
                await _cacheProvider.InvalidateRegionAsync(region, cancellationToken);

                // Also invalidate by pattern
                var pattern = _keyGenerator.GenerateInvalidationPattern(entityType);
                await _cacheProvider.InvalidateByPatternAsync(pattern, cancellationToken);

                _logger.LogDebug("Cache invalidated for entity type: {EntityType}", entityType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for entity type: {EntityType}", entityType.Name);
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidateentity-end EntityType={EntityType}", entityType.Name);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default)
        {
            if (id == null)
            {
                return;
            }

            _logger.LogDebug("application-cache-invalidatebyid-start EntityType={EntityType} EntityId={EntityId}", typeof(TEntity).Name, id);

            try
            {
                // Generate possible cache keys for this entity ID
                var entityType = typeof(TEntity);
                
                // Common key patterns for GetById operations
                var keys = new[]
                {
                    $"{entityType.Name}:GetById:{id}",
                    $"{entityType.Name}:GetByIdAsync:{id}",
                    $"{entityType.Name}:{id}",
                    $"{entityType.Name}:id={id}"
                };

                await InvalidateKeysAsync(keys, cancellationToken);

                _logger.LogDebug("Cache invalidated for entity: {EntityType}, Id: {EntityId}", entityType.Name, id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache for entity by ID: {EntityType}, Id: {EntityId}", typeof(TEntity).Name, id);
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidatebyid-end EntityType={EntityType} EntityId={EntityId}", typeof(TEntity).Name, id);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateRegionAsync(string region, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return;
            }

            _logger.LogDebug("application-cache-invalidateregion-start Region={Region}", region);

            try
            {
                await _cacheProvider.InvalidateRegionAsync(region, cancellationToken);
                _logger.LogDebug("Cache region invalidated: {Region}", region);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache region: {Region}", region);
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidateregion-end Region={Region}", region);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
        {
            if (tags == null || tags.Length == 0)
            {
                return;
            }

            _logger.LogDebug("application-cache-invalidatebytags-start Tags={Tags}", string.Join(",", tags));

            try
            {
                // Tags are implemented as regions in this implementation
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        await _cacheProvider.InvalidateRegionAsync($"tag:{tag}", cancellationToken);
                    }
                }

                _logger.LogDebug("Cache invalidated for tags: {Tags}", string.Join(", ", tags));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidatebytags-end Tags={Tags}", string.Join(",", tags));
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return;
            }

            _logger.LogDebug("application-cache-invalidatebypattern-start Pattern={Pattern}", pattern);

            try
            {
                await _cacheProvider.InvalidateByPatternAsync(pattern, cancellationToken);
                _logger.LogDebug("Cache invalidated by pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache by pattern: {Pattern}", pattern);
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidatebypattern-end Pattern={Pattern}", pattern);
            }
        }

        /// <inheritdoc/>
        public async Task InvalidateKeysAsync(string[] keys, CancellationToken cancellationToken = default)
        {
            if (keys == null || keys.Length == 0)
            {
                return;
            }

            _logger.LogDebug("application-cache-invalidatekeys-start Keys={Keys}", string.Join(",", keys));

            try
            {
                foreach (var key in keys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        await _cacheProvider.RemoveAsync(key, cancellationToken);
                    }
                }

                _logger.LogDebug("Cache keys invalidated: {KeyCount} keys", keys.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache keys");
            }
            finally
            {
                _logger.LogDebug("application-cache-invalidatekeys-end Keys={Keys}", string.Join(",", keys));
            }
        }
    }
}

