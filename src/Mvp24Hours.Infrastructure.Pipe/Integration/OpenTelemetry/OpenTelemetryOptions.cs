//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.OpenTelemetry
{
    /// <summary>
    /// Options for OpenTelemetry integration with pipelines.
    /// </summary>
    public class OpenTelemetryOptions
    {
        /// <summary>
        /// Gets or sets whether to use the full type name for operation spans.
        /// Default is false (uses short type name).
        /// </summary>
        public bool UseFullTypeName { get; set; }

        /// <summary>
        /// Gets or sets whether to include input details in span tags.
        /// Default is true.
        /// </summary>
        public bool IncludeInputDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include message details in span tags.
        /// Default is true.
        /// </summary>
        public bool IncludeMessageDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to record exceptions as span events.
        /// Default is true.
        /// </summary>
        public bool RecordExceptions { get; set; } = true;

        /// <summary>
        /// Gets or sets custom tags to add to all spans.
        /// </summary>
        public Dictionary<string, object>? CustomTags { get; set; }

        /// <summary>
        /// Gets or sets the service name for spans.
        /// Default is "Mvp24Hours.Pipeline".
        /// </summary>
        public string ServiceName { get; set; } = "Mvp24Hours.Pipeline";

        /// <summary>
        /// Gets or sets the service version for spans.
        /// </summary>
        public string? ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets whether to create child spans for nested operations.
        /// Default is true.
        /// </summary>
        public bool CreateChildSpans { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum duration in milliseconds for a span to be recorded.
        /// Spans shorter than this will be dropped.
        /// Default is 0 (record all spans).
        /// </summary>
        public int MinimumDurationMs { get; set; }
    }
}

