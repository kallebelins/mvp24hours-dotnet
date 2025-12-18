//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Observability;

namespace Mvp24Hours.Application.Logic.Observability;

/// <summary>
/// Default implementation of <see cref="IOperationMetrics"/> using System.Diagnostics.Metrics.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses the new .NET Metrics API which integrates with OpenTelemetry.
/// Metrics are exported automatically when OpenTelemetry is configured.
/// </para>
/// <para>
/// <strong>Metrics Exposed:</strong>
/// <list type="bullet">
/// <item>mvp24hours.application.operations.total - Counter of total operations</item>
/// <item>mvp24hours.application.operations.duration - Histogram of operation durations</item>
/// <item>mvp24hours.application.operations.active - Gauge of currently active operations</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry to collect Mvp24Hours metrics
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(builder =>
///     {
///         builder
///             .AddMeter(ApplicationOperationMetrics.MeterName)
///             .AddPrometheusExporter();
///     });
/// </code>
/// </example>
public sealed class ApplicationOperationMetrics : IOperationMetrics, IDisposable
{
    /// <summary>
    /// The name of the meter for Mvp24Hours Application operations.
    /// </summary>
    public const string MeterName = "Mvp24Hours.Application";

    private readonly Meter _meter;
    private readonly Counter<long> _operationsTotal;
    private readonly Counter<long> _operationsSuccess;
    private readonly Counter<long> _operationsFailure;
    private readonly Histogram<double> _operationDuration;
    private readonly UpDownCounter<long> _operationsActive;
    private readonly ILogger<ApplicationOperationMetrics>? _logger;
    private readonly OperationMetricsOptions _options;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the metrics collector.
    /// </summary>
    /// <param name="options">Metrics configuration options.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public ApplicationOperationMetrics(
        OperationMetricsOptions? options = null,
        ILogger<ApplicationOperationMetrics>? logger = null)
    {
        _options = options ?? new OperationMetricsOptions();
        _logger = logger;

        _meter = new Meter(MeterName, "1.0.0");

        _operationsTotal = _meter.CreateCounter<long>(
            "mvp24hours.application.operations.total",
            unit: "{operation}",
            description: "Total number of application service operations");

        _operationsSuccess = _meter.CreateCounter<long>(
            "mvp24hours.application.operations.success",
            unit: "{operation}",
            description: "Number of successful application service operations");

        _operationsFailure = _meter.CreateCounter<long>(
            "mvp24hours.application.operations.failure",
            unit: "{operation}",
            description: "Number of failed application service operations");

        _operationDuration = _meter.CreateHistogram<double>(
            "mvp24hours.application.operations.duration",
            unit: "ms",
            description: "Duration of application service operations in milliseconds");

        _operationsActive = _meter.CreateUpDownCounter<long>(
            "mvp24hours.application.operations.active",
            unit: "{operation}",
            description: "Number of currently active operations");
    }

    /// <inheritdoc />
    public void RecordOperationStart(string serviceName, string operationName, string operationType)
    {
        if (!_options.Enabled)
            return;

        var tags = CreateTags(serviceName, operationName, operationType);
        _operationsTotal.Add(1, tags);
        _operationsActive.Add(1, tags);

        _logger?.LogTrace(
            "Operation started: {ServiceName}.{OperationName} ({OperationType})",
            serviceName, operationName, operationType);
    }

    /// <inheritdoc />
    public void RecordOperationSuccess(string serviceName, string operationName, string operationType, long durationMs)
    {
        if (!_options.Enabled)
            return;

        var tags = CreateTags(serviceName, operationName, operationType);
        _operationsSuccess.Add(1, tags);
        _operationsActive.Add(-1, tags);
        _operationDuration.Record(durationMs, tags);

        _logger?.LogTrace(
            "Operation succeeded: {ServiceName}.{OperationName} ({OperationType}) in {DurationMs}ms",
            serviceName, operationName, operationType, durationMs);
    }

    /// <inheritdoc />
    public void RecordOperationFailure(string serviceName, string operationName, string operationType, long durationMs, string exceptionType)
    {
        if (!_options.Enabled)
            return;

        var tags = CreateTagsWithError(serviceName, operationName, operationType, exceptionType);
        _operationsFailure.Add(1, tags);
        _operationsActive.Add(-1, CreateTags(serviceName, operationName, operationType));
        _operationDuration.Record(durationMs, tags);

        _logger?.LogTrace(
            "Operation failed: {ServiceName}.{OperationName} ({OperationType}) in {DurationMs}ms with {ExceptionType}",
            serviceName, operationName, operationType, durationMs, exceptionType);
    }

    private static KeyValuePair<string, object?>[] CreateTags(string serviceName, string operationName, string operationType)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("service.name", serviceName),
            new("operation.name", operationName),
            new("operation.type", operationType)
        };
    }

    private static KeyValuePair<string, object?>[] CreateTagsWithError(string serviceName, string operationName, string operationType, string errorType)
    {
        return new KeyValuePair<string, object?>[]
        {
            new("service.name", serviceName),
            new("operation.name", operationName),
            new("operation.type", operationType),
            new("error.type", errorType)
        };
    }

    /// <summary>
    /// Disposes the meter and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _meter.Dispose();
    }
}

/// <summary>
/// No-operation implementation of <see cref="IOperationMetrics"/> when metrics are disabled.
/// </summary>
public sealed class NullOperationMetrics : IOperationMetrics
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullOperationMetrics Instance = new();

    private NullOperationMetrics() { }

    /// <inheritdoc />
    public void RecordOperationStart(string serviceName, string operationName, string operationType) { }

    /// <inheritdoc />
    public void RecordOperationSuccess(string serviceName, string operationName, string operationType, long durationMs) { }

    /// <inheritdoc />
    public void RecordOperationFailure(string serviceName, string operationName, string operationType, long durationMs, string exceptionType) { }
}

