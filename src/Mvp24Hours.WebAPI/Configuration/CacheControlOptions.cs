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
    /// Options for configuring Cache-Control header policies.
    /// </summary>
    public class CacheControlOptions
    {
        /// <summary>
        /// Gets or sets whether Cache-Control middleware is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default Cache-Control policy.
        /// </summary>
        public CacheControlPolicy? DefaultPolicy { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of route-specific Cache-Control policies.
        /// Key is the route pattern (e.g., "/api/users/*"), value is the policy.
        /// </summary>
        public Dictionary<string, CacheControlPolicy> RoutePolicies { get; set; } = new Dictionary<string, CacheControlPolicy>();

        /// <summary>
        /// Gets or sets the list of paths to exclude from Cache-Control header setting.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Represents a Cache-Control header policy.
    /// </summary>
    public class CacheControlPolicy
    {
        /// <summary>
        /// Gets or sets the maximum age for which the response is considered fresh.
        /// </summary>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Gets or sets the shared maximum age for shared caches.
        /// </summary>
        public TimeSpan? SharedMaxAge { get; set; }

        /// <summary>
        /// Gets or sets whether the response can be cached by public caches.
        /// </summary>
        public bool? Public { get; set; }

        /// <summary>
        /// Gets or sets whether the response can be cached by private caches only.
        /// </summary>
        public bool? Private { get; set; }

        /// <summary>
        /// Gets or sets whether the response must not be cached.
        /// </summary>
        public bool? NoCache { get; set; }

        /// <summary>
        /// Gets or sets whether the response must not be stored.
        /// </summary>
        public bool? NoStore { get; set; }

        /// <summary>
        /// Gets or sets whether the response must be revalidated before reuse.
        /// </summary>
        public bool? MustRevalidate { get; set; }

        /// <summary>
        /// Gets or sets whether the response can be served stale if revalidation fails.
        /// </summary>
        public bool? ProxyRevalidate { get; set; }

        /// <summary>
        /// Gets or sets whether the response can be served stale during revalidation.
        /// </summary>
        public bool? StaleWhileRevalidate { get; set; }

        /// <summary>
        /// Gets or sets whether the response can be served stale if an error occurs.
        /// </summary>
        public bool? StaleIfError { get; set; }

        /// <summary>
        /// Gets or sets the immutable directive (response never changes).
        /// </summary>
        public bool? Immutable { get; set; }
    }
}

