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
    /// Configuration options for Health Checks endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class configures the health check endpoints and their behavior:
    /// <list type="bullet">
    /// <item><strong>/health</strong> - Overall health status (all checks)</item>
    /// <item><strong>/health/ready</strong> - Readiness probe (checks with "ready" tag)</item>
    /// <item><strong>/health/live</strong> - Liveness probe (checks with "live" tag)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class HealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the path for the overall health check endpoint.
        /// Default is "/health".
        /// </summary>
        public string HealthPath { get; set; } = "/health";

        /// <summary>
        /// Gets or sets the path for the readiness probe endpoint.
        /// Default is "/health/ready".
        /// </summary>
        public string ReadinessPath { get; set; } = "/health/ready";

        /// <summary>
        /// Gets or sets the path for the liveness probe endpoint.
        /// Default is "/health/live".
        /// </summary>
        public string LivenessPath { get; set; } = "/health/live";

        /// <summary>
        /// Gets or sets whether to enable detailed health check responses.
        /// When enabled, includes detailed information about each health check.
        /// Default is false.
        /// </summary>
        public bool EnableDetailedResponses { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include exception details in health check responses.
        /// Should only be enabled in development environments.
        /// Default is false.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout for health check execution.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable Health Check UI.
        /// Requires AspNetCore.HealthChecks.UI package.
        /// Default is false.
        /// </summary>
        public bool EnableUI { get; set; } = false;

        /// <summary>
        /// Gets or sets the path for the Health Check UI endpoint.
        /// Default is "/health-ui".
        /// </summary>
        public string UIPath { get; set; } = "/health-ui";

        /// <summary>
        /// Gets or sets tags to include in the overall health check.
        /// If empty, all health checks are included.
        /// </summary>
        public HashSet<string> HealthTags { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets tags to include in the readiness check.
        /// Default includes "ready" tag.
        /// </summary>
        public HashSet<string> ReadinessTags { get; set; } = new HashSet<string> { "ready" };

        /// <summary>
        /// Gets or sets tags to include in the liveness check.
        /// Default includes "live" tag.
        /// </summary>
        public HashSet<string> LivenessTags { get; set; } = new HashSet<string> { "live" };

        /// <summary>
        /// Gets or sets whether to allow anonymous access to health check endpoints.
        /// Default is true (health checks are typically public).
        /// </summary>
        public bool AllowAnonymous { get; set; } = true;
    }
}

