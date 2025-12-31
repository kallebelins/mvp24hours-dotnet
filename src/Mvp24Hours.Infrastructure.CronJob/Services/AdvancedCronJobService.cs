//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Scheduling;
using Mvp24Hours.Infrastructure.CronJob.State;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Services
{
    /// <summary>
    /// Advanced CronJob service with full feature support including:
    /// context management, event hooks, dependencies, distributed locking,
    /// state persistence, and pause/resume control.
    /// </summary>
    /// <typeparam name="T">The type of the CronJob service.</typeparam>
    public abstract class AdvancedCronJobService<T> : ResilientCronJobService<T>
    {
        private readonly ICronJobStateStore? _stateStore;
        private readonly ICronJobController? _controller;
        private readonly ICronJobDependencyTracker? _dependencyTracker;
        private readonly ICronJobEventDispatcher? _eventDispatcher;
        private readonly IDistributedCronJobLock? _distributedLock;
        private readonly IAdvancedCronJobOptions<T>? _advancedOptions;
        private readonly ILogger<AdvancedCronJobService<T>> _logger;
        private CronJobContext? _currentContext;

        /// <summary>
        /// Gets the current execution context.
        /// </summary>
        protected ICronJobContext? Context => _currentContext;

        /// <summary>
        /// Creates a new instance of AdvancedCronJobService.
        /// </summary>
        protected AdvancedCronJobService(
            IResilientScheduleConfig<T> config,
            IHostApplicationLifetime hostApplication,
            IServiceProvider rootServiceProvider,
            ICronJobExecutionLock executionLock,
            CronJobCircuitBreaker circuitBreaker,
            ILogger<AdvancedCronJobService<T>> logger,
            TimeProvider? timeProvider = null)
            : base(config, hostApplication, rootServiceProvider, executionLock, circuitBreaker, logger, timeProvider)
        {
            _logger = logger;
            _stateStore = rootServiceProvider.GetService<ICronJobStateStore>();
            _controller = rootServiceProvider.GetService<ICronJobController>();
            _dependencyTracker = rootServiceProvider.GetService<ICronJobDependencyTracker>();
            _eventDispatcher = rootServiceProvider.GetService<ICronJobEventDispatcher>();
            _distributedLock = rootServiceProvider.GetService<IDistributedCronJobLock>();
            _advancedOptions = rootServiceProvider.GetService<IAdvancedCronJobOptions<T>>();

            // Register with controller if available
            if (_controller is CronJobController controller)
            {
                controller.Register(JobName, CronExpression, async ct => await DoWork(ct));
            }
        }

        /// <summary>
        /// Gets the current execution context, throwing if not available.
        /// </summary>
        /// <returns>The current context.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called outside of DoWork execution.</exception>
        protected ICronJobContext GetContext()
        {
            return _currentContext ?? throw new InvalidOperationException(
                "Context is only available during DoWork execution.");
        }

        /// <summary>
        /// Sets a custom property on the current context.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        protected void SetContextProperty(string key, object? value)
        {
            _currentContext?.SetProperty(key, value);
        }

        /// <inheritdoc />
        public sealed override async Task DoWork(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            // Check if paused
            if (_stateStore != null && await _stateStore.IsPausedAsync(JobName, cancellationToken))
            {
                _logger.LogDebug("CronJob {JobName} is paused, skipping execution", JobName);
                await _eventDispatcher?.DispatchSkippedAsync(JobName, SkipReason.Paused, cancellationToken)!;
                return;
            }

            // Check dependencies
            if (_dependencyTracker != null)
            {
                var satisfied = await _dependencyTracker.AreDependenciesSatisfiedAsync(JobName, cancellationToken);
                if (!satisfied)
                {
                    var unsatisfied = await _dependencyTracker.GetUnsatisfiedDependenciesAsync(JobName, cancellationToken);
                    _logger.LogDebug("CronJob {JobName} dependencies not met: {Dependencies}",
                        JobName, string.Join(", ", unsatisfied));
                    await _eventDispatcher?.DispatchSkippedAsync(JobName, SkipReason.DependencyNotMet, cancellationToken)!;
                    return;
                }
            }

            // Acquire distributed lock if configured
            IDistributedCronJobLockHandle? distributedLockHandle = null;
            if (_distributedLock != null && (_advancedOptions?.UseDistributedLocking ?? false))
            {
                var instanceId = Environment.MachineName + "_" + Environment.ProcessId;
                var lockDuration = _advancedOptions?.DistributedLockDuration ?? TimeSpan.FromMinutes(5);

                distributedLockHandle = await _distributedLock.TryAcquireAsync(
                    JobName, instanceId, lockDuration, cancellationToken);

                if (distributedLockHandle == null)
                {
                    _logger.LogDebug("CronJob {JobName} could not acquire distributed lock, skipping", JobName);
                    await _eventDispatcher?.DispatchSkippedAsync(JobName, SkipReason.Overlapping, cancellationToken)!;
                    return;
                }
            }

            try
            {
                // Create context
                _currentContext = new CronJobContext(
                    JobName,
                    CronExpression,
                    null, // TimeZone not accessible from base
                    cancellationToken,
                    ExecutionCount,
                    1, // Will be updated by retry logic
                    null, // Timeout from resilience config
                    null, // Scheduled time
                    null, // Parent job ID
                    null  // Correlation ID - auto-generated
                );

                CronJobContextAccessor.SetContext(_currentContext);

                // Dispatch starting event
                if (_eventDispatcher != null)
                {
                    await _eventDispatcher.DispatchStartingAsync(_currentContext, cancellationToken);
                }

                // Call hooks
                await OnJobStartingAsync(_currentContext, cancellationToken);

                // Execute actual work
                await ExecuteAsync(_currentContext, cancellationToken);

                stopwatch.Stop();

                // Dispatch completed event
                if (_eventDispatcher != null)
                {
                    await _eventDispatcher.DispatchCompletedAsync(_currentContext, stopwatch.Elapsed, cancellationToken);
                }

                // Call hooks
                await OnJobCompletedAsync(_currentContext, stopwatch.Elapsed, cancellationToken);

                // Update state
                if (_stateStore != null)
                {
                    var state = await _stateStore.GetStateAsync(JobName, cancellationToken)
                        ?? new CronJobState(JobName);
                    state.RecordSuccess(stopwatch.Elapsed);
                    await _stateStore.SaveStateAsync(state, cancellationToken);
                }

                // Record completion for dependency tracking
                if (_dependencyTracker != null)
                {
                    await _dependencyTracker.RecordCompletionAsync(JobName, success: true, _currentContext.JobId);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                if (_eventDispatcher != null && _currentContext != null)
                {
                    await _eventDispatcher.DispatchCancelledAsync(_currentContext, stopwatch.Elapsed, CancellationToken.None);
                }

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (_eventDispatcher != null && _currentContext != null)
                {
                    await _eventDispatcher.DispatchFailedAsync(_currentContext, ex, stopwatch.Elapsed, cancellationToken);
                }

                if (_currentContext != null)
                {
                    await OnJobFailedAsync(_currentContext, ex, stopwatch.Elapsed, cancellationToken);
                }

                // Update state
                if (_stateStore != null)
                {
                    var state = await _stateStore.GetStateAsync(JobName, cancellationToken)
                        ?? new CronJobState(JobName);
                    state.RecordFailure(stopwatch.Elapsed, ex.Message);
                    await _stateStore.SaveStateAsync(state, cancellationToken);
                }

                // Record completion for dependency tracking
                if (_dependencyTracker != null && _currentContext != null)
                {
                    await _dependencyTracker.RecordCompletionAsync(JobName, success: false, _currentContext.JobId);
                }

                throw;
            }
            finally
            {
                if (distributedLockHandle != null)
                {
                    await distributedLockHandle.DisposeAsync();
                }

                CronJobContextAccessor.ClearContext();
                _currentContext = null;
            }
        }

        /// <summary>
        /// Execute the job work. Override this instead of DoWork.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        protected abstract Task ExecuteAsync(ICronJobContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Called when the job is about to start execution.
        /// Override to add custom pre-execution logic.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        protected virtual Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the job has completed successfully.
        /// Override to add custom post-execution logic.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="duration">The execution duration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        protected virtual Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the job has failed with an exception.
        /// Override to add custom error handling logic.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="duration">The execution duration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        protected virtual Task OnJobFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Options for advanced CronJob configuration.
    /// </summary>
    /// <typeparam name="T">The CronJob type.</typeparam>
    public interface IAdvancedCronJobOptions<T>
    {
        /// <summary>
        /// Gets whether to use distributed locking.
        /// </summary>
        bool UseDistributedLocking { get; }

        /// <summary>
        /// Gets the distributed lock duration.
        /// </summary>
        TimeSpan DistributedLockDuration { get; }

        /// <summary>
        /// Gets whether to track dependencies.
        /// </summary>
        bool TrackDependencies { get; }

        /// <summary>
        /// Gets whether to persist state.
        /// </summary>
        bool PersistState { get; }
    }

    /// <summary>
    /// Default implementation of advanced CronJob options.
    /// </summary>
    public sealed class AdvancedCronJobOptions<T> : IAdvancedCronJobOptions<T>
    {
        /// <inheritdoc />
        public bool UseDistributedLocking { get; set; } = false;

        /// <inheritdoc />
        public TimeSpan DistributedLockDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <inheritdoc />
        public bool TrackDependencies { get; set; } = true;

        /// <inheritdoc />
        public bool PersistState { get; set; } = true;
    }
}

