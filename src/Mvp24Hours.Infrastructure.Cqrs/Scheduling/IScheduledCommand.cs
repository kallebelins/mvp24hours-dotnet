//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Marker interface for commands that can be scheduled for future execution.
    /// </summary>
    public interface IScheduledCommand : IMediatorCommand
    {
    }

    /// <summary>
    /// Marker interface for scheduled commands with a response.
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    public interface IScheduledCommand<out TResponse> : IMediatorCommand<TResponse>
    {
    }

    /// <summary>
    /// Represents the status of a scheduled command.
    /// </summary>
    public enum ScheduledCommandStatus
    {
        /// <summary>
        /// The command is pending execution.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The command is currently being processed.
        /// </summary>
        Processing = 1,

        /// <summary>
        /// The command completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// The command failed after all retry attempts.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// The command was cancelled.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// The command expired before execution.
        /// </summary>
        Expired = 5
    }

    /// <summary>
    /// Represents a scheduled command entry in the store.
    /// </summary>
    public class ScheduledCommandEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for this scheduled command.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the type name of the command.
        /// </summary>
        public string CommandType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the serialized command payload.
        /// </summary>
        public string CommandPayload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the scheduled execution time (UTC).
        /// </summary>
        public DateTime ScheduledAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the command was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the time when the command was last processed (UTC).
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when the command completed (UTC).
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the current status of the command.
        /// </summary>
        public ScheduledCommandStatus Status { get; set; } = ScheduledCommandStatus.Pending;

        /// <summary>
        /// Gets or sets the number of retry attempts made.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the expiration time (UTC). Commands not executed before this time will be marked as expired.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the priority (lower values = higher priority).
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the next retry time (UTC) for failed commands.
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Gets whether the command can be retried.
        /// </summary>
        public bool CanRetry => Status == ScheduledCommandStatus.Failed && RetryCount < MaxRetries;

        /// <summary>
        /// Gets whether the command is expired.
        /// </summary>
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

        /// <summary>
        /// Gets whether the command is ready for execution.
        /// </summary>
        public bool IsReady => Status == ScheduledCommandStatus.Pending && DateTime.UtcNow >= ScheduledAt && !IsExpired;
    }

    /// <summary>
    /// Options for scheduling a command.
    /// </summary>
    public class ScheduleOptions
    {
        /// <summary>
        /// Gets or sets when to execute the command (UTC).
        /// </summary>
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the expiration time (UTC).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the priority (lower values = higher priority).
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Gets or sets the correlation ID for tracking.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Creates options for immediate execution.
        /// </summary>
        public static ScheduleOptions Now() => new() { ScheduledAt = DateTime.UtcNow };

        /// <summary>
        /// Creates options for execution after a delay.
        /// </summary>
        /// <param name="delay">The delay before execution</param>
        public static ScheduleOptions After(TimeSpan delay) => new() { ScheduledAt = DateTime.UtcNow.Add(delay) };

        /// <summary>
        /// Creates options for execution at a specific time.
        /// </summary>
        /// <param name="scheduledAt">The time to execute (UTC)</param>
        public static ScheduleOptions At(DateTime scheduledAt) => new() { ScheduledAt = scheduledAt };

        /// <summary>
        /// Sets the maximum number of retries.
        /// </summary>
        /// <param name="maxRetries">Maximum retry attempts</param>
        public ScheduleOptions WithMaxRetries(int maxRetries)
        {
            MaxRetries = maxRetries;
            return this;
        }

        /// <summary>
        /// Sets the expiration time.
        /// </summary>
        /// <param name="expiresAt">Expiration time (UTC)</param>
        public ScheduleOptions WithExpiration(DateTime expiresAt)
        {
            ExpiresAt = expiresAt;
            return this;
        }

        /// <summary>
        /// Sets the expiration as a TTL (time to live).
        /// </summary>
        /// <param name="ttl">Time to live</param>
        public ScheduleOptions WithTtl(TimeSpan ttl)
        {
            ExpiresAt = DateTime.UtcNow.Add(ttl);
            return this;
        }

        /// <summary>
        /// Sets the priority.
        /// </summary>
        /// <param name="priority">Priority (lower = higher priority)</param>
        public ScheduleOptions WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        /// <summary>
        /// Sets the correlation ID.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        public ScheduleOptions WithCorrelationId(string correlationId)
        {
            CorrelationId = correlationId;
            return this;
        }
    }
}

