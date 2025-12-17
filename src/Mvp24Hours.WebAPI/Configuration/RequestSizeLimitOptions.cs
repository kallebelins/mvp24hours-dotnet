//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for request size limiting middleware.
/// </summary>
/// <remarks>
/// <para>
/// This configuration allows setting maximum request body sizes globally
/// and per endpoint to prevent denial-of-service attacks via large payloads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursRequestSizeLimit(options =>
/// {
///     options.DefaultMaxBodySize = 10 * 1024 * 1024; // 10MB
///     options.EndpointLimits["/api/upload/*"] = 100 * 1024 * 1024; // 100MB for uploads
///     options.EndpointLimits["/api/config/*"] = 1024; // 1KB for config
/// });
/// </code>
/// </example>
public class RequestSizeLimitOptions
{
    /// <summary>
    /// Gets or sets the default maximum request body size in bytes.
    /// Default: 30MB (31457280 bytes)
    /// </summary>
    /// <remarks>
    /// Set to null to disable the limit (not recommended for production).
    /// </remarks>
    public long? DefaultMaxBodySize { get; set; } = 30 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum form field count.
    /// Default: 1024
    /// </summary>
    public int MaxFormFieldCount { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the maximum form field name length.
    /// Default: 2048
    /// </summary>
    public int MaxFormFieldNameLength { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the maximum form multipart boundary length.
    /// Default: 128
    /// </summary>
    public int MaxMultipartBoundaryLength { get; set; } = 128;

    /// <summary>
    /// Gets or sets endpoint-specific body size limits.
    /// Key: Path pattern (supports wildcards), Value: Max size in bytes
    /// </summary>
    public Dictionary<string, long> EndpointLimits { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets content-type specific body size limits.
    /// Key: Content type, Value: Max size in bytes
    /// </summary>
    public Dictionary<string, long> ContentTypeLimits { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/json"] = 10 * 1024 * 1024, // 10MB for JSON
        ["application/xml"] = 10 * 1024 * 1024, // 10MB for XML
        ["multipart/form-data"] = 100 * 1024 * 1024 // 100MB for file uploads
    };

    /// <summary>
    /// Gets or sets path patterns to exclude from size limiting.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/*",
        "/swagger",
        "/swagger/*"
    };

    /// <summary>
    /// Gets or sets whether to include the limit details in error responses.
    /// Default: true
    /// </summary>
    public bool IncludeLimitDetailsInError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log rejected requests.
    /// Default: true
    /// </summary>
    public bool LogRejectedRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP methods to apply size limits.
    /// Default: POST, PUT, PATCH
    /// </summary>
    public HashSet<string> LimitedMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST",
        "PUT",
        "PATCH"
    };
}

