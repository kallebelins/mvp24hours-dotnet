//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Mvp24Hours.Infrastructure.Caching.Helpers
{
    internal static class CacheConfigHelper
    {
        public static DateTimeOffset? AbsoluteExpiration { get; set; }
        public static TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public static TimeSpan? SlidingExpiration { get; set; }

        public static DistributedCacheEntryOptions GetCacheOptions(DateTimeOffset? time = default)
        {
            return new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = time ?? CacheConfigHelper.AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = CacheConfigHelper.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = CacheConfigHelper.SlidingExpiration
            };
        }
    }
}
