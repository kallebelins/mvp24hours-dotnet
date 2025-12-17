//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Observability;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that collects OpenTelemetry-compatible metrics and traces for HTTP requests.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides comprehensive telemetry including:
/// <list type="bullet">
/// <item>Distributed tracing using Activity API</item>
/// <item>Request/response metrics (counters, histograms, gauges)</item>
/// <item>Correlation ID propagation</item>
/// <item>User and tenant context enrichment</item>
/// <item>Slow request detection</item>
/// </list>
/// </para>
/// <para>
/// <strong>OpenTelemetry Integration:</strong>
/// Configure OpenTelemetry to include Mvp24Hours WebAPI activities and metrics:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(builder => builder.AddSource(WebApiActivitySource.SourceName))
///     .WithMetrics(builder => builder.AddMeter(WebApiActivitySource.MeterName));
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursRequestTelemetry(options =>
/// {
///     options.EnableTracing = true;
///     options.EnableMetrics = true;
///     options.EnrichWithUser = true;
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursRequestTelemetry();
/// </code>
/// </example>
public class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestTelemetryOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="RequestTelemetryMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The telemetry options.</param>
    public RequestTelemetryMiddleware(
        RequestDelegate next,
        IOptions<RequestTelemetryOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes the HTTP request with telemetry collection.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip telemetry for excluded paths
        if (ShouldSkipTelemetry(context))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var path = GetNormalizedPath(context);
        var correlationId = GetOrCreateCorrelationId(context);

        // Start activity for tracing
        using var activity = _options.EnableTracing
            ? WebApiActivitySource.StartHttpRequestActivity(method, path, correlationId)
            : null;

        // Increment in-progress counter
        if (_options.EnableMetrics)
        {
            WebApiActivitySource.IncrementInProgress();
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            // Enrich activity with context
            EnrichActivity(activity, context);

            await _next(context);
            stopwatch.Stop();

            // Record success
            if (activity != null)
            {
                WebApiActivitySource.SetSuccess(activity, context.Response.StatusCode);
                activity.SetTag(WebApiActivitySource.TagNames.DurationMs, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();

            // Record error
            if (activity != null)
            {
                var statusCode = GetStatusCodeForException(ex);
                WebApiActivitySource.SetError(activity, ex, statusCode);
                activity.SetTag(WebApiActivitySource.TagNames.DurationMs, stopwatch.Elapsed.TotalMilliseconds);
            }

            throw;
        }
        finally
        {
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;
            var statusCode = exception != null
                ? GetStatusCodeForException(exception)
                : context.Response.StatusCode;

            var isError = statusCode >= 400;
            var isSlow = durationMs > _options.DurationBuckets.LastOrDefault();

            // Decrement in-progress counter and record metrics
            if (_options.EnableMetrics)
            {
                WebApiActivitySource.DecrementInProgress();

                WebApiActivitySource.RecordRequest(
                    method,
                    path,
                    statusCode,
                    durationMs,
                    context.Request.ContentLength ?? 0,
                    context.Response.ContentLength ?? 0,
                    isError,
                    isSlow);
            }

            // Mark slow request in activity
            if (activity != null && isSlow)
            {
                activity.SetTag(WebApiActivitySource.TagNames.IsSlow, true);
            }
        }
    }

    private void EnrichActivity(Activity? activity, HttpContext context)
    {
        if (activity == null)
            return;

        // Basic enrichment
        activity.SetTag(WebApiActivitySource.TagNames.HttpScheme, context.Request.Scheme);
        activity.SetTag(WebApiActivitySource.TagNames.HttpHost, context.Request.Host.Value);

        // Custom tags
        foreach (var tag in _options.CustomTags)
        {
            activity.SetTag(tag.Key, tag.Value);
        }

        // User enrichment
        if (_options.EnrichWithUser)
        {
            var userId = GetUserId(context);
            if (!string.IsNullOrEmpty(userId))
            {
                activity.SetTag(WebApiActivitySource.TagNames.UserId, userId);
            }
        }

        // Tenant enrichment
        if (_options.EnrichWithTenant)
        {
            var tenantId = GetTenantId(context);
            if (!string.IsNullOrEmpty(tenantId))
            {
                activity.SetTag(WebApiActivitySource.TagNames.TenantId, tenantId);
            }
        }

        // Causation ID
        var causationId = context.Request.Headers[_options.CausationIdHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(causationId))
        {
            activity.SetTag(WebApiActivitySource.TagNames.CausationId, causationId);
        }

        // Client IP
        var clientIp = GetClientIp(context);
        if (!string.IsNullOrEmpty(clientIp))
        {
            activity.SetTag(WebApiActivitySource.TagNames.ClientIp, clientIp);
        }

        // User agent
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            activity.SetTag(WebApiActivitySource.TagNames.HttpUserAgent, userAgent);
        }

        // Request size
        if (context.Request.ContentLength.HasValue)
        {
            activity.SetTag(WebApiActivitySource.TagNames.HttpRequestContentLength, context.Request.ContentLength.Value);
        }

        // Header enrichment (if enabled)
        if (_options.EnrichWithHeaders)
        {
            // Add safe headers (non-sensitive)
            var safeHeaders = new[] { "Accept", "Accept-Language", "Content-Type", "Origin", "Referer" };
            foreach (var header in safeHeaders)
            {
                var value = context.Request.Headers[header].FirstOrDefault();
                if (!string.IsNullOrEmpty(value))
                {
                    activity.SetTag($"http.request.header.{header.ToLowerInvariant()}", value);
                }
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[_options.CorrelationIdHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }
        else
        {
            // Use provided correlation ID as trace identifier
            context.TraceIdentifier = correlationId;
        }

        // Ensure correlation ID is in response headers
        if (!context.Response.Headers.ContainsKey(_options.CorrelationIdHeader))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(_options.CorrelationIdHeader, correlationId);
                return Task.CompletedTask;
            });
        }

        return correlationId;
    }

    private bool ShouldSkipTelemetry(HttpContext context)
    {
        if (!_options.EnableTracing && !_options.EnableMetrics)
            return true;

        var path = context.Request.Path.Value ?? "/";

        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Exact match
        if (string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private static string GetNormalizedPath(HttpContext context)
    {
        // Try to get route pattern for better metric cardinality
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var routePattern = endpoint.Metadata
                .OfType<Microsoft.AspNetCore.Routing.RouteNameMetadata>()
                .FirstOrDefault()?.RouteName;

            if (!string.IsNullOrEmpty(routePattern))
                return routePattern;

            // Try to get the route pattern from RouteEndpoint
            if (endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint)
            {
                var pattern = routeEndpoint.RoutePattern.RawText;
                if (!string.IsNullOrEmpty(pattern))
                    return "/" + pattern.TrimStart('/');
            }
        }

        return context.Request.Path.Value ?? "/";
    }

    private string? GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.Identity.Name;
    }

    private string? GetTenantId(HttpContext context)
    {
        // Check header first
        var tenantHeader = context.Request.Headers[_options.TenantIdHeader].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantHeader))
            return tenantHeader;

        // Check claims
        return context.User?.FindFirstValue("tenant_id")
            ?? context.User?.FindFirstValue("tid");
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static int GetStatusCodeForException(Exception exception)
    {
        // Map known exception types to status codes
        return exception switch
        {
            UnauthorizedAccessException => 401,
            InvalidOperationException => 400,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            NotImplementedException => 501,
            TimeoutException => 504,
            _ => 500
        };
    }
}

