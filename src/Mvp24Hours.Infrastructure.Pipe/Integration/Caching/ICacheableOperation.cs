//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Caching
{
    /// <summary>
    /// Marker interface for operations that support result caching.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    public interface ICacheableOperation<in TInput>
    {
        /// <summary>
        /// Gets the cache key for the given input.
        /// </summary>
        /// <param name="input">The operation input.</param>
        /// <returns>A unique cache key, or null to skip caching.</returns>
        string? GetCacheKey(TInput input);
    }

    /// <summary>
    /// Options for caching operation results.
    /// </summary>
    public class CacheOperationOptions
    {
        /// <summary>
        /// Gets or sets the default absolute expiration for cached results.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default sliding expiration for cached results.
        /// Default is null (no sliding expiration).
        /// </summary>
        public TimeSpan? DefaultSlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets whether to cache failed results.
        /// Default is false.
        /// </summary>
        public bool CacheFailedResults { get; set; }

        /// <summary>
        /// Gets or sets the prefix for cache keys.
        /// Default is "pipe:".
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "pipe:";

        /// <summary>
        /// Gets or sets whether to use compression for cached values.
        /// Default is false.
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// Gets or sets the minimum size in bytes for compression to be applied.
        /// Default is 1024 (1KB).
        /// </summary>
        public int CompressionThreshold { get; set; } = 1024;
    }
}

