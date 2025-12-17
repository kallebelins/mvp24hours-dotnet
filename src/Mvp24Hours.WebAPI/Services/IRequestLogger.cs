//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Services;

/// <summary>
/// Interface for logging HTTP requests and responses with configurable levels.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface handle the actual logging of request/response
/// data with support for sensitive data masking and configurable detail levels.
/// </para>
/// </remarks>
public interface IRequestLogger
{
    /// <summary>
    /// Logs the incoming HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context containing request information.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogRequestAsync(HttpContext context);

    /// <summary>
    /// Logs the outgoing HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context containing response information.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogResponseAsync(HttpContext context, double durationMs);

    /// <summary>
    /// Logs an exception that occurred during request processing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogExceptionAsync(HttpContext context, Exception exception, double durationMs);

    /// <summary>
    /// Logs a slow request warning.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="thresholdMs">The configured threshold in milliseconds.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogSlowRequestAsync(HttpContext context, double durationMs, int thresholdMs);
}

/// <summary>
/// Data transfer object containing logged request information.
/// </summary>
public record RequestLogData
{
    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.).
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the query string (masked).
    /// </summary>
    public string? QueryString { get; init; }

    /// <summary>
    /// Gets or sets the request scheme (http/https).
    /// </summary>
    public string Scheme { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host.
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the client IP address.
    /// </summary>
    public string? ClientIp { get; init; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the content length in bytes.
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// Gets or sets the request headers (masked).
    /// </summary>
    public string? Headers { get; init; }

    /// <summary>
    /// Gets or sets the request body (masked and truncated).
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets or sets the authenticated user ID.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the request was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Data transfer object containing logged response information.
/// </summary>
public record ResponseLogData
{
    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.).
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets the request duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the content length in bytes.
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// Gets or sets the response headers (masked).
    /// </summary>
    public string? Headers { get; init; }

    /// <summary>
    /// Gets or sets the response body (masked and truncated).
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the response was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets whether the request was successful (2xx status).
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets or sets whether the request was slow.
    /// </summary>
    public bool IsSlow { get; init; }
}

/// <summary>
/// Data transfer object containing logged exception information.
/// </summary>
public record ExceptionLogData
{
    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method (GET, POST, etc.).
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception type name.
    /// </summary>
    public string ExceptionType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string ExceptionMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the stack trace (if enabled).
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets the request duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the exception occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

