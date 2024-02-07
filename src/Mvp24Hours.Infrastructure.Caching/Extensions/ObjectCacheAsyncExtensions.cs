//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    public static class ObjectCacheAsyncExtensions
    {
        public static async Task<T> GetObjectAsync<T>(this IDistributedCache cache, string key, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-getobject-start");
            try
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-getobject-end"); }
        }

        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                await cache.SetStringAsync(key, result, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-end"); }
        }

        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, int minutes, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                await cache.SetStringAsync(key, result, minutes, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-end"); }
        }

        public static async Task SetObjectAsync(this IDistributedCache cache, string key, object value, DateTimeOffset time, JsonSerializerSettings jsonSerializerSettings = null, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                await cache.SetStringAsync(key, result, time, cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheasyncextensions-setobjectasync-end"); }
        }
    }
}
