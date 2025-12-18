//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Application.Contract.Observability;

/// <summary>
/// Interface for collecting metrics about application service operations.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to integrate with your metrics backend (Prometheus, Application Insights, etc.).
/// </para>
/// <para>
/// <strong>Typical Metrics:</strong>
/// <list type="bullet">
/// <item>Operation count (total, success, failure)</item>
/// <item>Operation duration (histogram)</item>
/// <item>Active operations (gauge)</item>
/// </list>
/// </para>
/// </remarks>
public interface IOperationMetrics
{
    /// <summary>
    /// Records the start of an operation.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operationType">The operation type (Query/Command).</param>
    void RecordOperationStart(string serviceName, string operationName, string operationType);

    /// <summary>
    /// Records the successful completion of an operation.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operationType">The operation type (Query/Command).</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    void RecordOperationSuccess(string serviceName, string operationName, string operationType, long durationMs);

    /// <summary>
    /// Records the failure of an operation.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="operationType">The operation type (Query/Command).</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    /// <param name="exceptionType">The type of exception that occurred.</param>
    void RecordOperationFailure(string serviceName, string operationName, string operationType, long durationMs, string exceptionType);
}

/// <summary>
/// Configuration options for operation metrics collection.
/// </summary>
public class OperationMetricsOptions
{
    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include entity type in metric tags.
    /// Default is true.
    /// </summary>
    public bool IncludeEntityType { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include tenant ID in metric tags.
    /// Default is false (for cardinality reasons).
    /// </summary>
    public bool IncludeTenantId { get; set; } = false;

    /// <summary>
    /// Gets or sets the histogram buckets for duration metrics (in milliseconds).
    /// </summary>
    public double[] DurationBuckets { get; set; } = new[] { 5.0, 10.0, 25.0, 50.0, 100.0, 250.0, 500.0, 1000.0, 2500.0, 5000.0, 10000.0 };
}

