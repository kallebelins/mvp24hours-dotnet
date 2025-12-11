//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Messaging;

/// <summary>
/// Configuration options for Inbox and Outbox patterns.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the <see cref="InboxProcessor"/>,
/// <see cref="OutboxProcessor"/>, and related background services.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.Configure&lt;InboxOutboxOptions&gt;(options =>
/// {
///     options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
///     options.MaxRetries = 5;
///     options.BatchSize = 100;
///     options.RetryBaseDelayMilliseconds = 1000;
/// });
/// </code>
/// </example>
public sealed class InboxOutboxOptions
{
    /// <summary>
    /// The section name in configuration.
    /// </summary>
    public const string SectionName = "InboxOutbox";

    #region [ Outbox Configuration ]

    /// <summary>
    /// Gets or sets the polling interval for the outbox processor.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan OutboxPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of messages to process per batch.
    /// Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before moving to DLQ.
    /// Default is 5.
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base delay in milliseconds for exponential backoff.
    /// Default is 1000ms (1 second).
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds for exponential backoff.
    /// Default is 60000ms (1 minute).
    /// </summary>
    public int RetryMaxDelayMilliseconds { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the number of days to retain processed outbox messages.
    /// Default is 7 days.
    /// </summary>
    public int OutboxRetentionDays { get; set; } = 7;

    #endregion

    #region [ Inbox Configuration ]

    /// <summary>
    /// Gets or sets the number of days to retain inbox messages for deduplication.
    /// Default is 7 days.
    /// </summary>
    public int InboxRetentionDays { get; set; } = 7;

    #endregion

    #region [ Cleanup Configuration ]

    /// <summary>
    /// Gets or sets the interval for cleanup operations.
    /// Default is 1 hour.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup of old messages.
    /// Default is true.
    /// </summary>
    public bool EnableAutomaticCleanup { get; set; } = true;

    #endregion

    #region [ Dead Letter Queue Configuration ]

    /// <summary>
    /// Gets or sets whether to enable Dead Letter Queue.
    /// Default is true.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days to retain dead letter messages.
    /// Default is 30 days.
    /// </summary>
    public int DeadLetterRetentionDays { get; set; } = 30;

    #endregion

    #region [ Performance Configuration ]

    /// <summary>
    /// Gets or sets whether to process messages in parallel.
    /// Default is false (sequential processing).
    /// </summary>
    public bool EnableParallelProcessing { get; set; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism when parallel processing is enabled.
    /// Default is 4.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    #endregion
}


