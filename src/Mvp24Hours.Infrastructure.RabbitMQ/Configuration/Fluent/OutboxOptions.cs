//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Configuration options for the outbox pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The outbox pattern ensures reliable message publishing by storing
    /// messages in a transactional outbox before publishing to the broker.
    /// This guarantees at-least-once delivery semantics.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// cfg.UseInMemoryOutbox(opts =>
    /// {
    ///     opts.PublishInterval = TimeSpan.FromSeconds(5);
    ///     opts.BatchSize = 100;
    ///     opts.MaxRetries = 5;
    /// });
    /// </code>
    /// </example>
    public class OutboxOptions
    {
        /// <summary>
        /// Gets or sets the interval between outbox publish attempts.
        /// Default is 1 second.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lower values provide faster message delivery but increase
        /// database/storage load. Higher values reduce load but increase latency.
        /// </para>
        /// </remarks>
        public TimeSpan PublishInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum number of messages to publish per batch.
        /// Default is 100.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Larger batch sizes can improve throughput but may increase
        /// memory usage and processing time per batch.
        /// </para>
        /// </remarks>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed messages.
        /// Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retry attempts.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum delay between retry attempts.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the time to keep processed messages in the outbox.
        /// Default is 24 hours.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Keeping processed messages allows for auditing and debugging.
        /// Set to TimeSpan.Zero to delete messages immediately after publishing.
        /// </para>
        /// </remarks>
        public TimeSpan ProcessedMessageRetention { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the time to keep failed messages in the outbox.
        /// Default is 7 days.
        /// </summary>
        public TimeSpan FailedMessageRetention { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets whether to enable message deduplication in the outbox.
        /// Default is true.
        /// </summary>
        public bool EnableDeduplication { get; set; } = true;

        /// <summary>
        /// Gets or sets the deduplication window.
        /// Default is 1 hour.
        /// </summary>
        public TimeSpan DeduplicationWindow { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to enable message ordering (FIFO).
        /// Default is false.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, messages are published in the order they were added to the outbox.
        /// This may reduce throughput but guarantees message ordering.
        /// </para>
        /// </remarks>
        public bool EnableOrdering { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout for outbox operations.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable cleanup of old messages.
        /// Default is true.
        /// </summary>
        public bool EnableCleanup { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval between cleanup operations.
        /// Default is 1 hour.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to include message headers in the outbox.
        /// Default is true.
        /// </summary>
        public bool IncludeHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to log detailed outbox operations.
        /// Default is false.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum message size in bytes.
        /// Default is 1MB (1,048,576 bytes).
        /// </summary>
        public int MaxMessageSize { get; set; } = 1_048_576;

        /// <summary>
        /// Gets or sets whether to compress large messages.
        /// Default is false.
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// Gets or sets the compression threshold in bytes.
        /// Messages larger than this will be compressed.
        /// Default is 10KB (10,240 bytes).
        /// </summary>
        public int CompressionThreshold { get; set; } = 10_240;

        /// <summary>
        /// Creates default outbox options.
        /// </summary>
        public static OutboxOptions Default => new();

        /// <summary>
        /// Creates outbox options optimized for high throughput.
        /// </summary>
        public static OutboxOptions HighThroughput => new()
        {
            PublishInterval = TimeSpan.FromMilliseconds(100),
            BatchSize = 500,
            MaxRetries = 5,
            EnableOrdering = false,
            EnableDeduplication = false
        };

        /// <summary>
        /// Creates outbox options optimized for reliability.
        /// </summary>
        public static OutboxOptions HighReliability => new()
        {
            PublishInterval = TimeSpan.FromSeconds(5),
            BatchSize = 50,
            MaxRetries = 10,
            EnableOrdering = true,
            EnableDeduplication = true,
            ProcessedMessageRetention = TimeSpan.FromDays(7),
            FailedMessageRetention = TimeSpan.FromDays(30)
        };

        /// <summary>
        /// Creates outbox options optimized for low latency.
        /// </summary>
        public static OutboxOptions LowLatency => new()
        {
            PublishInterval = TimeSpan.FromMilliseconds(50),
            BatchSize = 10,
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            UseExponentialBackoff = false
        };
    }
}

