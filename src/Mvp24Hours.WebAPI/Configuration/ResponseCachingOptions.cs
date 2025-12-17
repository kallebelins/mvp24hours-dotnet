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
    /// Options for configuring response caching.
    /// </summary>
    public class ResponseCachingOptions
    {
        /// <summary>
        /// Gets or sets whether response caching is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default cache profile name.
        /// </summary>
        public string? DefaultProfile { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of cache profiles.
        /// </summary>
        public Dictionary<string, CacheProfile> Profiles { get; set; } = new Dictionary<string, CacheProfile>();

        /// <summary>
        /// Gets or sets the maximum response body size (in bytes) to cache.
        /// Responses larger than this will not be cached.
        /// </summary>
        public long MaximumBodySize { get; set; } = 100 * 1024; // 100KB

        /// <summary>
        /// Gets or sets the size limit for the response cache (in bytes).
        /// </summary>
        public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the list of paths to exclude from caching.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets whether to vary cache by query string keys.
        /// </summary>
        public bool VaryByQueryKeys { get; set; } = true;
    }

    /// <summary>
    /// Represents a cache profile configuration.
    /// </summary>
    public class CacheProfile
    {
        /// <summary>
        /// Gets or sets the duration in seconds for which the response is cached.
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Gets or sets the location where the response can be cached.
        /// </summary>
        public ResponseCacheLocation Location { get; set; } = ResponseCacheLocation.Any;

        /// <summary>
        /// Gets or sets whether the response can be cached by clients.
        /// </summary>
        public bool NoStore { get; set; }

        /// <summary>
        /// Gets or sets the value for the Vary response header.
        /// </summary>
        public string? VaryByHeader { get; set; }

        /// <summary>
        /// Gets or sets the query string keys to vary the cache by.
        /// </summary>
        public string[]? VaryByQueryKeys { get; set; }
    }

    /// <summary>
    /// Specifies the location where the response can be cached.
    /// </summary>
    public enum ResponseCacheLocation
    {
        /// <summary>
        /// Cached in both client and proxy caches.
        /// </summary>
        Any = 0,

        /// <summary>
        /// Cached only in client caches.
        /// </summary>
        Client = 1,

        /// <summary>
        /// Cached only in proxy caches.
        /// </summary>
        Proxy = 2,

        /// <summary>
        /// Not cached.
        /// </summary>
        None = 3
    }
}

