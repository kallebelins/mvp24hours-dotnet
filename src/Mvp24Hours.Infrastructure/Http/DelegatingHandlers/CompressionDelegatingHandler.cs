//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.DelegatingHandlers
{
    /// <summary>
    /// Delegating handler that compresses outgoing request bodies using Gzip or Brotli.
    /// Useful for large payloads to reduce bandwidth and improve performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler automatically compresses request bodies before sending and adds
    /// the appropriate Content-Encoding header. It supports Gzip and Brotli compression.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Response decompression is typically handled by the
    /// HttpClientHandler's AutomaticDecompression property. This handler focuses
    /// on compressing outgoing request bodies.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Gzip and Brotli compression support</item>
    /// <item>Configurable compression level</item>
    /// <item>Minimum size threshold to avoid compressing small payloads</item>
    /// <item>Automatic Content-Encoding header</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTransient(sp => new CompressionDelegatingHandler(
    ///     sp.GetRequiredService&lt;ILogger&lt;CompressionDelegatingHandler&gt;&gt;(),
    ///     new CompressionHandlerOptions
    ///     {
    ///         Algorithm = CompressionAlgorithm.Gzip,
    ///         CompressionLevel = CompressionLevel.Fastest,
    ///         MinimumSizeBytes = 1024
    ///     }));
    /// 
    /// services.AddHttpClient("MyApi")
    ///     .AddHttpMessageHandler&lt;CompressionDelegatingHandler&gt;();
    /// </code>
    /// </example>
    public class CompressionDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<CompressionDelegatingHandler> _logger;
        private readonly CompressionHandlerOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CompressionDelegatingHandler(ILogger<CompressionDelegatingHandler> logger)
            : this(logger, new CompressionHandlerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionDelegatingHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The compression options.</param>
        public CompressionDelegatingHandler(
            ILogger<CompressionDelegatingHandler> logger,
            CompressionHandlerOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new CompressionHandlerOptions();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Only compress if enabled and request has content
            if (_options.Enabled && request.Content != null)
            {
                await CompressRequestContentAsync(request, cancellationToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task CompressRequestContentAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Check if content is already compressed
            if (request.Content!.Headers.ContentEncoding.Count > 0)
            {
                _logger.LogDebug("Request content already has Content-Encoding, skipping compression");
                return;
            }

            // Read original content
            var originalContent = await request.Content.ReadAsByteArrayAsync(cancellationToken);

            // Skip compression for small payloads
            if (originalContent.Length < _options.MinimumSizeBytes)
            {
                _logger.LogDebug(
                    "Request content size ({Size} bytes) is below minimum threshold ({Threshold} bytes), skipping compression",
                    originalContent.Length, _options.MinimumSizeBytes);
                return;
            }

            // Compress the content
            var compressedContent = await CompressAsync(originalContent, cancellationToken);

            // Check if compression actually reduced size
            if (compressedContent.Length >= originalContent.Length)
            {
                _logger.LogDebug(
                    "Compressed size ({CompressedSize} bytes) >= original size ({OriginalSize} bytes), skipping compression",
                    compressedContent.Length, originalContent.Length);
                return;
            }

            _logger.LogDebug(
                "Compressed request content from {OriginalSize} to {CompressedSize} bytes ({Ratio:P1} reduction) using {Algorithm}",
                originalContent.Length,
                compressedContent.Length,
                1 - (double)compressedContent.Length / originalContent.Length,
                _options.Algorithm);

            // Create new content with compressed data
            var newContent = new ByteArrayContent(compressedContent);

            // Copy original content headers
            foreach (var header in request.Content.Headers)
            {
                if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Set content encoding header
            newContent.Headers.ContentEncoding.Add(GetContentEncodingValue());

            // Replace the content
            request.Content.Dispose();
            request.Content = newContent;
        }

        private async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken)
        {
            using var outputStream = new MemoryStream();

            await using (var compressionStream = CreateCompressionStream(outputStream))
            {
                await compressionStream.WriteAsync(data, cancellationToken);
            }

            return outputStream.ToArray();
        }

        private Stream CreateCompressionStream(Stream outputStream)
        {
            return _options.Algorithm switch
            {
                CompressionAlgorithm.Gzip => new GZipStream(outputStream, _options.CompressionLevel, leaveOpen: true),
                CompressionAlgorithm.Brotli => new BrotliStream(outputStream, _options.CompressionLevel, leaveOpen: true),
                CompressionAlgorithm.Deflate => new DeflateStream(outputStream, _options.CompressionLevel, leaveOpen: true),
                _ => new GZipStream(outputStream, _options.CompressionLevel, leaveOpen: true)
            };
        }

        private string GetContentEncodingValue()
        {
            return _options.Algorithm switch
            {
                CompressionAlgorithm.Gzip => "gzip",
                CompressionAlgorithm.Brotli => "br",
                CompressionAlgorithm.Deflate => "deflate",
                _ => "gzip"
            };
        }
    }

    /// <summary>
    /// Defines the compression algorithm to use.
    /// </summary>
    public enum CompressionAlgorithm
    {
        /// <summary>
        /// Gzip compression (widely supported).
        /// </summary>
        Gzip = 0,

        /// <summary>
        /// Brotli compression (better ratio, newer).
        /// </summary>
        Brotli = 1,

        /// <summary>
        /// Deflate compression.
        /// </summary>
        Deflate = 2
    }

    /// <summary>
    /// Configuration options for request compression.
    /// </summary>
    public class CompressionHandlerOptions
    {
        /// <summary>
        /// Gets or sets whether compression is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the compression algorithm. Default is Gzip.
        /// </summary>
        public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.Gzip;

        /// <summary>
        /// Gets or sets the compression level. Default is Fastest.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;

        /// <summary>
        /// Gets or sets the minimum content size in bytes to trigger compression.
        /// Content smaller than this will not be compressed. Default is 1024 (1KB).
        /// </summary>
        public int MinimumSizeBytes { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the content types that should be compressed.
        /// If empty, all content types are compressed.
        /// </summary>
        public string[] CompressibleContentTypes { get; set; } = new[]
        {
            "application/json",
            "application/xml",
            "text/plain",
            "text/html",
            "text/xml",
            "text/json"
        };
    }

    /// <summary>
    /// Extension methods for compression configuration.
    /// </summary>
    public static class CompressionExtensions
    {
        /// <summary>
        /// Marks a request to skip compression.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>The request message for chaining.</returns>
        public static HttpRequestMessage SkipCompression(this HttpRequestMessage request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

#if NET5_0_OR_GREATER
            request.Options.Set(new HttpRequestOptionsKey<bool>("Mvp24Hours.SkipCompression"), true);
#else
            request.Properties["Mvp24Hours.SkipCompression"] = true;
#endif
            return request;
        }
    }
}

