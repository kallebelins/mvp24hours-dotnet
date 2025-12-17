//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Options for configuring ETag and conditional requests.
    /// </summary>
    public class ETagOptions
    {
        /// <summary>
        /// Gets or sets whether ETag support is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use weak ETags (W/"...").
        /// Weak ETags indicate that the resource is semantically equivalent but may differ in representation.
        /// </summary>
        public bool UseWeakETags { get; set; } = false;

        /// <summary>
        /// Gets or sets the algorithm used to generate ETags.
        /// </summary>
        public ETagAlgorithm Algorithm { get; set; } = ETagAlgorithm.ContentHash;

        /// <summary>
        /// Gets or sets whether to support If-None-Match header (for GET requests).
        /// </summary>
        public bool SupportIfNoneMatch { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to support If-Modified-Since header.
        /// </summary>
        public bool SupportIfModifiedSince { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to support If-Match header (for PUT/PATCH/DELETE requests).
        /// </summary>
        public bool SupportIfMatch { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of paths to exclude from ETag generation.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the list of HTTP methods to exclude from ETag generation.
        /// </summary>
        public HashSet<string> ExcludedMethods { get; set; } = new HashSet<string>
        {
            "HEAD",
            "OPTIONS"
        };
    }

    /// <summary>
    /// Specifies the algorithm used to generate ETags.
    /// </summary>
    public enum ETagAlgorithm
    {
        /// <summary>
        /// Generate ETag based on content hash (MD5, SHA256, etc.).
        /// </summary>
        ContentHash = 0,

        /// <summary>
        /// Generate ETag based on last modified timestamp.
        /// </summary>
        LastModified = 1,

        /// <summary>
        /// Generate ETag based on version number (for versioned resources).
        /// </summary>
        Version = 2
    }
}

