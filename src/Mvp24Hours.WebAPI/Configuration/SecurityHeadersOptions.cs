//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for security headers middleware.
/// </summary>
/// <remarks>
/// <para>
/// These options control which security headers are added to HTTP responses
/// to protect against common web vulnerabilities:
/// <list type="bullet">
/// <item><strong>HSTS</strong> - Forces HTTPS connections</item>
/// <item><strong>CSP</strong> - Prevents XSS and injection attacks</item>
/// <item><strong>X-Frame-Options</strong> - Prevents clickjacking</item>
/// <item><strong>X-Content-Type-Options</strong> - Prevents MIME sniffing</item>
/// <item><strong>X-XSS-Protection</strong> - Legacy XSS protection</item>
/// <item><strong>Referrer-Policy</strong> - Controls referrer information</item>
/// <item><strong>Permissions-Policy</strong> - Controls browser features</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursSecurityHeaders(options =>
/// {
///     options.EnableHsts = true;
///     options.HstsMaxAgeSeconds = 31536000;
///     options.EnableContentSecurityPolicy = true;
///     options.ContentSecurityPolicy = "default-src 'self'";
/// });
/// </code>
/// </example>
public class SecurityHeadersOptions
{
    #region HSTS (HTTP Strict Transport Security)

    /// <summary>
    /// Gets or sets whether to enable HSTS header.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// HSTS instructs browsers to only access the site via HTTPS.
    /// Should be enabled in production when using HTTPS.
    /// </remarks>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// Gets or sets the max-age value for HSTS in seconds.
    /// Default: 31536000 (1 year)
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000;

    /// <summary>
    /// Gets or sets whether to include subdomains in HSTS.
    /// Default: true
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include preload directive in HSTS.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Only enable if you want to submit your domain to the HSTS preload list.
    /// </remarks>
    public bool HstsPreload { get; set; } = false;

    #endregion

    #region Content-Security-Policy

    /// <summary>
    /// Gets or sets whether to enable Content-Security-Policy header.
    /// Default: true
    /// </summary>
    public bool EnableContentSecurityPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Content-Security-Policy value.
    /// Default: "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; object-src 'none'; frame-ancestors 'self'; base-uri 'self'; form-action 'self'"
    /// </summary>
    /// <remarks>
    /// <para>
    /// CSP helps prevent XSS, clickjacking, and other code injection attacks.
    /// Customize based on your application's needs.
    /// </para>
    /// </remarks>
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    /// <summary>
    /// Gets or sets whether to use Content-Security-Policy-Report-Only header
    /// instead of Content-Security-Policy.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// Use report-only mode to test CSP policies without blocking content.
    /// </remarks>
    public bool ContentSecurityPolicyReportOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets the report-uri for CSP violations.
    /// </summary>
    public string? ContentSecurityPolicyReportUri { get; set; }

    #endregion

    #region X-Frame-Options

    /// <summary>
    /// Gets or sets whether to enable X-Frame-Options header.
    /// Default: true
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-Frame-Options value.
    /// Default: DENY
    /// </summary>
    /// <remarks>
    /// Options: DENY, SAMEORIGIN, or ALLOW-FROM uri
    /// </remarks>
    public XFrameOptionsValue XFrameOptions { get; set; } = XFrameOptionsValue.Deny;

    /// <summary>
    /// Gets or sets the allowed URI for X-Frame-Options ALLOW-FROM.
    /// Only used when XFrameOptions is set to AllowFrom.
    /// </summary>
    public string? XFrameOptionsAllowFromUri { get; set; }

    #endregion

    #region X-Content-Type-Options

    /// <summary>
    /// Gets or sets whether to enable X-Content-Type-Options header.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// This header prevents MIME type sniffing.
    /// </remarks>
    public bool EnableXContentTypeOptions { get; set; } = true;

    #endregion

    #region X-XSS-Protection

