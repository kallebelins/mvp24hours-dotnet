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
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that filters requests based on client IP address.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides IP-based access control with support for:
/// <list type="bullet">
/// <item>Whitelist mode - only allow specified IPs</item>
/// <item>Blacklist mode - block specified IPs</item>
/// <item>CIDR notation for IP ranges</item>
/// <item>Path-specific IP rules</item>
/// <item>Proxy-aware IP extraction</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursIpFiltering()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursIpFiltering(options =>
/// {
///     options.Enabled = true;
///     options.Mode = IpFilteringMode.Whitelist;
///     options.WhitelistedIps.Add("192.168.1.0/24");
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursIpFiltering();
/// </code>
/// </example>
public class IpFilteringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IpFilteringOptions _options;
    private readonly ILogger<IpFilteringMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="IpFilteringMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The IP filtering options.</param>
    /// <param name="logger">The logger.</param>
    public IpFilteringMiddleware(
        RequestDelegate next,
        IOptions<IpFilteringOptions> options,
        ILogger<IpFilteringMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with IP filtering.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if filtering is enabled
        if (!_options.Enabled || _options.Mode == IpFilteringMode.Disabled)
        {
            await _next(context);
            return;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value ?? "/";
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Get client IP
        var clientIp = GetClientIpAddress(context);
        if (clientIp == null)
        {
            _logger.LogWarning("Could not determine client IP address for request to {Path}", path);
            await HandleBlockedRequest(context, "Unknown");
            return;
        }

        // Check if localhost is always allowed
        if (_options.AlwaysAllowLocalhost && IsLocalhost(clientIp))
        {
            await _next(context);
            return;
        }

        // Check path-specific rules first
        var pathAllowed = CheckPathSpecificRules(path, clientIp);
        if (pathAllowed.HasValue)
        {
            if (pathAllowed.Value)
            {
                await _next(context);
                return;
            }
            else
            {
                await HandleBlockedRequest(context, clientIp.ToString());
                return;
            }
        }

        // Check global rules
        var isAllowed = _options.Mode switch
        {
            IpFilteringMode.Whitelist => IsIpAllowed(clientIp, _options.WhitelistedIps),
            IpFilteringMode.Blacklist => !IsIpAllowed(clientIp, _options.BlacklistedIps),
            _ => true
        };

        if (!isAllowed)
        {
            await HandleBlockedRequest(context, clientIp.ToString());
            return;
        }

        await _next(context);
    }

    private IPAddress? GetClientIpAddress(HttpContext context)
    {
        IPAddress? clientIp = null;

        // Try forwarded headers first if enabled
        if (_options.UseForwardedHeaders)
        {
            clientIp = GetIpFromForwardedHeaders(context);
        }

        // Fall back to connection remote IP
        if (clientIp == null)
        {
            clientIp = context.Connection.RemoteIpAddress;
        }

        // Handle IPv4-mapped IPv6 addresses
        if (clientIp?.IsIPv4MappedToIPv6 == true)
        {
            clientIp = clientIp.MapToIPv4();
        }

        return clientIp;
    }

    private IPAddress? GetIpFromForwardedHeaders(HttpContext context)
    {
        // Try primary forwarded header
        if (context.Request.Headers.TryGetValue(_options.ForwardedHeaderName, out var forwardedFor))
        {
            var ip = ParseForwardedForHeader(forwardedFor.ToString());
            if (ip != null) return ip;
        }

        // Try alternative headers
        foreach (var header in _options.AlternativeIpHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var headerValue))
            {
                var ipString = headerValue.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ipString) && IPAddress.TryParse(ipString, out var ip))
                {
                    return ip;
                }
            }
        }

        return null;
    }

    private IPAddress? ParseForwardedForHeader(string header)
    {
        var ips = header.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        // Find the first IP that's not a trusted proxy
        foreach (var ipString in ips)
        {
            if (IPAddress.TryParse(ipString, out var ip))
            {
                // Check if it's a trusted proxy
                if (!IsIpAllowed(ip, _options.TrustedProxies))
                {
                    return ip;
                }
            }
        }

        // If all are trusted proxies, return the first one
        if (ips.Count > 0 && IPAddress.TryParse(ips[0], out var firstIp))
        {
            return firstIp;
        }

        return null;
    }

    private bool? CheckPathSpecificRules(string path, IPAddress clientIp)
    {
        // Check path-specific whitelist
        foreach (var rule in _options.PathWhitelists)
        {
            if (MatchesPattern(path, rule.Key))
            {
                return IsIpAllowed(clientIp, rule.Value);
            }
        }

        // Check path-specific blacklist
        foreach (var rule in _options.PathBlacklists)
        {
            if (MatchesPattern(path, rule.Key))
            {
                return !IsIpAllowed(clientIp, rule.Value);
            }
        }

        return null; // No path-specific rule found
    }

    private static bool IsIpAllowed(IPAddress clientIp, System.Collections.Generic.IEnumerable<string> ipList)
    {
        foreach (var entry in ipList)
        {
            if (MatchesIpOrRange(clientIp, entry))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesIpOrRange(IPAddress clientIp, string entry)
    {
        // Check if it's a CIDR range
        if (entry.Contains('/'))
        {
            return IsInCidrRange(clientIp, entry);
        }

        // Check exact IP match
        if (IPAddress.TryParse(entry, out var allowedIp))
        {
            return clientIp.Equals(allowedIp);
        }

        return false;
    }

    private static bool IsInCidrRange(IPAddress clientIp, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkAddress) || !int.TryParse(parts[1], out var prefixLength))
            {
                return false;
            }

            // Ensure both addresses are the same type
            if (networkAddress.AddressFamily != clientIp.AddressFamily)
            {
                // Try to map IPv4 to IPv6 or vice versa
                if (clientIp.IsIPv4MappedToIPv6)
                {
                    clientIp = clientIp.MapToIPv4();
                }
                if (networkAddress.IsIPv4MappedToIPv6)
                {
                    networkAddress = networkAddress.MapToIPv4();
                }

                if (networkAddress.AddressFamily != clientIp.AddressFamily)
                {
                    return false;
                }
            }

            var networkBytes = networkAddress.GetAddressBytes();
            var clientBytes = clientIp.GetAddressBytes();

            if (networkBytes.Length != clientBytes.Length)
            {
                return false;
            }

            int totalBits = networkBytes.Length * 8;
            if (prefixLength < 0 || prefixLength > totalBits)
            {
                return false;
            }

            // Compare bytes
            int fullBytes = prefixLength / 8;
            int remainingBits = prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (networkBytes[i] != clientBytes[i])
                {
                    return false;
                }
            }

            if (remainingBits > 0 && fullBytes < networkBytes.Length)
            {
                int mask = (byte)(0xFF << (8 - remainingBits));
                if ((networkBytes[fullBytes] & mask) != (clientBytes[fullBytes] & mask))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocalhost(IPAddress ip)
    {
        return IPAddress.IsLoopback(ip) ||
               ip.Equals(IPAddress.Parse("127.0.0.1")) ||
               ip.Equals(IPAddress.IPv6Loopback);
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private async Task HandleBlockedRequest(HttpContext context, string clientIp)
    {
        if (_options.LogBlockedRequests)
        {
            _logger.LogWarning(
                "IP {ClientIp} blocked from accessing {Path}",
                clientIp,
                context.Request.Path);
        }

        context.Response.StatusCode = _options.BlockedStatusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Forbidden",
            status = _options.BlockedStatusCode,
            detail = _options.BlockedMessage,
            instance = context.Request.Path.Value,
            extensions = _options.IncludeIpInResponse
                ? new { clientIp }
                : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
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

