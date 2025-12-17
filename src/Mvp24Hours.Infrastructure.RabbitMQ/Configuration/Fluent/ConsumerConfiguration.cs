//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Configuration options for a message consumer.
    /// </summary>
    public class ConsumerConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent messages being processed.
        /// Default is 1 (sequential processing).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set to a higher value for parallel processing of messages.
        /// Be careful with stateful consumers or when message ordering matters.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// cfg.AddConsumer&lt;OrderConsumer&gt;(c => c.ConcurrentMessageLimit = 10);
        /// </code>
        /// </example>
        public int ConcurrentMessageLimit { get; set; } = 1;

        /// <summary>
        /// Gets or sets the prefetch count (QoS) for the consumer.
        /// Default is 16.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This controls how many messages RabbitMQ will deliver to the consumer at once.
        /// A higher value improves throughput but increases memory usage and can cause
        /// message hoarding in multi-consumer scenarios.
        /// </para>
        /// </remarks>
        public ushort PrefetchCount { get; set; } = 16;

        /// <summary>
        /// Gets or sets the number of retry attempts for failed messages.
        /// Default is 3.
        /// </summary>
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial retry delay.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// Default is true.
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the queue name for this consumer.
        /// If not set, a convention-based name is used.
        /// </summary>
        public string? QueueName { get; set; }

        /// <summary>
        /// Gets or sets the exchange name for this consumer.
        /// If not set, the default exchange is used.
        /// </summary>
        public string? Exchange { get; set; }

        /// <summary>
        /// Gets or sets the routing key for this consumer.
        /// If not set, a convention-based routing key is used.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets whether the queue is durable.
        /// Default is true.
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-delete the queue when the consumer disconnects.
        /// Default is false.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets whether the queue is exclusive to this consumer.
        /// Default is false.
        /// </summary>
        public bool Exclusive { get; set; }

        /// <summary>
        /// Gets or sets the message TTL (Time-To-Live) in milliseconds.
        /// Messages older than this will be discarded or dead-lettered.
        /// </summary>
        public int? MessageTtl { get; set; }

        /// <summary>
        /// Gets or sets the dead letter exchange name.
        /// </summary>
        public string? DeadLetterExchange { get; set; }

        /// <summary>
        /// Gets or sets the dead letter routing key.
        /// </summary>
        public string? DeadLetterRoutingKey { get; set; }

        /// <summary>
        /// Gets or sets whether to enable priority queue.
        /// Default is false.
        /// </summary>
        public bool EnablePriorityQueue { get; set; }

        /// <summary>
        /// Gets or sets the maximum priority value (1-255).
        /// Default is 10.
        /// </summary>
        public byte MaxPriority { get; set; } = 10;

        /// <summary>
        /// Gets or sets the consumer tag.
        /// If not set, RabbitMQ will generate one.
        /// </summary>
        public string? ConsumerTag { get; set; }

        /// <summary>
        /// Gets or sets whether to requeue messages on failure.
        /// Default is false (send to DLQ instead).
        /// </summary>
        public bool RequeueOnFailure { get; set; }

        /// <summary>
        /// Gets or sets a custom timeout for message processing.
        /// If not set, no timeout is applied.
        /// </summary>
        public TimeSpan? ProcessingTimeout { get; set; }
    }
}

