//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.Middlewares;

/// <summary>
/// Middleware that captures request bodies into Activity tags with redaction support.
/// </summary>
public class RequestBodyTracingMiddleware(
    RequestDelegate next,
    IOptions<RequestBodyTracingOptions> options)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly RequestBodyTracingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Processes the HTTP request and stores the redacted body in the current activity.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        Activity? activity = Activity.Current;
        if (activity == null && !_options.TraceWithoutActivity)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        (string body, bool truncated, int redactedFields) = await CaptureBodyAsync(context.Request, context.RequestAborted);

        if (!string.IsNullOrWhiteSpace(body))
        {
            activity?.SetTag(_options.BodyTagName, body);
            activity?.SetTag(_options.BodySizeTagName, context.Request.ContentLength ?? body.Length);
            activity?.SetTag(_options.TruncatedTagName, truncated);

            if (redactedFields > 0)
            {
                activity?.SetTag(_options.RedactedFieldsTagName, redactedFields);
            }
        }

        await _next(context);
    }

    private bool ShouldSkip(HttpContext context)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        if (!_options.TracedMethods.Contains(context.Request.Method))
        {
            return true;
        }

        string path = context.Request.Path.Value ?? "/";
        if (_options.ExcludedPaths.Any(pattern => MatchesPattern(path, pattern)))
        {
            return true;
        }

        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value == 0)
        {
            return true;
        }

        return !IsTracedContentType(context.Request.ContentType);
    }

    private bool IsTracedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        string normalized = contentType.Split(';')[0].Trim();
        return _options.TracedContentTypes.Any(pattern => MatchesPattern(normalized, pattern));
    }

    private async Task<(string Body, bool Truncated, int RedactedFields)> CaptureBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        int maxBodySize = Math.Max(1, _options.MaxBodySizeBytes);
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            using var capture = new MemoryStream(capacity: Math.Min(maxBodySize, 4096));
            int totalBytesRead = 0;
            bool truncated = false;

            while (true)
            {
                int remainingToCapture = maxBodySize - totalBytesRead;
                int bytesToRead = Math.Min(rentedBuffer.Length, remainingToCapture + 1);

                if (bytesToRead <= 0)
                {
                    truncated = true;
                    break;
                }

                int read = await request.Body.ReadAsync(rentedBuffer.AsMemory(0, bytesToRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                int bytesToWrite = read;
                if (read > remainingToCapture)
                {
                    bytesToWrite = remainingToCapture;
                    truncated = true;
                }

                if (bytesToWrite > 0)
                {
                    await capture.WriteAsync(rentedBuffer.AsMemory(0, bytesToWrite), cancellationToken);
                    totalBytesRead += bytesToWrite;
                }

                if (truncated)
                {
                    break;
                }
            }

            request.Body.Position = 0;

            string body = Encoding.UTF8.GetString(capture.ToArray());
            int redactedFields = 0;

            if (IsJsonContentType(request.ContentType))
            {
                body = RedactJsonPayload(body, out redactedFields);
            }

            if (truncated && _options.AppendTruncationSuffix)
            {
                body += _options.TruncationSuffix;
            }

            return (body, truncated, redactedFields);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        string normalized = contentType.Split(';')[0].Trim();
        return normalized.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("text/json", StringComparison.OrdinalIgnoreCase);
    }

    private string RedactJsonPayload(string payload, out int redactedFields)
    {
        redactedFields = 0;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return payload;
        }

        try
        {
            var root = JsonNode.Parse(payload);
            if (root == null)
            {
                return payload;
            }

            RedactNode(root, ref redactedFields);
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return payload;
        }
    }

    private void RedactNode(JsonNode node, ref int redactedFields)
    {
        if (node is JsonObject jsonObject)
        {
            foreach ((string key, JsonNode? value) in jsonObject.ToArray())
            {
                if (_options.SensitiveProperties.Contains(key))
                {
                    jsonObject[key] = _options.RedactedValue;
                    redactedFields++;
                    continue;
                }

                if (value != null)
                {
                    RedactNode(value, ref redactedFields);
                }
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? item in jsonArray)
            {
                if (item != null)
                {
                    RedactNode(item, ref redactedFields);
                }
            }
        }
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        if (string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }
}
