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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IRepositoryCacheAsync{T}"/>
    /// </summary>
    public class RepositoryCacheAsync<T>(IDistributedCache cache) : RepositoryCacheBase(cache), IRepositoryCacheAsync<T>
        where T : class
    {
        public virtual async Task<T> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-getasync-start");
            try
            {
                return await Cache.GetObjectAsync<T>(key, cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-getasync-end"); }
        }

        public virtual async Task<string> GetStringAsync(string key, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-getstringasync-start");
            try
            {
                return await Cache.GetStringAsync(key, token: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-getstringasync-end"); }
        }

        public virtual async Task SetAsync(string key, T model, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-setasync-start");
            try
            {
                await Cache.SetObjectAsync(key, model, cancellationToken: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-setasync-end"); }
        }

        public virtual async Task SetStringAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-setstringasync-start");
            try
            {
                await Cache.SetStringAsync(key, value, token: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-setstringasync-end"); }
        }

        public virtual async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-removeasync-start");
            try
            {
                await Cache.RemoveAsync(key, token: cancellationToken);
            }
            finally { TelemetryHelper.Execute(TelemetryLevels.Verbose, "caching-repositorycacheasync-removeasync-end"); }
        }
    }
}
