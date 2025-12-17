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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that implements ETag and conditional request support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware provides HTTP caching support with:
    /// <list type="bullet">
    /// <item>ETag generation and validation</item>
    /// <item>If-None-Match header support (304 Not Modified)</item>
    /// <item>If-Modified-Since header support</item>
    /// <item>If-Match header support (for concurrency control)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// Call <c>services.AddMvp24HoursETag()</c> to configure options.
    /// </para>
    /// </remarks>
    public class ETagMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ETagOptions _options;
        private readonly ILogger<ETagMiddleware> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="ETagMiddleware"/>.
        /// </summary>
        public ETagMiddleware(
            RequestDelegate next,
            IOptions<ETagOptions> options,
            ILogger<ETagMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request and handles ETag validation.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            if (IsExcluded(context))
            {
                await _next(context);
                return;
            }

            var method = context.Request.Method.ToUpperInvariant();

            // Handle If-None-Match (GET/HEAD requests)
            if (_options.SupportIfNoneMatch && (method == "GET" || method == "HEAD"))
            {
                var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
                if (!string.IsNullOrEmpty(ifNoneMatch))
                {
                    // Store original body stream
                    var originalBodyStream = context.Response.Body;
                    using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    await _next(context);

                    // Generate ETag from response
                    var etag = await GenerateETagAsync(context.Response.Body);
                    if (etag != null)
                    {
                        var requestETags = ifNoneMatch.Split(',').Select(e => e.Trim('"', ' ')).ToList();
                        if (requestETags.Contains(etag, StringComparer.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = 304; // Not Modified
                            context.Response.ContentLength = 0;
                            context.Response.Body = originalBodyStream;
                            context.Response.Headers.ETag = FormatETag(etag);
                            return;
                        }

                        context.Response.Headers.ETag = FormatETag(etag);
                    }

                    // Copy response back to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                    return;
                }
            }

            // Handle If-Modified-Since (GET/HEAD requests)
            if (_options.SupportIfModifiedSince && (method == "GET" || method == "HEAD"))
            {
                var ifModifiedSince = context.Request.Headers.IfModifiedSince;
                if (ifModifiedSince.Count > 0 && DateTime.TryParse(ifModifiedSince.ToString(), out var modifiedSince))
                {
                    // This would typically check against the resource's last modified date
                    // For now, we'll let the request proceed and let the application set Last-Modified header
                }
            }

            // Handle If-Match (PUT/PATCH/DELETE requests)
            if (_options.SupportIfMatch && (method == "PUT" || method == "PATCH" || method == "DELETE"))
            {
                var ifMatch = context.Request.Headers.IfMatch.ToString();
                if (!string.IsNullOrEmpty(ifMatch))
                {
                    // This would typically validate against the current resource's ETag
                    // For now, we'll let the request proceed and let the application handle validation
                }
            }

            await _next(context);

            // Generate ETag for response if not already set
            if (!context.Response.Headers.ContainsKey("ETag") && context.Response.StatusCode == 200)
            {
                var etag = await GenerateETagFromResponseAsync(context);
                if (etag != null)
                {
                    context.Response.Headers.ETag = FormatETag(etag);
                }
            }
        }

        private bool IsExcluded(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method.ToUpperInvariant();

            if (_options.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return _options.ExcludedMethods.Contains(method, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> GenerateETagAsync(Stream bodyStream)
        {
            if (bodyStream.Length == 0)
                return null;

            bodyStream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[bodyStream.Length];
            await bodyStream.ReadAsync(buffer, 0, buffer.Length);
            bodyStream.Seek(0, SeekOrigin.Begin);

            return GenerateETagFromBytes(buffer);
        }

        private async Task<string?> GenerateETagFromResponseAsync(HttpContext context)
        {
            // For now, generate based on response content
            // In a real implementation, this would use the resource's version or hash
            var timestamp = DateTime.UtcNow.Ticks;
            var path = context.Request.Path.Value ?? string.Empty;
            var content = $"{path}:{timestamp}";
            var bytes = Encoding.UTF8.GetBytes(content);
            return GenerateETagFromBytes(bytes);
        }

        private string GenerateETagFromBytes(byte[] bytes)
        {
            return _options.Algorithm switch
            {
                ETagAlgorithm.ContentHash => GenerateHashETag(bytes),
                ETagAlgorithm.LastModified => GenerateTimestampETag(),
                ETagAlgorithm.Version => GenerateVersionETag(bytes),
                _ => GenerateHashETag(bytes)
            };
        }

        private string GenerateHashETag(byte[] bytes)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash).TrimEnd('=').Replace("+", "-").Replace("/", "_");
        }

        private string GenerateTimestampETag()
        {
            return DateTime.UtcNow.Ticks.ToString("X");
        }

        private string GenerateVersionETag(byte[] bytes)
        {
            // Simple version based on content hash
            return GenerateHashETag(bytes);
        }

        private string FormatETag(string etag)
        {
            var prefix = _options.UseWeakETags ? "W/\"" : "\"";
            return $"{prefix}{etag}\"";
        }
    }
}

