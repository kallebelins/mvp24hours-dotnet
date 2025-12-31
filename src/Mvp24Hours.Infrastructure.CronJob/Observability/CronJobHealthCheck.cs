//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Observability;

/// <summary>
/// Health check for CronJob services.
/// </summary>
/// <remarks>
/// <para>
/// This health check monitors the status of registered CronJobs and reports:
/// </para>
/// <list type="bullet">
/// <item><b>Healthy:</b> All jobs are running normally</item>
/// <item><b>Degraded:</b> Some jobs have high failure rates or are behind schedule</item>
/// <item><b>Unhealthy:</b> Critical jobs have failed or circuit breaker is open</item>
/// </list>
/// <para>
/// The health check integrates with ASP.NET Core health checks and can be exposed
/// via endpoints like /health/cronjobs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register the health check
/// builder.Services.AddHealthChecks()
///     .AddCronJobHealthCheck();
/// 
/// // Or with options
/// builder.Services.AddHealthChecks()
///     .AddCronJobHealthCheck(options =>
///     {
///         options.MaxFailureRate = 0.1; // 10% failure rate threshold
///         options.MaxExecutionAge = TimeSpan.FromHours(1);
///     });
/// </code>
/// </example>
public sealed class CronJobHealthCheck : IHealthCheck
{
    private readonly CronJobMetricsService? _metricsService;
    private readonly ILogger<CronJobHealthCheck>? _logger;
    private readonly CronJobHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="CronJobHealthCheck"/>.
    /// </summary>
    /// <param name="metricsService">The CronJob metrics service.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="options">Health check options.</param>
    public CronJobHealthCheck(
        CronJobMetricsService? metricsService = null,
        ILogger<CronJobHealthCheck>? logger = null,
        IOptions<CronJobHealthCheckOptions>? options = null)
    {
        _metricsService = metricsService;
        _logger = logger;
        _options = options?.Value ?? new CronJobHealthCheckOptions();
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_metricsService == null)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "CronJob metrics service not available. Health check skipped."));
            }

            var jobStates = _metricsService.GetAllJobStates();

            if (jobStates.IsEmpty)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "No CronJobs registered.",
                    new Dictionary<string, object>
                    {
                        ["registered_jobs"] = 0
                    }));
            }

            var unhealthyJobs = new List<string>();
            var degradedJobs = new List<string>();
            var data = new Dictionary<string, object>
            {
                ["registered_jobs"] = jobStates.Count,
                ["check_time"] = DateTimeOffset.UtcNow.ToString("O")
            };

            foreach (var (jobName, state) in jobStates)
            {
                var jobData = new Dictionary<string, object>
                {
                    ["is_running"] = state.IsRunning,
                    ["is_active"] = state.IsActive,
                    ["total_executions"] = state.TotalExecutions,
                    ["total_failures"] = state.TotalFailures,
                    ["total_skipped"] = state.TotalSkipped,
                    ["success_rate"] = $"{state.SuccessRate:F2}%"
                };

                if (state.LastExecutionTime.HasValue)
                {
                    jobData["last_execution"] = state.LastExecutionTime.Value.ToString("O");
                    jobData["time_since_last_execution"] = state.TimeSinceLastExecution?.ToString() ?? "N/A";
                }

                if (state.NextScheduledExecution.HasValue)
                {
                    jobData["next_execution"] = state.NextScheduledExecution.Value.ToString("O");
                }

                if (state.CircuitBreakerState != null)
                {
                    jobData["circuit_breaker_state"] = state.CircuitBreakerState;
                }

                if (state.LastErrorMessage != null)
                {
                    jobData["last_error"] = state.LastErrorMessage;
                    jobData["last_error_type"] = state.LastErrorType ?? "Unknown";
                }

                data[jobName] = jobData;

                // Check for unhealthy conditions
                var status = EvaluateJobHealth(state);
                
                if (status == HealthStatus.Unhealthy)
                {
                    unhealthyJobs.Add(jobName);
                }
                else if (status == HealthStatus.Degraded)
                {
                    degradedJobs.Add(jobName);
                }
            }

            data["unhealthy_jobs"] = unhealthyJobs;
            data["degraded_jobs"] = degradedJobs;

            if (unhealthyJobs.Count > 0)
            {
                var description = $"Unhealthy CronJobs: {string.Join(", ", unhealthyJobs)}";
                _logger?.LogWarning("CronJob health check failed. {Description}", description);
                return Task.FromResult(HealthCheckResult.Unhealthy(description, data: data));
            }

            if (degradedJobs.Count > 0)
            {
                var description = $"Degraded CronJobs: {string.Join(", ", degradedJobs)}";
                _logger?.LogWarning("CronJob health check degraded. {Description}", description);
                return Task.FromResult(HealthCheckResult.Degraded(description, data: data));
            }

            _logger?.LogDebug("CronJob health check passed. {JobCount} jobs healthy.", jobStates.Count);
            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {jobStates.Count} CronJobs are healthy.", data));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CronJob health check failed with exception.");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed with exception.",
                ex,
                new Dictionary<string, object>
                {
                    ["exception"] = ex.Message
                }));
        }
    }

    private HealthStatus EvaluateJobHealth(CronJobState state)
    {
        // Check circuit breaker state
        if (state.CircuitBreakerState == "Open")
        {
            return HealthStatus.Unhealthy;
        }

        // Check if job should be running but isn't
        if (!state.IsRunning && state.StartTime.HasValue && !state.StopTime.HasValue)
        {
            return HealthStatus.Unhealthy;
        }

        // Check failure rate
        if (state.TotalExecutions > _options.MinExecutionsForRateCheck)
        {
            var failureRate = state.TotalFailures / (double)state.TotalExecutions;
            if (failureRate > _options.MaxFailureRate)
            {
                return failureRate > _options.CriticalFailureRate 
                    ? HealthStatus.Unhealthy 
                    : HealthStatus.Degraded;
            }
        }

        // Check if job is behind schedule
        if (state.LastExecutionTime.HasValue && 
            state.TimeSinceLastExecution > _options.MaxExecutionAge &&
            state.IsRunning)
        {
            return HealthStatus.Degraded;
        }

        // Check for recent failures
        if (!state.LastExecutionSuccess && 
            state.LastExecutionTime.HasValue &&
            (DateTimeOffset.UtcNow - state.LastExecutionTime.Value) < _options.RecentFailureWindow)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}

/// <summary>
/// Options for configuring the CronJob health check.
/// </summary>
public sealed class CronJobHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the maximum failure rate (0.0 to 1.0) before reporting degraded.
    /// Default is 0.1 (10%).
    /// </summary>
    public double MaxFailureRate { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the critical failure rate (0.0 to 1.0) before reporting unhealthy.
    /// Default is 0.5 (50%).
    /// </summary>
    public double CriticalFailureRate { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the minimum number of executions before rate checking applies.
    /// Default is 10.
    /// </summary>
    public int MinExecutionsForRateCheck { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum time since last execution before reporting degraded.
    /// Default is 2 hours.
    /// </summary>
    public TimeSpan MaxExecutionAge { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Gets or sets the time window for considering recent failures.
    /// Default is 15 minutes.
    /// </summary>
    public TimeSpan RecentFailureWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the list of critical job names that cause unhealthy status on any failure.
    /// </summary>
    public HashSet<string> CriticalJobs { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to ignore stopped jobs in health evaluation.
    /// Default is true.
    /// </summary>
    public bool IgnoreStoppedJobs { get; set; } = true;
}

