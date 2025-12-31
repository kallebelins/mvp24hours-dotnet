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
    /// <remarks>
    /// <para>
    /// Output caching stores HTTP responses on the server, reducing server load
    /// and improving response times for cacheable content. Unlike response caching
    /// (which uses HTTP cache headers), output caching is controlled entirely by
    /// the server.
    /// </para>
    /// <para>
    /// Key differences from Response Caching:
    /// <list type="bullet">
    /// <item>Server-side storage (not dependent on client/proxy cache headers)</item>
    /// <item>Tag-based invalidation for selective cache clearing</item>
    /// <item>Support for distributed cache backends (Redis)</item>
    /// <item>Policy-based configuration with named policies</item>
    /// </list>
    /// </para>
    /// </remarks>
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
        /// Default is 100MB.
        /// </summary>
        public long MaximumBodySize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the size limit for the in-memory cache (in bytes).
        /// Default is 100MB.
        /// </summary>
        public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the dictionary of named cache policies.
        /// </summary>
        public Dictionary<string, OutputCachePolicyOptions> Policies { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of paths to exclude from output caching.
        /// Supports wildcards: /api/admin/* excludes all admin endpoints.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to use distributed cache (Redis) for output caching.
        /// When true, requires Redis to be configured via AddStackExchangeRedisOutputCache().
        /// </summary>
        public bool UseDistributedCache { get; set; } = false;

        /// <summary>
        /// Gets or sets the Redis connection string when using distributed cache.
        /// </summary>
        public string? RedisConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Redis instance name prefix.
        /// Default is "mvp24h-oc:".
        /// </summary>
        public string RedisInstanceName { get; set; } = "mvp24h-oc:";

        /// <summary>
        /// Gets or sets whether to respect the Vary header from the response.
        /// </summary>
        public bool RespectVaryHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use case-sensitive paths for cache keys.
        /// </summary>
        public bool UseCaseSensitivePaths { get; set; } = false;

        /// <summary>
        /// Gets or sets the default policy name.
        /// When set, this policy is applied to all cacheable endpoints by default.
        /// </summary>
        public string? DefaultPolicyName { get; set; }

        /// <summary>
        /// Gets or sets whether to include query string in cache key by default.
        /// </summary>
        public bool VaryByQueryStringByDefault { get; set; } = true;

        /// <summary>
        /// Gets or sets the HTTP methods that are cacheable.
        /// Default is GET and HEAD.
        /// </summary>
        public HashSet<string> CacheableMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET",
            "HEAD"
        };

        /// <summary>
        /// Gets or sets the default content types that can be cached.
        /// Empty set means all content types are cacheable.
        /// </summary>
        public HashSet<string> CacheableContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the status codes that can be cached.
        /// Default is 200 (OK) only.
        /// </summary>
        public HashSet<int> CacheableStatusCodes { get; set; } = new() { 200 };

        /// <summary>
        /// Adds a named policy with the specified configuration.
        /// </summary>
        /// <param name="name">The policy name.</param>
        /// <param name="configure">Action to configure the policy.</param>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddPolicy(string name, Action<OutputCachePolicyOptions> configure)
        {
            var policy = new OutputCachePolicyOptions();
            configure(policy);
            Policies[name] = policy;
            return this;
        }

        /// <summary>
        /// Adds the "Default" policy with the specified expiration.
        /// </summary>
        /// <param name="expiration">The cache expiration duration.</param>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddDefaultPolicy(TimeSpan expiration)
        {
            return AddPolicy("Default", p => p.Expire(expiration));
        }

        /// <summary>
        /// Adds a "NoCache" policy that disables caching.
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddNoCachePolicy()
        {
            return AddPolicy("NoCache", p => p.NoCache = true);
        }

        /// <summary>
        /// Adds a short-lived cache policy (1 minute).
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddShortPolicy()
        {
            return AddPolicy("Short", p => p.Expire(TimeSpan.FromMinutes(1)));
        }

        /// <summary>
        /// Adds a medium-lived cache policy (10 minutes).
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddMediumPolicy()
        {
            return AddPolicy("Medium", p => p.Expire(TimeSpan.FromMinutes(10)));
        }

        /// <summary>
        /// Adds a long-lived cache policy (1 hour).
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddLongPolicy()
        {
            return AddPolicy("Long", p => p.Expire(TimeSpan.FromHours(1)));
        }

        /// <summary>
        /// Adds the standard set of policies (Default, Short, Medium, Long, NoCache).
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public OutputCachingOptions AddStandardPolicies()
        {
            AddDefaultPolicy(DefaultExpirationTimeSpan);
            AddShortPolicy();
            AddMediumPolicy();
            AddLongPolicy();
            AddNoCachePolicy();
            return this;
        }
    }

    /// <summary>
    /// Represents an output cache policy configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Policies define how responses are cached and invalidated.
    /// Named policies can be referenced by endpoints using the [OutputCache(PolicyName = "...")] attribute.
    /// </para>
    /// </remarks>
    public class OutputCachePolicyOptions
    {
        /// <summary>
        /// Gets or sets the expiration time span.
        /// </summary>
        public TimeSpan? ExpirationTimeSpan { get; set; }

        /// <summary>
        /// Gets or sets whether to disable caching for this policy.
        /// </summary>
        public bool NoCache { get; set; }

        /// <summary>
        /// Gets or sets the tags for cache invalidation.
        /// Tags allow selective cache eviction: cache.EvictByTagAsync("products").
        /// </summary>
        public HashSet<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the headers to vary the cache by.
        /// Different header values result in different cache entries.
        /// </summary>
        public HashSet<string> VaryByHeader { get; set; } = new();

        /// <summary>
        /// Gets or sets the query string keys to vary the cache by.
        /// </summary>
        public HashSet<string> VaryByQueryKeys { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to vary by all query string keys.
        /// </summary>
        public bool VaryByAllQueryKeys { get; set; }

        /// <summary>
        /// Gets or sets custom values to vary the cache by.
        /// Can be used for user-specific caching (e.g., user ID, tenant ID).
        /// </summary>
        public HashSet<string> VaryByValues { get; set; } = new();

        /// <summary>
        /// Gets or sets the route parameters to vary the cache by.
        /// </summary>
        public HashSet<string> VaryByRouteValue { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to lock during cache population to prevent stampede.
        /// </summary>
        public bool LockDuringPopulation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether authenticated requests can be cached.
        /// Default is false for security.
        /// </summary>
        public bool CacheAuthenticatedRequests { get; set; } = false;

        /// <summary>
        /// Sets the cache expiration.
        /// </summary>
        /// <param name="duration">The expiration duration.</param>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions Expire(TimeSpan duration)
        {
            ExpirationTimeSpan = duration;
            return this;
        }

        /// <summary>
        /// Adds tags for cache invalidation.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions SetTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                Tags.Add(tag);
            }
            return this;
        }

        /// <summary>
        /// Sets headers to vary the cache by.
        /// </summary>
        /// <param name="headers">The headers to vary by.</param>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions SetVaryByHeader(params string[] headers)
        {
            foreach (var header in headers)
            {
                VaryByHeader.Add(header);
            }
            return this;
        }

        /// <summary>
        /// Sets query string keys to vary the cache by.
        /// </summary>
        /// <param name="keys">The query string keys to vary by.</param>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions SetVaryByQuery(params string[] keys)
        {
            foreach (var key in keys)
            {
                VaryByQueryKeys.Add(key);
            }
            return this;
        }

        /// <summary>
        /// Sets route values to vary the cache by.
        /// </summary>
        /// <param name="routeValues">The route values to vary by.</param>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions SetVaryByRouteValue(params string[] routeValues)
        {
            foreach (var value in routeValues)
            {
                VaryByRouteValue.Add(value);
            }
            return this;
        }

        /// <summary>
        /// Enables caching for authenticated requests.
        /// Use with caution - ensure cache is varied by user if needed.
        /// </summary>
        /// <returns>The policy instance for chaining.</returns>
        public OutputCachePolicyOptions AllowAuthenticatedRequests()
        {
            CacheAuthenticatedRequests = true;
            return this;
        }
    }

    /// <summary>
    /// Preset cache policy types for common scenarios.
    /// </summary>
    public enum OutputCachePolicyPreset
    {
        /// <summary>
        /// No caching - responses are never cached.
        /// </summary>
        NoCache,

        /// <summary>
        /// Short cache duration (1 minute).
        /// Good for frequently changing data that benefits from minimal caching.
        /// </summary>
        Short,

        /// <summary>
        /// Medium cache duration (10 minutes).
        /// Good for moderately changing data like listings and search results.
        /// </summary>
        Medium,

        /// <summary>
        /// Long cache duration (1 hour).
        /// Good for rarely changing data like static content and reference data.
        /// </summary>
        Long,

        /// <summary>
        /// Very long cache duration (24 hours).
        /// Good for static content that rarely changes.
        /// </summary>
        VeryLong,

        /// <summary>
        /// Custom policy - use named policy.
        /// </summary>
        Custom
    }
}

