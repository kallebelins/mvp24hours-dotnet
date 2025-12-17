//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Options for configuring request timeouts.
    /// </summary>
    public class RequestTimeoutOptions
    {
        /// <summary>
        /// Gets or sets whether request timeout is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default request timeout.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the dictionary of endpoint-specific timeouts.
        /// Key is the endpoint pattern (e.g., "/api/users/*"), value is the timeout.
        /// </summary>
        public Dictionary<string, TimeSpan> EndpointTimeouts { get; set; } = new Dictionary<string, TimeSpan>();

        /// <summary>
        /// Gets or sets the dictionary of HTTP method-specific timeouts.
        /// </summary>
        public Dictionary<string, TimeSpan> MethodTimeouts { get; set; } = new Dictionary<string, TimeSpan>();

        /// <summary>
        /// Gets or sets the list of paths to exclude from timeout enforcement.
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets whether to send a timeout response with Retry-After header.
        /// </summary>
        public bool SendRetryAfter { get; set; } = false;
    }
}

