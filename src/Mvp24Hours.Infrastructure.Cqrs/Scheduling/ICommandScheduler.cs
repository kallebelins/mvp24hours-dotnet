//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Interface for scheduling commands for future execution.
    /// </summary>
    public interface ICommandScheduler
    {
        /// <summary>
        /// Schedules a command for execution at a specified time.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="command">The command to schedule</param>
        /// <param name="options">Scheduling options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The scheduled command entry ID</returns>
        Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            ScheduleOptions options,
            CancellationToken cancellationToken = default)
            where TCommand : class;

        /// <summary>
        /// Schedules a command for immediate execution.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="command">The command to schedule</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The scheduled command entry ID</returns>
        Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class;

        /// <summary>
        /// Schedules a command for execution after a delay.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="command">The command to schedule</param>
        /// <param name="delay">The delay before execution</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The scheduled command entry ID</returns>
        Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            TimeSpan delay,
            CancellationToken cancellationToken = default)
            where TCommand : class;

        /// <summary>
        /// Schedules a command for execution at a specific time.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="command">The command to schedule</param>
        /// <param name="scheduledAt">The time to execute (UTC)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The scheduled command entry ID</returns>
        Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            DateTime scheduledAt,
            CancellationToken cancellationToken = default)
            where TCommand : class;

        /// <summary>
        /// Cancels a scheduled command.
        /// </summary>
        /// <param name="commandId">The command entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the command was cancelled, false if not found or already processed</returns>
        Task<bool> CancelAsync(string commandId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reschedules a command to a new time.
        /// </summary>
        /// <param name="commandId">The command entry ID</param>
        /// <param name="newScheduledAt">The new scheduled time (UTC)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the command was rescheduled, false if not found or already processed</returns>
        Task<bool> RescheduleAsync(string commandId, DateTime newScheduledAt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a scheduled command.
        /// </summary>
        /// <param name="commandId">The command entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The command entry or null if not found</returns>
        Task<ScheduledCommandEntry?> GetStatusAsync(string commandId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retries a failed command.
        /// </summary>
        /// <param name="commandId">The command entry ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the command was queued for retry, false if not found or cannot retry</returns>
        Task<bool> RetryAsync(string commandId, CancellationToken cancellationToken = default);
    }
}

