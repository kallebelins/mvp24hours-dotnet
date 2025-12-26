//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Observability.Contract
{
    /// <summary>
    /// Interface for aggregated diagnostics across all Infrastructure subsystems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a unified view of the health and status of all
    /// infrastructure subsystems, including HTTP clients, email, SMS, file storage,
    /// distributed locking, and background jobs.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Health check endpoints</item>
    /// <item>Diagnostic dashboards</item>
    /// <item>Alerting and monitoring</item>
    /// <item>Troubleshooting and debugging</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IInfrastructureDiagnostics
    {
        /// <summary>
        /// Gets the overall health status of all infrastructure subsystems.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary of subsystem names and their health status.</returns>
        /// <remarks>
        /// <para>
        /// Each subsystem reports its health status as:
        /// - <c>Healthy</c>: All operations are functioning normally
        /// - <c>Degraded</c>: Some operations may be slow or experiencing issues
        /// - <c>Unhealthy</c>: Critical operations are failing
        /// </para>
        /// </remarks>
        Task<Dictionary<string, SubsystemHealth>> GetHealthStatusAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed diagnostics for a specific subsystem.
        /// </summary>
        /// <param name="subsystemName">The name of the subsystem (e.g., "Email", "Sms", "FileStorage").</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Detailed diagnostics for the subsystem, or null if the subsystem is not found.</returns>
        Task<SubsystemDiagnostics?> GetSubsystemDiagnosticsAsync(
            string subsystemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated metrics across all subsystems.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary of metric names and their values.</returns>
        /// <remarks>
        /// <para>
        /// Metrics include:
        /// - Request/operation counts
        /// - Success/failure rates
        /// - Average latencies
        /// - Error rates
        /// - Resource utilization
        /// </para>
        /// </remarks>
        Task<Dictionary<string, object>> GetAggregatedMetricsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a summary of recent errors across all subsystems.
        /// </summary>
        /// <param name="maxErrors">Maximum number of errors to return per subsystem.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary of subsystem names and their recent errors.</returns>
        Task<Dictionary<string, IReadOnlyList<ErrorInfo>>> GetRecentErrorsAsync(
            int maxErrors = 10,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Health status of a subsystem.
    /// </summary>
    public enum SubsystemHealth
    {
        /// <summary>
        /// All operations are functioning normally.
        /// </summary>
        Healthy,

        /// <summary>
        /// Some operations may be slow or experiencing issues, but core functionality is available.
        /// </summary>
        Degraded,

        /// <summary>
        /// Critical operations are failing or the subsystem is unavailable.
        /// </summary>
        Unhealthy
    }

    /// <summary>
    /// Detailed diagnostics for a subsystem.
    /// </summary>
    public class SubsystemDiagnostics
    {
        /// <summary>
        /// Gets or sets the subsystem name.
        /// </summary>
        public string SubsystemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the health status.
        /// </summary>
        public SubsystemHealth Health { get; set; }

        /// <summary>
        /// Gets or sets the provider/implementation name (e.g., "SendGrid", "AzureBlobStorage").
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the configuration status (e.g., "Configured", "Misconfigured").
        /// </summary>
        public string? ConfigurationStatus { get; set; }

        /// <summary>
        /// Gets or sets the last successful operation timestamp.
        /// </summary>
        public DateTimeOffset? LastSuccess { get; set; }

        /// <summary>
        /// Gets or sets the last failed operation timestamp.
        /// </summary>
        public DateTimeOffset? LastFailure { get; set; }

        /// <summary>
        /// Gets or sets the error message if the subsystem is unhealthy.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the subsystem.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Information about an error that occurred in a subsystem.
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error type/exception name.
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// Gets or sets the operation that failed.
        /// </summary>
        public string? Operation { get; set; }

        /// <summary>
        /// Gets or sets additional context about the error.
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();
    }
}

