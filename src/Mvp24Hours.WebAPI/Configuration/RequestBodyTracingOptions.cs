//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.WebAPI.Configuration;

/// <summary>
/// Configuration options for request body tracing middleware.
/// </summary>
public class RequestBodyTracingOptions
{
    /// <summary>
    /// Gets or sets whether request body tracing is enabled.
    /// Default is false to avoid changing existing observability behavior.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether the middleware should trace bodies when there is no current activity.
    /// </summary>
    public bool TraceWithoutActivity { get; set; }

    /// <summary>
    /// Gets or sets the HTTP methods eligible for body tracing.
    /// </summary>
    public HashSet<string> TracedMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST",
        "PUT",
        "PATCH"
    };

    /// <summary>
    /// Gets or sets path patterns to exclude from body tracing.
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
    /// Gets or sets content types that should have body tracing.
    /// Supports wildcard patterns such as application/*+json.
    /// </summary>
    public HashSet<string> TracedContentTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/*+json",
        "text/json",
        "application/x-www-form-urlencoded"
    };

    /// <summary>
    /// Gets or sets the maximum number of bytes captured from the request body.
    /// </summary>
    public int MaxBodySizeBytes { get; set; } = 16 * 1024;

    /// <summary>
    /// Gets or sets whether to append a suffix when content is truncated.
    /// </summary>
    public bool AppendTruncationSuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets suffix added when captured body is truncated.
    /// </summary>
    public string TruncationSuffix { get; set; } = "...[TRUNCATED]";

    /// <summary>
    /// Gets or sets JSON properties that must be redacted from traced payloads.
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
    /// Gets or sets the replacement text for redacted values.
    /// </summary>
    public string RedactedValue { get; set; } = "***REDACTED***";

    /// <summary>
    /// Gets or sets the activity tag name used to store the captured request body.
    /// </summary>
    public string BodyTagName { get; set; } = "http.request.body";

    /// <summary>
    /// Gets or sets the activity tag name used to store the request body size.
    /// </summary>
    public string BodySizeTagName { get; set; } = "http.request.body_size";

    /// <summary>
    /// Gets or sets the activity tag name used to mark body truncation.
    /// </summary>
    public string TruncatedTagName { get; set; } = "http.request.body_truncated";

    /// <summary>
    /// Gets or sets the activity tag name used to store the redacted field count.
    /// </summary>
    public string RedactedFieldsTagName { get; set; } = "http.request.body_redacted_fields";
}
