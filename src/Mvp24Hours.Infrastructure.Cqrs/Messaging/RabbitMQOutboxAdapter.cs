//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Messaging
{
    /// <summary>
    /// Adapter that bridges the CQRS IIntegrationEventOutbox with RabbitMQ transactional messaging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Integration with RabbitMQ Module:</strong>
    /// </para>
    /// <para>
    /// This adapter allows you to use the <see cref="IIntegrationEventOutbox"/> from the CQRS module
    /// as the backing store for RabbitMQ transactional messaging. This provides a unified outbox
    /// pattern across both CQRS integration events and RabbitMQ messages.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Single outbox table for all integration events</item>
    /// <item>Consistent retry and dead-letter handling</item>
    /// <item>Reuse existing outbox implementations (EF Core, MongoDB, etc.)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RabbitMQOutboxAdapter : IRabbitMQOutbox
    {
        private readonly IIntegrationEventOutbox _cqrsOutbox;
        private readonly ILogger<RabbitMQOutboxAdapter>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Creates a new instance of the RabbitMQ outbox adapter.
        /// </summary>
        /// <param name="cqrsOutbox">The CQRS integration event outbox.</param>
        /// <param name="logger">Optional logger for recording operations.</param>
        public RabbitMQOutboxAdapter(
            IIntegrationEventOutbox cqrsOutbox,
            ILogger<RabbitMQOutboxAdapter>? logger = null)
        {
            _cqrsOutbox = cqrsOutbox ?? throw new ArgumentNullException(nameof(cqrsOutbox));
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public async Task AddAsync(RabbitMQOutboxMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var integrationEvent = new RabbitMQOutboxIntegrationEvent
            {
                Id = message.Id,
                OccurredOn = message.CreatedAt,
                CorrelationId = message.CorrelationId,
                MessageType = message.MessageType,
                Payload = message.Payload,
                RoutingKey = message.RoutingKey,
                Exchange = message.Exchange,
                Headers = message.Headers,
                CausationId = message.CausationId,
                Priority = message.Priority,
                TenantId = message.TenantId,
                ScheduledAt = message.ScheduledAt
            };

            await _cqrsOutbox.AddAsync(integrationEvent, cancellationToken);

            _logger?.LogDebug(
                "[RabbitMQOutboxAdapter] Added message {MessageId} to CQRS outbox",
                message.Id);
        }

        /// <inheritdoc />
        public async Task AddRangeAsync(IEnumerable<RabbitMQOutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            foreach (var message in messages)
            {
                await AddAsync(message, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<RabbitMQOutboxMessage>> GetPendingAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var cqrsMessages = await _cqrsOutbox.GetPendingAsync(batchSize, cancellationToken);

            var result = new List<RabbitMQOutboxMessage>();

            foreach (var cqrsMessage in cqrsMessages)
            {
                // Only process RabbitMQ outbox messages
                if (!cqrsMessage.EventType.Contains(nameof(RabbitMQOutboxIntegrationEvent)))
                {
                    continue;
                }

                try
                {
                    var integrationEvent = JsonSerializer.Deserialize<RabbitMQOutboxIntegrationEvent>(
                        cqrsMessage.Payload, _jsonOptions);

                    if (integrationEvent != null)
                    {
                        result.Add(new RabbitMQOutboxMessage
                        {
                            Id = cqrsMessage.Id,
                            MessageType = integrationEvent.MessageType,
                            Payload = integrationEvent.Payload,
                            RoutingKey = integrationEvent.RoutingKey,
                            Exchange = integrationEvent.Exchange,
                            Headers = integrationEvent.Headers,
                            CorrelationId = integrationEvent.CorrelationId,
                            CausationId = integrationEvent.CausationId,
                            CreatedAt = cqrsMessage.CreatedAt,
                            Status = MapStatus(cqrsMessage.Status),
                            RetryCount = cqrsMessage.RetryCount,
                            LastError = cqrsMessage.Error,
                            Priority = integrationEvent.Priority,
                            TenantId = integrationEvent.TenantId,
                            ScheduledAt = integrationEvent.ScheduledAt
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        ex,
                        "[RabbitMQOutboxAdapter] Failed to deserialize message {MessageId}",
                        cqrsMessage.Id);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            await _cqrsOutbox.MarkAsPublishedAsync(messageId, cancellationToken);

            _logger?.LogDebug(
                "[RabbitMQOutboxAdapter] Marked message {MessageId} as published",
                messageId);
        }

        /// <inheritdoc />
        public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            await _cqrsOutbox.MarkAsFailedAsync(messageId, error, cancellationToken);

            _logger?.LogDebug(
                "[RabbitMQOutboxAdapter] Marked message {MessageId} as failed: {Error}",
                messageId,
                error);
        }

        /// <inheritdoc />
        public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _cqrsOutbox.GetPendingAsync(int.MaxValue, cancellationToken);
            return pending.Count(m => m.EventType.Contains(nameof(RabbitMQOutboxIntegrationEvent)));
        }

        /// <inheritdoc />
        public async Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            return await _cqrsOutbox.CleanupAsync(olderThan, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<RabbitMQOutboxMessage>> GetDeadLettersAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var pending = await GetPendingAsync(batchSize * 10, cancellationToken);
            return pending
                .Where(m => m.Status == RabbitMQOutboxStatus.DeadLetter)
                .Take(batchSize)
                .ToList();
        }

        private static RabbitMQOutboxStatus MapStatus(OutboxMessageStatus status)
        {
            return status switch
            {
                OutboxMessageStatus.Pending => RabbitMQOutboxStatus.Pending,
                OutboxMessageStatus.Published => RabbitMQOutboxStatus.Published,
                OutboxMessageStatus.Failed => RabbitMQOutboxStatus.Failed,
                OutboxMessageStatus.DeadLetter => RabbitMQOutboxStatus.DeadLetter,
                _ => RabbitMQOutboxStatus.Pending
            };
        }
    }

    /// <summary>
    /// Interface for RabbitMQ-specific outbox storage.
    /// </summary>
    public interface IRabbitMQOutbox
    {
        /// <summary>
        /// Adds a message to the outbox.
        /// </summary>
        Task AddAsync(RabbitMQOutboxMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple messages to the outbox.
        /// </summary>
        Task AddRangeAsync(IEnumerable<RabbitMQOutboxMessage> messages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets pending messages ready for publishing.
        /// </summary>
        Task<IReadOnlyList<RabbitMQOutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as published.
        /// </summary>
        Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a message as failed.
        /// </summary>
        Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of pending messages.
        /// </summary>
        Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old processed messages.
        /// </summary>
        Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dead letter messages.
        /// </summary>
        Task<IReadOnlyList<RabbitMQOutboxMessage>> GetDeadLettersAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a RabbitMQ message in the outbox.
    /// </summary>
    public sealed class RabbitMQOutboxMessage
    {
        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The fully qualified type name of the message.
        /// </summary>
        public string MessageType { get; init; } = string.Empty;

        /// <summary>
        /// The serialized message payload.
        /// </summary>
        public string Payload { get; init; } = string.Empty;

        /// <summary>
        /// The routing key for RabbitMQ.
        /// </summary>
        public string? RoutingKey { get; init; }

        /// <summary>
        /// The exchange name.
        /// </summary>
        public string? Exchange { get; init; }

        /// <summary>
        /// Serialized headers JSON.
        /// </summary>
        public string? Headers { get; init; }

        /// <summary>
        /// Correlation ID for tracing.
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Causation ID.
        /// </summary>
        public string? CausationId { get; init; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Publication timestamp.
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Current status.
        /// </summary>
        public RabbitMQOutboxStatus Status { get; set; } = RabbitMQOutboxStatus.Pending;

        /// <summary>
        /// Retry count.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Last error message.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Next retry time.
        /// </summary>
        public DateTime? NextRetryAt { get; set; }

        /// <summary>
        /// Scheduled publish time.
        /// </summary>
        public DateTime? ScheduledAt { get; init; }

        /// <summary>
        /// Message priority.
        /// </summary>
        public byte Priority { get; init; }

        /// <summary>
        /// Tenant ID.
        /// </summary>
        public string? TenantId { get; init; }
    }

    /// <summary>
    /// Status of a RabbitMQ outbox message.
    /// </summary>
    public enum RabbitMQOutboxStatus
    {
        /// <summary>Pending publication.</summary>
        Pending = 0,
        /// <summary>Being processed.</summary>
        Processing = 1,
        /// <summary>Successfully published.</summary>
        Published = 2,
        /// <summary>Failed (will retry).</summary>
        Failed = 3,
        /// <summary>Exceeded retry limit.</summary>
        DeadLetter = 4,
        /// <summary>Scheduled for later.</summary>
        Scheduled = 5
    }

    /// <summary>
    /// Integration event wrapper for RabbitMQ outbox messages.
    /// </summary>
    public sealed class RabbitMQOutboxIntegrationEvent : IIntegrationEvent
    {
        /// <inheritdoc />
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <inheritdoc />
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        /// <inheritdoc />
        public string? CorrelationId { get; init; }

        /// <summary>The original message type.</summary>
        public string MessageType { get; init; } = string.Empty;

        /// <summary>The serialized message payload.</summary>
        public string Payload { get; init; } = string.Empty;

        /// <summary>The RabbitMQ routing key.</summary>
        public string? RoutingKey { get; init; }

        /// <summary>The RabbitMQ exchange name.</summary>
        public string? Exchange { get; init; }

        /// <summary>Serialized headers JSON.</summary>
        public string? Headers { get; init; }

        /// <summary>Causation ID.</summary>
        public string? CausationId { get; init; }

        /// <summary>Message priority.</summary>
        public byte Priority { get; init; }

        /// <summary>Tenant ID.</summary>
        public string? TenantId { get; init; }

        /// <summary>Scheduled publish time.</summary>
        public DateTime? ScheduledAt { get; init; }
    }
}

