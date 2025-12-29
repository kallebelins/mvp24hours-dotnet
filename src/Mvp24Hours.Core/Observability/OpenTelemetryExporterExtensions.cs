//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for configuring OpenTelemetry exporters with Mvp24Hours observability.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration helpers for common OpenTelemetry exporters:
/// </para>
/// <list type="bullet">
/// <item><strong>OTLP Exporter</strong>: Export to OTLP-compatible backends (Jaeger, Tempo, Grafana, etc.)</item>
/// <item><strong>Console Exporter</strong>: Export to console for development/debugging</item>
/// <item><strong>Prometheus Exporter</strong>: Export metrics in Prometheus format</item>
/// </list>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Configure Mvp24Hours observability with OTLP exporter
/// services.AddMvp24HoursObservability(options =>
/// {
///     options.ServiceName = "MyService";
///     options.ServiceVersion = "1.0.0";
/// });
/// 
/// // Add OpenTelemetry with Mvp24Hours sources and exporters
/// services.AddMvp24HoursOpenTelemetry(options =>
/// {
///     options.ServiceName = "MyService";
///     options.ServiceVersion = "1.0.0";
///     
///     // Configure OTLP exporter
///     options.Otlp.Endpoint = "http://localhost:4317";
///     options.Otlp.Protocol = OtlpExportProtocol.Grpc;
///     
///     // Configure Prometheus for metrics
///     options.Prometheus.ScrapeEndpoint = "/metrics";
///     
///     // Enable Console for development
///     options.Console.Enabled = true; // Only in Development
/// });
/// </code>
/// </remarks>
public static class OpenTelemetryExporterExtensions
{
    /// <summary>
    /// Adds Mvp24Hours OpenTelemetry configuration with exporter options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for exporter options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="OpenTelemetryExporterOptions"/> in the DI container,
    /// which can then be used to configure OpenTelemetry exporters.
    /// </para>
    /// <para>
    /// <strong>Example with OpenTelemetry SDK:</strong>
    /// </para>
    /// <code>
    /// // First, add Mvp24Hours configuration
    /// services.AddMvp24HoursOpenTelemetry(opts =>
    /// {
    ///     opts.ServiceName = "OrderService";
    ///     opts.Otlp.Endpoint = "http://jaeger:4317";
    ///     opts.Prometheus.Enabled = true;
    /// });
    /// 
    /// // Then configure OpenTelemetry SDK
    /// var exporterOptions = services.BuildServiceProvider()
    ///     .GetRequiredService&lt;OpenTelemetryExporterOptions&gt;();
    /// 
    /// services.AddOpenTelemetry()
    ///     .ConfigureResource(r => r.AddService(
    ///         exporterOptions.ServiceName,
    ///         serviceVersion: exporterOptions.ServiceVersion))
    ///     .WithTracing(builder =>
    ///     {
    ///         builder.AddSource(Mvp24HoursActivitySources.AllSourceNames);
    ///         
    ///         if (exporterOptions.Otlp.Enabled)
    ///             builder.AddOtlpExporter(o =>
    ///             {
    ///                 o.Endpoint = new Uri(exporterOptions.Otlp.Endpoint);
    ///                 o.Protocol = exporterOptions.Otlp.Protocol == OtlpExportProtocol.Grpc
    ///                     ? OtlpExportProtocol.Grpc
    ///                     : OtlpExportProtocol.HttpProtobuf;
    ///             });
    ///         
    ///         if (exporterOptions.Console.Enabled)
    ///             builder.AddConsoleExporter();
    ///     })
    ///     .WithMetrics(builder =>
    ///     {
    ///         builder.AddMeter(Mvp24HoursMeters.AllMeterNames);
    ///         
    ///         if (exporterOptions.Prometheus.Enabled)
    ///             builder.AddPrometheusExporter();
    ///         
    ///         if (exporterOptions.Otlp.Enabled)
    ///             builder.AddOtlpExporter();
    ///     });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursOpenTelemetry(
        this IServiceCollection services,
        Action<OpenTelemetryExporterOptions>? configure = null)
    {
        var options = new OpenTelemetryExporterOptions();
        configure?.Invoke(options);

        // Register exporter options
        services.AddSingleton(options);
        services.AddSingleton(options.Otlp);
        services.AddSingleton(options.Console);
        services.AddSingleton(options.Prometheus);

        // Also configure base observability if not already done
        services.AddMvp24HoursObservability(obs =>
        {
            obs.ServiceName = options.ServiceName;
            obs.ServiceVersion = options.ServiceVersion;
            obs.Environment = options.Environment;
        });

        return services;
    }

    /// <summary>
    /// Gets the recommended OpenTelemetry configuration for development environments.
    /// </summary>
    /// <returns>Configured options for development.</returns>
    /// <remarks>
    /// <para>
    /// Development configuration includes:
    /// </para>
    /// <list type="bullet">
    /// <item>Console exporter enabled for immediate visibility</item>
    /// <item>OTLP exporter to localhost:4317 (default Jaeger/OTEL Collector port)</item>
    /// <item>Verbose logging enabled</item>
    /// </list>
    /// </remarks>
    public static OpenTelemetryExporterOptions GetDevelopmentDefaults(string serviceName)
    {
        return new OpenTelemetryExporterOptions
        {
            ServiceName = serviceName,
            ServiceVersion = "1.0.0-dev",
            Environment = "Development",
            Console = new ConsoleExporterOptions
            {
                Enabled = true,
                EnableTimestamps = true,
                EnableTracing = true,
                EnableMetrics = false // Avoid too much output
            },
            Otlp = new OtlpExporterOptions
            {
                Enabled = true,
                Endpoint = "http://localhost:4317",
                Protocol = OtlpExportProtocol.Grpc,
                EnableTracing = true,
                EnableMetrics = true,
                EnableLogging = true
            },
            Prometheus = new PrometheusExporterOptions
            {
                Enabled = true,
                ScrapeEndpoint = "/metrics"
            }
        };
    }

    /// <summary>
    /// Gets the recommended OpenTelemetry configuration for production environments.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="otlpEndpoint">The OTLP collector endpoint.</param>
    /// <returns>Configured options for production.</returns>
    /// <remarks>
    /// <para>
    /// Production configuration includes:
    /// </para>
    /// <list type="bullet">
    /// <item>Console exporter disabled</item>
    /// <item>OTLP exporter enabled with the provided endpoint</item>
    /// <item>Prometheus metrics enabled</item>
    /// <item>Batch export for better performance</item>
    /// </list>
    /// </remarks>
    public static OpenTelemetryExporterOptions GetProductionDefaults(
        string serviceName,
        string otlpEndpoint)
    {
        return new OpenTelemetryExporterOptions
        {
            ServiceName = serviceName,
            ServiceVersion = GetAssemblyVersion(),
            Environment = "Production",
            Console = new ConsoleExporterOptions
            {
                Enabled = false
            },
            Otlp = new OtlpExporterOptions
            {
                Enabled = true,
                Endpoint = otlpEndpoint,
                Protocol = OtlpExportProtocol.Grpc,
                EnableTracing = true,
                EnableMetrics = true,
                EnableLogging = true,
                BatchExportScheduledDelayMs = 5000,
                MaxExportBatchSize = 512
            },
            Prometheus = new PrometheusExporterOptions
            {
                Enabled = true,
                ScrapeEndpoint = "/metrics",
                ScrapeResponseCacheDurationMs = 5000
            }
        };
    }

    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() 
               ?? "1.0.0";
    }
}

