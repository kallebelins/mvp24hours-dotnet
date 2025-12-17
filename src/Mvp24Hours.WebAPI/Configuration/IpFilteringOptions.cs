//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for IP filtering/whitelisting middleware.
/// </summary>
/// <remarks>
/// <para>
/// This configuration supports both whitelist (allow only specified IPs)
/// and blacklist (block specified IPs) modes. Features include:
/// <list type="bullet">
/// <item>IPv4 and IPv6 support</item>
/// <item>CIDR notation for IP ranges</item>
/// <item>Path-specific filtering</item>
/// <item>Proxy-aware IP extraction</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursIpFiltering(options =>
/// {
///     options.Mode = IpFilteringMode.Whitelist;
///     options.WhitelistedIps.Add("192.168.1.0/24");
///     options.WhitelistedIps.Add("10.0.0.1");
/// });
/// </code>
/// </example>
public class IpFilteringOptions
{
    /// <summary>
    /// Gets or sets the filtering mode.
    /// Default: Whitelist
    /// </summary>
    public IpFilteringMode Mode { get; set; } = IpFilteringMode.Disabled;

    /// <summary>
    /// Gets or sets whether the middleware is enabled.
    /// Default: false
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the whitelisted IP addresses or CIDR ranges.
    /// Only used when Mode is Whitelist.
    /// </summary>
    /// <example>
    /// <code>
    /// options.WhitelistedIps.Add("192.168.1.1");       // Single IP
    /// options.WhitelistedIps.Add("10.0.0.0/8");        // CIDR range
    /// options.WhitelistedIps.Add("::1");               // IPv6 localhost
    /// options.WhitelistedIps.Add("2001:db8::/32");     // IPv6 range
    /// </code>
    /// </example>
    public HashSet<string> WhitelistedIps { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "127.0.0.1",
        "::1"
    };

    /// <summary>
    /// Gets or sets the blacklisted IP addresses or CIDR ranges.
    /// Only used when Mode is Blacklist.
    /// </summary>
    public HashSet<string> BlacklistedIps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets path-specific whitelist rules.
    /// Key: Path pattern, Value: Set of allowed IPs/ranges
    /// </summary>
    /// <remarks>
    /// This allows different IP restrictions for different endpoints.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.PathWhitelists["/api/admin/*"] = new HashSet&lt;string&gt; { "10.0.0.0/8" };
    /// options.PathWhitelists["/api/internal/*"] = new HashSet&lt;string&gt; { "192.168.1.0/24" };
    /// </code>
    /// </example>
    public Dictionary<string, HashSet<string>> PathWhitelists { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets path-specific blacklist rules.
    /// Key: Path pattern, Value: Set of blocked IPs/ranges
    /// </summary>
    public Dictionary<string, HashSet<string>> PathBlacklists { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets path patterns to exclude from IP filtering.
    /// These paths are always allowed regardless of IP.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/*"
    };

    /// <summary>
    /// Gets or sets whether to check X-Forwarded-For header for client IP.
    /// Enable this when behind a reverse proxy or load balancer.
    /// Default: false
    /// </summary>
    /// <remarks>
    /// When enabled, the middleware will look for the X-Forwarded-For header
    /// to determine the original client IP. Ensure your proxy is configured correctly.
    /// </remarks>
    public bool UseForwardedHeaders { get; set; } = false;

    /// <summary>
    /// Gets or sets the header name to use for client IP when behind a proxy.
    /// Default: X-Forwarded-For
    /// </summary>
    public string ForwardedHeaderName { get; set; } = "X-Forwarded-For";

    /// <summary>
    /// Gets or sets alternative header names to check for client IP.
    /// </summary>
    public HashSet<string> AlternativeIpHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Real-IP",
        "CF-Connecting-IP", // Cloudflare
        "True-Client-IP"    // Akamai
    };

    /// <summary>
    /// Gets or sets known proxy IPs to trust.
    /// Only the first non-proxy IP in X-Forwarded-For will be used.
    /// </summary>
    public HashSet<string> TrustedProxies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether to always allow localhost connections.
    /// Default: true
    /// </summary>
    public bool AlwaysAllowLocalhost { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log blocked requests.
    /// Default: true
    /// </summary>
    public bool LogBlockedRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include client IP in error response.
    /// Should be false in production for security.
    /// Default: false
    /// </summary>
    public bool IncludeIpInResponse { get; set; } = false;

    /// <summary>
    /// Gets or sets the custom blocked response message.
    /// </summary>
    public string BlockedMessage { get; set; } = "Access denied.";

    /// <summary>
    /// Gets or sets the HTTP status code for blocked requests.
    /// Default: 403 Forbidden
    /// </summary>
    public int BlockedStatusCode { get; set; } = 403;
}

/// <summary>
/// IP filtering mode.
/// </summary>
public enum IpFilteringMode
{
    /// <summary>
    /// IP filtering is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Only allow IPs in the whitelist.
    /// </summary>
    Whitelist,

    /// <summary>
    /// Block IPs in the blacklist, allow all others.
    /// </summary>
    Blacklist
}

