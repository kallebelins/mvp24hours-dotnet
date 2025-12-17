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
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Services;

/// <summary>
/// Default implementation of <see cref="IRequestLogger"/> that logs HTTP requests
/// and responses using structured logging with sensitive data masking.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides:
/// <list type="bullet">
/// <item>Structured logging compatible with any ILogger provider (Serilog, NLog, etc.)</item>
/// <item>Automatic masking of sensitive headers and body properties</item>
/// <item>Configurable logging levels (Basic, Standard, Detailed, Full)</item>
/// <item>Request/response body capture with size limits</item>
/// <item>Slow request detection and warning</item>
/// </list>
/// </para>
/// </remarks>
public class DefaultRequestLogger : IRequestLogger
{
    private readonly ILogger<DefaultRequestLogger> _logger;
    private readonly RequestLoggingOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultRequestLogger"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The logging options.</param>
    public DefaultRequestLogger(
        ILogger<DefaultRequestLogger> logger,
        IOptions<RequestLoggingOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task LogRequestAsync(HttpContext context)
    {
        if (_options.LoggingLevel == RequestLoggingLevel.None)
            return;

        var requestData = await BuildRequestLogDataAsync(context);

        _logger.LogInformation(
            "HTTP {Method} {Path} started | CorrelationId: {CorrelationId} | Client: {ClientIp} | User: {UserId}",
            requestData.Method,
            requestData.Path,
            requestData.CorrelationId,
            requestData.ClientIp ?? "unknown",
            requestData.UserId ?? "anonymous");

        if (_options.LoggingLevel >= RequestLoggingLevel.Standard && _options.LogRequestHeaders)
        {
            _logger.LogDebug(
                "Request Headers | CorrelationId: {CorrelationId} | Headers: {Headers}",
                requestData.CorrelationId,
                requestData.Headers);
        }

        if (_options.LoggingLevel >= RequestLoggingLevel.Detailed && _options.LogRequestBody && !string.IsNullOrEmpty(requestData.Body))
        {
            _logger.LogDebug(
                "Request Body | CorrelationId: {CorrelationId} | ContentType: {ContentType} | Body: {Body}",
                requestData.CorrelationId,
                requestData.ContentType,
                requestData.Body);
        }
    }

    /// <inheritdoc />
    public Task LogResponseAsync(HttpContext context, double durationMs)
    {
        if (_options.LoggingLevel == RequestLoggingLevel.None)
            return Task.CompletedTask;

        var responseData = BuildResponseLogData(context, durationMs);
        var logLevel = GetLogLevelForStatusCode(responseData.StatusCode);

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} completed | CorrelationId: {CorrelationId} | Status: {StatusCode} | Duration: {DurationMs:F2}ms | Success: {IsSuccess}",
            responseData.Method,
            responseData.Path,
            responseData.CorrelationId,
            responseData.StatusCode,
            responseData.DurationMs,
            responseData.IsSuccess);

