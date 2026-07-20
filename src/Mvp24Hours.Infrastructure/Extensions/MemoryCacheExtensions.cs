//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections;
using Microsoft.Extensions.Caching.Memory;

namespace Mvp24Hours.Extensions;

/// <summary>
/// 
/// </summary>
public static class MemoryCacheExtensions
{
    /// <summary>
    /// 
    /// </summary>
    public static IEnumerable GetKeys(this IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);

        if (memoryCache is MemoryCache cache)
        {
            return cache.Keys;
        }

        throw new ArgumentException("Memory cache must be an instance of MemoryCache.", nameof(memoryCache));
    }

    /// <summary>
    /// 
    /// </summary>
    public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache)
    {
        return GetKeys(memoryCache).OfType<T>();
    }
}
