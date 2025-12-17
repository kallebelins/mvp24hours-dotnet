//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract
{
    /// <summary>
    /// Extended consume context for saga consumers with saga-specific operations.
    /// </summary>
    /// <typeparam name="TData">The type of saga data.</typeparam>
    /// <typeparam name="TMessage">The type of the consumed message.</typeparam>
    public interface ISagaConsumeContext<TData, out TMessage> : IConsumeContext<TMessage>
        where TData : class, new()
        where TMessage : class
    {
        /// <summary>
        /// Gets the unique identifier of the saga instance.
        /// </summary>
        Guid SagaId { get; }

        /// <summary>
        /// Gets the saga data for reading and modification.
        /// </summary>
        TData SagaData { get; }

        /// <summary>
        /// Gets the current state of the saga.
        /// </summary>
        string CurrentState { get; }

        /// <summary>
        /// Gets whether this is a newly created saga instance.
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        /// Gets whether the saga has been completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets whether the saga has been faulted.
        /// </summary>
        bool IsFaulted { get; }

        /// <summary>
        /// Gets the timestamp when the saga was created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the saga was last updated.
        /// </summary>
        DateTime LastUpdatedAt { get; }

        /// <summary>
        /// Transitions the saga to a new state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TransitionToAsync(string newState, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions the saga to a new state (generic version for typed states).
        /// </summary>
        /// <typeparam name="TState">The enum type representing saga states.</typeparam>
        /// <param name="newState">The new state to transition to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TransitionToAsync<TState>(TState newState, CancellationToken cancellationToken = default) where TState : Enum;

        /// <summary>
        /// Marks the saga as completed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CompleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the saga as faulted with an error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FaultAsync(string errorMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules a timeout message for the saga.
        /// </summary>
        /// <typeparam name="TTimeout">The type of timeout message.</typeparam>
        /// <param name="delay">The delay before the timeout is triggered.</param>
        /// <param name="timeoutMessage">The timeout message to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled timeout ID.</returns>
        Task<Guid> ScheduleTimeoutAsync<TTimeout>(TimeSpan delay, TTimeout timeoutMessage, CancellationToken cancellationToken = default) where TTimeout : class;

        /// <summary>
        /// Cancels a previously scheduled timeout.
        /// </summary>
        /// <param name="timeoutId">The ID of the timeout to cancel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the timeout was cancelled, false otherwise.</returns>
        Task<bool> CancelTimeoutAsync(Guid timeoutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Raises an event that will be handled by event handlers.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to raise.</typeparam>
        /// <param name="event">The event instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RaiseEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;

        /// <summary>
        /// Sets a metadata value for the saga.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        void SetMetadata(string key, string value);

        /// <summary>
        /// Gets a metadata value from the saga.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <returns>The metadata value or null if not found.</returns>
        string? GetMetadata(string key);
    }
}