        if (_options.LoggingLevel >= RequestLoggingLevel.Standard && _options.LogResponseHeaders)
        {
            _logger.LogDebug(
                "Response Headers | CorrelationId: {CorrelationId} | Headers: {Headers}",
                responseData.CorrelationId,
                responseData.Headers);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogExceptionAsync(HttpContext context, Exception exception, double durationMs)
    {
        var exceptionData = BuildExceptionLogData(context, exception, durationMs);

        _logger.LogError(
            exception,
            "HTTP {Method} {Path} failed | CorrelationId: {CorrelationId} | Status: {StatusCode} | Duration: {DurationMs:F2}ms | Exception: {ExceptionType} | Message: {ExceptionMessage}",
            exceptionData.Method,
            exceptionData.Path,
            exceptionData.CorrelationId,
            exceptionData.StatusCode,
            exceptionData.DurationMs,
            exceptionData.ExceptionType,
            exceptionData.ExceptionMessage);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogSlowRequestAsync(HttpContext context, double durationMs, int thresholdMs)
    {
        var correlationId = context.TraceIdentifier;
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        _logger.LogWarning(
            "SLOW REQUEST | HTTP {Method} {Path} | CorrelationId: {CorrelationId} | Duration: {DurationMs:F2}ms | Threshold: {ThresholdMs}ms",
            method,
            path,
            correlationId,
            durationMs,
            thresholdMs);

        return Task.CompletedTask;
    }

    #region Private Methods

    private async Task<RequestLogData> BuildRequestLogDataAsync(HttpContext context)
    {
        var request = context.Request;
        var correlationId = context.TraceIdentifier;

        string? body = null;
        if (_options.LogRequestBody && ShouldLogBody(request.ContentType))
        {
            body = await ReadRequestBodyAsync(request);
            body = MaskSensitiveData(body);
        }

        return new RequestLogData
        {
            CorrelationId = correlationId,
            Method = request.Method,
            Path = request.Path.Value ?? "/",
            QueryString = MaskQueryString(request.QueryString.Value),
            Scheme = request.Scheme,
            Host = request.Host.Value,
            ClientIp = GetClientIp(context),
            UserAgent = request.Headers.UserAgent.FirstOrDefault(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            Headers = _options.LogRequestHeaders ? MaskHeaders(request.Headers) : null,
            Body = body,
            UserId = GetUserId(context),
            TenantId = GetTenantId(context),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private ResponseLogData BuildResponseLogData(HttpContext context, double durationMs)
    {
        var response = context.Response;
        var correlationId = context.TraceIdentifier;
        var statusCode = response.StatusCode;

        return new ResponseLogData
        {
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "/",
            StatusCode = statusCode,
            DurationMs = durationMs,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            Headers = _options.LogResponseHeaders ? MaskHeaders(response.Headers) : null,
            Timestamp = DateTimeOffset.UtcNow,
            IsSuccess = statusCode >= 200 && statusCode < 300,
            IsSlow = durationMs > _options.SlowRequestThresholdMs
        };
    }

    private ExceptionLogData BuildExceptionLogData(HttpContext context, Exception exception, double durationMs)
    {
        var correlationId = context.TraceIdentifier;

        return new ExceptionLogData
        {
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "/",
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = _options.IncludeExceptionDetails ? exception.StackTrace : null,
            StatusCode = context.Response.StatusCode,
            DurationMs = durationMs,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (body.Length > _options.MaxRequestBodySize)
        {
            body = body[.._options.MaxRequestBodySize] + "...[TRUNCATED]";
        }

        return body;
    }

    private string MaskHeaders(IHeaderDictionary headers)
    {
        var maskedHeaders = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            if (_options.SensitiveHeaders.Contains(header.Key))
            {
                maskedHeaders[header.Key] = _options.MaskValue;
            }
            else
            {
                maskedHeaders[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }

        return JsonSerializer.Serialize(maskedHeaders);
    }

    private string? MaskQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return null;

        if (!_options.LogQueryString)
            return "[REDACTED]";

        var result = queryString;

        foreach (var param in _options.SensitiveQueryParameters)
        {
            var pattern = $@"({param}=)([^&]*)";
            result = Regex.Replace(result, pattern, $"$1{_options.MaskValue}", RegexOptions.IgnoreCase);
        }

        return result;
    }

    private string? MaskSensitiveData(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        if (_options.LoggingLevel == RequestLoggingLevel.Full)
            return content;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var maskedJson = MaskJsonElement(doc.RootElement);
            return maskedJson;
        }
        catch
        {
            // Not valid JSON, return as-is (might mask using regex for other formats)
            return content;
        }
    }

    private string MaskJsonElement(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        MaskJsonElementRecursive(element, writer, null);

        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void MaskJsonElementRecursive(JsonElement element, Utf8JsonWriter writer, string? propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    MaskJsonElementRecursive(prop.Value, writer, prop.Name);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    MaskJsonElementRecursive(item, writer, null);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                if (propertyName != null && _options.SensitiveProperties.Contains(propertyName))
                {
                    writer.WriteStringValue(_options.MaskValue);
                }
                else
                {
                    writer.WriteStringValue(element.GetString());
                }
                break;

            case JsonValueKind.Number:
                if (propertyName != null && _options.SensitiveProperties.Contains(propertyName))
                {
                    writer.WriteStringValue(_options.MaskValue);
                }
                else
                {
                    writer.WriteRawValue(element.GetRawText());
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                writer.WriteBooleanValue(element.GetBoolean());
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }

    private bool ShouldLogBody(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return _options.LoggableContentTypes.Any(ct =>
            contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetClientIp(HttpContext context)
    {
        // Check for forwarded headers (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.Identity.Name;
    }

    private static string? GetTenantId(HttpContext context)
    {
        // Check header first
        var tenantHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantHeader))
            return tenantHeader;

        // Check claims
        return context.User?.FindFirstValue("tenant_id")
            ?? context.User?.FindFirstValue("tid");
    }

    private static LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }

    #endregion
}

