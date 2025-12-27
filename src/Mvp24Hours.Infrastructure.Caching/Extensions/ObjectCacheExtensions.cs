//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for IDistributedCache to work with objects (serialization).
    /// </summary>
    public static class ObjectCacheExtensions
    {
        /// <summary>
        /// Gets an object from the cache by deserializing the cached string value.
        /// </summary>
        /// <typeparam name="T">The type of object to retrieve.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        /// <returns>The deserialized object or default if not found.</returns>
        public static T GetObject<T>(this IDistributedCache cache, string key, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            if (cache == null || !key.HasValue())
            {
                return default;
            }
            string value = cache.GetString(key);
            if (!value.HasValue())
            {
                return default;
            }
            return value.ToDeserialize<T>(jsonSerializerSettings);
        }

        /// <summary>
        /// Sets an object in the cache by serializing it to JSON.
        /// </summary>
        /// <typeparam name="T">The type of object to cache.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        public static void SetObject<T>(this IDistributedCache cache, string key, T value, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            cache.SetString(key, result);
        }

        /// <summary>
        /// Sets an object in the cache with expiration in minutes.
        /// </summary>
        /// <typeparam name="T">The type of object to cache.</typeparam>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="minutes">Expiration time in minutes.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        public static void SetObject<T>(this IDistributedCache cache, string key, T value, int minutes, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            cache.SetString(key, result, minutes);
        }

        /// <summary>
        /// Sets an object in the cache with absolute expiration time.
        /// </summary>
        /// <param name="cache">The distributed cache instance.</param>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="time">Absolute expiration time.</param>
        /// <param name="jsonSerializerSettings">Optional JSON serializer settings.</param>
        public static void SetObject(this IDistributedCache cache, string key, object value, DateTimeOffset time, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (cache == null || !key.HasValue() || value == null)
            {
                return;
            }
            string result = value.ToSerialize(jsonSerializerSettings);
            cache.SetString(key, result, time);
        }
    }
}
