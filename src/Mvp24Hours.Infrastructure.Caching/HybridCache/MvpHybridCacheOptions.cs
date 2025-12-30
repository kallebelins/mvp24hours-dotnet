//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Caching.HybridCache
{
    /// <summary>
    /// Configuration options for HybridCache integration in Mvp24Hours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HybridCache (.NET 9) combines the best of both worlds:
    /// <list type="bullet">
    /// <item><strong>L1 Cache (In-Memory):</strong> Fast, local cache per application instance</item>
    /// <item><strong>L2 Cache (Distributed):</strong> Shared cache across all instances (Redis, SQL, etc.)</item>
    /// <item><strong>Stampede Protection:</strong> Built-in prevention of cache stampedes</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits over custom MultiLevelCache:</strong>
    /// <list type="bullet">
    /// <item>Native .NET 9 implementation with better performance</item>
    /// <item>Automatic L1/L2 synchronization</item>
    /// <item>Built-in serialization with configurable options</item>
    /// <item>Stampede protection without custom SemaphoreSlim</item>
    /// <item>Better integration with IDistributedCache backends</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic configuration
    /// services.AddMvpHybridCache();
    /// 
    /// // With Redis as L2
    /// services.AddMvpHybridCache(options =>
    /// {
    ///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
    ///     options.MaximumPayloadBytes = 1024 * 1024; // 1MB
    ///     options.UseRedisAsL2 = true;
    ///     options.RedisConnectionString = "localhost:6379";
    /// });
    /// </code>
    /// </example>
    public class MvpHybridCacheOptions
    {
        /// <summary>
        /// Gets or sets the default expiration time for cache entries.
        /// Default: 5 minutes.
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default local (L1) cache expiration time.
        /// If not set, uses DefaultExpiration.
        /// </summary>
        public TimeSpan? DefaultLocalCacheExpiration { get; set; }

        /// <summary>
        /// Gets or sets the maximum payload size in bytes for cached values.
        /// Values larger than this will not be cached in L1 (memory).
        /// Default: 1MB (1,048,576 bytes).
        /// </summary>
        public long MaximumPayloadBytes { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets the maximum key length.
        /// Keys longer than this will be hashed.
        /// Default: 1024 characters.
        /// </summary>
        public int MaximumKeyLength { get; set; } = 1024;

        /// <summary>
        /// Gets or sets whether to use Redis as the L2 (distributed) cache.
        /// If false, only in-memory L1 cache is used.
        /// </summary>
        public bool UseRedisAsL2 { get; set; }

        /// <summary>
        /// Gets or sets the Redis connection string.
        /// Required if UseRedisAsL2 is true.
        /// </summary>
        public string? RedisConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Redis instance name (prefix for keys).
        /// </summary>
        public string? RedisInstanceName { get; set; } = "mvp24h:";

        /// <summary>
        /// Gets or sets whether to enable stampede protection.
        /// Default: true.
        /// </summary>
        /// <remarks>
        /// When enabled, concurrent requests for the same key will wait for the first
        /// request to complete and share the result, preventing cache stampedes.
        /// </remarks>
        public bool EnableStampedeProtection { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to report tag-based statistics.
        /// Default: true.
        /// </summary>
        public bool ReportTagStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the default tags applied to all cache entries.
        /// Useful for global invalidation.
        /// </summary>
        public IList<string> DefaultTags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to use compression for large values.
        /// Default: false.
        /// </summary>
        public bool EnableCompression { get; set; }

        /// <summary>
        /// Gets or sets the minimum size in bytes for compression.
        /// Only values larger than this will be compressed.
        /// Default: 1024 bytes (1KB).
        /// </summary>
        public int CompressionThresholdBytes { get; set; } = 1024;

        /// <summary>
        /// Gets or sets whether to enable detailed logging.
        /// Default: false (only errors are logged).
        /// </summary>
        public bool EnableDetailedLogging { get; set; }

        /// <summary>
        /// Gets or sets the cache key prefix applied to all keys.
        /// Useful for multi-tenant scenarios.
        /// </summary>
        public string? KeyPrefix { get; set; }

        /// <summary>
        /// Gets or sets the serializer type to use.
        /// Default: SystemTextJson.
        /// </summary>
        public HybridCacheSerializerType SerializerType { get; set; } = HybridCacheSerializerType.SystemTextJson;

        /// <summary>
        /// Gets or sets custom serializer options (JSON options, MessagePack options, etc.).
        /// </summary>
        public object? SerializerOptions { get; set; }
    }

    /// <summary>
    /// Serializer types supported by HybridCache.
    /// </summary>
    public enum HybridCacheSerializerType
    {
        /// <summary>
        /// System.Text.Json serializer (default, best compatibility).
        /// </summary>
        SystemTextJson,

        /// <summary>
        /// MessagePack serializer (faster, smaller payloads).
        /// </summary>
        MessagePack,

        /// <summary>
        /// Custom serializer provided via IHybridCacheSerializer.
        /// </summary>
        Custom
    }
}

