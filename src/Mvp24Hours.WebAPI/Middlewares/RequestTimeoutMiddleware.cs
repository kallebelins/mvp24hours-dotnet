//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that enforces request timeouts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware provides configurable request timeouts with support for:
    /// <list type="bullet">
    /// <item>Global default timeout</item>
    /// <item>Endpoint-specific timeouts</item>
    /// <item>HTTP method-specific timeouts</item>
    /// <item>Path exclusions</item>
    /// <item>Retry-After header support</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// Call <c>services.AddMvp24HoursRequestTimeout()</c> to configure options.
    /// </para>
    /// </remarks>
    public class RequestTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestTimeoutOptions _options;
        private readonly ILogger<RequestTimeoutMiddleware> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="RequestTimeoutMiddleware"/>.
        /// </summary>
        public RequestTimeoutMiddleware(
            RequestDelegate next,
            IOptions<RequestTimeoutOptions> options,
            ILogger<RequestTimeoutMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request with timeout enforcement.
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

            var timeout = GetTimeoutForRequest(context);
            if (timeout == TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            {
                await _next(context);
                return;
            }

            using var cts = new CancellationTokenSource(timeout);
            var originalCancellationToken = context.RequestAborted;

            try
            {
                context.RequestAborted = cts.Token;
                await _next(context);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                if (!context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "Request timeout after {Timeout}ms for {Method} {Path}",
                        timeout.TotalMilliseconds,
                        context.Request.Method,
                        context.Request.Path);

                    context.Response.StatusCode = 408; // Request Timeout
                    context.Response.ContentType = "application/json";

                    if (_options.SendRetryAfter)
                    {
                        context.Response.Headers.RetryAfter = timeout.TotalSeconds.ToString();
                    }

                    await context.Response.WriteAsync(
                        $"{{\"error\":\"Request timeout after {timeout.TotalSeconds} seconds\"}}",
                        CancellationToken.None);
                }
            }
            finally
            {
                context.RequestAborted = originalCancellationToken;
            }
        }

        private bool IsExcludedPath(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return _options.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private TimeSpan GetTimeoutForRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method.ToUpperInvariant();

            // Check endpoint-specific timeout
            foreach (var (pattern, timeout) in _options.EndpointTimeouts)
            {
                if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
                    MatchesPattern(path, pattern))
                {
                    return timeout;
                }
            }

            // Check method-specific timeout
            if (_options.MethodTimeouts.TryGetValue(method, out var methodTimeout))
            {
                return methodTimeout;
            }

            // Return default timeout
            return _options.DefaultTimeout;
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
    }
}

