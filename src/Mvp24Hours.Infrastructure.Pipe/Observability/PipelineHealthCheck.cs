//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Health check implementation that monitors pipeline execution health.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check monitors:
    /// <list type="bullet">
    /// <item>Pipeline success rate against configurable threshold</item>
    /// <item>Operation success rate</item>
    /// <item>Average operation duration against threshold</item>
    /// <item>Active execution count</item>
    /// <item>Recent failure information</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class PipelineHealthCheck : IHealthCheck
    {
        private readonly IPipelineMetrics _metrics;
        private readonly PipelineHealthCheckOptions _options;

        /// <summary>
        /// Creates a new instance of PipelineHealthCheck.
        /// </summary>
        /// <param name="metrics">The pipeline metrics provider.</param>
        /// <param name="options">Optional health check configuration.</param>
        public PipelineHealthCheck(
            IPipelineMetrics metrics,
            PipelineHealthCheckOptions? options = null)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _options = options ?? new PipelineHealthCheckOptions();
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var snapshot = _metrics.GetSnapshot();

            var data = new Dictionary<string, object>
            {
                ["TotalPipelineExecutions"] = snapshot.TotalPipelineExecutions,
                ["SuccessfulPipelineExecutions"] = snapshot.SuccessfulPipelineExecutions,
                ["FailedPipelineExecutions"] = snapshot.FailedPipelineExecutions,
                ["PipelineSuccessRate"] = snapshot.PipelineSuccessRate,
                ["TotalOperationsExecuted"] = snapshot.TotalOperationsExecuted,
                ["OperationSuccessRate"] = snapshot.OperationSuccessRate,
                ["AveragePipelineDurationMs"] = snapshot.AveragePipelineDurationMs,
                ["AverageOperationDurationMs"] = snapshot.AverageOperationDurationMs,
                ["TotalMemoryAllocated"] = snapshot.TotalMemoryAllocated,
                ["CapturedAt"] = snapshot.CapturedAt
            };

            // Check if we have enough data to evaluate
            if (snapshot.TotalPipelineExecutions < _options.MinimumExecutionsForEvaluation)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Insufficient data for evaluation. Total executions: {snapshot.TotalPipelineExecutions}",
                    data));
            }

            var issues = new List<string>();

            // Check pipeline success rate
            if (snapshot.PipelineSuccessRate < _options.MinimumSuccessRate)
            {
                issues.Add($"Pipeline success rate ({snapshot.PipelineSuccessRate:P2}) is below threshold ({_options.MinimumSuccessRate:P2})");
            }

            // Check operation success rate
            if (snapshot.OperationSuccessRate < _options.MinimumOperationSuccessRate)
            {
                issues.Add($"Operation success rate ({snapshot.OperationSuccessRate:P2}) is below threshold ({_options.MinimumOperationSuccessRate:P2})");
            }

            // Check average duration
            if (_options.MaxAverageDurationMs.HasValue &&
                snapshot.AveragePipelineDurationMs > _options.MaxAverageDurationMs.Value)
            {
                issues.Add($"Average pipeline duration ({snapshot.AveragePipelineDurationMs:F2}ms) exceeds threshold ({_options.MaxAverageDurationMs.Value}ms)");
            }

            // Check individual pipeline health
            foreach (var pipelineKvp in snapshot.PipelineMetrics)
            {
                var pipelineMetrics = pipelineKvp.Value;

                // Check for pipelines with high active executions (potential stuck pipelines)
                if (_options.MaxActiveExecutions.HasValue &&
                    pipelineMetrics.ActiveExecutions > _options.MaxActiveExecutions.Value)
                {
                    issues.Add($"Pipeline '{pipelineKvp.Key}' has {pipelineMetrics.ActiveExecutions} active executions (threshold: {_options.MaxActiveExecutions.Value})");
                }

                // Check individual pipeline success rate
                if (pipelineMetrics.TotalExecutions >= _options.MinimumExecutionsForEvaluation &&
                    pipelineMetrics.SuccessRate < _options.MinimumSuccessRate)
                {
                    issues.Add($"Pipeline '{pipelineKvp.Key}' success rate ({pipelineMetrics.SuccessRate:P2}) is below threshold");
                }
            }

            // Check individual operation health
            foreach (var opKvp in snapshot.OperationMetrics)
            {
                var opMetrics = opKvp.Value;

                if (opMetrics.TotalExecutions >= _options.MinimumExecutionsForEvaluation &&
                    opMetrics.SuccessRate < _options.MinimumOperationSuccessRate)
                {
                    issues.Add($"Operation '{opKvp.Key}' success rate ({opMetrics.SuccessRate:P2}) is below threshold");

                    if (!string.IsNullOrEmpty(opMetrics.LastExceptionType))
                    {
                        data[$"LastException_{opKvp.Key}"] = $"{opMetrics.LastExceptionType}: {opMetrics.LastExceptionMessage}";
                    }
                }

                // Check P95 latency
                if (_options.MaxP95DurationMs.HasValue &&
                    opMetrics.P95DurationMs > _options.MaxP95DurationMs.Value)
                {
                    issues.Add($"Operation '{opKvp.Key}' P95 latency ({opMetrics.P95DurationMs:F2}ms) exceeds threshold ({_options.MaxP95DurationMs.Value}ms)");
                }
            }

            // Determine health status
            if (issues.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Pipeline health is good. Success rate: {snapshot.PipelineSuccessRate:P2}",
                    data));
            }

            var description = string.Join("; ", issues);

            // Degraded if some issues but above critical thresholds
            if (snapshot.PipelineSuccessRate >= _options.CriticalSuccessRate)
            {
                return Task.FromResult(HealthCheckResult.Degraded(description, data: data));
            }

            // Unhealthy if below critical thresholds
            return Task.FromResult(HealthCheckResult.Unhealthy(description, data: data));
        }
    }

    /// <summary>
    /// Configuration options for pipeline health check.
    /// </summary>
    public class PipelineHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the minimum success rate threshold (0-1). Default: 0.95 (95%).
        /// </summary>
        public double MinimumSuccessRate { get; set; } = 0.95;

        /// <summary>
        /// Gets or sets the minimum operation success rate threshold (0-1). Default: 0.95 (95%).
        /// </summary>
        public double MinimumOperationSuccessRate { get; set; } = 0.95;

        /// <summary>
        /// Gets or sets the critical success rate below which the health is Unhealthy (0-1). Default: 0.80 (80%).
        /// </summary>
        public double CriticalSuccessRate { get; set; } = 0.80;

        /// <summary>
        /// Gets or sets the minimum number of executions required before evaluation. Default: 10.
        /// </summary>
        public long MinimumExecutionsForEvaluation { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum average pipeline duration in milliseconds (null to disable). Default: null.
        /// </summary>
        public double? MaxAverageDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum P95 operation duration in milliseconds (null to disable). Default: null.
        /// </summary>
        public double? MaxP95DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of active executions per pipeline (null to disable). Default: null.
        /// </summary>
        public int? MaxActiveExecutions { get; set; }
    }

    /// <summary>
    /// Health check result for a specific pipeline.
    /// </summary>
    public sealed class PipelineHealthStatus
    {
        /// <summary>
        /// Gets or sets the pipeline name.
        /// </summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the pipeline is healthy.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets or sets the health status.
        /// </summary>
        public HealthStatus Status { get; init; }

        /// <summary>
        /// Gets or sets the status description.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets or sets the success rate.
        /// </summary>
        public double SuccessRate { get; init; }

        /// <summary>
        /// Gets or sets the total executions.
        /// </summary>
        public long TotalExecutions { get; init; }

        /// <summary>
        /// Gets or sets the average duration in milliseconds.
        /// </summary>
        public double AverageDurationMs { get; init; }

        /// <summary>
        /// Gets or sets the active execution count.
        /// </summary>
        public int ActiveExecutions { get; init; }

        /// <summary>
        /// Gets or sets the last execution timestamp.
        /// </summary>
        public DateTime? LastExecutionAt { get; init; }
    }

    /// <summary>
    /// Provides aggregated health status for all pipelines.
    /// </summary>
    public interface IPipelineHealthMonitor
    {
        /// <summary>
        /// Gets the health status for all pipelines.
        /// </summary>
        /// <returns>A collection of pipeline health statuses.</returns>
        IReadOnlyList<PipelineHealthStatus> GetAllPipelineHealth();

        /// <summary>
        /// Gets the health status for a specific pipeline.
        /// </summary>
        /// <param name="pipelineName">The pipeline name.</param>
        /// <returns>The pipeline health status, or null if not found.</returns>
        PipelineHealthStatus? GetPipelineHealth(string pipelineName);

        /// <summary>
        /// Gets the overall system health based on all pipelines.
        /// </summary>
        /// <returns>The overall health status.</returns>
        HealthStatus GetOverallHealth();
    }

    /// <summary>
    /// Default implementation of <see cref="IPipelineHealthMonitor"/>.
    /// </summary>
    public class PipelineHealthMonitor : IPipelineHealthMonitor
    {
        private readonly IPipelineMetrics _metrics;
        private readonly PipelineHealthCheckOptions _options;

        /// <summary>
        /// Creates a new instance of PipelineHealthMonitor.
        /// </summary>
        public PipelineHealthMonitor(
            IPipelineMetrics metrics,
            PipelineHealthCheckOptions? options = null)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _options = options ?? new PipelineHealthCheckOptions();
        }

        /// <inheritdoc />
        public IReadOnlyList<PipelineHealthStatus> GetAllPipelineHealth()
        {
            var snapshot = _metrics.GetSnapshot();
            var results = new List<PipelineHealthStatus>();

            foreach (var kvp in snapshot.PipelineMetrics)
            {
                results.Add(CreateHealthStatus(kvp.Key, kvp.Value));
            }

            return results;
        }

        /// <inheritdoc />
        public PipelineHealthStatus? GetPipelineHealth(string pipelineName)
        {
            if (string.IsNullOrEmpty(pipelineName))
                return null;

            var metrics = _metrics.GetPipelineMetrics(pipelineName);
            if (metrics == null)
                return null;

            return CreateHealthStatus(pipelineName, metrics);
        }

        /// <inheritdoc />
        public HealthStatus GetOverallHealth()
        {
            var snapshot = _metrics.GetSnapshot();

            if (snapshot.TotalPipelineExecutions < _options.MinimumExecutionsForEvaluation)
                return HealthStatus.Healthy;

            if (snapshot.PipelineSuccessRate < _options.CriticalSuccessRate)
                return HealthStatus.Unhealthy;

            if (snapshot.PipelineSuccessRate < _options.MinimumSuccessRate)
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }

        private PipelineHealthStatus CreateHealthStatus(string pipelineName, PipelineExecutionMetrics metrics)
        {
            var status = DetermineStatus(metrics);
            var issues = new List<string>();

            if (metrics.SuccessRate < _options.MinimumSuccessRate)
                issues.Add($"Success rate {metrics.SuccessRate:P2} below threshold");

            if (_options.MaxActiveExecutions.HasValue && metrics.ActiveExecutions > _options.MaxActiveExecutions.Value)
                issues.Add($"Active executions ({metrics.ActiveExecutions}) above threshold");

            return new PipelineHealthStatus
            {
                PipelineName = pipelineName,
                IsHealthy = status == HealthStatus.Healthy,
                Status = status,
                Description = issues.Count > 0 ? string.Join("; ", issues) : null,
                SuccessRate = metrics.SuccessRate,
                TotalExecutions = metrics.TotalExecutions,
                AverageDurationMs = metrics.AverageDurationMs,
                ActiveExecutions = metrics.ActiveExecutions,
                LastExecutionAt = metrics.LastExecutionAt
            };
        }

        private HealthStatus DetermineStatus(PipelineExecutionMetrics metrics)
        {
            if (metrics.TotalExecutions < _options.MinimumExecutionsForEvaluation)
                return HealthStatus.Healthy;

            if (metrics.SuccessRate < _options.CriticalSuccessRate)
                return HealthStatus.Unhealthy;

            if (metrics.SuccessRate < _options.MinimumSuccessRate)
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }
    }
}

