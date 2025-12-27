//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Centralized Meter definitions for all Mvp24Hours modules.
/// </summary>
/// <remarks>
/// <para>
/// Meters are used to create metrics (counters, histograms, gauges) for
/// monitoring performance and behavior across different modules. Each module
/// has its own meter with semantic naming conventions following OpenTelemetry standards.
/// </para>
/// <para>
/// <strong>Naming Convention:</strong>
/// Meter names follow the pattern: <c>Mvp24Hours.{Module}</c>
/// Metric names follow the pattern: <c>mvp24hours.{module}.{metric_name}</c>
/// This ensures proper categorization and filtering in observability platforms.
/// </para>
/// <para>
/// <strong>Usage with OpenTelemetry:</strong>
/// </para>
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(metrics =>
///     {
///         metrics
///             .AddMeter(Mvp24HoursMeters.Core.Name)
///             .AddMeter(Mvp24HoursMeters.Pipe.Name)
///             .AddMeter(Mvp24HoursMeters.Cqrs.Name)
///             // Or add all at once:
///             .AddMvp24HoursMeters()
///             .AddPrometheusExporter();
///     });
/// </code>
/// </remarks>
public static class Mvp24HoursMeters
{
    /// <summary>
    /// The version of all Meters.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// All meter names for bulk registration.
    /// </summary>
    public static readonly string[] AllMeterNames =
    [
        Core.Name,
        Pipe.Name,
        Cqrs.Name,
        Data.Name,
        RabbitMQ.Name,
        WebAPI.Name,
        Caching.Name,
        CronJob.Name,
        Infrastructure.Name
    ];

    /// <summary>
    /// Core module meter for fundamental operations.
    /// </summary>
    public static class Core
    {
        /// <summary>The name of the Core Meter.</summary>
        public const string Name = "Mvp24Hours.Core";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// Pipe module meter for pipeline operations.
    /// </summary>
    public static class Pipe
    {
        /// <summary>The name of the Pipe Meter.</summary>
        public const string Name = "Mvp24Hours.Pipe";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// CQRS module meter for mediator operations.
    /// </summary>
    public static class Cqrs
    {
        /// <summary>The name of the CQRS Meter.</summary>
        public const string Name = "Mvp24Hours.Cqrs";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// Data module meter for repository and database operations.
    /// </summary>
    public static class Data
    {
        /// <summary>The name of the Data Meter.</summary>
        public const string Name = "Mvp24Hours.Data";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// RabbitMQ module meter for messaging operations.
    /// </summary>
    public static class RabbitMQ
    {
        /// <summary>The name of the RabbitMQ Meter.</summary>
        public const string Name = "Mvp24Hours.RabbitMQ";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// WebAPI module meter for HTTP operations.
    /// </summary>
    public static class WebAPI
    {
        /// <summary>The name of the WebAPI Meter.</summary>
        public const string Name = "Mvp24Hours.WebAPI";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// Caching module meter for cache operations.
    /// </summary>
    public static class Caching
    {
        /// <summary>The name of the Caching Meter.</summary>
        public const string Name = "Mvp24Hours.Caching";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// CronJob module meter for scheduled job operations.
    /// </summary>
    public static class CronJob
    {
        /// <summary>The name of the CronJob Meter.</summary>
        public const string Name = "Mvp24Hours.CronJob";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }

    /// <summary>
    /// Infrastructure module meter for cross-cutting concerns.
    /// </summary>
    public static class Infrastructure
    {
        /// <summary>The name of the Infrastructure Meter.</summary>
        public const string Name = "Mvp24Hours.Infrastructure";

        /// <summary>The Meter instance.</summary>
        public static readonly Meter Meter = new(Name, Version);
    }
}

