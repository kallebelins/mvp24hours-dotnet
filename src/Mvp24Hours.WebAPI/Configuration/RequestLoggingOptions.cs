//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for request/response logging middleware.
/// </summary>
/// <remarks>
/// <para>
/// These options control what information is logged for incoming HTTP requests
/// and outgoing responses, including sensitive data masking.
/// </para>
/// </remarks>
public class RequestLoggingOptions
{
    /// <summary>
    /// Gets or sets the logging level for requests.
    /// </summary>
    public RequestLoggingLevel LoggingLevel { get; set; } = RequestLoggingLevel.Basic;

    /// <summary>
    /// Gets or sets whether to log request headers.
    /// </summary>
    public bool LogRequestHeaders { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log request body.
    /// </summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log response headers.
    /// </summary>
    public bool LogResponseHeaders { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log response body.
    /// </summary>
    public bool LogResponseBody { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum request body size to log (in bytes).
    /// Bodies larger than this will be truncated.
    /// </summary>
    public int MaxRequestBodySize { get; set; } = 32768; // 32KB

    /// <summary>
    /// Gets or sets the maximum response body size to log (in bytes).
    /// Bodies larger than this will be truncated.
    /// </summary>
    public int MaxResponseBodySize { get; set; } = 32768; // 32KB

    /// <summary>
    /// Gets or sets the header names that should be masked in logs.
    /// Default includes Authorization and common sensitive headers.
    /// </summary>
    public HashSet<string> SensitiveHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "X-Api-Key",
        "Api-Key",
        "X-Auth-Token",
        "Cookie",
        "Set-Cookie",
        "X-CSRF-Token",
        "X-Requested-With"
    };

    /// <summary>
    /// Gets or sets the JSON property names that should be masked in request/response bodies.
    /// </summary>
    public HashSet<string> SensitiveProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "senha",
        "secret",
        "token",
        "accessToken",
        "refreshToken",
        "creditCard",
        "cardNumber",
        "cvv",
        "ssn",
        "cpf",
        "cnpj"
    };

    /// <summary>
    /// Gets or sets the mask string used to replace sensitive values.
    /// </summary>
    public string MaskValue { get; set; } = "***REDACTED***";

    /// <summary>
    /// Gets or sets the path patterns to exclude from logging.
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
    /// Gets or sets the content types that should have body logging.
    /// </summary>
    public HashSet<string> LoggableContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/json",
        "text/xml",
        "application/x-www-form-urlencoded"
    };

    /// <summary>
    /// Gets or sets whether to include exception details in logs.
    /// Should be false in production.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log slow requests.
    /// </summary>
    public bool LogSlowRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold in milliseconds for a request to be considered slow.
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 3000;

    /// <summary>
    /// Gets or sets whether to log query strings.
    /// </summary>
    public bool LogQueryString { get; set; } = true;

    /// <summary>
    /// Gets or sets query string parameters that should be masked.
    /// </summary>
    public HashSet<string> SensitiveQueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "token",
        "api_key",
        "apikey",
        "key",
        "password",
        "secret"
    };
}

/// <summary>
/// Defines the level of detail for request logging.
/// </summary>
public enum RequestLoggingLevel
{
    /// <summary>
    /// No logging.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic logging: method, path, status code, duration.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Standard logging: basic + headers (masked).
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Detailed logging: standard + request/response bodies (masked).
    /// </summary>
    Detailed = 3,

    /// <summary>
    /// Full logging: all information including sensitive data (use only for debugging).
    /// </summary>
    Full = 4
}

