//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Observability
{
    /// <summary>
    /// Activity sources for OpenTelemetry tracing across Infrastructure module subsystems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Activity sources are used to create distributed tracing spans for operations
    /// across different subsystems. Each subsystem has its own activity source with
    /// semantic naming conventions following OpenTelemetry standards.
    /// </para>
    /// <para>
    /// <strong>Naming Convention:</strong>
    /// Activity source names follow the pattern: <c>Mvp24Hours.Infrastructure.{Subsystem}</c>
    /// This ensures proper categorization and filtering in observability platforms.
    /// </para>
    /// </remarks>
    public static class ActivitySources
    {
        /// <summary>
        /// Activity source for HTTP client operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing outgoing HTTP requests, including retries, circuit breakers,
        /// and response handling.
        /// </remarks>
        public static readonly ActivitySource Http = new(
            "Mvp24Hours.Infrastructure.Http",
            "1.0.0");

        /// <summary>
        /// Activity source for email service operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing email sending, template rendering, bulk operations, and delivery tracking.
        /// </remarks>
        public static readonly ActivitySource Email = new(
            "Mvp24Hours.Infrastructure.Email",
            "1.0.0");

        /// <summary>
        /// Activity source for SMS service operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing SMS sending, MMS operations, delivery reports, and rate limiting.
        /// </remarks>
        public static readonly ActivitySource Sms = new(
            "Mvp24Hours.Infrastructure.Sms",
            "1.0.0");

        /// <summary>
        /// Activity source for file storage operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing file uploads, downloads, deletions, metadata operations,
        /// and streaming operations across different storage providers.
        /// </remarks>
        public static readonly ActivitySource FileStorage = new(
            "Mvp24Hours.Infrastructure.FileStorage",
            "1.0.0");

        /// <summary>
        /// Activity source for distributed locking operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing lock acquisition, release, renewal, and contention scenarios.
        /// </remarks>
        public static readonly ActivitySource DistributedLocking = new(
            "Mvp24Hours.Infrastructure.DistributedLocking",
            "1.0.0");

        /// <summary>
        /// Activity source for background job operations.
        /// </summary>
        /// <remarks>
        /// Used for tracing job scheduling, execution, retries, continuations, and batch operations.
        /// </remarks>
        public static readonly ActivitySource BackgroundJobs = new(
            "Mvp24Hours.Infrastructure.BackgroundJobs",
            "1.0.0");

        /// <summary>
        /// Activity source for resilience operations (retry, circuit breaker, bulkhead).
        /// </summary>
        /// <remarks>
        /// Used for tracing retry attempts, circuit breaker state changes, and bulkhead rejections.
        /// </remarks>
        public static readonly ActivitySource Resilience = new(
            "Mvp24Hours.Infrastructure.Resilience",
            "1.0.0");
    }
}

