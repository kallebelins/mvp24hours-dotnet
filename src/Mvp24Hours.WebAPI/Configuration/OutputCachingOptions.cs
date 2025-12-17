//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Options for configuring output caching (.NET 7+).
    /// </summary>
    public class OutputCachingOptions
    {
        /// <summary>
        /// Gets or sets whether output caching is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default cache duration.
        /// </summary>
        public TimeSpan DefaultExpirationTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the maximum cache size (in bytes).
        /// </summary>
        public long MaximumBodySize { get; set; } = 100 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the dictionary of named cache policies.
        /// </summary>
        public Dictionary<string, OutputCachePolicy> Policies { get; set; } = new Dictionary<string, OutputCachePolicy>();

        /// <summary>
        /// Gets or sets the list of paths to exclude from output caching.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets whether to use distributed cache (Redis) for output caching.
        /// </summary>
        public bool UseDistributedCache { get; set; } = false;
    }

    /// <summary>
    /// Represents an output cache policy configuration.
    /// </summary>
    public class OutputCachePolicy
    {
        /// <summary>
        /// Gets or sets the expiration time span.
        /// </summary>
        public TimeSpan? ExpirationTimeSpan { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time span.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the tags for cache invalidation.
        /// </summary>
        public HashSet<string> Tags { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the headers to vary the cache by.
        /// </summary>
        public HashSet<string> VaryByHeader { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the query string keys to vary the cache by.
        /// </summary>
        public HashSet<string> VaryByQueryKeys { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the value to vary the cache by.
        /// </summary>
        public string? VaryByValue { get; set; }
    }
}

