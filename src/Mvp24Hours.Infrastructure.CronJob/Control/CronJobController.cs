//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Control
{
    /// <summary>
    /// Default implementation of <see cref="ICronJobController"/>.
    /// </summary>
    public sealed class CronJobController : ICronJobController
    {
        private readonly ICronJobStateStore _stateStore;
        private readonly CronJobCircuitBreaker _circuitBreaker;
        private readonly ILogger<CronJobController> _logger;
        private readonly ConcurrentDictionary<string, CronJobRegistration> _registrations = new();
        private readonly ConcurrentDictionary<string, Func<CancellationToken, Task>> _triggerCallbacks = new();

        /// <summary>
        /// Creates a new instance of <see cref="CronJobController"/>.
        /// </summary>
        public CronJobController(
            ICronJobStateStore stateStore,
            CronJobCircuitBreaker circuitBreaker,
            ILogger<CronJobController> logger)
        {
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a job for control.
        /// </summary>
        public void Register(string jobName, string? cronExpression = null, Func<CancellationToken, Task>? triggerCallback = null)
        {
            _registrations[jobName] = new CronJobRegistration
            {
                JobName = jobName,
                CronExpression = cronExpression,
                RegisteredAt = DateTimeOffset.UtcNow
            };

            if (triggerCallback != null)
            {
                _triggerCallbacks[jobName] = triggerCallback;
            }
        }

        /// <inheritdoc />
        public async Task<bool> PauseAsync(string jobName, string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Pausing CronJob {JobName}. Reason: {Reason}", jobName, reason ?? "None");

            await _stateStore.SetPausedAsync(jobName, true, cancellationToken);

            var state = await _stateStore.GetStateAsync(jobName, cancellationToken);
            if (state != null)
            {
                state.PauseReason = reason;
                await _stateStore.SaveStateAsync(state, cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public Task<bool> PauseAsync<T>(string? reason = null, CancellationToken cancellationToken = default) where T : class
        {
            return PauseAsync(typeof(T).Name, reason, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ResumeAsync(string jobName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resuming CronJob {JobName}", jobName);

            await _stateStore.SetPausedAsync(jobName, false, cancellationToken);

            return true;
        }

        /// <inheritdoc />
        public Task<bool> ResumeAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            return ResumeAsync(typeof(T).Name, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default)
        {
            return _stateStore.IsPausedAsync(jobName, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsPausedAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            return IsPausedAsync(typeof(T).Name, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<CronJobStatus?> GetStatusAsync(string jobName, CancellationToken cancellationToken = default)
        {
            var state = await _stateStore.GetStateAsync(jobName, cancellationToken);
            if (state == null)
            {
                return null;
            }

            _registrations.TryGetValue(jobName, out var registration);

            var circuitState = _circuitBreaker.GetState(jobName);

            return new CronJobStatus
            {
                JobName = state.JobName,
                CronExpression = registration?.CronExpression,
                State = state.IsPaused ? CronJobExecutionState.Paused : CronJobExecutionState.Idle,
                IsPaused = state.IsPaused,
                PauseReason = state.PauseReason,
                PausedAt = state.PausedAt,
                LastExecutionTime = state.LastExecutionTime,
                NextExecutionTime = null, // Would need CRON parser to calculate
                ExecutionCount = state.ExecutionCount,
                SuccessCount = state.SuccessCount,
                FailureCount = state.FailureCount,
                LastError = state.LastErrorMessage,
                AverageDurationMs = state.AverageDurationMs,
                CircuitBreakerState = circuitState
            };
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<CronJobStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
        {
            var states = await _stateStore.GetAllStatesAsync(cancellationToken);
            var statuses = new List<CronJobStatus>();

            foreach (var state in states)
            {
                _registrations.TryGetValue(state.JobName, out var registration);
                var circuitState = _circuitBreaker.GetState(state.JobName);

                statuses.Add(new CronJobStatus
                {
                    JobName = state.JobName,
                    CronExpression = registration?.CronExpression,
                    State = state.IsPaused ? CronJobExecutionState.Paused : CronJobExecutionState.Idle,
                    IsPaused = state.IsPaused,
                    PauseReason = state.PauseReason,
                    PausedAt = state.PausedAt,
                    LastExecutionTime = state.LastExecutionTime,
                    ExecutionCount = state.ExecutionCount,
                    SuccessCount = state.SuccessCount,
                    FailureCount = state.FailureCount,
                    LastError = state.LastErrorMessage,
                    AverageDurationMs = state.AverageDurationMs,
                    CircuitBreakerState = circuitState
                });
            }

            // Add registered jobs that don't have state yet
            foreach (var registration in _registrations.Values)
            {
                if (!statuses.Any(s => s.JobName == registration.JobName))
                {
                    statuses.Add(new CronJobStatus
                    {
                        JobName = registration.JobName,
                        CronExpression = registration.CronExpression,
                        State = CronJobExecutionState.Idle,
                        IsPaused = false,
                        CircuitBreakerState = CircuitBreakerState.Closed
                    });
                }
            }

            return statuses;
        }

        /// <inheritdoc />
        public async Task<bool> TriggerAsync(string jobName, CancellationToken cancellationToken = default)
        {
            if (await IsPausedAsync(jobName, cancellationToken))
            {
                _logger.LogWarning("Cannot trigger paused CronJob {JobName}", jobName);
                return false;
            }

            if (_triggerCallbacks.TryGetValue(jobName, out var callback))
            {
                _logger.LogInformation("Manually triggering CronJob {JobName}", jobName);
                try
                {
                    await callback(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error triggering CronJob {JobName}", jobName);
                    return false;
                }
            }

            _logger.LogWarning("No trigger callback registered for CronJob {JobName}", jobName);
            return false;
        }

        /// <inheritdoc />
        public Task<bool> TriggerAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            return TriggerAsync(typeof(T).Name, cancellationToken);
        }

        /// <inheritdoc />
        public async Task PauseAllAsync(string? reason = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Pausing all CronJobs. Reason: {Reason}", reason ?? "None");

            foreach (var registration in _registrations.Keys)
            {
                await PauseAsync(registration, reason, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task ResumeAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resuming all CronJobs");

            foreach (var registration in _registrations.Keys)
            {
                await ResumeAsync(registration, cancellationToken);
            }
        }

        private sealed class CronJobRegistration
        {
            public string JobName { get; init; } = string.Empty;
            public string? CronExpression { get; init; }
            public DateTimeOffset RegisteredAt { get; init; }
        }
    }
}