/// <summary>
/// Unified options for OpenTelemetry exporter configuration with Mvp24Hours.
/// </summary>
public class OpenTelemetryExporterOptions
{
    /// <summary>
    /// Gets or sets the service name for telemetry resource.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for telemetry resource.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment (Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service namespace for telemetry resource.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Gets or sets the service instance ID.
    /// </summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>
    /// Gets or sets OTLP exporter configuration.
    /// </summary>
    public OtlpExporterOptions Otlp { get; set; } = new();

    /// <summary>
    /// Gets or sets Console exporter configuration.
    /// </summary>
    public ConsoleExporterOptions Console { get; set; } = new();

    /// <summary>
    /// Gets or sets Prometheus exporter configuration.
    /// </summary>
    public PrometheusExporterOptions Prometheus { get; set; } = new();

    /// <summary>
    /// Gets or sets additional resource attributes.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}

/// <summary>
/// Configuration options for OTLP (OpenTelemetry Protocol) exporter.
/// </summary>
/// <remarks>
/// <para>
/// OTLP is the native protocol for OpenTelemetry and is supported by many backends:
/// </para>
/// <list type="bullet">
/// <item><strong>Jaeger</strong>: gRPC on port 4317, HTTP on port 4318</item>
/// <item><strong>Grafana Tempo</strong>: gRPC on port 4317</item>
/// <item><strong>Azure Monitor</strong>: HTTP with specific endpoint</item>
/// <item><strong>Datadog</strong>: HTTP with specific endpoint</item>
/// <item><strong>OTEL Collector</strong>: gRPC on port 4317, HTTP on port 4318</item>
/// </list>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Configure OTLP for Jaeger
/// options.Otlp.Endpoint = "http://jaeger:4317";
/// options.Otlp.Protocol = OtlpExportProtocol.Grpc;
/// 
/// // Configure OTLP for Azure Monitor
/// options.Otlp.Endpoint = "https://....azure.monitor.com/...";
/// options.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
/// options.Otlp.Headers["Authorization"] = "Bearer token...";
/// </code>
/// </remarks>
public class OtlpExporterOptions
{
    /// <summary>
    /// Gets or sets whether the OTLP exporter is enabled.
    /// Default is <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// Default is <c>"http://localhost:4317"</c> (standard gRPC port).
    /// </summary>
    /// <remarks>
    /// Common endpoints:
    /// <list type="bullet">
    /// <item>gRPC: <c>http://localhost:4317</c></item>
    /// <item>HTTP/protobuf: <c>http://localhost:4318</c></item>
    /// </list>
    /// </remarks>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the export protocol (gRPC or HTTP/protobuf).
    /// Default is <see cref="OtlpExportProtocol.Grpc"/>.
    /// </summary>
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

