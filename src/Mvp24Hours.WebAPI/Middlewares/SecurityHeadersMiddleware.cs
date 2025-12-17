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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that adds security headers to HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// This middleware adds essential security headers to protect against common
/// web vulnerabilities including XSS, clickjacking, MIME sniffing, and more.
/// </para>
/// <para>
/// <strong>Headers Added:</strong>
/// <list type="bullet">
/// <item><strong>Strict-Transport-Security (HSTS)</strong> - Forces HTTPS</item>
/// <item><strong>Content-Security-Policy</strong> - Prevents XSS and injection</item>
/// <item><strong>X-Frame-Options</strong> - Prevents clickjacking</item>
/// <item><strong>X-Content-Type-Options</strong> - Prevents MIME sniffing</item>
/// <item><strong>X-XSS-Protection</strong> - Legacy XSS protection</item>
/// <item><strong>Referrer-Policy</strong> - Controls referrer information</item>
/// <item><strong>Permissions-Policy</strong> - Controls browser features</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursSecurityHeaders()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursSecurityHeaders(options =>
/// {
///     options.EnableHsts = true;
///     options.EnableContentSecurityPolicy = true;
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursSecurityHeaders();
/// </code>
/// </example>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="SecurityHeadersMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The security headers options.</param>
    /// <param name="logger">The logger.</param>
    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if path is excluded
        if (!ShouldApplySecurityHeaders(context))
        {
            await _next(context);
            return;
        }

        // Register callback to add headers before response is sent
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        try
        {
            // Remove headers
            RemoveHeaders(response);

            // Add HSTS
            if (_options.EnableHsts && context.Request.IsHttps)
            {
                AddHstsHeader(response);
            }

            // Add Content-Security-Policy
            if (_options.EnableContentSecurityPolicy)
            {
                AddContentSecurityPolicyHeader(response);
            }

            // Add X-Frame-Options
            if (_options.EnableXFrameOptions)
            {
                AddXFrameOptionsHeader(response);
            }

            // Add X-Content-Type-Options
            if (_options.EnableXContentTypeOptions)
            {
                response.Headers["X-Content-Type-Options"] = "nosniff";
            }

            // Add X-XSS-Protection
            if (_options.EnableXXssProtection)
            {
                AddXXssProtectionHeader(response);
            }

            // Add Referrer-Policy
            if (_options.EnableReferrerPolicy)
            {
                AddReferrerPolicyHeader(response);
            }

            // Add Permissions-Policy
            if (_options.EnablePermissionsPolicy)
            {
                response.Headers["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // Add Cache-Control for sensitive paths
            if (_options.EnableCacheControlForSensitiveEndpoints)
            {
                AddCacheControlForSensitivePaths(context, response);
            }

            // Add custom headers
            foreach (var header in _options.CustomHeaders)
            {
                response.Headers[header.Key] = header.Value;
            }

            _logger.LogDebug("Security headers added for request {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add some security headers for request {Path}", context.Request.Path);
        }
    }

    private void RemoveHeaders(HttpResponse response)
    {
        foreach (var header in _options.HeadersToRemove)
        {
            if (response.Headers.ContainsKey(header))
            {
                response.Headers.Remove(header);
            }
        }
    }

    private void AddHstsHeader(HttpResponse response)
    {
        var hstsValue = $"max-age={_options.HstsMaxAgeSeconds}";

        if (_options.HstsIncludeSubDomains)
        {
            hstsValue += "; includeSubDomains";
        }

        if (_options.HstsPreload)
        {
            hstsValue += "; preload";
        }

        response.Headers["Strict-Transport-Security"] = hstsValue;
    }

    private void AddContentSecurityPolicyHeader(HttpResponse response)
    {
        var cspValue = _options.ContentSecurityPolicy;

        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicyReportUri))
        {
            cspValue += $"; report-uri {_options.ContentSecurityPolicyReportUri}";
        }

        var headerName = _options.ContentSecurityPolicyReportOnly
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy";

        response.Headers[headerName] = cspValue;
    }

    private void AddXFrameOptionsHeader(HttpResponse response)
    {
        var value = _options.XFrameOptions switch
        {
            XFrameOptionsValue.Deny => "DENY",
            XFrameOptionsValue.SameOrigin => "SAMEORIGIN",
            XFrameOptionsValue.AllowFrom => $"ALLOW-FROM {_options.XFrameOptionsAllowFromUri}",
            _ => "DENY"
        };

        response.Headers["X-Frame-Options"] = value;
    }

    private void AddXXssProtectionHeader(HttpResponse response)
    {
        var value = _options.XXssProtection switch
        {
            XssProtectionMode.Disabled => "0",
            XssProtectionMode.Enabled => "1",
            XssProtectionMode.Block => "1; mode=block",
            _ => "1; mode=block"
        };

        response.Headers["X-XSS-Protection"] = value;
    }

    private void AddReferrerPolicyHeader(HttpResponse response)
    {
        var value = _options.ReferrerPolicy switch
        {
            ReferrerPolicyValue.NoReferrer => "no-referrer",
            ReferrerPolicyValue.NoReferrerWhenDowngrade => "no-referrer-when-downgrade",
            ReferrerPolicyValue.Origin => "origin",
            ReferrerPolicyValue.OriginWhenCrossOrigin => "origin-when-cross-origin",
            ReferrerPolicyValue.SameOrigin => "same-origin",
            ReferrerPolicyValue.StrictOrigin => "strict-origin",
            ReferrerPolicyValue.StrictOriginWhenCrossOrigin => "strict-origin-when-cross-origin",
            ReferrerPolicyValue.UnsafeUrl => "unsafe-url",
            _ => "strict-origin-when-cross-origin"
        };

        response.Headers["Referrer-Policy"] = value;
    }

    private void AddCacheControlForSensitivePaths(HttpContext context, HttpResponse response)
    {
        var path = context.Request.Path.Value ?? "/";

        if (_options.SensitivePaths.Any(pattern => MatchesPattern(path, pattern)))
        {
            response.Headers["Cache-Control"] = _options.SensitiveCacheControl;
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
        }
    }

    private bool ShouldApplySecurityHeaders(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        return !_options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}

