//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;

namespace Mvp24Hours.Infrastructure.Caching
{
    /// <summary>
    /// Global options for cache configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides global configuration options for the caching infrastructure,
    /// including default expiration times, key generation settings, and serialization options.
    /// </para>
    /// </remarks>
    public class CacheOptions
    {
        /// <summary>
        /// Gets or sets the default absolute expiration relative to now.
        /// Used when no expiration is specified in CacheEntryOptions.
        /// </summary>
        public TimeSpan? DefaultAbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default sliding expiration.
        /// Used when no sliding expiration is specified in CacheEntryOptions.
        /// </summary>
        public TimeSpan? DefaultSlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the default prefix for cache keys.
        /// </summary>
        public string? DefaultKeyPrefix { get; set; }

        /// <summary>
        /// Gets or sets the separator used for cache keys.
        /// </summary>
        public string KeySeparator { get; set; } = ":";

        /// <summary>
        /// Gets or sets whether to use hash-based keys for long/complex keys.
        /// </summary>
        public bool UseHashForLongKeys { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum key length before using hash.
        /// Only applies if UseHashForLongKeys is true.
        /// </summary>
        public int MaxKeyLength { get; set; } = 250;

        /// <summary>
        /// Gets or sets whether to compress large values.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum value size (in bytes) to compress.
        /// Only applies if EnableCompression is true.
        /// </summary>
        public int CompressionThresholdBytes { get; set; } = 1024; // 1KB

        /// <summary>
        /// Gets or sets the compression algorithm to use (Brotli or Gzip).
        /// Only applies if EnableCompression is true.
        /// </summary>
        public CompressionAlgorithm CompressionAlgorithm { get; set; } = CompressionAlgorithm.Brotli;

        /// <summary>
        /// Gets or sets the batch size for batch operations (GetManyAsync, SetManyAsync).
        /// Larger batches reduce network overhead but increase memory usage.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum concurrency for batch operations.
        /// </summary>
        public int MaxBatchConcurrency { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to enable cache prefetching.
        /// </summary>
        public bool EnablePrefetching { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable cache warming on startup.
        /// </summary>
        public bool EnableWarming { get; set; } = true;
    }
}

