//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.RateLimiting;
using System;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that implements rate limiting using .NET 7+ built-in RateLimiter.
    /// Supports Fixed Window, Sliding Window, Token Bucket, and Concurrency limiting algorithms.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitingOptions _options;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initializes a new instance of <see cref="RateLimitingMiddleware"/>.
        /// </summary>
        public RateLimitingMiddleware(
            RequestDelegate next,
            IOptions<RateLimitingOptions> options,
            RateLimitPartitionResolver partitionResolver,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            ArgumentNullException.ThrowIfNull(partitionResolver);
            
            // Create the partitioned rate limiter that resolves partitions based on the request
            _rateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                ctx => partitionResolver.GetPartition(ctx));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Acquire a permit from the rate limiter
            using var lease = await _rateLimiter.AcquireAsync(context, 1, context.RequestAborted);

            if (lease.IsAcquired)
            {
                // Add rate limit headers
                AddRateLimitHeaders(context, lease);

                await _next(context);
            }
            else
            {
                // Rate limited
                _logger.LogWarning(
                    "Rate limit exceeded for {Path} from {ClientInfo}. Retry after: {RetryAfter}",
                    context.Request.Path,
                    GetClientInfo(context),
                    lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : TimeSpan.Zero);

                await HandleRateLimitedResponse(context, lease);
            }
        }

        /// <summary>
        /// Adds rate limit headers to the response.
        /// </summary>
        private void AddRateLimitHeaders(HttpContext context, RateLimitLease lease)
        {
            if (!_options.IncludeRateLimitHeaders)
                return;

            // Get metadata from the lease
            if (lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reasonPhrase))
            {
                // Add limit info from metadata if available
            }

            // For built-in limiters, we need to track limits manually
            // The .NET rate limiters don't expose current count via metadata
            // We add the configured limit from options as a reference

            var policy = GetCurrentPolicy(context);
            if (policy != null)
            {
                context.Response.Headers[_options.RateLimitHeaderName] = policy.PermitLimit.ToString();
                
                // Retry-After for sliding windows
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.Response.Headers[_options.RateLimitResetHeaderName] = 
                        DateTimeOffset.UtcNow.Add(retryAfter).ToUnixTimeSeconds().ToString();
                }
            }
        }

        /// <summary>
        /// Handles the response when rate limit is exceeded.
        /// </summary>
        private async Task HandleRateLimitedResponse(HttpContext context, RateLimitLease lease)
        {
            context.Response.StatusCode = _options.RateLimitedStatusCode;

            // Add Retry-After header
            if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.Response.Headers[_options.RetryAfterHeaderName] = ((int)retryAfter.TotalSeconds).ToString();
            }

            // Add rate limit headers
            var policy = GetCurrentPolicy(context);
            if (policy != null && _options.IncludeRateLimitHeaders)
            {
                context.Response.Headers[_options.RateLimitHeaderName] = policy.PermitLimit.ToString();
                context.Response.Headers[_options.RateLimitRemainingHeaderName] = "0";
                
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var resetAfter))
                {
                    context.Response.Headers[_options.RateLimitResetHeaderName] = 
                        DateTimeOffset.UtcNow.Add(resetAfter).ToUnixTimeSeconds().ToString();
                }
            }

            if (_options.UseProblemDetails)
            {
                await WriteRateLimitProblemDetails(context, lease);
            }
            else
            {
                await WriteSimpleRateLimitResponse(context);
            }
        }

        /// <summary>
        /// Writes a ProblemDetails response for rate limiting.
        /// </summary>
        private async Task WriteRateLimitProblemDetails(HttpContext context, RateLimitLease lease)
        {
            var retryAfterSeconds = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)retryAfter.TotalSeconds
                : 60;

            var problemDetails = new ProblemDetails
            {
                Type = "https://httpstatuses.com/429",
                Title = "Too Many Requests",
                Status = _options.RateLimitedStatusCode,
                Detail = _options.RateLimitedMessage,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["retryAfter"] = retryAfterSeconds;
            
            // Add trace ID if available
            var traceId = context.TraceIdentifier;
            if (!string.IsNullOrEmpty(traceId))
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            // Add correlation ID if present
            var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
            if (!string.IsNullOrEmpty(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }

            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
        }

        /// <summary>
        /// Writes a simple text response for rate limiting.
        /// </summary>
        private async Task WriteSimpleRateLimitResponse(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(_options.RateLimitedMessage);
        }

        /// <summary>
        /// Gets the current policy for the request.
        /// </summary>
        private RateLimitPolicy? GetCurrentPolicy(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "/";

            // Check endpoint-specific policies
            foreach (var (pattern, policyName) in _options.EndpointPolicies)
            {
                if (PathMatches(path, pattern) && _options.Policies.TryGetValue(policyName, out var policy))
                {
                    return policy;
                }
            }

            // Return default policy
            if (_options.Policies.TryGetValue(_options.DefaultPolicyName, out var defaultPolicy))
            {
                return defaultPolicy;
            }

            return null;
        }

        /// <summary>
        /// Simple path matching helper.
        /// </summary>
        private static bool PathMatches(string path, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                return path.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
            }
            return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets client information for logging.
        /// </summary>
        private static string GetClientInfo(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User?.Identity?.Name ?? "anonymous";
            return $"IP={ip}, User={userId}";
        }
    }
}
