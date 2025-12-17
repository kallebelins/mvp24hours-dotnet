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
    /// Rate limiting algorithm types.
    /// </summary>
    public enum RateLimitingAlgorithm
    {
        /// <summary>
        /// Fixed window rate limiting. Counts requests in fixed time windows.
        /// Simple but can allow bursts at window boundaries.
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Sliding window rate limiting. Smooths the fixed window boundaries.
        /// More accurate but slightly more complex.
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Token bucket rate limiting. Allows controlled bursts with smooth refill.
        /// Good for APIs that allow occasional bursts.
        /// </summary>
        TokenBucket,

        /// <summary>
        /// Concurrency limiter. Limits concurrent requests instead of rate.
        /// Useful for resource-intensive endpoints.
        /// </summary>
        Concurrency
    }

    /// <summary>
    /// Rate limiting key source options.
    /// </summary>
    [Flags]
    public enum RateLimitKeySource
    {
        /// <summary>
        /// No specific key source - global rate limit.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rate limit by client IP address.
        /// </summary>
        ClientIp = 1,

        /// <summary>
        /// Rate limit by authenticated user identity.
        /// </summary>
        UserId = 2,

        /// <summary>
        /// Rate limit by API key.
        /// </summary>
        ApiKey = 4,

        /// <summary>
        /// Rate limit by tenant ID for multi-tenant applications.
        /// </summary>
        TenantId = 8,

        /// <summary>
        /// Rate limit by custom header value.
        /// </summary>
        CustomHeader = 16
    }

    /// <summary>
    /// Configuration options for a rate limiting policy.
    /// </summary>
    public class RateLimitPolicy
    {
        /// <summary>
        /// Gets or sets the policy name.
        /// </summary>
        public string Name { get; set; } = "default";

        /// <summary>
        /// Gets or sets the rate limiting algorithm to use.
        /// Default: SlidingWindow.
        /// </summary>
        public RateLimitingAlgorithm Algorithm { get; set; } = RateLimitingAlgorithm.SlidingWindow;

        /// <summary>
        /// Gets or sets the key source for rate limiting.
        /// Default: ClientIp.
        /// </summary>
        public RateLimitKeySource KeySource { get; set; } = RateLimitKeySource.ClientIp;

        /// <summary>
        /// Gets or sets the maximum number of requests/tokens allowed in the time window.
        /// </summary>
        public int PermitLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time window duration for the rate limit.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the number of segments per window (for sliding window only).
        /// Default: 4.
        /// </summary>
        public int SegmentsPerWindow { get; set; } = 4;

        /// <summary>
        /// Gets or sets the token replenishment period (for token bucket only).
        /// </summary>
        public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the number of tokens to add per replenishment period (for token bucket only).
        /// </summary>
        public int TokensPerPeriod { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to auto-replenish tokens (for token bucket only).
        /// Default: true.
        /// </summary>
        public bool AutoReplenishment { get; set; } = true;

        /// <summary>
        /// Gets or sets the queue limit for requests waiting for permits.
        /// Default: 0 (no queuing).
        /// </summary>
        public int QueueLimit { get; set; } = 0;

        /// <summary>
        /// Gets or sets the queue processing order.
        /// Default: OldestFirst.
        /// </summary>
        public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;

        /// <summary>
        /// Gets or sets a custom header name for rate limit key (when KeySource includes CustomHeader).
        /// </summary>
        public string? CustomHeaderName { get; set; }

        /// <summary>
        /// Gets or sets paths that this policy applies to (supports wildcards).
        /// Empty list means all paths.
        /// </summary>
        public List<string> AppliedPaths { get; set; } = new();

        /// <summary>
        /// Gets or sets paths excluded from this policy (supports wildcards).
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new();
    }

    /// <summary>
    /// Queue processing order for rate limiting.
    /// </summary>
    public enum QueueProcessingOrder
    {
        /// <summary>
        /// Process oldest requests first (FIFO).
        /// </summary>
        OldestFirst,

        /// <summary>
        /// Process newest requests first (LIFO).
        /// </summary>
        NewestFirst
    }

    /// <summary>
    /// Configuration options for rate limiting middleware.
    /// </summary>
    public class RateLimitingOptions
    {
        /// <summary>
        /// Gets or sets whether rate limiting is enabled.
        /// Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default policy name to apply when no specific policy matches.
        /// </summary>
        public string DefaultPolicyName { get; set; } = "default";

        /// <summary>
        /// Gets or sets whether to include rate limit headers in responses.
        /// Default: true.
        /// </summary>
        public bool IncludeRateLimitHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the header name for rate limit.
        /// Default: "X-RateLimit-Limit".
        /// </summary>
        public string RateLimitHeaderName { get; set; } = "X-RateLimit-Limit";

        /// <summary>
        /// Gets or sets the header name for remaining requests.
        /// Default: "X-RateLimit-Remaining".
        /// </summary>
        public string RateLimitRemainingHeaderName { get; set; } = "X-RateLimit-Remaining";

        /// <summary>
        /// Gets or sets the header name for reset time.
        /// Default: "X-RateLimit-Reset".
        /// </summary>
        public string RateLimitResetHeaderName { get; set; } = "X-RateLimit-Reset";

        /// <summary>
        /// Gets or sets the header name for retry after (when rate limited).
        /// Default: "Retry-After".
        /// </summary>
        public string RetryAfterHeaderName { get; set; } = "Retry-After";

        /// <summary>
        /// Gets or sets the header name for API key extraction.
        /// Default: "X-Api-Key".
        /// </summary>
        public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

        /// <summary>
        /// Gets or sets the header name for tenant ID extraction.
        /// Default: "X-Tenant-Id".
        /// </summary>
        public string TenantIdHeaderName { get; set; } = "X-Tenant-Id";

        /// <summary>
        /// Gets or sets the forwarded header name for client IP (when behind proxy).
        /// Default: "X-Forwarded-For".
        /// </summary>
        public string ForwardedForHeaderName { get; set; } = "X-Forwarded-For";

        /// <summary>
        /// Gets or sets whether to use forwarded headers for IP extraction.
        /// Default: true.
        /// </summary>
        public bool UseForwardedHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the HTTP status code to return when rate limited.
        /// Default: 429 (Too Many Requests).
        /// </summary>
        public int RateLimitedStatusCode { get; set; } = 429;

        /// <summary>
        /// Gets or sets the response message when rate limited.
        /// </summary>
        public string RateLimitedMessage { get; set; } = "Too many requests. Please try again later.";

        /// <summary>
        /// Gets or sets whether to use ProblemDetails format for rate limit responses.
        /// Default: true.
        /// </summary>
        public bool UseProblemDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets globally excluded paths from rate limiting (supports wildcards).
        /// </summary>
        public List<string> GlobalExcludedPaths { get; set; } = new()
        {
            "/health",
            "/health/*",
            "/swagger",
            "/swagger/*"
        };

        /// <summary>
        /// Gets or sets the configured rate limiting policies.
        /// </summary>
        public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();

        /// <summary>
        /// Gets or sets endpoint-specific policy assignments.
        /// Key: endpoint path pattern, Value: policy name.
        /// </summary>
        public Dictionary<string, string> EndpointPolicies { get; set; } = new();

        /// <summary>
        /// Gets or sets whitelisted IP addresses that bypass rate limiting.
        /// Supports CIDR notation.
        /// </summary>
        public List<string> WhitelistedIps { get; set; } = new();

        /// <summary>
        /// Gets or sets whitelisted API keys that bypass rate limiting.
        /// </summary>
        public List<string> WhitelistedApiKeys { get; set; } = new();

        /// <summary>
        /// Gets or sets whitelisted user IDs that bypass rate limiting.
        /// </summary>
        public List<string> WhitelistedUserIds { get; set; } = new();

        /// <summary>
        /// Creates a default policy with the given settings.
        /// </summary>
        public void AddDefaultPolicy(int permitLimit = 100, TimeSpan? window = null)
        {
            Policies[DefaultPolicyName] = new RateLimitPolicy
            {
                Name = DefaultPolicyName,
                PermitLimit = permitLimit,
                Window = window ?? TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Adds a fixed window rate limiting policy.
        /// </summary>
        public RateLimitPolicy AddFixedWindowPolicy(string name, int permitLimit, TimeSpan window)
        {
            var policy = new RateLimitPolicy
            {
                Name = name,
                Algorithm = RateLimitingAlgorithm.FixedWindow,
                PermitLimit = permitLimit,
                Window = window
            };
            Policies[name] = policy;
            return policy;
        }

        /// <summary>
        /// Adds a sliding window rate limiting policy.
        /// </summary>
        public RateLimitPolicy AddSlidingWindowPolicy(string name, int permitLimit, TimeSpan window, int segmentsPerWindow = 4)
        {
            var policy = new RateLimitPolicy
            {
                Name = name,
                Algorithm = RateLimitingAlgorithm.SlidingWindow,
                PermitLimit = permitLimit,
                Window = window,
                SegmentsPerWindow = segmentsPerWindow
            };
            Policies[name] = policy;
            return policy;
        }

        /// <summary>
        /// Adds a token bucket rate limiting policy.
        /// </summary>
        public RateLimitPolicy AddTokenBucketPolicy(string name, int tokenLimit, TimeSpan replenishmentPeriod, int tokensPerPeriod)
        {
            var policy = new RateLimitPolicy
            {
                Name = name,
                Algorithm = RateLimitingAlgorithm.TokenBucket,
                PermitLimit = tokenLimit,
                ReplenishmentPeriod = replenishmentPeriod,
                TokensPerPeriod = tokensPerPeriod,
                AutoReplenishment = true
            };
            Policies[name] = policy;
            return policy;
        }

        /// <summary>
        /// Adds a concurrency limiter policy.
        /// </summary>
        public RateLimitPolicy AddConcurrencyPolicy(string name, int permitLimit, int queueLimit = 0)
        {
            var policy = new RateLimitPolicy
            {
                Name = name,
                Algorithm = RateLimitingAlgorithm.Concurrency,
                PermitLimit = permitLimit,
                QueueLimit = queueLimit
            };
            Policies[name] = policy;
            return policy;
        }

        /// <summary>
        /// Maps an endpoint pattern to a specific policy.
        /// </summary>
        public void MapEndpointToPolicy(string endpointPattern, string policyName)
        {
            EndpointPolicies[endpointPattern] = policyName;
        }
    }

    /// <summary>
    /// Configuration options for distributed rate limiting with Redis.
    /// </summary>
    public class DistributedRateLimitingOptions
    {
        /// <summary>
        /// Gets or sets whether distributed rate limiting is enabled.
        /// Default: false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the Redis connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the Redis instance name prefix.
        /// Default: "mvp24h:ratelimit:".
        /// </summary>
        public string InstanceName { get; set; } = "mvp24h:ratelimit:";

        /// <summary>
        /// Gets or sets the default expiry time for rate limit keys.
        /// Default: 1 hour.
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the Redis database number.
        /// Default: 0.
        /// </summary>
        public int Database { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to use SSL for Redis connection.
        /// Default: false.
        /// </summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>
        /// Gets or sets the connection timeout in milliseconds.
        /// Default: 5000.
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the sync timeout in milliseconds.
        /// Default: 1000.
        /// </summary>
        public int SyncTimeout { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to fallback to in-memory rate limiting when Redis is unavailable.
        /// Default: true.
        /// </summary>
        public bool FallbackToInMemory { get; set; } = true;
    }
}