    /// <summary>
    /// Gets or sets whether to enable X-XSS-Protection header.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// This is a legacy header for older browsers. Modern browsers use CSP instead.
    /// </remarks>
    public bool EnableXXssProtection { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-XSS-Protection mode.
    /// Default: Block
    /// </summary>
    public XssProtectionMode XXssProtection { get; set; } = XssProtectionMode.Block;

    #endregion

    #region Referrer-Policy

    /// <summary>
    /// Gets or sets whether to enable Referrer-Policy header.
    /// Default: true
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Referrer-Policy value.
    /// Default: strict-origin-when-cross-origin
    /// </summary>
    public ReferrerPolicyValue ReferrerPolicy { get; set; } = ReferrerPolicyValue.StrictOriginWhenCrossOrigin;

    #endregion

    #region Permissions-Policy (formerly Feature-Policy)

    /// <summary>
    /// Gets or sets whether to enable Permissions-Policy header.
    /// Default: true
    /// </summary>
    public bool EnablePermissionsPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Permissions-Policy value.
    /// Default: disables commonly abused features.
    /// </summary>
    public string PermissionsPolicy { get; set; } = 
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";

    #endregion

    #region Cache-Control for Sensitive Content

    /// <summary>
    /// Gets or sets whether to add Cache-Control headers for sensitive endpoints.
    /// Default: true
    /// </summary>
    public bool EnableCacheControlForSensitiveEndpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the Cache-Control header for sensitive endpoints.
    /// Default: no-store, no-cache, must-revalidate
    /// </summary>
    public string SensitiveCacheControl { get; set; } = "no-store, no-cache, must-revalidate";

    /// <summary>
    /// Gets or sets the path patterns that are considered sensitive.
    /// </summary>
    public HashSet<string> SensitivePaths { get; set; } = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/*",
        "/api/account/*",
        "/api/user/*",
        "/api/admin/*"
    };

    #endregion

    #region Custom Headers

    /// <summary>
    /// Gets or sets additional custom headers to add to responses.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets headers to remove from responses.
    /// Default includes Server and X-Powered-By.
    /// </summary>
    public HashSet<string> HeadersToRemove { get; set; } = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "Server",
        "X-Powered-By",
        "X-AspNet-Version",
        "X-AspNetMvc-Version"
    };

    #endregion

    #region Path Exclusions

    /// <summary>
    /// Gets or sets path patterns to exclude from security headers.
    /// Supports wildcards (*).
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "/swagger",
        "/swagger/*",
        "/health",
        "/health/*"
    };

    #endregion
}

/// <summary>
/// X-Frame-Options header values.
/// </summary>
public enum XFrameOptionsValue
{
    /// <summary>
    /// The page cannot be displayed in a frame.
    /// </summary>
    Deny,

    /// <summary>
    /// The page can only be displayed in a frame on the same origin.
    /// </summary>
    SameOrigin,

    /// <summary>
    /// The page can be displayed in a frame on the specified origin.
    /// </summary>
    AllowFrom
}

/// <summary>
/// X-XSS-Protection mode values.
/// </summary>
public enum XssProtectionMode
{
    /// <summary>
    /// Disables XSS filtering.
    /// </summary>
    Disabled,

    /// <summary>
    /// Enables XSS filtering (sanitizes the page).
    /// </summary>
    Enabled,

    /// <summary>
    /// Enables XSS filtering and blocks rendering if attack is detected.
    /// </summary>
    Block
}

/// <summary>
/// Referrer-Policy header values.
/// </summary>
public enum ReferrerPolicyValue
{
    /// <summary>
    /// No referrer information is sent.
    /// </summary>
    NoReferrer,

    /// <summary>
    /// No referrer for cross-origin requests.
    /// </summary>
    NoReferrerWhenDowngrade,

    /// <summary>
    /// Only the origin is sent.
    /// </summary>
    Origin,

    /// <summary>
    /// Origin for cross-origin, full URL for same-origin.
    /// </summary>
    OriginWhenCrossOrigin,

    /// <summary>
    /// Full URL for same-origin only.
    /// </summary>
    SameOrigin,

    /// <summary>
    /// Origin only if same security level.
    /// </summary>
    StrictOrigin,

    /// <summary>
    /// Full URL for same-origin, origin for cross-origin if same security level.
    /// </summary>
    StrictOriginWhenCrossOrigin,

    /// <summary>
    /// Full URL is sent (not recommended).
    /// </summary>
    UnsafeUrl
}

