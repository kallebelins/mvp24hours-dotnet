//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Options for configuring request/response compression.
    /// </summary>
    public class CompressionOptions
    {
        /// <summary>
        /// Gets or sets whether compression is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether compression is enabled for HTTPS connections.
        /// </summary>
        public bool EnableForHttps { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of MIME types that should be compressed.
        /// </summary>
        public HashSet<string> MimeTypes { get; set; } = new HashSet<string>
        {
            "application/json",
            "application/xml",
            "text/json",
            "text/xml",
            "text/plain",
            "text/css",
            "text/javascript",
            "application/javascript",
            "application/x-javascript"
        };

        /// <summary>
        /// Gets or sets the minimum response size (in bytes) to compress.
        /// Responses smaller than this will not be compressed.
        /// </summary>
        public int MinimumCompressionSize { get; set; } = 1024; // 1KB

        /// <summary>
        /// Gets or sets the compression level (0-9, where 9 is maximum compression).
        /// </summary>
        public int CompressionLevel { get; set; } = 6;

        /// <summary>
        /// Gets or sets whether to use Brotli compression (preferred over Gzip when available).
        /// </summary>
        public bool UseBrotli { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use Gzip compression as fallback.
        /// </summary>
        public bool UseGzip { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of paths to exclude from compression.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();
    }
}

