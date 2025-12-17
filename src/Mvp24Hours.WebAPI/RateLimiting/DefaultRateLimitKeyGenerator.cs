//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.RateLimiting
{
    /// <summary>
    /// Default implementation of <see cref="IRateLimitKeyGenerator"/> that generates
    /// rate limit keys based on configured key sources (IP, User ID, API Key, Tenant ID).
    /// </summary>
    public class DefaultRateLimitKeyGenerator : IRateLimitKeyGenerator
    {
        private readonly RateLimitingOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultRateLimitKeyGenerator"/>.
        /// </summary>
        /// <param name="options">The rate limiting options.</param>
        public DefaultRateLimitKeyGenerator(IOptions<RateLimitingOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public Task<string> GenerateKeyAsync(HttpContext context, RateLimitPolicy policy)
        {
            return Task.FromResult(GenerateKey(context, policy));
        }

        /// <inheritdoc />
        public string GenerateKey(HttpContext context, RateLimitPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(policy);

            var keyBuilder = new StringBuilder();
            keyBuilder.Append(policy.Name);
            keyBuilder.Append(':');

            if (policy.KeySource == RateLimitKeySource.None)
            {
                keyBuilder.Append("global");
                return keyBuilder.ToString();
            }

            var keyParts = new StringBuilder();

            if (policy.KeySource.HasFlag(RateLimitKeySource.ClientIp))
            {
                var clientIp = GetClientIpAddress(context);
                if (!string.IsNullOrEmpty(clientIp))
                {
                    keyParts.Append("ip:");
                    keyParts.Append(clientIp);
                    keyParts.Append('|');
                }
            }

            if (policy.KeySource.HasFlag(RateLimitKeySource.UserId))
            {
                var userId = GetUserId(context);
                if (!string.IsNullOrEmpty(userId))
                {
                    keyParts.Append("user:");
                    keyParts.Append(userId);
                    keyParts.Append('|');
                }
            }

            if (policy.KeySource.HasFlag(RateLimitKeySource.ApiKey))
            {
                var apiKey = GetApiKey(context);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    keyParts.Append("apikey:");
                    keyParts.Append(apiKey);
                    keyParts.Append('|');
                }
            }

            if (policy.KeySource.HasFlag(RateLimitKeySource.TenantId))
            {
                var tenantId = GetTenantId(context);
                if (!string.IsNullOrEmpty(tenantId))
                {
                    keyParts.Append("tenant:");
                    keyParts.Append(tenantId);
                    keyParts.Append('|');
                }
            }

            if (policy.KeySource.HasFlag(RateLimitKeySource.CustomHeader) && 
                !string.IsNullOrEmpty(policy.CustomHeaderName))
            {
                var headerValue = GetCustomHeader(context, policy.CustomHeaderName);
                if (!string.IsNullOrEmpty(headerValue))
                {
                    keyParts.Append("header:");
                    keyParts.Append(headerValue);
                    keyParts.Append('|');
                }
            }

            var key = keyParts.ToString().TrimEnd('|');
            if (string.IsNullOrEmpty(key))
            {
                // Fallback to client IP if no key sources matched
                var fallbackIp = GetClientIpAddress(context) ?? "unknown";
                key = $"ip:{fallbackIp}";
            }

            keyBuilder.Append(key);
            return keyBuilder.ToString();
        }

        /// <summary>
        /// Gets the client IP address from the HTTP context.
        /// </summary>
        protected virtual string? GetClientIpAddress(HttpContext context)
        {
            // Check forwarded headers first if enabled
            if (_options.UseForwardedHeaders)
            {
                var forwardedFor = context.Request.Headers[_options.ForwardedForHeaderName].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Take the first IP in the chain (client IP)
                    var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstIp) && IPAddress.TryParse(firstIp, out _))
                    {
                        return firstIp;
                    }
                }

                // Also check X-Real-IP header
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
                {
                    return realIp;
                }
            }

            // Fallback to direct connection IP
            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Gets the authenticated user ID from the HTTP context.
        /// </summary>
        protected virtual string? GetUserId(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return null;

            // Try different common claim types for user ID
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("user_id")?.Value
                ?? context.User.FindFirst("id")?.Value;

            return userId;
        }

        /// <summary>
        /// Gets the API key from the HTTP context.
        /// </summary>
        protected virtual string? GetApiKey(HttpContext context)
        {
            // Check header
            var apiKey = context.Request.Headers[_options.ApiKeyHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(apiKey))
                return apiKey;

            // Check query string
            if (context.Request.Query.TryGetValue("api_key", out var queryApiKey))
                return queryApiKey.FirstOrDefault();

            if (context.Request.Query.TryGetValue("apikey", out var queryApiKey2))
                return queryApiKey2.FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Gets the tenant ID from the HTTP context.
        /// </summary>
        protected virtual string? GetTenantId(HttpContext context)
        {
            // Check header
            var tenantId = context.Request.Headers[_options.TenantIdHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
                return tenantId;

            // Check claims for tenant information
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var claimTenantId = context.User.FindFirst("tenant_id")?.Value
                    ?? context.User.FindFirst("tenantid")?.Value
                    ?? context.User.FindFirst("tid")?.Value;

                if (!string.IsNullOrEmpty(claimTenantId))
                    return claimTenantId;
            }

            return null;
        }

        /// <summary>
        /// Gets a custom header value from the HTTP context.
        /// </summary>
        protected virtual string? GetCustomHeader(HttpContext context, string headerName)
        {
            return context.Request.Headers[headerName].FirstOrDefault();
        }
    }
}

