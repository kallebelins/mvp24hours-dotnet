//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Processes batch consumers by collecting messages and dispatching to the batch consumer when ready.
    /// </summary>
    /// <typeparam name="TMessage">The type of messages being processed.</typeparam>
    public class BatchConsumerProcessor<TMessage> : IDisposable where TMessage : class
    {
        private readonly BatchConsumerOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageSerializer _messageSerializer;
        private readonly ILogger<BatchConsumerProcessor<TMessage>> _logger;
        private readonly IMvpRabbitMQClient? _rabbitMQClient;
        private readonly IModel? _channel;
        
        private readonly ConcurrentQueue<BatchMessageItem<TMessage>> _messageQueue;
        private readonly SemaphoreSlim _batchSemaphore;
        private readonly Timer _batchTimer;
        private readonly object _batchLock = new();
        
        private DateTimeOffset _batchStartTime;
        private bool _isDisposed;
        private string? _queueName;
        private string? _exchange;
        private string? _consumerTag;

        /// <summary>
        /// Creates a new batch consumer processor.
        /// </summary>
        public BatchConsumerProcessor(
            BatchConsumerOptions options,
            IServiceProvider serviceProvider,
            IMessageSerializer messageSerializer,
            ILogger<BatchConsumerProcessor<TMessage>> logger,
            IMvpRabbitMQClient? rabbitMQClient = null,
            IModel? channel = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rabbitMQClient = rabbitMQClient;
            _channel = channel;

            _options.Validate();

            _messageQueue = new ConcurrentQueue<BatchMessageItem<TMessage>>();
            _batchSemaphore = new SemaphoreSlim(1, 1);
            _batchTimer = new Timer(OnBatchTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _batchStartTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Sets the queue metadata for this processor.
        /// </summary>
        public void SetQueueMetadata(string? queueName, string? exchange, string? consumerTag)
        {
            _queueName = queueName;
            _exchange = exchange;
            _consumerTag = consumerTag;
        }

        /// <summary>
        /// Adds a message to the batch queue.
        /// </summary>
        /// <param name="eventArgs">The delivery event args from RabbitMQ.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task AddMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BatchConsumerProcessor<TMessage>));

            TMessage? message;
            try
            {
                message = _messageSerializer.Deserialize<TMessage>(eventArgs.Body.ToArray());
                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize message with delivery tag {DeliveryTag}", eventArgs.DeliveryTag);
                    _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: _options.RequeueOnFailure);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing message with delivery tag {DeliveryTag}", eventArgs.DeliveryTag);
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var item = new BatchMessageItem<TMessage>(message, eventArgs);
            _messageQueue.Enqueue(item);

            _logger.LogDebug("Message {MessageId} added to batch. Current batch size: {BatchSize}",
                item.MessageId, _messageQueue.Count);

            // Start the batch timer if this is the first message
            lock (_batchLock)
            {
                if (_messageQueue.Count == 1)
                {
                    _batchStartTime = DateTimeOffset.UtcNow;
                    _batchTimer.Change(_options.BatchTimeout, Timeout.InfiniteTimeSpan);
                }
            }

            // Check if we should process the batch
            if (_messageQueue.Count >= _options.MaxBatchSize)
            {
                await ProcessBatchAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Forces processing of the current batch regardless of size.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_messageQueue.Count > 0)
            {
                await ProcessBatchAsync(cancellationToken);
            }
        }

        private void OnBatchTimerElapsed(object? state)
        {
            try
            {
                // Only process if we have at least the minimum batch size
                if (_messageQueue.Count >= _options.MinBatchSize)
                {
                    _ = ProcessBatchAsync(CancellationToken.None);
                }
                else if (_messageQueue.Count > 0)
                {
                    // Keep waiting, but check again after message wait timeout
                    _batchTimer.Change(_options.MessageWaitTimeout, Timeout.InfiniteTimeSpan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch timer callback");
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            if (!await _batchSemaphore.WaitAsync(0, cancellationToken))
            {
                // Another batch is being processed
                return;
            }

            try
            {
                // Stop the timer while processing
                _batchTimer.Change(Timeout.Infinite, Timeout.Infinite);

                var messages = new List<BatchMessageItem<TMessage>>();
                while (messages.Count < _options.MaxBatchSize && _messageQueue.TryDequeue(out var item))
                {
                    messages.Add(item);
                }

                if (messages.Count == 0)
                    return;

                _logger.LogInformation("Processing batch of {BatchSize} messages", messages.Count);

                var batchContext = new BatchConsumeContext<TMessage>(
                    messages,
                    _serviceProvider,
                    _rabbitMQClient,
                    _queueName,
                    _exchange,
                    _consumerTag,
                    _batchStartTime,
                    cancellationToken);

                IEnumerable<IBatchMessageResult>? results = null;
                Exception? processingException = null;

                try
                {
                    results = await InvokeConsumerAsync(batchContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    processingException = ex;
                    _logger.LogError(ex, "Error processing batch {BatchId}", batchContext.BatchId);
                }

                await AcknowledgeMessagesAsync(messages, results, processingException);

                // Reset for next batch
                _batchStartTime = DateTimeOffset.UtcNow;

                // If there are more messages, restart the timer
                if (_messageQueue.Count > 0)
                {
                    _batchTimer.Change(_options.BatchTimeout, Timeout.InfiniteTimeSpan);
                }
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }

        private async Task<IEnumerable<IBatchMessageResult>?> InvokeConsumerAsync(
            BatchConsumeContext<TMessage> context,
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var consumer = scope.ServiceProvider.GetService<IBatchConsumer<TMessage>>();

            if (consumer == null)
            {
                _logger.LogWarning("No batch consumer registered for message type {MessageType}", typeof(TMessage).Name);
                return null;
            }

            if (_options.EnableParallelProcessing)
            {
                return await InvokeConsumerWithParallelismAsync(consumer, context, cancellationToken);
            }

            return await consumer.ConsumeAsync(context, cancellationToken);
        }

        private async Task<IEnumerable<IBatchMessageResult>?> InvokeConsumerWithParallelismAsync(
            IBatchConsumer<TMessage> consumer,
            BatchConsumeContext<TMessage> context,
            CancellationToken cancellationToken)
        {
            // For parallel processing, we need to handle this differently
            // The consumer is still called once with the full batch, but internally
            // the consumer can process messages in parallel
            return await consumer.ConsumeAsync(context, cancellationToken);
        }

        private async Task AcknowledgeMessagesAsync(
            List<BatchMessageItem<TMessage>> messages,
            IEnumerable<IBatchMessageResult>? results,
            Exception? processingException)
        {
            if (_channel == null)
            {
                _logger.LogWarning("Channel is not available for acknowledgment");
                return;
            }

            if (processingException != null)
            {
                // All messages failed due to exception
                foreach (var msg in messages)
                {
                    try
                    {
                        _channel.BasicNack(msg.DeliveryTag, multiple: false, requeue: _options.RequeueOnFailure);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error nacking message {DeliveryTag}", msg.DeliveryTag);
                    }
                }
                return;
            }

            if (results == null || !results.Any())
            {
                // No explicit results - ack all messages
                if (_options.UseBatchAcknowledgment)
                {
                    var lastDeliveryTag = messages.Max(m => m.DeliveryTag);
                    try
                    {
                        _channel.BasicAck(lastDeliveryTag, multiple: true);
                        _logger.LogDebug("Batch acknowledged with delivery tag {DeliveryTag}", lastDeliveryTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error batch acking up to delivery tag {DeliveryTag}", lastDeliveryTag);
                    }
                }
                else
                {
                    foreach (var msg in messages)
                    {
                        try
                        {
                            _channel.BasicAck(msg.DeliveryTag, multiple: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error acking message {DeliveryTag}", msg.DeliveryTag);
                        }
                    }
                }
                return;
            }

            // Process individual results for partial acknowledgment
            var resultsList = results.ToList();
            var resultsByDeliveryTag = resultsList.ToDictionary(r => r.DeliveryTag);

            foreach (var msg in messages)
            {
                try
                {
                    if (resultsByDeliveryTag.TryGetValue(msg.DeliveryTag, out var result))
                    {
                        if (result.Success)
                        {
                            _channel.BasicAck(msg.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            _channel.BasicNack(msg.DeliveryTag, multiple: false, requeue: result.Requeue);
                            if (!string.IsNullOrEmpty(result.ErrorMessage))
                            {
                                _logger.LogWarning("Message {DeliveryTag} nacked: {ErrorMessage}",
                                    msg.DeliveryTag, result.ErrorMessage);
                            }
                        }
                    }
                    else
                    {
                        // No explicit result for this message - assume success
                        _channel.BasicAck(msg.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error acknowledging message {DeliveryTag}", msg.DeliveryTag);
                }
            }
        }

        /// <summary>
        /// Disposes the batch consumer processor.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _batchTimer.Dispose();
            _batchSemaphore.Dispose();

            // Nack any remaining messages
            while (_messageQueue.TryDequeue(out var item))
            {
                try
                {
                    _channel?.BasicNack(item.DeliveryTag, multiple: false, requeue: true);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}

