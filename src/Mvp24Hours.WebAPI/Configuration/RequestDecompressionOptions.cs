//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Options for configuring request decompression.
    /// </summary>
    public class RequestDecompressionOptions
    {
        /// <summary>
        /// Gets or sets whether request decompression is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum request body size (in bytes) to decompress.
        /// Requests larger than this will not be decompressed.
        /// </summary>
        public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Gets or sets the list of content encodings supported for decompression.
        /// </summary>
        public HashSet<string> SupportedEncodings { get; set; } = new HashSet<string>
        {
            "gzip",
            "deflate",
            "br" // Brotli
        };

        /// <summary>
        /// Gets or sets the list of paths to exclude from decompression.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();
    }
}

