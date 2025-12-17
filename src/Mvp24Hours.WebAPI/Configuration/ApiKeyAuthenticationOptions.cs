//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for API Key authentication middleware.
/// </summary>
/// <remarks>
/// <para>
/// This configuration supports multiple API key sources and validation strategies:
/// <list type="bullet">
/// <item><strong>Header-based</strong> - API key in custom header (default: X-Api-Key)</item>
/// <item><strong>Query string</strong> - API key in query parameter</item>
/// <item><strong>Multiple keys</strong> - Support for multiple valid API keys</item>
/// <item><strong>Key scopes</strong> - Different keys for different access levels</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursApiKeyAuthentication(options =>
/// {
///     options.ApiKeys.Add("my-secret-api-key");
///     options.HeaderName = "X-Api-Key";
///     options.EnableQueryStringKey = false;
/// });
/// </code>
/// </example>
public class ApiKeyAuthenticationOptions
{
    /// <summary>
    /// Gets or sets the header name for API key.
    /// Default: X-Api-Key
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Gets or sets the query string parameter name for API key.
    /// Default: api_key
    /// </summary>
    public string QueryParameterName { get; set; } = "api_key";

    /// <summary>
    /// Gets or sets whether to enable API key in query string.
    /// Default: false (not recommended for production)
    /// </summary>
    /// <remarks>
    /// Query string API keys are logged in server logs and browser history.
    /// Use only for development or specific scenarios.
    /// </remarks>
    public bool EnableQueryStringKey { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable API key in header.
    /// Default: true
    /// </summary>
    public bool EnableHeaderKey { get; set; } = true;

    /// <summary>
    /// Gets or sets the valid API keys.
    /// </summary>
    public HashSet<string> ApiKeys { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets API keys with associated scopes/permissions.
    /// Key: API Key, Value: Set of scope names
    /// </summary>
    public Dictionary<string, HashSet<string>> ApiKeyScopes { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the custom API key validator function.
    /// This takes precedence over the ApiKeys collection if set.
    /// </summary>
    /// <remarks>
    /// Use this for database-backed or external API key validation.
    /// </remarks>
    public Func<string, Task<ApiKeyValidationResult>>? CustomValidator { get; set; }

    /// <summary>
    /// Gets or sets whether authentication is required for all requests.
    /// If false, endpoints must be explicitly marked with [ApiKeyRequired].
    /// Default: true
    /// </summary>
    public bool RequireAuthenticationByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets path patterns to exclude from API key authentication.
    /// Supports wildcards (*).
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/*",
        "/swagger",
        "/swagger/*",
        "/favicon.ico"
    };

    /// <summary>
    /// Gets or sets path patterns that require API key authentication.
    /// Only used when RequireAuthenticationByDefault is false.
    /// Supports wildcards (*).
    /// </summary>
    public HashSet<string> ProtectedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether to include API key name/identifier in response for debugging.
    /// Should be false in production.
    /// Default: false
    /// </summary>
    public bool IncludeKeyIdentifierInResponse { get; set; } = false;

    /// <summary>
    /// Gets or sets the realm name for 401 responses.
    /// Default: "API"
    /// </summary>
    public string Realm { get; set; } = "API";

    /// <summary>
    /// Gets or sets the challenge scheme name.
    /// Default: "ApiKey"
    /// </summary>
    public string ChallengeScheme { get; set; } = "ApiKey";

    /// <summary>
    /// Gets or sets whether to log failed authentication attempts.
    /// Default: true
    /// </summary>
    public bool LogFailedAttempts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log successful authentication.
    /// Default: false
    /// </summary>
    public bool LogSuccessfulAuthentication { get; set; } = false;

    /// <summary>
    /// Gets or sets rate limiting options per API key.
    /// </summary>
    public ApiKeyRateLimitOptions RateLimit { get; set; } = new();
}

/// <summary>
/// Rate limiting options for API keys.
/// </summary>
public class ApiKeyRateLimitOptions
{
    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// Default: false
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the default requests per minute limit.
    /// Default: 60
    /// </summary>
    public int DefaultRequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Gets or sets custom limits per API key.
    /// Key: API Key, Value: Requests per minute
    /// </summary>
    public Dictionary<string, int> KeyLimits { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Result of API key validation.
/// </summary>
public class ApiKeyValidationResult
{
    /// <summary>
    /// Gets or sets whether the API key is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the key identifier (for logging/tracking).
    /// </summary>
    public string? KeyIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the associated user/client ID.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the associated scopes.
    /// </summary>
    public HashSet<string> Scopes { get; set; } = new();

    /// <summary>
    /// Gets or sets additional claims to add to the principal.
    /// </summary>
    public Dictionary<string, string> Claims { get; set; } = new();

    /// <summary>
    /// Gets or sets the failure reason (if not valid).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ApiKeyValidationResult Success(string? keyIdentifier = null, string? clientId = null)
        => new() { IsValid = true, KeyIdentifier = keyIdentifier, ClientId = clientId };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ApiKeyValidationResult Failure(string reason)
        => new() { IsValid = false, FailureReason = reason };
}

