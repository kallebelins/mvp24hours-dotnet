//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// Represents a scheduled message to be published at a specific time.
    /// </summary>
    public class ScheduledMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the scheduled message.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the serialized message payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full type name of the message for deserialization.
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the routing key for the message.
        /// </summary>
        public string RoutingKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exchange name to publish to.
        /// </summary>
        public string? Exchange { get; set; }

        /// <summary>
        /// Gets or sets the scheduled time for message delivery.
        /// </summary>
        public DateTimeOffset ScheduledTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the message was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the time when the message was last processed.
        /// </summary>
        public DateTimeOffset? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the current status of the scheduled message.
        /// </summary>
        public ScheduledMessageStatus Status { get; set; } = ScheduledMessageStatus.Pending;

        /// <summary>
        /// Gets or sets the number of delivery attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the last error message if delivery failed.
        /// </summary>
        public string? LastError { get; set; }

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

        /// <summary>
        /// Gets or sets the recurring schedule configuration (CRON or interval).
        /// </summary>
        public RecurringSchedule? RecurringSchedule { get; set; }

        /// <summary>
        /// Gets or sets the next execution time for recurring messages.
        /// </summary>
        public DateTimeOffset? NextExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the total number of executions for recurring messages.
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// Gets whether this is a recurring message.
        /// </summary>
        public bool IsRecurring => RecurringSchedule != null;
    }

    /// <summary>
    /// Represents the status of a scheduled message.
    /// </summary>
    public enum ScheduledMessageStatus
    {
        /// <summary>
        /// Message is waiting to be delivered.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Message is currently being processed.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Message was successfully delivered.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Message delivery failed after all retries.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Message was cancelled by user.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Recurring message is active.
        /// </summary>
        Active = 5,

        /// <summary>
        /// Recurring message was paused.
        /// </summary>
        Paused = 6
    }

    /// <summary>
    /// Represents a recurring schedule configuration.
    /// </summary>
    public class RecurringSchedule
    {
        /// <summary>
        /// Gets or sets the schedule type.
        /// </summary>
        public RecurringScheduleType Type { get; set; }

        /// <summary>
        /// Gets or sets the interval for interval-based schedules.
        /// </summary>
        public TimeSpan? Interval { get; set; }

        /// <summary>
        /// Gets or sets the CRON expression for CRON-based schedules.
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of executions (null for unlimited).
        /// </summary>
        public int? MaxExecutions { get; set; }

        /// <summary>
        /// Gets or sets the end date for the recurring schedule.
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the timezone for the schedule. Default is UTC.
        /// </summary>
        public string TimeZone { get; set; } = "UTC";
    }

    /// <summary>
    /// Represents the type of recurring schedule.
    /// </summary>
    public enum RecurringScheduleType
    {
        /// <summary>
        /// Fixed interval between executions.
        /// </summary>
        Interval = 0,

        /// <summary>
        /// CRON expression-based schedule.
        /// </summary>
        Cron = 1
    }
}

