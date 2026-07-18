//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Services;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that logs HTTP requests and responses with structured logging
/// and sensitive data masking.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides comprehensive request/response logging with:
/// <list type="bullet">
/// <item>Configurable logging levels (Basic, Standard, Detailed, Full)</item>
/// <item>Automatic masking of sensitive headers and body properties</item>
/// <item>Request/response body capture with size limits</item>
/// <item>Slow request detection and warning</item>
/// <item>Path exclusion patterns</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursRequestLogging()</c> to register required services.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursRequestLogging(options =>
/// {
///     options.LoggingLevel = RequestLoggingLevel.Standard;
///     options.LogRequestHeaders = true;
///     options.LogSlowRequests = true;
///     options.SlowRequestThresholdMs = 3000;
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursRequestLogging();
/// </code>
/// </example>
/// <remarks>
/// Creates a new instance of <see cref="RequestLoggingMiddleware"/>.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The request logger service.</param>
/// <param name="options">The logging options.</param>
public class RequestLoggingMiddleware(
    RequestDelegate next,
    IRequestLogger logger,
    IOptions<RequestLoggingOptions> options)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly IRequestLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RequestLoggingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Processes the HTTP request with logging.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for excluded paths
        if (ShouldSkipLogging(context))
        {
            await _next(context);
            return;
        }

        // Enable request body buffering for logging
        if (_options.LogRequestBody)
        {
            context.Request.EnableBuffering();
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            // Log request
            await _logger.LogRequestAsync(context);

            // Capture response body if needed
            if (_options.LogResponseBody)
            {
                await ProcessWithResponseCapture(context, stopwatch);
            }
            else
            {
                await _next(context);
                stopwatch.Stop();

                // Log response
                await _logger.LogResponseAsync(context, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            stopwatch.Stop();

            // Log exception
            await _logger.LogExceptionAsync(context, ex, stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
        finally
        {
            // Log slow request warning if applicable
            if (exception == null && _options.LogSlowRequests)
            {
                double durationMs = stopwatch.Elapsed.TotalMilliseconds;
                if (durationMs > _options.SlowRequestThresholdMs)
                {
                    await _logger.LogSlowRequestAsync(context, durationMs, _options.SlowRequestThresholdMs);
                }
            }
        }
    }

    private async Task ProcessWithResponseCapture(HttpContext context, Stopwatch stopwatch)
    {
        Stream originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Copy the response body back to the original stream
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBodyStream);

            // Log response with body
            await _logger.LogResponseAsync(context, stopwatch.Elapsed.TotalMilliseconds);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldSkipLogging(HttpContext context)
    {
        if (_options.LoggingLevel == RequestLoggingLevel.None)
        {
            return true;
        }

        string path = context.Request.Path.Value ?? "/";

        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Convert glob pattern to regex
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}

