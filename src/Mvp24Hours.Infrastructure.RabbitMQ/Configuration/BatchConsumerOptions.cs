//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for batch message consumption.
    /// </summary>
    [Serializable]
    public class BatchConsumerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of messages to include in a batch.
        /// Default is 10 messages.
        /// </summary>
        /// <remarks>
        /// Higher values increase throughput but also memory usage.
        /// Consider the message size and processing time when configuring this value.
        /// </remarks>
        public int MaxBatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minimum number of messages to wait for before processing.
        /// Default is 1 (process as soon as any message is available after timeout).
        /// </summary>
        public int MinBatchSize { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum time to wait for a batch to fill.
        /// Default is 1 second.
        /// </summary>
        /// <remarks>
        /// The batch will be processed either when MaxBatchSize is reached
        /// or when this timeout expires, whichever comes first.
        /// </remarks>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the maximum time to wait for messages before processing the batch.
        /// This is the timeout between receiving individual messages.
        /// Default is 500 milliseconds.
        /// </summary>
        public TimeSpan MessageWaitTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets whether to enable parallel processing of messages within the batch.
        /// Default is false (sequential processing).
        /// </summary>
        /// <remarks>
        /// Enable this when message processing is independent and can benefit from parallelism.
        /// Consider the order guarantees of your system when enabling this option.
        /// </remarks>
        public bool EnableParallelProcessing { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism when EnableParallelProcessing is true.
        /// Default is 0 (use processor count).
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to acknowledge all messages together as a batch (multiple=true)
        /// or individually.
        /// Default is true (batch acknowledgment).
        /// </summary>
        /// <remarks>
        /// Batch acknowledgment is more efficient but requires all messages to succeed.
        /// Individual acknowledgment allows partial batch success at the cost of more RabbitMQ calls.
        /// </remarks>
        public bool UseBatchAcknowledgment { get; set; } = true;

        /// <summary>
        /// Gets or sets whether messages should be requeued when the batch processing fails.
        /// Default is true.
        /// </summary>
        public bool RequeueOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of retry attempts for failed batch processing.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the prefetch count for the batch consumer.
        /// Should be equal to or greater than MaxBatchSize.
        /// Default is 20 (2x the default MaxBatchSize).
        /// </summary>
        public ushort PrefetchCount { get; set; } = 20;

        /// <summary>
        /// Creates default batch consumer options.
        /// </summary>
        public static BatchConsumerOptions Default => new();

        /// <summary>
        /// Creates options optimized for high throughput.
        /// </summary>
        public static BatchConsumerOptions HighThroughput => new()
        {
            MaxBatchSize = 100,
            MinBatchSize = 10,
            BatchTimeout = TimeSpan.FromSeconds(5),
            MessageWaitTimeout = TimeSpan.FromSeconds(1),
            EnableParallelProcessing = true,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            UseBatchAcknowledgment = true,
            PrefetchCount = 200
        };

        /// <summary>
        /// Creates options optimized for low latency.
        /// </summary>
        public static BatchConsumerOptions LowLatency => new()
        {
            MaxBatchSize = 5,
            MinBatchSize = 1,
            BatchTimeout = TimeSpan.FromMilliseconds(100),
            MessageWaitTimeout = TimeSpan.FromMilliseconds(50),
            EnableParallelProcessing = false,
            UseBatchAcknowledgment = false,
            PrefetchCount = 10
        };

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
        public void Validate()
        {
            if (MaxBatchSize <= 0)
                throw new InvalidOperationException("MaxBatchSize must be greater than 0.");

            if (MinBatchSize <= 0)
                throw new InvalidOperationException("MinBatchSize must be greater than 0.");

            if (MinBatchSize > MaxBatchSize)
                throw new InvalidOperationException("MinBatchSize cannot be greater than MaxBatchSize.");

            if (BatchTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("BatchTimeout must be greater than zero.");

            if (MessageWaitTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException("MessageWaitTimeout must be greater than zero.");

            if (MaxDegreeOfParallelism < 0)
                throw new InvalidOperationException("MaxDegreeOfParallelism cannot be negative.");

            if (MaxRetryAttempts < 0)
                throw new InvalidOperationException("MaxRetryAttempts cannot be negative.");

            if (PrefetchCount < MaxBatchSize)
                throw new InvalidOperationException("PrefetchCount should be at least equal to MaxBatchSize.");
        }
    }
}

