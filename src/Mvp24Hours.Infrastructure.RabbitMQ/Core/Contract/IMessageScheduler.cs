//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Interface for scheduling messages to be published at a later time.
    /// </summary>
    public interface IMessageScheduler
    {
        /// <summary>
        /// Schedules a message to be published at a specific time.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="scheduledTime">The time when the message should be published.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key for the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled message ID.</returns>
        Task<Guid> ScheduleMessageAsync<T>(
            DateTime scheduledTime,
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Schedules a message to be published at a specific time with additional options.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="scheduledTime">The time when the message should be published.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">Additional scheduling options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled message ID.</returns>
        Task<Guid> ScheduleMessageAsync<T>(
            DateTimeOffset scheduledTime,
            T message,
            ScheduleMessageOptions? options = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Schedules a message to be published after a delay.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="delay">The delay before the message should be published.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key for the message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled message ID.</returns>
        Task<Guid> ScheduleMessageAsync<T>(
            TimeSpan delay,
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Schedules a recurring message to be published periodically.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="interval">The interval between message publications.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key for the message.</param>
        /// <param name="startTime">Optional start time. Defaults to now.</param>
        /// <param name="maxExecutions">Optional maximum number of executions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recurring message ID.</returns>
        Task<Guid> ScheduleRecurringMessageAsync<T>(
            TimeSpan interval,
            T message,
            string? routingKey = null,
            DateTimeOffset? startTime = null,
            int? maxExecutions = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Schedules a recurring message using a CRON expression.
        /// </summary>
        /// <typeparam name="T">The type of message.</typeparam>
        /// <param name="cronExpression">The CRON expression defining the schedule.</param>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">The routing key for the message.</param>
        /// <param name="timeZone">The timezone for the CRON expression. Default is UTC.</param>
        /// <param name="maxExecutions">Optional maximum number of executions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recurring message ID.</returns>
        Task<Guid> ScheduleRecurringMessageAsync<T>(
            string cronExpression,
            T message,
            string? routingKey = null,
            string timeZone = "UTC",
            int? maxExecutions = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Cancels a scheduled message.
        /// </summary>
        /// <param name="scheduledMessageId">The ID of the scheduled message to cancel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was cancelled; false if not found or already processed.</returns>
        Task<bool> CancelScheduledMessageAsync(
            Guid scheduledMessageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses a recurring message.
        /// </summary>
        /// <param name="recurringMessageId">The ID of the recurring message to pause.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was paused; false if not found or not recurring.</returns>
        Task<bool> PauseRecurringMessageAsync(
            Guid recurringMessageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes a paused recurring message.
        /// </summary>
        /// <param name="recurringMessageId">The ID of the recurring message to resume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the message was resumed; false if not found or not paused.</returns>
        Task<bool> ResumeRecurringMessageAsync(
            Guid recurringMessageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a scheduled message by ID.
        /// </summary>
        /// <param name="scheduledMessageId">The ID of the scheduled message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The scheduled message or null if not found.</returns>
        Task<ScheduledMessage?> GetScheduledMessageAsync(
            Guid scheduledMessageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending scheduled messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of pending scheduled messages.</returns>
        Task<IEnumerable<ScheduledMessage>> GetPendingMessagesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active recurring messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of active recurring messages.</returns>
        Task<IEnumerable<ScheduledMessage>> GetActiveRecurringMessagesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets messages due for delivery.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of messages ready for delivery.</returns>
        Task<IEnumerable<ScheduledMessage>> GetDueMessagesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for scheduling a message.
    /// </summary>
    public class ScheduleMessageOptions
    {
        /// <summary>
        /// Gets or sets the routing key for the message.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the exchange to publish to.
        /// </summary>
        public string? Exchange { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets optional headers to include with the message.
        /// </summary>
        public Dictionary<string, object>? Headers { get; set; }

        /// <summary>
        /// Gets or sets the message priority (0-255).
        /// </summary>
        public byte? Priority { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live for the message in milliseconds.
        /// </summary>
        public int? TtlMilliseconds { get; set; }
    }
}

