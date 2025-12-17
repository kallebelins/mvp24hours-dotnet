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
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that provides API Key authentication.
/// </summary>
/// <remarks>
/// <para>
/// This middleware validates API keys from headers or query strings and
/// creates an authenticated principal for successful validations.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// <list type="bullet">
/// <item>Header-based API key (configurable header name)</item>
/// <item>Optional query string API key</item>
/// <item>Multiple valid API keys support</item>
/// <item>Custom validator function support</item>
/// <item>Scope-based authorization support</item>
/// <item>Path-based exclusions</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursApiKeyAuthentication()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursApiKeyAuthentication(options =>
/// {
///     options.ApiKeys.Add("my-secret-api-key");
///     options.HeaderName = "X-Api-Key";
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursApiKeyAuthentication();
/// </code>
/// </example>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyAuthenticationOptions _options;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    private const string ApiKeyContextKey = "Mvp24Hours.ApiKey";
    private const string ApiKeyValidationResultKey = "Mvp24Hours.ApiKeyValidationResult";

    /// <summary>
    /// Creates a new instance of <see cref="ApiKeyAuthenticationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The API key authentication options.</param>
    /// <param name="logger">The logger.</param>
    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<ApiKeyAuthenticationOptions> options,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with API key authentication.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if path is excluded
        if (IsExcludedPath(context))
        {
            await _next(context);
            return;
        }

        // Check if path requires authentication
        if (!RequiresAuthentication(context))
        {
            await _next(context);
            return;
        }

        // Try to extract API key
        var apiKey = ExtractApiKey(context);

        if (string.IsNullOrEmpty(apiKey))
        {
            await HandleUnauthorized(context, "API key is missing");
            return;
        }

        // Validate API key
        var validationResult = await ValidateApiKey(apiKey);

        if (!validationResult.IsValid)
        {
            if (_options.LogFailedAttempts)
            {
                _logger.LogWarning(
                    "API key authentication failed for path {Path}: {Reason}",
                    context.Request.Path,
                    validationResult.FailureReason);
            }

            await HandleUnauthorized(context, validationResult.FailureReason ?? "Invalid API key");
            return;
        }

        // Store validation result for downstream use
        context.Items[ApiKeyContextKey] = apiKey;
        context.Items[ApiKeyValidationResultKey] = validationResult;

        // Create authenticated principal
        var principal = CreatePrincipal(validationResult);
        context.User = principal;

        if (_options.LogSuccessfulAuthentication)
        {
            _logger.LogInformation(
                "API key authentication successful for path {Path}, Client: {ClientId}",
                context.Request.Path,
                validationResult.ClientId ?? validationResult.KeyIdentifier ?? "unknown");
        }

        await _next(context);
    }

    private string? ExtractApiKey(HttpContext context)
    {
        string? apiKey = null;

        // Try header first
        if (_options.EnableHeaderKey &&
            context.Request.Headers.TryGetValue(_options.HeaderName, out var headerValue))
        {
            apiKey = headerValue.ToString();
        }

        // Try query string if header not found and enabled
        if (string.IsNullOrEmpty(apiKey) &&
            _options.EnableQueryStringKey &&
            context.Request.Query.TryGetValue(_options.QueryParameterName, out var queryValue))
        {
            apiKey = queryValue.ToString();
        }

        return apiKey;
    }

    private async Task<ApiKeyValidationResult> ValidateApiKey(string apiKey)
    {
        // Use custom validator if provided
        if (_options.CustomValidator != null)
        {
            return await _options.CustomValidator(apiKey);
        }

        // Check against configured API keys
        if (_options.ApiKeys.Contains(apiKey))
        {
            var result = ApiKeyValidationResult.Success(GenerateKeyIdentifier(apiKey));

            // Add scopes if configured
            if (_options.ApiKeyScopes.TryGetValue(apiKey, out var scopes))
            {
                result.Scopes = scopes;
            }

            return result;
        }

        // Check scoped keys
        if (_options.ApiKeyScopes.ContainsKey(apiKey))
        {
            var result = ApiKeyValidationResult.Success(GenerateKeyIdentifier(apiKey));
            result.Scopes = _options.ApiKeyScopes[apiKey];
            return result;
        }

        return ApiKeyValidationResult.Failure("Invalid API key");
    }

    private static string GenerateKeyIdentifier(string apiKey)
    {
        // Generate a safe identifier from the key (first 4 chars + last 4 chars)
        if (apiKey.Length <= 8)
        {
            return new string('*', apiKey.Length);
        }

        return $"{apiKey[..4]}****{apiKey[^4..]}";
    }

    private ClaimsPrincipal CreatePrincipal(ApiKeyValidationResult validationResult)
    {
        var claims = new System.Collections.Generic.List<Claim>
        {
            new Claim(ClaimTypes.AuthenticationMethod, _options.ChallengeScheme)
        };

        if (!string.IsNullOrEmpty(validationResult.ClientId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, validationResult.ClientId));
            claims.Add(new Claim(ClaimTypes.Name, validationResult.ClientId));
        }

        if (!string.IsNullOrEmpty(validationResult.KeyIdentifier))
        {
            claims.Add(new Claim("api_key_identifier", validationResult.KeyIdentifier));
        }

        // Add scopes as claims
        foreach (var scope in validationResult.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        // Add custom claims
        foreach (var claim in validationResult.Claims)
        {
            claims.Add(new Claim(claim.Key, claim.Value));
        }

        var identity = new ClaimsIdentity(claims, _options.ChallengeScheme);
        return new ClaimsPrincipal(identity);
    }

    private async Task HandleUnauthorized(HttpContext context, string reason)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = $"{_options.ChallengeScheme} realm=\"{_options.Realm}\"";
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            title = "Unauthorized",
            status = 401,
            detail = reason,
            instance = context.Request.Path.Value
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private bool IsExcludedPath(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private bool RequiresAuthentication(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (_options.RequireAuthenticationByDefault)
        {
            return true;
        }

        // Check if path matches protected patterns
        return _options.ProtectedPaths.Any(pattern => MatchesPattern(path, pattern));
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

/// <summary>
/// Attribute to require API key authentication on specific endpoints.
/// Used when RequireAuthenticationByDefault is false.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ApiKeyRequiredAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the required scopes for this endpoint.
    /// </summary>
    public string[]? Scopes { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ApiKeyRequiredAttribute"/>.
    /// </summary>
    public ApiKeyRequiredAttribute() { }

    /// <summary>
    /// Creates a new instance with required scopes.
    /// </summary>
    /// <param name="scopes">The required scopes.</param>
    public ApiKeyRequiredAttribute(params string[] scopes)
    {
        Scopes = scopes;
    }
}

