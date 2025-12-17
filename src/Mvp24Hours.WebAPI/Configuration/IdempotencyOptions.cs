//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Net;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Source for extracting the idempotency key from HTTP requests.
    /// </summary>
    public enum IdempotencyKeySource
    {
        /// <summary>
        /// Extract idempotency key from HTTP header.
        /// </summary>
        Header,

        /// <summary>
        /// Generate idempotency key from request body hash.
        /// </summary>
        RequestBody,

        /// <summary>
        /// Use both header (preferred) and fall back to request body.
        /// </summary>
        HeaderOrRequestBody,

        /// <summary>
        /// Use custom key generator.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Storage type for idempotency records.
    /// </summary>
    public enum IdempotencyStorageType
    {
        /// <summary>
        /// In-memory storage (single instance only).
        /// </summary>
        InMemory,

        /// <summary>
        /// Distributed cache (IDistributedCache).
        /// </summary>
        DistributedCache,

        /// <summary>
        /// Custom storage implementation.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Configuration options for the idempotency middleware.
    /// Provides HTTP-level idempotency for POST, PUT, and PATCH requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The idempotency middleware ensures that duplicate requests with the same
    /// idempotency key return the cached response instead of re-executing the request.
    /// </para>
    /// <para>
    /// <strong>Supported HTTP Methods:</strong>
    /// <list type="bullet">
    /// <item>POST - Create operations</item>
    /// <item>PUT - Full update operations</item>
    /// <item>PATCH - Partial update operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Integration with CQRS:</strong>
    /// This middleware can work alongside the <c>IIdempotentCommand</c> interface
    /// from the CQRS module. When a command implements <c>IIdempotentCommand</c>,
    /// the middleware can use its idempotency key if configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMvp24HoursIdempotency(options =>
    /// {
    ///     options.Enabled = true;
    ///     options.KeySource = IdempotencyKeySource.Header;
    ///     options.HeaderName = "Idempotency-Key";
    ///     options.CacheDuration = TimeSpan.FromHours(24);
    ///     options.StorageType = IdempotencyStorageType.DistributedCache;
    /// });
    /// </code>
    /// </example>
    public class IdempotencyOptions
    {
        /// <summary>
        /// Gets or sets whether idempotency middleware is enabled.
        /// Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the source for extracting the idempotency key.
        /// Default: HeaderOrRequestBody.
        /// </summary>
        public IdempotencyKeySource KeySource { get; set; } = IdempotencyKeySource.HeaderOrRequestBody;

        /// <summary>
        /// Gets or sets the HTTP header name for the idempotency key.
        /// Default: "Idempotency-Key".
        /// </summary>
        /// <remarks>
        /// This follows the standard Idempotency-Key header convention used by
        /// Stripe, PayPal, and other major APIs.
        /// </remarks>
        public string HeaderName { get; set; } = "Idempotency-Key";

        /// <summary>
        /// Gets or sets the storage type for idempotency records.
        /// Default: DistributedCache.
        /// </summary>
        public IdempotencyStorageType StorageType { get; set; } = IdempotencyStorageType.DistributedCache;

        /// <summary>
        /// Gets or sets the duration to cache idempotency results.
        /// Default: 24 hours.
        /// </summary>
        /// <remarks>
        /// Choose a duration that covers your expected retry window.
        /// Too short may cause duplicates; too long wastes storage.
        /// </remarks>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the minimum duration before a duplicate request is allowed.
        /// Default: 1 second.
        /// </summary>
        /// <remarks>
        /// This helps prevent race conditions where the same request is sent
        /// multiple times in quick succession before the first completes.
        /// </remarks>
        public TimeSpan MinimumRetryInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to require an idempotency key for supported methods.
        /// Default: false.
        /// </summary>
        /// <remarks>
        /// When true, requests without an idempotency key will receive a 400 Bad Request response.
        /// When false, requests without a key will be processed normally (no idempotency protection).
        /// </remarks>
        public bool RequireIdempotencyKey { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include the original response headers in cached responses.
        /// Default: true.
        /// </summary>
        public bool CacheResponseHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include the Idempotency-Key in the response header.
        /// Default: true.
        /// </summary>
        public bool IncludeKeyInResponse { get; set; } = true;

        /// <summary>
        /// Gets or sets the header name for retry-after information.
        /// Default: "Retry-After".
        /// </summary>
        public string RetryAfterHeaderName { get; set; } = "Retry-After";

        /// <summary>
        /// Gets or sets the header name for indicating idempotency status.
        /// Default: "Idempotency-Replayed".
        /// </summary>
        /// <remarks>
        /// When a cached response is returned, this header will be set to "true".
        /// </remarks>
        public string ReplayedHeaderName { get; set; } = "Idempotency-Replayed";

        /// <summary>
        /// Gets or sets the HTTP status code for duplicate in-flight requests.
        /// Default: 409 (Conflict).
        /// </summary>
        /// <remarks>
        /// When a request with the same idempotency key is already being processed,
        /// this status code is returned. The client should retry after the specified
        /// duration in the Retry-After header.
        /// </remarks>
        public int InFlightStatusCode { get; set; } = (int)HttpStatusCode.Conflict;

        /// <summary>
        /// Gets or sets the retry-after duration for in-flight requests in seconds.
        /// Default: 1 second.
        /// </summary>
        public int InFlightRetryAfterSeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to use ProblemDetails format for error responses.
        /// Default: true.
        /// </summary>
        public bool UseProblemDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets the HTTP methods that should be idempotent.
        /// Default: POST, PUT, PATCH.
        /// </summary>
        public HashSet<string> IdempotentMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST",
            "PUT",
            "PATCH"
        };

        /// <summary>
        /// Gets or sets paths excluded from idempotency processing.
        /// Supports wildcard patterns (e.g., "/api/internal/*").
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/health/*",
            "/swagger",
            "/swagger/*",
            "/metrics"
        };

        /// <summary>
        /// Gets or sets paths that explicitly require idempotency keys.
        /// Takes precedence over RequireIdempotencyKey for specific paths.
        /// </summary>
        public List<string> RequiredPaths { get; set; } = new();

        /// <summary>
        /// Gets or sets the maximum request body size to hash for key generation.
        /// Default: 1 MB.
        /// </summary>
        /// <remarks>
        /// Requests larger than this will use a truncated hash.
        /// </remarks>
        public int MaxRequestBodySizeForHashing { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the cache key prefix for idempotency records.
        /// Default: "mvp24h:idempotency:".
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "mvp24h:idempotency:";

        /// <summary>
        /// Gets or sets status codes that should not be cached.
        /// Default: 500-599 (Server errors).
        /// </summary>
        /// <remarks>
        /// Server errors are typically transient and should be retried.
        /// </remarks>
        public HashSet<int> NonCacheableStatusCodes { get; set; } = new()
        {
            500, 501, 502, 503, 504
        };

        /// <summary>
        /// Gets or sets whether to integrate with IIdempotentCommand from CQRS module.
        /// Default: true.
        /// </summary>
        /// <remarks>
        /// When enabled, commands implementing IIdempotentCommand will have their
        /// idempotency key used if available.
        /// </remarks>
        public bool IntegrateWithCqrs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log idempotency events.
        /// Default: true.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the message for duplicate in-flight requests.
        /// </summary>
        public string InFlightMessage { get; set; } = "A request with this idempotency key is already being processed. Please retry after the specified duration.";

        /// <summary>
        /// Gets or sets the message when idempotency key is required but missing.
        /// </summary>
        public string MissingKeyMessage { get; set; } = "An Idempotency-Key header is required for this request.";

        /// <summary>
        /// Adds a path that requires idempotency keys.
        /// </summary>
        public void RequireIdempotencyForPath(string pathPattern)
        {
            RequiredPaths.Add(pathPattern);
        }

        /// <summary>
        /// Excludes a path from idempotency processing.
        /// </summary>
        public void ExcludePath(string pathPattern)
        {
            ExcludedPaths.Add(pathPattern);
        }

        /// <summary>
        /// Adds a status code that should not be cached.
        /// </summary>
        public void AddNonCacheableStatusCode(int statusCode)
        {
            NonCacheableStatusCodes.Add(statusCode);
        }
    }
}

