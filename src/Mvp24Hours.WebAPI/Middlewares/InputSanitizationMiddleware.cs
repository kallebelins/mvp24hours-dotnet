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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that sanitizes and validates input to protect against common attacks.
/// </summary>
/// <remarks>
/// <para>
/// This middleware provides protection against:
/// <list type="bullet">
/// <item>XSS (Cross-Site Scripting) attacks</item>
/// <item>SQL Injection patterns</item>
/// <item>Path Traversal attempts</item>
/// <item>Command Injection patterns</item>
/// <item>LDAP Injection patterns</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Call <c>services.AddMvp24HoursInputSanitization()</c> to configure options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs
/// builder.Services.AddMvp24HoursInputSanitization(options =>
/// {
///     options.Mode = SanitizationMode.Validate;
///     options.EnableXssSanitization = true;
/// });
/// 
/// var app = builder.Build();
/// app.UseMvp24HoursInputSanitization();
/// </code>
/// </example>
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InputSanitizationOptions _options;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    // Pre-compiled regex patterns for performance
    private static readonly Regex XssPattern = new(
        @"<script[^>]*>.*?</script>|javascript:|on\w+\s*=|<\s*\/?\s*(script|iframe|object|embed|applet|form|input|body|html|head|meta|link|style)[^>]*>|\%3c|\%3e",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(union|select|insert|update|delete|drop|truncate|alter|exec|execute|xp_|sp_|0x|char\(|nchar\(|varchar\()\b)|('|\""|;|--|\*|\/\*|\*\/|@@|@|\+\+|concat|benchmark|sleep|waitfor|delay)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathTraversalPattern = new(
        @"\.\.[\\/]|\.\.%2[fF]|%2[eE]%2[eE][\\/]|%252[eE]|%c0%ae|%c1%1c",
        RegexOptions.Compiled);

    private static readonly Regex CommandInjectionPattern = new(
        @"[;&|`$]|\$\(|\bsh\b|\bbash\b|\bcmd\b|\bpowershell\b|\bexec\b|\beval\b|%0[aAdD]|\r|\n",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LdapInjectionPattern = new(
        @"[)(\\|*]|%28|%29|%5c|%7c|%2a",
        RegexOptions.Compiled);

    /// <summary>
    /// Creates a new instance of <see cref="InputSanitizationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The input sanitization options.</param>
    /// <param name="logger">The logger.</param>
    public InputSanitizationMiddleware(
        RequestDelegate next,
        IOptions<InputSanitizationOptions> options,
        ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request with input sanitization.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if sanitization is enabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value ?? "/";
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        var detectedThreats = new List<ThreatDetection>();

        // Validate query strings
        if (_options.SanitizeQueryStrings)
        {
            var queryThreats = ValidateQueryString(context);
            detectedThreats.AddRange(queryThreats);
        }

        // Validate headers
        if (_options.SanitizeHeaders)
        {
            var headerThreats = ValidateHeaders(context);
            detectedThreats.AddRange(headerThreats);
        }

        // Validate request body for applicable content types
        if (_options.SanitizeRequestBody && HasSanitizableBody(context))
        {
            var bodyThreats = await ValidateRequestBody(context);
            detectedThreats.AddRange(bodyThreats);
        }

        // Handle detected threats
        if (detectedThreats.Count > 0)
        {
            if (_options.LogDetectedAttacks)
            {
                LogThreats(context, detectedThreats);
            }

            if (_options.Mode == SanitizationMode.Validate)
            {
                await HandleSuspiciousRequest(context, detectedThreats);
                return;
            }
            // In Sanitize mode, we'd modify the request (complex, not fully implemented here)
            // In LogOnly mode, we just continue
        }

        await _next(context);
    }

    private List<ThreatDetection> ValidateQueryString(HttpContext context)
    {
        var threats = new List<ThreatDetection>();

        foreach (var param in context.Request.Query)
        {
            if (_options.ExcludedQueryParameters.Contains(param.Key))
                continue;

            foreach (var value in param.Value)
            {
                if (string.IsNullOrEmpty(value) || value.Length > _options.MaxInputLengthToScan)
                    continue;

                var decodedValue = HttpUtility.UrlDecode(value);
                var paramThreats = DetectThreats(decodedValue, $"QueryString[{param.Key}]");
                threats.AddRange(paramThreats);
            }
        }

        return threats;
    }

    private List<ThreatDetection> ValidateHeaders(HttpContext context)
    {
        var threats = new List<ThreatDetection>();

        foreach (var header in context.Request.Headers)
        {
            if (_options.ExcludedHeaders.Contains(header.Key))
                continue;

            foreach (var value in header.Value)
            {
                if (string.IsNullOrEmpty(value) || value.Length > _options.MaxInputLengthToScan)
                    continue;

                var headerThreats = DetectThreats(value, $"Header[{header.Key}]");
                threats.AddRange(headerThreats);
            }
        }

        return threats;
    }

    private async Task<List<ThreatDetection>> ValidateRequestBody(HttpContext context)
    {
        var threats = new List<ThreatDetection>();

        // Enable buffering so we can read the body multiple times
        context.Request.EnableBuffering();

        try
        {
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            // Reset position for downstream handlers
            context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body) || body.Length > _options.MaxInputLengthToScan)
                return threats;

            // For JSON content, parse and validate each field
            var contentType = context.Request.ContentType?.Split(';').FirstOrDefault()?.Trim();
            if (contentType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    var jsonThreats = ValidateJsonBody(body);
                    threats.AddRange(jsonThreats);
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, validate as plain text
                    var bodyThreats = DetectThreats(body, "RequestBody");
                    threats.AddRange(bodyThreats);
                }
            }
            else
            {
                var bodyThreats = DetectThreats(body, "RequestBody");
                threats.AddRange(bodyThreats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate request body");
        }

        return threats;
    }

    private List<ThreatDetection> ValidateJsonBody(string json)
    {
        var threats = new List<ThreatDetection>();

        try
        {
            using var document = JsonDocument.Parse(json);
            ValidateJsonElement(document.RootElement, "$", threats);
        }
        catch (JsonException)
        {
            // Not valid JSON, skip
        }

        return threats;
    }

    private void ValidateJsonElement(JsonElement element, string path, List<ThreatDetection> threats)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = $"{path}.{property.Name}";

                    // Check if property is excluded
                    if (_options.ExcludedJsonProperties.Contains(property.Name))
                        continue;

                    ValidateJsonElement(property.Value, propertyPath, threats);
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ValidateJsonElement(item, $"{path}[{index}]", threats);
                    index++;
                }
                break;

            case JsonValueKind.String:
                var value = element.GetString();
                if (!string.IsNullOrEmpty(value) && value.Length <= _options.MaxInputLengthToScan)
                {
                    var elementThreats = DetectThreats(value, $"JSON{path}");
                    threats.AddRange(elementThreats);
                }
                break;
        }
    }

    private List<ThreatDetection> DetectThreats(string input, string location)
    {
        var threats = new List<ThreatDetection>();

        if (_options.EnableXssSanitization && XssPattern.IsMatch(input))
        {
            threats.Add(new ThreatDetection("XSS", location, input));
        }

        if (_options.EnableSqlInjectionDetection && SqlInjectionPattern.IsMatch(input))
        {
            threats.Add(new ThreatDetection("SQL Injection", location, input));
        }

        if (_options.EnablePathTraversalDetection && PathTraversalPattern.IsMatch(input))
        {
            threats.Add(new ThreatDetection("Path Traversal", location, input));
        }

        if (_options.EnableCommandInjectionDetection && CommandInjectionPattern.IsMatch(input))
        {
            threats.Add(new ThreatDetection("Command Injection", location, input));
        }

        if (_options.EnableLdapInjectionDetection && LdapInjectionPattern.IsMatch(input))
        {
            threats.Add(new ThreatDetection("LDAP Injection", location, input));
        }

        // Check custom patterns
        foreach (var pattern in _options.CustomPatterns)
        {
            if (Regex.IsMatch(input, pattern.Value, RegexOptions.IgnoreCase))
            {
                threats.Add(new ThreatDetection(pattern.Key, location, input));
            }
        }

        return threats;
    }

    private void LogThreats(HttpContext context, List<ThreatDetection> threats)
    {
        foreach (var threat in threats)
        {
            if (_options.IncludeSuspiciousInputInLog)
            {
                _logger.LogWarning(
                    "Potential {ThreatType} attack detected from {ClientIp} at {Location}: {Input}",
                    threat.Type,
                    context.Connection.RemoteIpAddress,
                    threat.Location,
                    threat.Input?.Substring(0, Math.Min(threat.Input.Length, 200)));
            }
            else
            {
                _logger.LogWarning(
                    "Potential {ThreatType} attack detected from {ClientIp} at {Location}",
                    threat.Type,
                    context.Connection.RemoteIpAddress,
                    threat.Location);
            }
        }
    }

    private bool HasSanitizableBody(HttpContext context)
    {
        var contentType = context.Request.ContentType?.Split(';').FirstOrDefault()?.Trim();
        return !string.IsNullOrEmpty(contentType) &&
               _options.SanitizableContentTypes.Contains(contentType) &&
               context.Request.ContentLength > 0;
    }

    private bool IsExcludedPath(string path)
    {
        return _options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern));
    }

    private async Task HandleSuspiciousRequest(HttpContext context, List<ThreatDetection> threats)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Bad Request",
            status = 400,
            detail = _options.RejectionMessage,
            instance = context.Request.Path.Value
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private class ThreatDetection
    {
        public string Type { get; }
        public string Location { get; }
        public string? Input { get; }

        public ThreatDetection(string type, string location, string? input)
        {
            Type = type;
            Location = location;
            Input = input;
        }
    }
}

