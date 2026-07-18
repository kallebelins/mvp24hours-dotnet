//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.RateLimiting;

/// <summary>
/// Resolves rate limit partitions based on request context and configured policies.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="RateLimitPartitionResolver"/>.
/// </remarks>
public class RateLimitPartitionResolver(
    IOptions<RateLimitingOptions> options,
    IRateLimitKeyGenerator keyGenerator)
{
    private readonly RateLimitingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IRateLimitKeyGenerator _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
    private readonly ConcurrentDictionary<string, Regex> _pathPatternCache = new();

    /// <summary>
    /// Gets the rate limit partition for the given HTTP context.
    /// </summary>
    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        if (!_options.Enabled)
        {
            return RateLimitPartition.GetNoLimiter<string>("disabled");
        }

        // Check if request should be bypassed
        if (ShouldBypassRateLimiting(context))
        {
            return RateLimitPartition.GetNoLimiter<string>("bypassed");
        }

        // Find the applicable policy
        RateLimitPolicy? policy = FindApplicablePolicy(context);
        if (policy == null)
        {
            return RateLimitPartition.GetNoLimiter<string>("no-policy");
        }

        // Generate the partition key
        string partitionKey = _keyGenerator.GenerateKey(context, policy);

        // Create the appropriate rate limiter based on algorithm
        return CreatePartition(partitionKey, policy);
    }

    /// <summary>
    /// Determines if the request should bypass rate limiting.
    /// </summary>
    private bool ShouldBypassRateLimiting(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "/";

        // Check globally excluded paths
        if (_options.GlobalExcludedPaths.Any(pattern => MatchesPath(path, pattern)))
        {
            return true;
        }

        // Check whitelisted IPs
        string? clientIp = GetClientIpAddress(context);
        if (!string.IsNullOrEmpty(clientIp) && IsWhitelistedIp(clientIp))
        {
            return true;
        }

        // Check whitelisted API keys
        string? apiKey = context.Request.Headers[_options.ApiKeyHeaderName].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey) && _options.WhitelistedApiKeys.Contains(apiKey))
        {
            return true;
        }

        // Check whitelisted user IDs
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            string? userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId) && _options.WhitelistedUserIds.Contains(userId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the applicable rate limit policy for the request.
    /// </summary>
    private RateLimitPolicy? FindApplicablePolicy(HttpContext context)
    {
        string path = context.Request.Path.Value ?? "/";

        // Check endpoint-specific policies
        foreach ((string? pattern, string? policyName) in _options.EndpointPolicies)
        {
            if (MatchesPath(path, pattern) && _options.Policies.TryGetValue(policyName, out RateLimitPolicy? policy))
            {
                // Check if path is excluded from this policy
                if (!policy.ExcludedPaths.Any(excluded => MatchesPath(path, excluded)))
                {
                    return policy;
                }
            }
        }

        // Check policies with applied paths
        foreach ((string? name, RateLimitPolicy? policy) in _options.Policies)
        {
            if (policy.AppliedPaths.Count > 0)
            {
                if (policy.AppliedPaths.Any(pattern => MatchesPath(path, pattern)) &&
                    !policy.ExcludedPaths.Any(excluded => MatchesPath(path, excluded)))
                {
                    return policy;
                }
            }
        }

        // Return default policy
        if (_options.Policies.TryGetValue(_options.DefaultPolicyName, out RateLimitPolicy? defaultPolicy))
        {
            if (!defaultPolicy.ExcludedPaths.Any(excluded => MatchesPath(path, excluded)))
            {
                return defaultPolicy;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a rate limit partition based on the policy algorithm.
    /// </summary>
    private RateLimitPartition<string> CreatePartition(string partitionKey, RateLimitPolicy policy)
    {
        return policy.Algorithm switch
        {
            RateLimitingAlgorithm.FixedWindow => CreateFixedWindowPartition(partitionKey, policy),
            RateLimitingAlgorithm.SlidingWindow => CreateSlidingWindowPartition(partitionKey, policy),
            RateLimitingAlgorithm.TokenBucket => CreateTokenBucketPartition(partitionKey, policy),
            RateLimitingAlgorithm.Concurrency => CreateConcurrencyPartition(partitionKey, policy),
            _ => CreateSlidingWindowPartition(partitionKey, policy)
        };
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(string key, RateLimitPolicy policy)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = policy.Window,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = policy.QueueProcessingOrder == Configuration.QueueProcessingOrder.OldestFirst
                ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
            AutoReplenishment = policy.AutoReplenishment
        });
    }

    private static RateLimitPartition<string> CreateSlidingWindowPartition(string key, RateLimitPolicy policy)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = policy.Window,
            SegmentsPerWindow = policy.SegmentsPerWindow,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = policy.QueueProcessingOrder == Configuration.QueueProcessingOrder.OldestFirst
                ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
            AutoReplenishment = policy.AutoReplenishment
        });
    }

    private static RateLimitPartition<string> CreateTokenBucketPartition(string key, RateLimitPolicy policy)
    {
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = policy.PermitLimit,
            ReplenishmentPeriod = policy.ReplenishmentPeriod,
            TokensPerPeriod = policy.TokensPerPeriod,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = policy.QueueProcessingOrder == Configuration.QueueProcessingOrder.OldestFirst
                ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst,
            AutoReplenishment = policy.AutoReplenishment
        });
    }

    private static RateLimitPartition<string> CreateConcurrencyPartition(string key, RateLimitPolicy policy)
    {
        return RateLimitPartition.GetConcurrencyLimiter(key, _ => new ConcurrencyLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = policy.QueueProcessingOrder == Configuration.QueueProcessingOrder.OldestFirst
                ? System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
                : System.Threading.RateLimiting.QueueProcessingOrder.NewestFirst
        });
    }

    /// <summary>
    /// Checks if a path matches a pattern (supports wildcards).
    /// </summary>
    private bool MatchesPath(string path, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Exact match
        if (path.Equals(pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Convert pattern to regex and cache
        Regex regex = _pathPatternCache.GetOrAdd(pattern, p =>
        {
            string regexPattern = "^" + Regex.Escape(p)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*") + "$";
            return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        });

        return regex.IsMatch(path);
    }

    /// <summary>
    /// Checks if an IP is whitelisted (supports CIDR notation).
    /// </summary>
    private bool IsWhitelistedIp(string clientIp)
    {
        if (!IPAddress.TryParse(clientIp, out IPAddress? ipAddress))
        {
            return false;
        }

        foreach (string whitelistEntry in _options.WhitelistedIps)
        {
            if (whitelistEntry.Contains('/'))
            {
                // CIDR notation
                if (IsIpInRange(ipAddress, whitelistEntry))
                {
                    return true;
                }
            }
            else
            {
                // Exact IP match
                if (IPAddress.TryParse(whitelistEntry, out IPAddress? whitelistIp) &&
                    ipAddress.Equals(whitelistIp))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an IP is within a CIDR range.
    /// </summary>
    private static bool IsIpInRange(IPAddress address, string cidr)
    {
        try
        {
            string[] parts = cidr.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress? networkAddress) ||
                !int.TryParse(parts[1], out int prefixLength))
            {
                return false;
            }

            byte[] addressBytes = address.GetAddressBytes();
            byte[] networkBytes = networkAddress.GetAddressBytes();

            if (addressBytes.Length != networkBytes.Length)
            {
                return false;
            }

            int bytesToCheck = prefixLength / 8;
            int bitsToCheck = prefixLength % 8;

            for (int i = 0; i < bytesToCheck; i++)
            {
                if (addressBytes[i] != networkBytes[i])
                {
                    return false;
                }
            }

            if (bitsToCheck > 0 && bytesToCheck < addressBytes.Length)
            {
                byte mask = (byte)(0xFF << (8 - bitsToCheck));
                if ((addressBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
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

    /// <summary>
    /// Gets the client IP address from the HTTP context.
    /// </summary>
    private string? GetClientIpAddress(HttpContext context)
    {
        if (_options.UseForwardedHeaders)
        {
            string? forwardedFor = context.Request.Headers[_options.ForwardedForHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                string? firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(firstIp))
                {
                    return firstIp;
                }
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}

