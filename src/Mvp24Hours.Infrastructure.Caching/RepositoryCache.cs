//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Caching.Base;

namespace Mvp24Hours.Infrastructure.Caching
{
    /// <summary>
    ///  <see cref="IRepositoryCache{T}"/>
    /// </summary>
    public class RepositoryCache<T>(IDistributedCache cache, ILogger<RepositoryCache<T>> logger = null) : RepositoryCacheBase(cache), IRepositoryCache<T>
        where T : class
    {
        private readonly ILogger<RepositoryCache<T>> _logger = logger;

        public virtual T Get(string key)
        {
            _logger?.LogDebug("Getting cached object with key: {CacheKey}", key);
            try
            {
                var result = Cache.GetObject<T>(key);
                _logger?.LogDebug("Cache {(CacheHit)} for key: {CacheKey}", result != null ? "HIT" : "MISS", key);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error getting cached object with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual string GetString(string key)
        {
            _logger?.LogDebug("Getting cached string with key: {CacheKey}", key);
            try
            {
                var result = Cache.GetString(key);
                _logger?.LogDebug("Cache {(CacheHit)} for key: {CacheKey}", result != null ? "HIT" : "MISS", key);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error getting cached string with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual void Set(string key, T model)
        {
            _logger?.LogDebug("Setting cached object with key: {CacheKey}", key);
            try
            {
                Cache.SetObject(key, model);
                _logger?.LogDebug("Object cached successfully with key: {CacheKey}", key);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error setting cached object with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual void SetString(string key, string value)
        {
            _logger?.LogDebug("Setting cached string with key: {CacheKey}", key);
            try
            {
                Cache.SetString(key, value);
                _logger?.LogDebug("String cached successfully with key: {CacheKey}", key);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error setting cached string with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual void Remove(string key)
        {
            _logger?.LogDebug("Removing cached item with key: {CacheKey}", key);
            try
            {
                Cache.Remove(key);
                _logger?.LogDebug("Cache item removed successfully with key: {CacheKey}", key);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error removing cached item with key: {CacheKey}", key);
                throw;
            }
        }
    }
}
