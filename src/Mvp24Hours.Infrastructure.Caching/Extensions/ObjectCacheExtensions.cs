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

namespace Mvp24Hours.Extensions
{
    public static class ObjectCacheExtensions
    {
        public static T GetObject<T>(this IDistributedCache cache, string key, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-getobject-start");
            try
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
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-getobject-end"); }
        }

        public static void SetObject<T>(this IDistributedCache cache, string key, T value, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                cache.SetString(key, result);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-end"); }
        }

        public static void SetObject<T>(this IDistributedCache cache, string key, T value, int minutes, JsonSerializerSettings jsonSerializerSettings = null)
            where T : class
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                cache.SetString(key, result, minutes);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-end"); }
        }

        public static void SetObject(this IDistributedCache cache, string key, object value, DateTimeOffset time, JsonSerializerSettings jsonSerializerSettings = null)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-start");
            try
            {
                if (cache == null || !key.HasValue() || value == null)
                {
                    return;
                }
                string result = value.ToSerialize(jsonSerializerSettings);
                cache.SetString(key, result, time);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-objectcacheextensions-setobject-end"); }
        }
    }
}
