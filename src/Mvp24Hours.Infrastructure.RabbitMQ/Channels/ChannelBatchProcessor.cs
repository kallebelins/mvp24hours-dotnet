//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Infrastructure.Channels;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Channels;

/// <summary>
/// Enhanced batch processor using System.Threading.Channels for high-performance
/// producer/consumer pattern with backpressure support.
/// </summary>
/// <typeparam name="TMessage">The type of messages being processed.</typeparam>
/// <remarks>
/// <para>
/// This implementation replaces ConcurrentQueue-based batch processing with
/// Channel-based processing, providing:
/// </para>
/// <list type="bullet">
/// <item>Backpressure when the batch queue is full</item>
/// <item>Efficient async/await patterns</item>
/// <item>Better memory management</item>
/// <item>Native support for cancellation</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var processor = new ChannelBatchProcessor&lt;Order&gt;(
///     options,
///     serviceProvider,
///     serializer,
///     logger);
/// 
/// // Messages are added as they arrive from RabbitMQ
/// await processor.AddMessageAsync(eventArgs);
/// 
/// // Batch processing happens automatically when:
/// // - Batch reaches MaxBatchSize
/// // - Batch timeout expires
/// // - Flush is called
/// </code>
/// </example>
public sealed class ChannelBatchProcessor<TMessage> : IAsyncDisposable where TMessage : class
{
    private readonly BatchConsumerOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ILogger<ChannelBatchProcessor<TMessage>> _logger;
    private readonly IMvpRabbitMQClient? _rabbitMQClient;
    private readonly IModel? _channel;

    private readonly Channel<BatchMessageItem<TMessage>> _messageChannel;
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;

    private string? _queueName;
    private string? _exchange;
    private string? _consumerTag;
    private bool _disposed;

    /// <summary>
    /// Creates a new channel-based batch processor.
    /// </summary>
    public ChannelBatchProcessor(
        BatchConsumerOptions options,
        IServiceProvider serviceProvider,
        IMessageSerializer messageSerializer,
        ILogger<ChannelBatchProcessor<TMessage>> logger,
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

        // Create bounded channel for backpressure
        _messageChannel = System.Threading.Channels.Channel.CreateBounded<BatchMessageItem<TMessage>>(
            new BoundedChannelOptions(_options.MaxBatchSize * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true, // Only one batch processor
                SingleWriter = false, // Multiple RabbitMQ consumers can write
                AllowSynchronousContinuations = false
            });
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
    /// Starts the background batch processing task.
    /// </summary>
    public void Start()
    {
        if (_processingTask != null) return;

        _processingTask = Task.Run(ProcessBatchesAsync, _cts.Token);
        _logger.LogInformation(
            "Channel batch processor started for {MessageType}",
            typeof(TMessage).Name);
    }

    /// <summary>
    /// Adds a message to the batch channel.
    /// Will block if the channel is full (backpressure).
    /// </summary>
    public async ValueTask AddMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        TMessage? message;
        try
        {
            message = _messageSerializer.Deserialize<TMessage>(eventArgs.Body.ToArray());
            if (message == null)
            {
                _logger.LogWarning(
                    "Failed to deserialize message with delivery tag {DeliveryTag}",
                    eventArgs.DeliveryTag);
                _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: _options.RequeueOnFailure);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deserializing message with delivery tag {DeliveryTag}",
                eventArgs.DeliveryTag);
            _channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var item = new BatchMessageItem<TMessage>(message, eventArgs);

        // This will block (backpressure) if the channel is full
        await _messageChannel.Writer.WriteAsync(item, cancellationToken);

        _logger.LogDebug(
            "Message {MessageId} added to channel. Current count: {Count}",
            item.MessageId,
            _messageChannel.Reader.Count);
    }

    /// <summary>
    /// Forces processing of all pending messages in the channel.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _messageChannel.Writer.Complete();

