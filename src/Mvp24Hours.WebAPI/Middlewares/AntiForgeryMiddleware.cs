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
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that provides CSRF/Anti-Forgery protection for SPAs.
/// </summary>
/// <remarks>
/// <para>
/// This middleware implements the double-submit cookie pattern for CSRF protection:
/// <list type="bullet">
/// <item>Generates and sets a CSRF token in a cookie (readable by JavaScript)</item>
/// <item>Validates that the token from cookie matches the token in request header</item>
/// <item>Rejects requests that fail validation</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursAntiForgery()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursAntiForgery(options =>
/// {
///     options.CookieName = "XSRF-TOKEN";
///     options.HeaderName = "X-XSRF-TOKEN";
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursAntiForgery();
/// </code>
/// </example>
public class AntiForgeryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AntiForgeryOptions _options;
    private readonly ILogger<AntiForgeryMiddleware> _logger;

    private const string TokenContextKey = "Mvp24Hours.AntiForgeryToken";

    /// <summary>
    /// Creates a new instance of <see cref="AntiForgeryMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The anti-forgery options.</param>
    /// <param name="logger">The logger.</param>
    public AntiForgeryMiddleware(
        RequestDelegate next,
        IOptions<AntiForgeryOptions> options,
        ILogger<AntiForgeryMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with anti-forgery protection.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if anti-forgery is enabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Handle token endpoint
        if (_options.RegisterTokenEndpoint &&
            context.Request.Path.Equals(_options.TokenEndpoint, StringComparison.OrdinalIgnoreCase) &&
            context.Request.Method == "GET")
        {
            await HandleTokenEndpoint(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Check if path is excluded
        if (IsExcludedPath(path))
        {
            await EnsureTokenExists(context);
            await _next(context);
            return;
        }

        // Check if method requires validation
        if (!RequiresValidation(context))
        {
            await EnsureTokenExists(context);
            await _next(context);
            return;
        }

        // Validate token
        if (!await ValidateToken(context))
        {
            if (_options.LogValidationFailures)
            {
                _logger.LogWarning(
                    "Anti-forgery validation failed for {Method} {Path}",
                    context.Request.Method,
                    path);
            }

            await HandleValidationFailure(context);
            return;
        }

        // Refresh token if configured
        if (_options.RefreshTokenOnEachRequest)
        {
            SetNewToken(context);
        }

        await _next(context);
    }

    private async Task HandleTokenEndpoint(HttpContext context)
    {
        var token = GetOrCreateToken(context);
        SetTokenCookie(context, token);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        var response = new { token };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private Task<bool> ValidateToken(HttpContext context)
    {
        // Get token from cookie
        var cookieToken = context.Request.Cookies[_options.CookieName];

        // Skip validation if no cookie and configured to do so
        if (string.IsNullOrEmpty(cookieToken) && _options.SkipValidationForRequestsWithoutCookies)
        {
            return Task.FromResult(true);
        }

        // Get token from header
        var headerToken = context.Request.Headers[_options.HeaderName].FirstOrDefault();

        // Both must be present
        if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
        {
            return Task.FromResult(false);
        }

        // Tokens must match (constant-time comparison for security)
        var isValid = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(cookieToken),
            System.Text.Encoding.UTF8.GetBytes(headerToken));

        return Task.FromResult(isValid);
    }

    private Task EnsureTokenExists(HttpContext context)
    {
        // Only set token for GET requests to pages (not API calls without existing token)
        if (context.Request.Method == "GET" && !context.Request.Cookies.ContainsKey(_options.CookieName))
        {
            var token = GenerateToken();
            SetTokenCookie(context, token);
        }

        return Task.CompletedTask;
    }

    private void SetNewToken(HttpContext context)
    {
        var token = GenerateToken();
        SetTokenCookie(context, token);
        context.Items[TokenContextKey] = token;
    }

    private string GetOrCreateToken(HttpContext context)
    {
        // Check if we already have a token in context
        if (context.Items.TryGetValue(TokenContextKey, out var existingToken) && existingToken is string token)
        {
            return token;
        }

        // Generate new token
        var newToken = GenerateToken();
        context.Items[TokenContextKey] = newToken;
        return newToken;
    }

    private static string GenerateToken()
    {
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    private void SetTokenCookie(HttpContext context, string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = _options.CookieHttpOnly,
            Secure = _options.CookieSecure,
            SameSite = _options.CookieSameSite,
            Path = _options.CookiePath,
            Domain = _options.CookieDomain,
            Expires = DateTimeOffset.UtcNow.Add(_options.TokenExpiration)
        };

        context.Response.Cookies.Append(_options.CookieName, token, cookieOptions);
    }

    private bool RequiresValidation(HttpContext context)
    {
        // Check if method is protected
        if (!_options.ProtectedMethods.Contains(context.Request.Method))
        {
            return false;
        }

        // If protected paths are specified, check if current path matches
        if (_options.ProtectedPaths.Count > 0)
        {
            var path = context.Request.Path.Value ?? "/";
            return _options.ProtectedPaths.Any(pattern => MatchesPattern(path, pattern));
        }

        return true;
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private async Task HandleValidationFailure(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = 400,
            detail = "Anti-forgery token validation failed.",
            instance = context.Request.Path.Value
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
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

