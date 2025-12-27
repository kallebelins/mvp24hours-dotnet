//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Async extension methods for IDistributedCache to work with objects (serialization).
    /// </summary>
    public static class ObjectCacheAsyncExtensions
    {
        /// <summary>
        /// Gets an object from the cache asynchronously by deserializing the cached string value.
        /// </summary>
        /// <typeparam name="T">The type of object to retrieve.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object or default if not found.</returns>
        public static async Task<T> GetObjectAsync<T>(this IDistributedCache cache, string key, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            if (cache == null || !key.HasValue())
            {
                return default;
            }
            string value = await cache.GetStringAsync(key, cancellationToken);
            if (!value.HasValue())
            {
                return default;
            }
            return value.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// Sets an object in the cache asynchronously by serializing it to JSON.
        /// </summary>
        /// <typeparam name="T">The type of object to cache.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            await cache.SetStringAsync(key, result, cancellationToken);
        }

        /// <summary>
        /// Sets an object in the cache asynchronously with expiration in minutes.
        /// </summary>
        /// <typeparam name="T">The type of object to cache.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="minutes">Expiration time in minutes.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, int minutes, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            await cache.SetStringAsync(key, result, minutes, cancellationToken);
        }

        /// <summary>
        /// Sets an object in the cache asynchronously with absolute expiration time.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="time">Absolute expiration time.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SetObjectAsync(this IDistributedCache cache, string key, object value, DateTimeOffset time, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            await cache.SetStringAsync(key, result, time, cancellationToken);
        }
    }
}