        if (_processingTask != null)
        {
            await _processingTask;
        }
    }

    /// <summary>
    /// Background task that processes batches from the channel.
    /// </summary>
    private async Task ProcessBatchesAsync()
    {
        try
        {
            var reader = _messageChannel.Reader;

            await foreach (var batch in ReadBatchesAsync(_cts.Token))
            {
                if (batch.Count == 0) continue;

                await ProcessBatchAsync(batch, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Batch processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch processing loop");
        }
    }

    /// <summary>
    /// Reads messages in batches from the channel with timeout support.
    /// </summary>
    private async IAsyncEnumerable<List<BatchMessageItem<TMessage>>> ReadBatchesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = _messageChannel.Reader;
        var batch = new List<BatchMessageItem<TMessage>>(_options.MaxBatchSize);
        var shouldBreak = false;
        List<BatchMessageItem<TMessage>>? pendingBatch = null;

        while (!cancellationToken.IsCancellationRequested && !shouldBreak)
        {
            batch.Clear();
            var channelCompleted = false;
            var timedOut = false;

            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                timeoutCts.CancelAfter(_options.BatchTimeout);

                while (batch.Count < _options.MaxBatchSize)
                {
                    // Try to read without blocking first
                    if (reader.TryRead(out var item))
                    {
                        batch.Add(item);
                        continue;
                    }

                    // Wait for data with timeout
                    if (await reader.WaitToReadAsync(timeoutCts.Token))
                    {
                        if (reader.TryRead(out item))
                        {
                            batch.Add(item);
                        }
                    }
                    else
                    {
                        // Channel completed
                        channelCompleted = true;
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout reached
                timedOut = true;
            }
            finally
            {
                timeoutCts.Dispose();
            }

            // Handle channel completion
            if (channelCompleted)
            {
                if (batch.Count > 0)
                {
                    pendingBatch = new List<BatchMessageItem<TMessage>>(batch);
                }
                shouldBreak = true;
            }
            // Handle batch full
            else if (batch.Count >= _options.MaxBatchSize)
            {
                pendingBatch = new List<BatchMessageItem<TMessage>>(batch);
            }
            // Handle timeout - yield partial batch if it meets minimum size
            else if (timedOut && batch.Count >= _options.MinBatchSize)
            {
                pendingBatch = new List<BatchMessageItem<TMessage>>(batch);
            }

            // Yield outside try-catch
            if (pendingBatch != null)
            {
                yield return pendingBatch;
                pendingBatch = null;
            }
        }

        // Yield any remaining messages
        batch.Clear();
        while (reader.TryRead(out var item))
        {
            batch.Add(item);
            if (batch.Count >= _options.MaxBatchSize)
            {
                yield return batch;
                batch = new List<BatchMessageItem<TMessage>>(_options.MaxBatchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    /// <summary>
    /// Processes a batch of messages.
    /// </summary>
    private async Task ProcessBatchAsync(
        List<BatchMessageItem<TMessage>> messages,
        CancellationToken cancellationToken)
    {
        await _batchSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation(
                "Processing batch of {BatchSize} messages",
                messages.Count);

            var batchContext = new BatchConsumeContext<TMessage>(
                messages,
                _serviceProvider,
                _rabbitMQClient,
                _queueName,
                _exchange,
                _consumerTag,
                DateTimeOffset.UtcNow,
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
            _logger.LogWarning(
                "No batch consumer registered for message type {MessageType}",
                typeof(TMessage).Name);
            return null;
        }

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

        if (results == null)
        {
            // No explicit results - ack all
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
            return;
        }

        // Process individual results
        var resultsDict = new Dictionary<ulong, IBatchMessageResult>();
        foreach (var r in results)
        {
            resultsDict[r.DeliveryTag] = r;
        }

        foreach (var msg in messages)
        {
            try
            {
                if (resultsDict.TryGetValue(msg.DeliveryTag, out var result))
                {
                    if (result.Success)
                        _channel.BasicAck(msg.DeliveryTag, multiple: false);
                    else
                        _channel.BasicNack(msg.DeliveryTag, multiple: false, requeue: result.Requeue);
                }
                else
                {
                    _channel.BasicAck(msg.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging message {DeliveryTag}", msg.DeliveryTag);
            }
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _messageChannel.Writer.TryComplete();
        _cts.Cancel();

        if (_processingTask != null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _batchSemaphore.Dispose();
        _cts.Dispose();

        // Nack remaining messages
        while (_messageChannel.Reader.TryRead(out var item))
        {
            try
            {
                _channel?.BasicNack(item.DeliveryTag, multiple: false, requeue: true);
            }
            catch
            {
                // Ignore during disposal
            }
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Options for the channel batch processor.
/// </summary>
public class ChannelBatchProcessorOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages in a batch.
    /// Default is 100.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum number of messages required to process a batch.
    /// Default is 1.
    /// </summary>
    public int MinBatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum time to wait for a full batch.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the capacity of the internal channel.
    /// Default is MaxBatchSize * 2 for buffering.
    /// </summary>
    public int ChannelCapacity { get; set; } = 200;

    /// <summary>
    /// Gets or sets whether to requeue messages on failure.
    /// Default is true.
    /// </summary>
    public bool RequeueOnFailure { get; set; } = true;
}

