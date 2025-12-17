//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that enforces request body size limits.
/// </summary>
/// <remarks>
/// <para>
/// This middleware protects against denial-of-service attacks by limiting
/// the size of request bodies. It supports:
/// <list type="bullet">
/// <item>Global default size limit</item>
/// <item>Per-endpoint size limits</item>
/// <item>Per-content-type size limits</item>
/// <item>Method-specific limits (POST, PUT, PATCH)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursRequestSizeLimit()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursRequestSizeLimit(options =>
/// {
///     options.DefaultMaxBodySize = 10 * 1024 * 1024; // 10MB
///     options.EndpointLimits["/api/upload/*"] = 100 * 1024 * 1024; // 100MB for uploads
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursRequestSizeLimit();
/// </code>
/// </example>
public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestSizeLimitOptions _options;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="RequestSizeLimitMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The request size limit options.</param>
    /// <param name="logger">The logger.</param>
    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        IOptions<RequestSizeLimitOptions> options,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with size limiting.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if method should be limited
        if (!ShouldApplyLimit(context))
        {
            await _next(context);
            return;
        }

        // Determine the applicable limit
        var maxBodySize = GetApplicableLimit(context);

        // If limit is null, no limit applies
        if (!maxBodySize.HasValue)
        {
            await _next(context);
            return;
        }

        // Check Content-Length header first (quick check)
        var contentLength = context.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maxBodySize.Value)
        {
            if (_options.LogRejectedRequests)
            {
                _logger.LogWarning(
                    "Request rejected: Content-Length {ContentLength} exceeds limit {MaxSize} for path {Path}",
                    contentLength.Value,
                    maxBodySize.Value,
                    context.Request.Path);
            }

            await HandlePayloadTooLarge(context, maxBodySize.Value, contentLength.Value);
            return;
        }

        // Set the maximum request body size feature
        var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxRequestBodySizeFeature != null && !maxRequestBodySizeFeature.IsReadOnly)
        {
            maxRequestBodySizeFeature.MaxRequestBodySize = maxBodySize.Value;
        }

        // Set form options if applicable
        var formFeature = context.Features.Get<IFormFeature>();
        if (formFeature != null)
        {
            // Form options are set via IFormFeature, but we handle this at the service level
        }

        await _next(context);
    }

    private bool ShouldApplyLimit(HttpContext context)
    {
        // Check if method is in limited methods
        if (!_options.LimitedMethods.Contains(context.Request.Method))
        {
            return false;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value ?? "/";
        if (_options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern)))
        {
            return false;
        }

        return true;
    }

    private long? GetApplicableLimit(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var contentType = context.Request.ContentType?.Split(';').FirstOrDefault()?.Trim();

        // Check endpoint-specific limits first (most specific)
        foreach (var endpointLimit in _options.EndpointLimits)
        {
            if (MatchesPattern(path, endpointLimit.Key))
            {
                return endpointLimit.Value;
            }
        }

        // Check content-type specific limits
        if (!string.IsNullOrEmpty(contentType) &&
            _options.ContentTypeLimits.TryGetValue(contentType, out var contentTypeLimit))
        {
            return contentTypeLimit;
        }

        // Return default limit
        return _options.DefaultMaxBodySize;
    }

    private async Task HandlePayloadTooLarge(HttpContext context, long maxSize, long actualSize)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.11",
            title = "Payload Too Large",
            status = 413,
            detail = _options.IncludeLimitDetailsInError
                ? $"Request body size ({FormatBytes(actualSize)}) exceeds the maximum allowed size ({FormatBytes(maxSize)})."
                : "Request body size exceeds the maximum allowed size.",
            instance = context.Request.Path.Value,
            extensions = _options.IncludeLimitDetailsInError
                ? new { maxAllowedSize = maxSize, actualSize }
                : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
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

