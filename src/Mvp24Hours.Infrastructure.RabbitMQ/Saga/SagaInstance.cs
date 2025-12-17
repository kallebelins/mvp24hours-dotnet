//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga
{
    /// <summary>
    /// Represents a saga instance with its current state and data.
    /// Used by saga state machines and saga consumers for state management.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    public class SagaInstance<TData> where TData : class, new()
    {
        /// <summary>
        /// Gets or sets the unique correlation ID for this saga instance.
        /// This is the business identifier used to correlate messages.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the current state of the saga.
        /// </summary>
        public string CurrentState { get; set; } = "Initial";

        /// <summary>
        /// Gets or sets the saga data.
        /// </summary>
        public TData Data { get; set; } = new();

        /// <summary>
        /// Gets or sets the version number for optimistic concurrency.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the saga was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the saga was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the saga completed (if completed).
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the saga faulted (if faulted).
        /// </summary>
        public DateTime? FaultedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if the saga has faulted.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the list of errors that occurred during the saga.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets custom metadata for the saga.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of scheduled timeout IDs.
        /// </summary>
        public List<Guid> ScheduledTimeouts { get; set; } = new();

        /// <summary>
        /// Gets or sets the history of state transitions.
        /// </summary>
        public List<SagaStateTransition> StateHistory { get; set; } = new();

        /// <summary>
        /// Gets whether the saga is completed.
        /// </summary>
        public bool IsCompleted => CurrentState == "Completed" || CurrentState == "Final";

        /// <summary>
        /// Gets whether the saga has faulted.
        /// </summary>
        public bool IsFaulted => FaultedAt.HasValue;

        /// <summary>
        /// Gets whether the saga is active (not completed or faulted).
        /// </summary>
        public bool IsActive => !IsCompleted && !IsFaulted;

        /// <summary>
        /// Transitions the saga to a new state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        /// <param name="reason">Optional reason for the transition.</param>
        public void TransitionTo(string newState, string? reason = null)
        {
            var transition = new SagaStateTransition
            {
                FromState = CurrentState,
                ToState = newState,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            };

            StateHistory.Add(transition);
            CurrentState = newState;
            LastUpdatedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Marks the saga as completed.
        /// </summary>
        public void Complete()
        {
            TransitionTo("Completed", "Saga completed successfully");
            CompletedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the saga as faulted.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public void Fault(string errorMessage)
        {
            var previousState = CurrentState;
            TransitionTo("Faulted", errorMessage);
            FaultedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            Errors.Add($"[{DateTime.UtcNow:O}] State: {previousState} - {errorMessage}");
        }
    }

    /// <summary>
    /// Represents a state transition in the saga history.
    /// </summary>
    public class SagaStateTransition
    {
        /// <summary>
        /// Gets or sets the state before the transition.
        /// </summary>
        public string FromState { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state after the transition.
        /// </summary>
        public string ToState { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the transition.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the reason for the transition.
        /// </summary>
        public string? Reason { get; set; }
    }
}