    /// <summary>
    /// Gets or sets whether to export traces via OTLP.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to export metrics via OTLP.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to export logs via OTLP.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for export operations.
    /// Default is <c>10000</c> (10 seconds).
    /// </summary>
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets custom headers to include in OTLP requests.
    /// </summary>
    /// <remarks>
    /// Use this for authentication headers when connecting to cloud backends:
    /// <code>
    /// options.Otlp.Headers["Authorization"] = "Bearer my-token";
    /// options.Otlp.Headers["x-api-key"] = "my-api-key";
    /// </code>
    /// </remarks>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the delay in milliseconds between batch exports.
    /// Default is <c>5000</c> (5 seconds).
    /// </summary>
    public int BatchExportScheduledDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the maximum batch size for export.
    /// Default is <c>512</c>.
    /// </summary>
    public int MaxExportBatchSize { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum queue size for pending exports.
    /// Default is <c>2048</c>.
    /// </summary>
    public int MaxQueueSize { get; set; } = 2048;
}

/// <summary>
/// OTLP export protocol options.
/// </summary>
public enum OtlpExportProtocol
{
    /// <summary>
    /// gRPC protocol (default, port 4317).
    /// </summary>
    Grpc = 0,

    /// <summary>
    /// HTTP/protobuf protocol (port 4318).
    /// </summary>
    HttpProtobuf = 1
}

/// <summary>
/// Configuration options for Console exporter.
/// </summary>
/// <remarks>
/// <para>
/// The Console exporter is primarily used for development and debugging.
/// It outputs telemetry data to the console/stdout.
/// </para>
/// <para>
/// <strong>Note:</strong> Do not enable in production as it can impact performance
/// and generate excessive output.
/// </para>
/// </remarks>
public class ConsoleExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Console exporter is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to export traces to console.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to export metrics to console.
    /// Default is <c>false</c> (can be verbose).
    /// </summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to export logs to console.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include timestamps in output.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use colored output.
    /// Default is <c>true</c>.
    /// </summary>
    public bool UseColors { get; set; } = true;
}

/// <summary>
/// Configuration options for Prometheus exporter.
/// </summary>
/// <remarks>
/// <para>
/// The Prometheus exporter exposes metrics in Prometheus format via an HTTP endpoint.
/// Prometheus can then scrape this endpoint to collect metrics.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // In Startup.cs or Program.cs
/// services.AddMvp24HoursOpenTelemetry(opts =>
/// {
///     opts.Prometheus.Enabled = true;
///     opts.Prometheus.ScrapeEndpoint = "/metrics";
/// });
/// 
/// // In middleware configuration
/// app.UseOpenTelemetryPrometheusScrapingEndpoint();
/// </code>
/// <para>
/// <strong>Prometheus configuration (prometheus.yml):</strong>
/// </para>
/// <code>
/// scrape_configs:
///   - job_name: 'my-service'
///     scrape_interval: 15s
///     static_configs:
///       - targets: ['my-service:8080']
///     metrics_path: '/metrics'
/// </code>
/// </remarks>
public class PrometheusExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Prometheus exporter is enabled.
    /// Default is <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the scrape endpoint path.
    /// Default is <c>"/metrics"</c>.
    /// </summary>
    public string ScrapeEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the cache duration in milliseconds for scrape responses.
    /// This helps reduce CPU usage when Prometheus scrapes frequently.
    /// Default is <c>5000</c> (5 seconds).
    /// </summary>
    public int ScrapeResponseCacheDurationMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to include exemplars in histogram metrics.
    /// Exemplars link metrics to traces for enhanced observability.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableExemplars { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require authentication for the scrape endpoint.
    /// Default is <c>false</c>.
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;
}

