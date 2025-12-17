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
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that decompresses request bodies based on Content-Encoding header.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware automatically decompresses request bodies that are compressed
    /// with Gzip, Deflate, or Brotli encoding. It supports:
    /// <list type="bullet">
    /// <item>Gzip compression</item>
    /// <item>Deflate compression</item>
    /// <item>Brotli compression</item>
    /// <item>Configurable size limits</item>
    /// <item>Path exclusions</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Prerequisites:</strong>
    /// Call <c>services.AddMvp24HoursRequestDecompression()</c> to configure options.
    /// </para>
    /// <para>
    /// <strong>Pipeline Position:</strong>
    /// Should be added early in the pipeline, before body parsing middleware.
    /// </para>
    /// </remarks>
    public class RequestDecompressionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestDecompressionOptions _options;
        private readonly ILogger<RequestDecompressionMiddleware> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="RequestDecompressionMiddleware"/>.
        /// </summary>
        public RequestDecompressionMiddleware(
            RequestDelegate next,
            IOptions<RequestDecompressionOptions> options,
            ILogger<RequestDecompressionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the HTTP request and decompresses the body if needed.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            if (IsExcludedPath(context))
            {
                await _next(context);
                return;
            }

            var contentEncoding = context.Request.Headers.ContentEncoding.ToString();
            if (string.IsNullOrEmpty(contentEncoding))
            {
                await _next(context);
                return;
            }

            var encoding = contentEncoding.Split(',').FirstOrDefault()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(encoding) || !_options.SupportedEncodings.Contains(encoding))
            {
                await _next(context);
                return;
            }

            if (context.Request.ContentLength > _options.MaxRequestBodySize)
            {
                _logger.LogWarning(
                    "Request body size {Size} exceeds maximum allowed size {MaxSize}",
                    context.Request.ContentLength,
                    _options.MaxRequestBodySize);
                await _next(context);
                return;
            }

            var originalBodyStream = context.Request.Body;
            try
            {
                using var decompressedStream = CreateDecompressionStream(originalBodyStream, encoding);
                if (decompressedStream == null)
                {
                    await _next(context);
                    return;
                }

                context.Request.Body = decompressedStream;
                await _next(context);
            }
            finally
            {
                context.Request.Body = originalBodyStream;
            }
        }

        private bool IsExcludedPath(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return _options.ExcludedPaths.Any(excluded =>
                path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
        }

        private Stream? CreateDecompressionStream(Stream stream, string encoding)
        {
            return encoding switch
            {
                "gzip" => new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true),
                "deflate" => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true),
                "br" => new BrotliStream(stream, CompressionMode.Decompress, leaveOpen: true),
                _ => null
            };
        }
    }
}

