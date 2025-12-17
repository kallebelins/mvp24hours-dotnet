//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for input sanitization middleware.
/// </summary>
/// <remarks>
/// <para>
/// This configuration provides protection against common input-based attacks:
/// <list type="bullet">
/// <item><strong>XSS</strong> - Cross-Site Scripting attacks</item>
/// <item><strong>SQL Injection</strong> - Detection of SQL injection patterns</item>
/// <item><strong>Path Traversal</strong> - Directory traversal attempts</item>
/// <item><strong>Command Injection</strong> - OS command injection patterns</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddMvp24HoursInputSanitization(options =>
/// {
///     options.Mode = SanitizationMode.Validate;
///     options.EnableXssSanitization = true;
///     options.EnableSqlInjectionDetection = true;
/// });
/// </code>
/// </example>
public class InputSanitizationOptions
{
    /// <summary>
    /// Gets or sets whether sanitization is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the sanitization mode.
    /// Default: Validate (reject suspicious input)
    /// </summary>
    public SanitizationMode Mode { get; set; } = SanitizationMode.Validate;

    /// <summary>
    /// Gets or sets whether to enable XSS sanitization.
    /// Default: true
    /// </summary>
    public bool EnableXssSanitization { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable SQL injection detection.
    /// Default: true
    /// </summary>
    /// <remarks>
    /// This should be combined with parameterized queries. 
    /// It provides an additional layer of defense.
    /// </remarks>
    public bool EnableSqlInjectionDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable path traversal detection.
    /// Default: true
    /// </summary>
    public bool EnablePathTraversalDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable command injection detection.
    /// Default: true
    /// </summary>
    public bool EnableCommandInjectionDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable LDAP injection detection.
    /// Default: true
    /// </summary>
    public bool EnableLdapInjectionDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to sanitize query strings.
    /// Default: true
    /// </summary>
    public bool SanitizeQueryStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to sanitize headers.
    /// Default: true
    /// </summary>
    public bool SanitizeHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to sanitize request body.
    /// Default: true
    /// </summary>
    public bool SanitizeRequestBody { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to sanitize form fields.
    /// Default: true
    /// </summary>
    public bool SanitizeFormFields { get; set; } = true;

    /// <summary>
    /// Gets or sets headers to exclude from sanitization.
    /// </summary>
    public HashSet<string> ExcludedHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Content-Type",
        "Content-Length",
        "Accept",
        "Accept-Language",
        "Accept-Encoding",
        "User-Agent",
        "Host",
        "Origin",
        "Referer"
    };

    /// <summary>
    /// Gets or sets query parameters to exclude from sanitization.
    /// </summary>
    public HashSet<string> ExcludedQueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets JSON properties to exclude from sanitization.
    /// Useful for fields that legitimately contain code or markup.
    /// </summary>
    public HashSet<string> ExcludedJsonProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets path patterns to exclude from sanitization.
    /// </summary>
    public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/*",
        "/swagger",
        "/swagger/*"
    };

    /// <summary>
    /// Gets or sets content types that should have body sanitization.
    /// </summary>
    public HashSet<string> SanitizableContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "multipart/form-data",
        "text/plain"
    };

    /// <summary>
    /// Gets or sets custom dangerous patterns to detect.
    /// Key: Pattern name, Value: Regex pattern
    /// </summary>
    public Dictionary<string, string> CustomPatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether to log detected attacks.
    /// Default: true
    /// </summary>
    public bool LogDetectedAttacks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include pattern details in logs.
    /// Default: false (security consideration)
    /// </summary>
    public bool IncludePatternDetailsInLog { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include suspicious input in logs.
    /// Default: false (security consideration)
    /// </summary>
    public bool IncludeSuspiciousInputInLog { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum input length to scan.
    /// Default: 32KB
    /// </summary>
    public int MaxInputLengthToScan { get; set; } = 32768;

    /// <summary>
    /// Gets or sets the custom error message for rejected requests.
    /// </summary>
    public string RejectionMessage { get; set; } = "The request contains potentially dangerous content.";
}

/// <summary>
/// Sanitization mode.
/// </summary>
public enum SanitizationMode
{
    /// <summary>
    /// Validate input and reject if suspicious patterns are found.
    /// </summary>
    Validate,

    /// <summary>
    /// Sanitize input by removing/encoding dangerous content.
    /// </summary>
    Sanitize,

    /// <summary>
    /// Log only - detect and log but don't block.
    /// </summary>
    LogOnly
}

