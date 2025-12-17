//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for CSRF/Anti-Forgery protection for SPAs.
/// </summary>
/// <remarks>
/// <para>
/// This configuration provides CSRF protection for Single Page Applications (SPAs)
/// that use API endpoints. It supports the double-submit cookie pattern.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>Server sets an anti-forgery token in a cookie (readable by JavaScript)</item>
/// <item>Client reads the cookie and sends the token in a header with requests</item>
/// <item>Server validates that the header token matches the cookie token</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursAntiForgery(options =>
/// {
///     options.Enabled = true;
///     options.CookieName = "XSRF-TOKEN";
///     options.HeaderName = "X-XSRF-TOKEN";
/// });
/// </code>
/// </example>
public class AntiForgeryOptions
{
    /// <summary>
    /// Gets or sets whether anti-forgery protection is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the cookie that contains the anti-forgery token.
    /// Default: XSRF-TOKEN
    /// </summary>
    /// <remarks>
    /// This cookie should be readable by JavaScript to allow the SPA to read
    /// the token and include it in request headers.
    /// </remarks>
    public string CookieName { get; set; } = "XSRF-TOKEN";

    /// <summary>
    /// Gets or sets the header name that the client must use to send the token.
    /// Default: X-XSRF-TOKEN
    /// </summary>
    public string HeaderName { get; set; } = "X-XSRF-TOKEN";

    /// <summary>
    /// Gets or sets whether the cookie should have the HttpOnly flag.
    /// Default: false (must be readable by JavaScript for SPAs)
    /// </summary>
    /// <remarks>
    /// Set to false for SPAs so JavaScript can read the cookie value.
    /// The security comes from the same-origin policy and the requirement
    /// to include the value in a custom header.
    /// </remarks>
    public bool CookieHttpOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the cookie should have the Secure flag.
    /// Default: true (only sent over HTTPS)
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>
    /// Gets or sets the SameSite policy for the cookie.
    /// Default: Strict
    /// </summary>
    public Microsoft.AspNetCore.Http.SameSiteMode CookieSameSite { get; set; } = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    /// <summary>
    /// Gets or sets the path for the anti-forgery cookie.
    /// Default: /
    /// </summary>
    public string CookiePath { get; set; } = "/";

    /// <summary>
    /// Gets or sets the domain for the anti-forgery cookie.
    /// Default: null (current domain)
    /// </summary>
    public string? CookieDomain { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time.
    /// Default: 24 hours
    /// </summary>
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the HTTP methods that require anti-forgery validation.
    /// Default: POST, PUT, PATCH, DELETE
    /// </summary>
    public HashSet<string> ProtectedMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST",
        "PUT",
        "PATCH",
        "DELETE"
    };

    /// <summary>
    /// Gets or sets path patterns to exclude from anti-forgery validation.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/health",
        "/health/*",
        "/swagger",
        "/swagger/*"
    };

    /// <summary>
    /// Gets or sets path patterns that require anti-forgery validation.
    /// When empty, all paths (except excluded) are protected.
    /// </summary>
    public HashSet<string> ProtectedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether to refresh the token on each request.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Setting to true provides better security but may cause issues with
    /// concurrent requests in SPAs.
    /// </remarks>
    public bool RefreshTokenOnEachRequest { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to skip validation for AJAX requests without cookies.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Some SPAs may make requests that don't include cookies initially.
    /// Enable this only if needed for your authentication flow.
    /// </remarks>
    public bool SkipValidationForRequestsWithoutCookies { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log validation failures.
    /// Default: true
    /// </summary>
    public bool LogValidationFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets the endpoint path for the token generation endpoint.
    /// Default: /api/antiforgery/token
    /// </summary>
    public string TokenEndpoint { get; set; } = "/api/antiforgery/token";

    /// <summary>
    /// Gets or sets whether to automatically register the token endpoint.
    /// Default: true
    /// </summary>
    public bool RegisterTokenEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets trusted origins for CORS requests.
    /// When set, requests from these origins will include the token in the response.
    /// </summary>
    public HashSet<string> TrustedOrigins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

