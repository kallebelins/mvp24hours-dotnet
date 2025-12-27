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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IRepositoryCacheAsync{T}"/>
    /// </summary>
    public class RepositoryCacheAsync<T>(IDistributedCache cache, ILogger<RepositoryCacheAsync<T>> logger = null) : RepositoryCacheBase(cache), IRepositoryCacheAsync<T>
        where T : class
    {
        private readonly ILogger<RepositoryCacheAsync<T>> _logger = logger;

        public virtual async Task<T> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting cached object with key: {CacheKey}", key);
            try
            {
                var result = await Cache.GetObjectAsync<T>(key, cancellationToken: cancellationToken);
                _logger?.LogDebug("Cache {(CacheHit)} for key: {CacheKey}", result != null ? "HIT" : "MISS", key);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error getting cached object with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual async Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Getting cached string with key: {CacheKey}", key);
            try
            {
                var result = await Cache.GetStringAsync(key, token: cancellationToken);
                _logger?.LogDebug("Cache {(CacheHit)} for key: {CacheKey}", result != null ? "HIT" : "MISS", key);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error getting cached string with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual async Task SetAsync(string key, T model, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Setting cached object with key: {CacheKey}", key);
            try
            {
                await Cache.SetObjectAsync(key, model, cancellationToken: cancellationToken);
                _logger?.LogDebug("Object cached successfully with key: {CacheKey}", key);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error setting cached object with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual async Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Setting cached string with key: {CacheKey}", key);
            try
            {
                await Cache.SetStringAsync(key, value, token: cancellationToken);
                _logger?.LogDebug("String cached successfully with key: {CacheKey}", key);
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error setting cached string with key: {CacheKey}", key);
                throw;
            }
        }

        public virtual async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("Removing cached item with key: {CacheKey}", key);
            try
            {
                await Cache.RemoveAsync(key, token: cancellationToken);
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
