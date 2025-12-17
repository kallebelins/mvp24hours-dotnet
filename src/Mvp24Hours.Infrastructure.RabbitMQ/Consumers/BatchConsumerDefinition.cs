//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Base class for declarative batch consumer configuration.
    /// Inherit from this class to configure a batch consumer.
    /// </summary>
    /// <typeparam name="TConsumer">The batch consumer type.</typeparam>
    /// <example>
    /// <code>
    /// public class OrderBatchConsumerDefinition : BatchConsumerDefinition&lt;OrderBatchConsumer&gt;
    /// {
    ///     public OrderBatchConsumerDefinition()
    ///     {
    ///         Queue("orders-batch");
    ///         BatchSize(maxSize: 50, minSize: 5);
    ///         BatchTimeout(TimeSpan.FromSeconds(2));
    ///         EnableParallelProcessing(maxDegree: 4);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class BatchConsumerDefinition<TConsumer> : IBatchConsumerDefinition<TConsumer>
        where TConsumer : class
    {
        /// <summary>
        /// Creates a new batch consumer definition.
        /// </summary>
        protected BatchConsumerDefinition()
        {
            ConsumerType = typeof(TConsumer);

            // Auto-detect message type from IBatchConsumer<T> interface
            var consumerInterface = ConsumerType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IBatchConsumer<>));

            MessageType = consumerInterface?.GetGenericArguments().FirstOrDefault()
                ?? typeof(object);

            BatchOptions = new BatchConsumerOptions();
        }

        /// <inheritdoc />
        public Type ConsumerType { get; }

        /// <inheritdoc />
        public Type MessageType { get; }

        /// <inheritdoc />
        public virtual string? QueueName { get; protected set; }

        /// <inheritdoc />
        public virtual string? Exchange { get; protected set; }

        /// <inheritdoc />
        public virtual string? RoutingKey { get; protected set; }

        /// <inheritdoc />
        public virtual ushort? PrefetchCount { get; protected set; }

        /// <inheritdoc />
        public virtual int? ConcurrentConsumers { get; protected set; }

        /// <inheritdoc />
        public virtual int? MaxRetryCount { get; protected set; }

        /// <inheritdoc />
        public virtual bool UseDeadLetterQueue { get; protected set; } = true;

        /// <inheritdoc />
        public bool IsBatchConsumer => true;

        /// <inheritdoc />
        public BatchConsumerOptions? BatchOptions { get; protected set; }

        /// <summary>
        /// Configures the queue name for this batch consumer.
        /// </summary>
        protected void Queue(string queueName)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// Configures the exchange for this batch consumer.
        /// </summary>
        protected void ExchangeName(string exchange)
        {
            Exchange = exchange;
        }

        /// <summary>
        /// Configures the routing key for this batch consumer.
        /// </summary>
        protected void Route(string routingKey)
        {
            RoutingKey = routingKey;
        }

        /// <summary>
        /// Configures the batch size parameters.
        /// </summary>
        /// <param name="maxSize">Maximum number of messages in a batch.</param>
        /// <param name="minSize">Minimum number of messages before processing (default 1).</param>
        protected void BatchSize(int maxSize, int minSize = 1)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.MaxBatchSize = maxSize;
            BatchOptions.MinBatchSize = minSize;

            // Auto-configure prefetch count if not explicitly set
            if (!PrefetchCount.HasValue)
            {
                PrefetchCount = (ushort)(maxSize * 2);
                BatchOptions.PrefetchCount = PrefetchCount.Value;
            }
        }

        /// <summary>
        /// Configures the batch timeout (maximum time to wait for a batch to fill).
        /// </summary>
        /// <param name="timeout">The batch timeout.</param>
        protected void BatchTimeout(TimeSpan timeout)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.BatchTimeout = timeout;
        }

        /// <summary>
        /// Configures the message wait timeout (time between individual messages).
        /// </summary>
        /// <param name="timeout">The message wait timeout.</param>
        protected void MessageWaitTimeout(TimeSpan timeout)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.MessageWaitTimeout = timeout;
        }

        /// <summary>
        /// Enables parallel processing within the batch.
        /// </summary>
        /// <param name="maxDegree">Maximum degree of parallelism. 0 = use processor count.</param>
        protected void EnableParallelProcessing(int maxDegree = 0)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.EnableParallelProcessing = true;
            BatchOptions.MaxDegreeOfParallelism = maxDegree;
        }

        /// <summary>
        /// Disables batch acknowledgment in favor of individual message acknowledgment.
        /// </summary>
        protected void UseIndividualAcknowledgment()
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.UseBatchAcknowledgment = false;
        }

        /// <summary>
        /// Configures the prefetch count for this batch consumer.
        /// </summary>
        protected void Prefetch(ushort count)
        {
            PrefetchCount = count;

            if (BatchOptions != null)
                BatchOptions.PrefetchCount = count;
        }

        /// <summary>
        /// Configures the number of concurrent batch consumers.
        /// </summary>
        protected void Concurrent(int count)
        {
            ConcurrentConsumers = count;
        }

        /// <summary>
        /// Configures the maximum retry count.
        /// </summary>
        protected void Retry(int maxCount)
        {
            MaxRetryCount = maxCount;

            if (BatchOptions != null)
                BatchOptions.MaxRetryAttempts = maxCount;
        }

        /// <summary>
        /// Disables the dead letter queue for this batch consumer.
        /// </summary>
        protected void NoDeadLetter()
        {
            UseDeadLetterQueue = false;
        }

        /// <summary>
        /// Configures whether failed messages should be requeued.
        /// </summary>
        protected void RequeueOnFailure(bool requeue = true)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            BatchOptions.RequeueOnFailure = requeue;
        }

        /// <summary>
        /// Configures the batch consumer with options optimized for high throughput.
        /// </summary>
        protected void UseHighThroughputMode()
        {
            BatchOptions = BatchConsumerOptions.HighThroughput;
            PrefetchCount = BatchOptions.PrefetchCount;
        }

        /// <summary>
        /// Configures the batch consumer with options optimized for low latency.
        /// </summary>
        protected void UseLowLatencyMode()
        {
            BatchOptions = BatchConsumerOptions.LowLatency;
            PrefetchCount = BatchOptions.PrefetchCount;
        }

        /// <summary>
        /// Applies custom batch consumer options.
        /// </summary>
        protected void ConfigureBatchOptions(Action<BatchConsumerOptions> configure)
        {
            if (BatchOptions == null)
                BatchOptions = new BatchConsumerOptions();

            configure(BatchOptions);
        }
    }
}

