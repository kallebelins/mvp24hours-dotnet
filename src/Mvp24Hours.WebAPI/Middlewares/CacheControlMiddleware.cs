//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that sets Cache-Control headers based on route policies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware provides configurable Cache-Control headers with support for:
    /// <list type="bullet">
    /// <item>Route-specific cache policies</item>
    /// <item>Default cache policy</item>
    /// <item>Full Cache-Control directive support</item>
    /// <item>Path exclusions</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// Call <c>services.AddMvp24HoursCacheControl()</c> to configure options.
    /// </para>
    /// </remarks>
    public class CacheControlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CacheControlOptions _options;
        private readonly ILogger<CacheControlMiddleware> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="CacheControlMiddleware"/>.
        /// </summary>
        public CacheControlMiddleware(
            RequestDelegate next,
            IOptions<CacheControlOptions> options,
            ILogger<CacheControlMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request and sets Cache-Control headers.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            if (IsExcludedPath(context))
            {
                await _next(context);
                return;
            }

            // Determine which policy to apply
            var policy = GetPolicyForRequest(context);
            if (policy == null)
            {
                await _next(context);
                return;
            }

            // Execute next middleware first
            await _next(context);

            // Set Cache-Control header if response is successful and header not already set
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300 &&
                !context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
            {
                var cacheControl = BuildCacheControlHeader(policy);
                if (!string.IsNullOrEmpty(cacheControl))
                {
                    context.Response.Headers[HeaderNames.CacheControl] = cacheControl;
                }
            }
        }

        private bool IsExcludedPath(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return _options.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private CacheControlPolicy? GetPolicyForRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Check route-specific policies
            foreach (var (pattern, policy) in _options.RoutePolicies)
            {
                if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
                    MatchesPattern(path, pattern))
                {
                    return policy;
                }
            }

            // Return default policy
            return _options.DefaultPolicy;
        }

        private static bool MatchesPattern(string path, string pattern)
        {
            // Simple wildcard matching: * matches any sequence of characters
            if (pattern.Contains('*'))
            {
                var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private string BuildCacheControlHeader(CacheControlPolicy policy)
        {
            var directives = new System.Collections.Generic.List<string>();

            if (policy.NoStore == true)
            {
                directives.Add("no-store");
            }
            else
            {
                if (policy.NoCache == true)
                {
                    directives.Add("no-cache");
                }

                if (policy.Public == true)
                {
                    directives.Add("public");
                }
                else if (policy.Private == true)
                {
                    directives.Add("private");
                }

                if (policy.MaxAge.HasValue)
                {
                    directives.Add($"max-age={policy.MaxAge.Value.TotalSeconds:F0}");
                }

                if (policy.SharedMaxAge.HasValue)
                {
                    directives.Add($"s-maxage={policy.SharedMaxAge.Value.TotalSeconds:F0}");
                }

                if (policy.MustRevalidate == true)
                {
                    directives.Add("must-revalidate");
                }

                if (policy.ProxyRevalidate == true)
                {
                    directives.Add("proxy-revalidate");
                }

                if (policy.StaleWhileRevalidate == true)
                {
                    directives.Add("stale-while-revalidate");
                }

                if (policy.StaleIfError == true)
                {
                    directives.Add("stale-if-error");
                }

                if (policy.Immutable == true)
                {
                    directives.Add("immutable");
                }
            }

            return string.Join(", ", directives);
        }
    }
}

