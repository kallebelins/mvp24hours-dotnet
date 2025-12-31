//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Aspire;

/// <summary>
/// Configuration options for .NET Aspire integration.
/// </summary>
/// <remarks>
/// <para>
/// .NET Aspire 9 is the official stack for building observable, cloud-native applications.
/// It provides built-in telemetry, health checks, resilience, and service discovery.
/// </para>
/// <para>
/// These options configure how Mvp24Hours integrates with Aspire's service defaults.
/// </para>
/// </remarks>
public class AspireOptions
{
    /// <summary>
    /// Gets or sets the service name for telemetry.
    /// If not specified, uses the entry assembly name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for telemetry.
    /// If not specified, uses the entry assembly version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment (e.g., Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OpenTelemetry integration.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable resilience/retry policies.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableResilience { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable service discovery.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableServiceDiscovery { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint for telemetry export.
    /// If not specified, uses the OTEL_EXPORTER_OTLP_ENDPOINT environment variable.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the telemetry configuration options.
    /// </summary>
    public AspireTelemetryOptions Telemetry { get; set; } = new();

    /// <summary>
    /// Gets or sets the health check configuration options.
    /// </summary>
    public AspireHealthCheckOptions HealthChecks { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience configuration options.
    /// </summary>
    public AspireResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Gets or sets custom resource attributes for OpenTelemetry.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}

/// <summary>
/// Telemetry-specific options for Aspire integration.
/// </summary>
public class AspireTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether to enable logging export via OTLP.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable tracing (distributed traces).
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable ASP.NET Core instrumentation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable HttpClient instrumentation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Entity Framework Core instrumentation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableEfCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable Mvp24Hours custom instrumentation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMvp24HoursInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets the sampling ratio for tracing (0.0 to 1.0).
    /// Default is 1.0 (sample all).
    /// </summary>
    public double TraceSamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets additional ActivitySource names to include in tracing.
    /// </summary>
    public List<string> AdditionalActivitySources { get; set; } = new();

    /// <summary>
    /// Gets or sets additional Meter names to include in metrics.
    /// </summary>
    public List<string> AdditionalMeterNames { get; set; } = new();
}

/// <summary>
/// Health check options for Aspire integration.
/// </summary>
public class AspireHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the liveness endpoint path.
    /// Default is "/health/live".
    /// </summary>
    public string LivenessPath { get; set; } = "/health/live";

    /// <summary>
    /// Gets or sets the readiness endpoint path.
    /// Default is "/health/ready".
    /// </summary>
    public string ReadinessPath { get; set; } = "/health/ready";

    /// <summary>
    /// Gets or sets the startup endpoint path.
    /// Default is "/health/startup".
    /// </summary>
    public string StartupPath { get; set; } = "/health/startup";

    /// <summary>
    /// Gets or sets whether to enable database health checks.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableDatabaseHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable cache health checks (Redis).
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCacheHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable messaging health checks (RabbitMQ).
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableMessagingHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the health check timeout in seconds.
    /// Default is 5 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}

/// <summary>
/// Resilience options for Aspire integration.
/// </summary>
public class AspireResilienceOptions
{
    /// <summary>
    /// Gets or sets whether to enable retry policies for HTTP clients.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable circuit breaker for HTTP clients.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable timeout policies.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableTimeout { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold (number of failures before opening).
    /// Default is 5.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker break duration in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

