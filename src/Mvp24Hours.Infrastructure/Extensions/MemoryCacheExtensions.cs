//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class MemoryCacheExtensions
    {
        private static readonly Func<MemoryCache, object> GetEntriesCollection = Delegate.CreateDelegate(
            typeof(Func<MemoryCache, object>),
            typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(true),
            throwOnBindFailure: true) as Func<MemoryCache, object>;

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable GetKeys(this IMemoryCache memoryCache) =>
            ((IDictionary)GetEntriesCollection((MemoryCache)memoryCache)).Keys;

        /// <summary>
        /// 
        /// </summary>
        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
            GetKeys(memoryCache).OfType<T>();
    }
}
