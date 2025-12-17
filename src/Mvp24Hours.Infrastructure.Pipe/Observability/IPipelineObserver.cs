//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Observer interface for pipeline execution events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to receive notifications about pipeline lifecycle events:
    /// <list type="bullet">
    /// <item>Pipeline start and completion</item>
    /// <item>Operation start and completion</item>
    /// <item>Failure and rollback events</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IPipelineObserver
    {
        /// <summary>
        /// Called when a pipeline execution starts.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when a pipeline execution completes.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when an operation starts within a pipeline.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when an operation completes within a pipeline.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when an operation fails.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when a rollback operation starts.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when a rollback operation completes.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OnRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Sync observer interface for pipeline execution events.
    /// </summary>
    public interface IPipelineObserverSync
    {
        /// <summary>
        /// Called when a pipeline execution starts.
        /// </summary>
        void OnPipelineStart(PipelineStartEventArgs eventArgs);

        /// <summary>
        /// Called when a pipeline execution completes.
        /// </summary>
        void OnPipelineComplete(PipelineCompleteEventArgs eventArgs);

        /// <summary>
        /// Called when an operation starts within a pipeline.
        /// </summary>
        void OnOperationStart(OperationStartEventArgs eventArgs);

        /// <summary>
        /// Called when an operation completes within a pipeline.
        /// </summary>
        void OnOperationEnd(OperationEndEventArgs eventArgs);

        /// <summary>
        /// Called when an operation fails.
        /// </summary>
        void OnOperationFailure(OperationFailureEventArgs eventArgs);

        /// <summary>
        /// Called when a rollback operation starts.
        /// </summary>
        void OnRollbackStart(RollbackEventArgs eventArgs);

        /// <summary>
        /// Called when a rollback operation completes.
        /// </summary>
        void OnRollbackComplete(RollbackEventArgs eventArgs);
    }

    #region [ Event Args ]

    /// <summary>
    /// Base class for pipeline event arguments.
    /// </summary>
    public abstract class PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the pipeline execution ID.
        /// </summary>
        public string PipelineId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline name/type.
        /// </summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation ID.
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Gets or sets the causation ID.
        /// </summary>
        public string? CausationId { get; init; }

        /// <summary>
        /// Gets or sets the timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets custom metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>
    /// Event arguments for pipeline start events.
    /// </summary>
    public sealed class PipelineStartEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the total number of operations in the pipeline.
        /// </summary>
        public int OperationCount { get; init; }

        /// <summary>
        /// Gets or sets whether the pipeline is configured to break on fail.
        /// </summary>
        public bool IsBreakOnFail { get; init; }

        /// <summary>
        /// Gets or sets whether the pipeline will force rollback on failure.
        /// </summary>
        public bool ForceRollbackOnFailure { get; init; }

        /// <summary>
        /// Gets or sets the initial message state.
        /// </summary>
        public IPipelineMessage? InitialMessage { get; init; }
    }

    /// <summary>
    /// Event arguments for pipeline completion events.
    /// </summary>
    public sealed class PipelineCompleteEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets whether the pipeline completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets or sets whether the message is faulty.
        /// </summary>
        public bool IsFaulty { get; init; }

        /// <summary>
        /// Gets or sets whether the message is locked.
        /// </summary>
        public bool IsLocked { get; init; }

        /// <summary>
        /// Gets or sets the total duration of the pipeline execution.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets or sets the number of operations executed.
        /// </summary>
        public int OperationsExecuted { get; init; }

        /// <summary>
        /// Gets or sets the number of operations that succeeded.
        /// </summary>
        public int SuccessfulOperations { get; init; }

        /// <summary>
        /// Gets or sets the number of operations that failed.
        /// </summary>
        public int FailedOperations { get; init; }

        /// <summary>
        /// Gets or sets the final message state.
        /// </summary>
        public IPipelineMessage? FinalMessage { get; init; }

        /// <summary>
        /// Gets or sets the memory delta during execution (if tracked).
        /// </summary>
        public long MemoryDelta { get; init; }
    }

    /// <summary>
    /// Event arguments for operation start events.
    /// </summary>
    public sealed class OperationStartEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string OperationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation type name.
        /// </summary>
        public string OperationType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation index in the pipeline.
        /// </summary>
        public int OperationIndex { get; init; }

        /// <summary>
        /// Gets or sets whether the operation is required.
        /// </summary>
        public bool IsRequired { get; init; }

        /// <summary>
        /// Gets or sets the current message state.
        /// </summary>
        public IPipelineMessage? CurrentMessage { get; init; }
    }

    /// <summary>
    /// Event arguments for operation end events.
    /// </summary>
    public sealed class OperationEndEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string OperationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation type name.
        /// </summary>
        public string OperationType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation index in the pipeline.
        /// </summary>
        public int OperationIndex { get; init; }

        /// <summary>
        /// Gets or sets whether the operation completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets or sets the operation duration.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets or sets the current message state after the operation.
        /// </summary>
        public IPipelineMessage? CurrentMessage { get; init; }

        /// <summary>
        /// Gets or sets the memory delta during the operation (if tracked).
        /// </summary>
        public long MemoryDelta { get; init; }
    }

    /// <summary>
    /// Event arguments for operation failure events.
    /// </summary>
    public sealed class OperationFailureEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string OperationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation type name.
        /// </summary>
        public string OperationType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation index in the pipeline.
        /// </summary>
        public int OperationIndex { get; init; }

        /// <summary>
        /// Gets or sets the exception that caused the failure.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets or sets the duration until failure.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets or sets the current message state.
        /// </summary>
        public IPipelineMessage? CurrentMessage { get; init; }
    }

    /// <summary>
    /// Event arguments for rollback events.
    /// </summary>
    public sealed class RollbackEventArgs : PipelineEventArgsBase
    {
        /// <summary>
        /// Gets or sets the operation name being rolled back.
        /// </summary>
        public string OperationName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation index.
        /// </summary>
        public int OperationIndex { get; init; }

        /// <summary>
        /// Gets or sets whether the rollback succeeded.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets or sets the rollback duration.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets or sets the exception during rollback (if any).
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets or sets the total number of operations to rollback.
        /// </summary>
        public int TotalOperationsToRollback { get; init; }

        /// <summary>
        /// Gets or sets the current rollback index.
        /// </summary>
        public int CurrentRollbackIndex { get; init; }
    }

    #endregion

    #region [ Observer Manager ]

    /// <summary>
    /// Manages and coordinates multiple pipeline observers.
    /// </summary>
    public interface IPipelineObserverManager
    {
        /// <summary>
        /// Registers an observer.
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        void Register(IPipelineObserver observer);

        /// <summary>
        /// Registers a sync observer.
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        void Register(IPipelineObserverSync observer);

        /// <summary>
        /// Unregisters an observer.
        /// </summary>
        /// <param name="observer">The observer to unregister.</param>
        void Unregister(IPipelineObserver observer);

        /// <summary>
        /// Unregisters a sync observer.
        /// </summary>
        /// <param name="observer">The observer to unregister.</param>
        void Unregister(IPipelineObserverSync observer);

        /// <summary>
        /// Notifies all observers of a pipeline start event.
        /// </summary>
        Task NotifyPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of a pipeline complete event.
        /// </summary>
        Task NotifyPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of an operation start event.
        /// </summary>
        Task NotifyOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of an operation end event.
        /// </summary>
        Task NotifyOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of an operation failure event.
        /// </summary>
        Task NotifyOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of a rollback start event.
        /// </summary>
        Task NotifyRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies all observers of a rollback complete event.
        /// </summary>
        Task NotifyRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of <see cref="IPipelineObserverManager"/>.
    /// </summary>
    public class PipelineObserverManager : IPipelineObserverManager
    {
        private readonly List<IPipelineObserver> _observers = new();
        private readonly List<IPipelineObserverSync> _syncObservers = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public void Register(IPipelineObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            lock (_lock)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                }
            }
        }

        /// <inheritdoc />
        public void Register(IPipelineObserverSync observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            lock (_lock)
            {
                if (!_syncObservers.Contains(observer))
                {
                    _syncObservers.Add(observer);
                }
            }
        }

        /// <inheritdoc />
        public void Unregister(IPipelineObserver observer)
        {
            if (observer == null) return;

            lock (_lock)
            {
                _observers.Remove(observer);
            }
        }

        /// <inheritdoc />
        public void Unregister(IPipelineObserverSync observer)
        {
            if (observer == null) return;

            lock (_lock)
            {
                _syncObservers.Remove(observer);
            }
        }

        /// <inheritdoc />
        public async Task NotifyPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnPipelineStart(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnPipelineStartAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnPipelineComplete(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnPipelineCompleteAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnOperationStart(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnOperationStartAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnOperationEnd(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnOperationEndAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnOperationFailure(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnOperationFailureAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnRollbackStart(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnRollbackStartAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }

        /// <inheritdoc />
        public async Task NotifyRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            List<IPipelineObserver> observers;
            List<IPipelineObserverSync> syncObservers;

            lock (_lock)
            {
                observers = new List<IPipelineObserver>(_observers);
                syncObservers = new List<IPipelineObserverSync>(_syncObservers);
            }

            foreach (var observer in syncObservers)
            {
                try { observer.OnRollbackComplete(eventArgs); }
                catch { /* Observers should not break pipeline */ }
            }

            foreach (var observer in observers)
            {
                try { await observer.OnRollbackCompleteAsync(eventArgs, cancellationToken); }
                catch { /* Observers should not break pipeline */ }
            }
        }
    }

    #endregion

    #region [ Metrics Observer ]

    /// <summary>
    /// Pipeline observer that collects metrics.
    /// </summary>
    public class MetricsCollectorObserver : IPipelineObserver
    {
        private readonly IPipelineMetrics _metrics;

        /// <summary>
        /// Creates a new instance of MetricsCollectorObserver.
        /// </summary>
        public MetricsCollectorObserver(IPipelineMetrics metrics)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        /// <inheritdoc />
        public Task OnPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            _metrics.RecordPipelineStart(eventArgs.PipelineId, eventArgs.PipelineName);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            _metrics.RecordPipelineEnd(eventArgs.PipelineId, eventArgs.Success);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            _metrics.RecordOperationStart(eventArgs.PipelineId, eventArgs.OperationName, eventArgs.OperationIndex);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            _metrics.RecordOperationEnd(
                eventArgs.PipelineId,
                eventArgs.OperationName,
                eventArgs.Duration,
                eventArgs.Success,
                eventArgs.MemoryDelta > 0 ? eventArgs.MemoryDelta : null);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            if (eventArgs.Exception != null)
            {
                _metrics.RecordOperationFailure(eventArgs.PipelineId, eventArgs.OperationName, eventArgs.Exception);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}

