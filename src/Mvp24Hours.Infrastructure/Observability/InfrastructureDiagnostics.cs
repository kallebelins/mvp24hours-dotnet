//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Observability.Contract;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Observability
{
    /// <summary>
    /// Implementation of <see cref="IInfrastructureDiagnostics"/> that aggregates
    /// diagnostics from all Infrastructure subsystems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation collects diagnostics from registered subsystem diagnostics
    /// providers and aggregates them into a unified view. Each subsystem can register
    /// its own diagnostics provider via <see cref="ISubsystemDiagnosticsProvider"/>.
    /// </para>
    /// </remarks>
    public class InfrastructureDiagnostics : IInfrastructureDiagnostics
    {
        private readonly IEnumerable<ISubsystemDiagnosticsProvider> _providers;
        private readonly ILogger<InfrastructureDiagnostics> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureDiagnostics"/> class.
        /// </summary>
        /// <param name="providers">The subsystem diagnostics providers.</param>
        /// <param name="logger">The logger instance.</param>
        public InfrastructureDiagnostics(
            IEnumerable<ISubsystemDiagnosticsProvider> providers,
            ILogger<InfrastructureDiagnostics> logger)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, SubsystemHealth>> GetHealthStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var healthStatus = new Dictionary<string, SubsystemHealth>();

            foreach (var provider in _providers)
            {
                try
                {
                    var diagnostics = await provider.GetDiagnosticsAsync(cancellationToken);
                    healthStatus[diagnostics.SubsystemName] = diagnostics.Health;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get health status for subsystem {SubsystemName}", provider.SubsystemName);
                    healthStatus[provider.SubsystemName] = SubsystemHealth.Unhealthy;
                }
            }

            return healthStatus;
        }

        /// <inheritdoc/>
        public async Task<SubsystemDiagnostics?> GetSubsystemDiagnosticsAsync(
            string subsystemName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subsystemName))
            {
                throw new ArgumentException("Subsystem name cannot be null or empty.", nameof(subsystemName));
            }

            var provider = _providers.FirstOrDefault(p => 
                string.Equals(p.SubsystemName, subsystemName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                return null;
            }

            try
            {
                return await provider.GetDiagnosticsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get diagnostics for subsystem {SubsystemName}", subsystemName);
                return new SubsystemDiagnostics
                {
                    SubsystemName = subsystemName,
                    Health = SubsystemHealth.Unhealthy,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>> GetAggregatedMetricsAsync(
            CancellationToken cancellationToken = default)
        {
            var aggregatedMetrics = new Dictionary<string, object>();

            foreach (var provider in _providers)
            {
                try
                {
                    var metrics = await provider.GetMetricsAsync(cancellationToken);
                    foreach (var (key, value) in metrics)
                    {
                        var metricKey = $"{provider.SubsystemName}.{key}";
                        aggregatedMetrics[metricKey] = value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get metrics for subsystem {SubsystemName}", provider.SubsystemName);
                }
            }

            return aggregatedMetrics;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, IReadOnlyList<ErrorInfo>>> GetRecentErrorsAsync(
            int maxErrors = 10,
            CancellationToken cancellationToken = default)
        {
            var errors = new Dictionary<string, IReadOnlyList<ErrorInfo>>();

            foreach (var provider in _providers)
            {
                try
                {
                    var subsystemErrors = await provider.GetRecentErrorsAsync(maxErrors, cancellationToken);
                    if (subsystemErrors.Count > 0)
                    {
                        errors[provider.SubsystemName] = subsystemErrors;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get recent errors for subsystem {SubsystemName}", provider.SubsystemName);
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Interface for subsystem-specific diagnostics providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each Infrastructure subsystem should implement this interface to provide
    /// its own diagnostics. The diagnostics are then aggregated by <see cref="InfrastructureDiagnostics"/>.
    /// </para>
    /// </remarks>
    public interface ISubsystemDiagnosticsProvider
    {
        /// <summary>
        /// Gets the name of the subsystem (e.g., "Email", "Sms", "FileStorage").
        /// </summary>
        string SubsystemName { get; }

        /// <summary>
        /// Gets diagnostics for the subsystem.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Diagnostics for the subsystem.</returns>
        Task<SubsystemDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metrics for the subsystem.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary of metric names and their values.</returns>
        Task<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent errors for the subsystem.
        /// </summary>
        /// <param name="maxErrors">Maximum number of errors to return.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A list of recent errors.</returns>
        Task<IReadOnlyList<ErrorInfo>> GetRecentErrorsAsync(int maxErrors = 10, CancellationToken cancellationToken = default);
    }
}

