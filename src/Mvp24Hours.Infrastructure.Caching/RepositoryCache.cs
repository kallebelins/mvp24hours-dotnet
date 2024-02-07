//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Distributed;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Caching.Base;

namespace Mvp24Hours.Infrastructure.Caching
{
    /// <summary>
    ///  <see cref="IRepositoryCache{T}"/>
    /// </summary>
    public class RepositoryCache<T>(IDistributedCache cache) : RepositoryCacheBase(cache), IRepositoryCache<T>
        where T : class
    {
        public virtual T Get(string key)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-get-start");
            try
            {
                return Cache.GetObject<T>(key);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-get-end"); }
        }

        public virtual string GetString(string key)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-getstring-start");
            try
            {
                return Cache.GetString(key);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-getstring-end"); }
        }

        public virtual void Set(string key, T model)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-set-start");
            try
            {
                Cache.SetObject(key, model);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-set-end"); }
        }

        public virtual void SetString(string key, string value)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-setstring-start");
            try
            {
                Cache.SetString(key, value);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-setstring-end"); }
        }

        public virtual void Remove(string key)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-remove-start");
            try
            {
                Cache.Remove(key);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycache-remove-end"); }
        }
    }
}
