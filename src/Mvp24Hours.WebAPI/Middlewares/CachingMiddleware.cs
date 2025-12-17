//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using MvpResponseCachingOptions = Mvp24Hours.WebAPI.Configuration.ResponseCachingOptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that provides response caching with configurable cache profiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware provides response caching with support for:
    /// <list type="bullet">
    /// <item>Configurable cache profiles</item>
    /// <item>Route-specific cache policies</item>
    /// <item>Vary-by-query-keys support</item>
    /// <item>Path exclusions</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// Call <c>services.AddMvp24HoursResponseCaching()</c> to configure options.
    /// </para>
    /// </remarks>
    public class CachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MvpResponseCachingOptions _options;
        private readonly ILogger<CachingMiddleware> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="CachingMiddleware"/>.
        /// </summary>
        public CachingMiddleware(
            RequestDelegate next,
            IOptions<MvpResponseCachingOptions> options,
            ILogger<CachingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request and applies caching.
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

            var profile = GetCacheProfileForRequest(context);
            if (profile != null)
            {
                ApplyCacheProfile(context, profile);
            }

            await _next(context);
        }

        private bool IsExcludedPath(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return _options.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private CacheProfile? GetCacheProfileForRequest(HttpContext context)
        {
            // For now, return default profile if configured
            // In a full implementation, this would check route-specific profiles
            if (!string.IsNullOrEmpty(_options.DefaultProfile) &&
                _options.Profiles.TryGetValue(_options.DefaultProfile, out var profile))
            {
                return profile;
            }

            return null;
        }

        private void ApplyCacheProfile(HttpContext context, CacheProfile profile)
        {
            if (profile.NoStore)
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                    {
                        NoStore = true
                    };
                return;
            }

            var cacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue();

            if (profile.Duration.HasValue)
            {
                cacheControl.MaxAge = TimeSpan.FromSeconds(profile.Duration.Value);
            }

            cacheControl.Public = profile.Location == ResponseCacheLocation.Any || profile.Location == ResponseCacheLocation.Proxy;
            cacheControl.Private = profile.Location == ResponseCacheLocation.Client;

            if (profile.NoStore)
            {
                cacheControl.NoStore = true;
            }

            context.Response.GetTypedHeaders().CacheControl = cacheControl;

            if (!string.IsNullOrEmpty(profile.VaryByHeader))
            {
                context.Response.Headers.Vary = profile.VaryByHeader;
            }

            if (_options.VaryByQueryKeys && profile.VaryByQueryKeys != null && profile.VaryByQueryKeys.Length > 0)
            {
                var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
                if (responseCachingFeature != null)
                {
                    responseCachingFeature.VaryByQueryKeys = profile.VaryByQueryKeys;
                }
            }
        }
    }
}