/// <summary>
/// Extension methods for OpenTelemetry builder integration with Mvp24Hours.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide simplified methods for adding Mvp24Hours sources and meters
/// to OpenTelemetry builders.
/// </para>
/// <para>
/// <strong>Complete Example:</strong>
/// </para>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// // 1. Add Mvp24Hours exporter configuration
/// builder.Services.AddMvp24HoursOpenTelemetry(opts =>
/// {
///     opts.ServiceName = "OrderService";
///     opts.ServiceVersion = "1.0.0";
///     opts.Otlp.Endpoint = builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317";
///     opts.Prometheus.Enabled = true;
///     opts.Console.Enabled = builder.Environment.IsDevelopment();
/// });
/// 
/// // 2. Configure OpenTelemetry SDK
/// builder.Services.AddOpenTelemetry()
///     .ConfigureResource(resource => resource
///         .AddService("OrderService", serviceVersion: "1.0.0"))
///     .WithTracing(tracing => tracing
///         .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
///         .AddAspNetCoreInstrumentation()
///         .AddHttpClientInstrumentation()
///         .AddOtlpExporter())
///     .WithMetrics(metrics => metrics
///         .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
///         .AddAspNetCoreInstrumentation()
///         .AddHttpClientInstrumentation()
///         .AddPrometheusExporter());
/// 
/// // 3. Configure logging
/// builder.Logging.AddOpenTelemetry(logging =>
/// {
///     logging.IncludeScopes = true;
///     logging.AddOtlpExporter();
/// });
/// 
/// var app = builder.Build();
/// 
/// // 4. Add Prometheus scrape endpoint
/// app.UseOpenTelemetryPrometheusScrapingEndpoint();
/// 
/// app.Run();
/// </code>
/// </remarks>
public static class Mvp24HoursOpenTelemetryIntegrationExtensions
{
    /// <summary>
    /// Gets a formatted headers string for OTLP exporter configuration.
    /// </summary>
    /// <param name="options">The OTLP exporter options.</param>
    /// <returns>Headers formatted as "key1=value1,key2=value2".</returns>
    public static string GetFormattedHeaders(this OtlpExporterOptions options)
    {
        if (options.Headers == null || options.Headers.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var header in options.Headers)
        {
            parts.Add($"{header.Key}={header.Value}");
        }
        return string.Join(",", parts);
    }

    /// <summary>
    /// Creates resource attributes dictionary for OpenTelemetry resource configuration.
    /// </summary>
    /// <param name="options">The exporter options.</param>
    /// <returns>Dictionary of resource attributes.</returns>
    public static Dictionary<string, object> GetResourceAttributes(this OpenTelemetryExporterOptions options)
    {
        var attributes = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(options.ServiceName))
            attributes["service.name"] = options.ServiceName;

        if (!string.IsNullOrEmpty(options.ServiceVersion))
            attributes["service.version"] = options.ServiceVersion;

        if (!string.IsNullOrEmpty(options.Environment))
            attributes["deployment.environment"] = options.Environment;

        if (!string.IsNullOrEmpty(options.ServiceNamespace))
            attributes["service.namespace"] = options.ServiceNamespace;

        if (!string.IsNullOrEmpty(options.ServiceInstanceId))
            attributes["service.instance.id"] = options.ServiceInstanceId;

        // Add custom attributes
        foreach (var attr in options.ResourceAttributes)
        {
            attributes[attr.Key] = attr.Value;
        }

        // Add framework info
        attributes["telemetry.sdk.name"] = "Mvp24Hours";
        attributes["telemetry.sdk.language"] = "dotnet";

        return attributes;
    }

    /// <summary>
    /// Validates the exporter options and throws if invalid.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public static void Validate(this OpenTelemetryExporterOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new ArgumentException("ServiceName is required for OpenTelemetry configuration.", nameof(options));

        if (options.Otlp.Enabled && string.IsNullOrWhiteSpace(options.Otlp.Endpoint))
            throw new ArgumentException("OTLP Endpoint is required when OTLP exporter is enabled.", nameof(options));

        if (options.Otlp.Enabled && !Uri.TryCreate(options.Otlp.Endpoint, UriKind.Absolute, out _))
            throw new ArgumentException($"OTLP Endpoint '{options.Otlp.Endpoint}' is not a valid URI.", nameof(options));

        if (options.Prometheus.Enabled && string.IsNullOrWhiteSpace(options.Prometheus.ScrapeEndpoint))
            throw new ArgumentException("Prometheus ScrapeEndpoint is required when Prometheus exporter is enabled.", nameof(options));
    }
}


