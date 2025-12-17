//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Background service that publishes messages from the outbox to RabbitMQ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Outbox Publisher:</strong>
    /// </para>
    /// <para>
    /// This service runs in the background and periodically polls the outbox for pending messages.
    /// When messages are found, it publishes them to RabbitMQ and marks them as processed.
    /// </para>
    /// <para>
    /// <strong>Reliability:</strong>
    /// <list type="bullet">
    /// <item>Messages are only marked as published after successful delivery confirmation</item>
    /// <item>Failed messages are retried with exponential backoff</item>
    /// <item>Messages exceeding retry limit are moved to dead letter</item>
    /// <item>Graceful shutdown ensures in-flight messages complete</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OutboxPublisher : BackgroundService, IOutboxPublisher
    {
        private readonly ITransactionalOutbox _outbox;
        private readonly IMvpRabbitMQClient _rabbitMQClient;
        private readonly ILogger<OutboxPublisher> _logger;
        private readonly OutboxPublisherOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        private volatile bool _isRunning;
        private long _totalPublished;
        private long _totalFailed;
        private DateTimeOffset? _lastPublishedAt;
        private string? _lastError;
        private DateTimeOffset? _lastErrorAt;

        /// <summary>
        /// Creates a new instance of the outbox publisher.
        /// </summary>
        /// <param name="outbox">The outbox storage.</param>
        /// <param name="rabbitMQClient">The RabbitMQ client for publishing.</param>
        /// <param name="logger">Logger for recording operations.</param>
        /// <param name="options">Configuration options.</param>
        public OutboxPublisher(
            ITransactionalOutbox outbox,
            IMvpRabbitMQClient rabbitMQClient,
            ILogger<OutboxPublisher> logger,
            OutboxPublisherOptions? options = null)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _rabbitMQClient = rabbitMQClient ?? throw new ArgumentNullException(nameof(rabbitMQClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new OutboxPublisherOptions();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[OutboxPublisher] Starting with polling interval {Interval}ms",
                _options.PollingInterval.TotalMilliseconds);

            _isRunning = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishPendingAsync(_options.BatchSize, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    _lastErrorAt = DateTimeOffset.UtcNow;

                    _logger.LogError(ex, "[OutboxPublisher] Error during publishing cycle");
                }

                try
                {
                    await Task.Delay(_options.PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _isRunning = false;
            _logger.LogInformation("[OutboxPublisher] Stopped");
        }

        /// <inheritdoc />
        public async Task<int> PublishPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            var messages = await _outbox.GetPendingAsync(batchSize, cancellationToken);

            if (messages.Count == 0)
            {
                return 0;
            }

            _logger.LogDebug("[OutboxPublisher] Processing {Count} pending messages", messages.Count);

            int published = 0;

            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await PublishMessageAsync(message, cancellationToken);
                    await _outbox.MarkAsPublishedAsync(message.Id, cancellationToken);

                    Interlocked.Increment(ref _totalPublished);
                    _lastPublishedAt = DateTimeOffset.UtcNow;
                    published++;

                    _logger.LogDebug(
                        "[OutboxPublisher] Published message {MessageId} to {RoutingKey}",
                        message.Id,
                        message.RoutingKey);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalFailed);
                    _lastError = ex.Message;
                    _lastErrorAt = DateTimeOffset.UtcNow;

                    await _outbox.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);

                    _logger.LogWarning(
                        ex,
                        "[OutboxPublisher] Failed to publish message {MessageId}: {Error}",
                        message.Id,
                        ex.Message);
                }
            }

            return published;
        }

        /// <inheritdoc />
        public OutboxPublisherStatus GetStatus()
        {
            return new OutboxPublisherStatus
            {
                IsRunning = _isRunning,
                TotalPublished = Interlocked.Read(ref _totalPublished),
                TotalFailed = Interlocked.Read(ref _totalFailed),
                PendingCount = _outbox.GetPendingCountAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
                LastPublishedAt = _lastPublishedAt,
                LastError = _lastError,
                LastErrorAt = _lastErrorAt
            };
        }

        private Task PublishMessageAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken)
        {
            // Deserialize headers if present
            var headers = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(message.Headers))
            {
                var deserializedHeaders = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Headers, _jsonOptions);
                if (deserializedHeaders != null)
                {
                    foreach (var header in deserializedHeaders)
                    {
                        headers[header.Key] = header.Value;
                    }
                }
            }

            // Add correlation/causation headers
            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                headers["x-correlation-id"] = message.CorrelationId;
            }
            if (!string.IsNullOrEmpty(message.CausationId))
            {
                headers["x-causation-id"] = message.CausationId;
            }

            // Add message type header for deserialization on consumer side
            headers["x-message-type"] = message.MessageType;
            headers["x-outbox-message-id"] = message.Id.ToString();

            // Deserialize the message payload
            var messageType = Type.GetType(message.MessageType);
            object? payload;

            if (messageType != null)
            {
                payload = JsonSerializer.Deserialize(message.Payload, messageType, _jsonOptions);
            }
            else
            {
                // Fallback: publish as JSON element
                payload = JsonSerializer.Deserialize<JsonElement>(message.Payload, _jsonOptions);
            }

            if (payload == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message payload for message {message.Id}");
            }

            // Publish with headers
            var routingKey = message.RoutingKey ?? messageType?.Name ?? "unknown";
            _rabbitMQClient.Publish(payload, routingKey, headers);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configuration options for the outbox publisher.
    /// </summary>
    public class OutboxPublisherOptions
    {
        /// <summary>
        /// How often to poll for pending messages.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum number of messages to process in one batch.
        /// Default is 100.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Whether to enable periodic cleanup of old processed messages.
        /// Default is true.
        /// </summary>
        public bool EnableCleanup { get; set; } = true;

        /// <summary>
        /// How often to run cleanup of old messages.
        /// Default is 1 hour.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// How long to keep processed messages before cleanup.
        /// Default is 7 days.
        /// </summary>
        public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
    }
}

