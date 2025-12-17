//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// Implementation of transactional bus that uses the outbox pattern for reliable messaging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stages messages in memory during the transaction scope,
    /// then persists them to the outbox when the transaction commits.
    /// </para>
    /// <para>
    /// For full transactional guarantees, use with <see cref="TransactionalEnlistment"/>
    /// or integrate with Entity Framework's SaveChanges mechanism.
    /// </para>
    /// </remarks>
    public class TransactionalBus : ITransactionalBus
    {
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<TransactionalBus> _logger;
        private readonly TransactionalBusOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        // Thread-local pending messages for the current transaction scope
        private readonly AsyncLocal<List<TransactionalOutboxMessage>> _pendingMessages = new();

        /// <summary>
        /// Creates a new instance of the transactional bus.
        /// </summary>
        /// <param name="outbox">The outbox storage implementation.</param>
        /// <param name="logger">Logger for recording operations.</param>
        /// <param name="options">Configuration options.</param>
        public TransactionalBus(
            ITransactionalOutbox outbox,
            ILogger<TransactionalBus> logger,
            TransactionalBusOptions? options = null)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new TransactionalBusOptions();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public Task<Guid> PublishAsync<TMessage>(
            TMessage message,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            return PublishAsync(message, new Dictionary<string, object>(), routingKey, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Guid> PublishAsync<TMessage>(
            TMessage message,
            IDictionary<string, object> headers,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var messageType = typeof(TMessage);
            var outboxMessage = new TransactionalOutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name,
                Payload = JsonSerializer.Serialize(message, _jsonOptions),
                RoutingKey = routingKey ?? _options.GetRoutingKey(messageType),
                Exchange = _options.GetExchange(messageType),
                Headers = headers.Count > 0 ? JsonSerializer.Serialize(headers, _jsonOptions) : null,
                CorrelationId = GetCorrelationId(headers),
                CausationId = GetCausationId(headers),
                CreatedAt = DateTime.UtcNow,
                Priority = GetPriority(headers),
                TenantId = GetTenantId(headers)
            };

            EnsurePendingList();
            _pendingMessages.Value!.Add(outboxMessage);

            _logger.LogDebug(
                "[TransactionalBus] Staged message {MessageId} of type {MessageType} with routing key {RoutingKey}",
                outboxMessage.Id,
                messageType.Name,
                outboxMessage.RoutingKey);

            return Task.FromResult(outboxMessage.Id);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Guid>> PublishBatchAsync<TMessage>(
            IEnumerable<TMessage> messages,
            string? routingKey = null,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var messageIds = new List<Guid>();

            foreach (var message in messages)
            {
                var id = await PublishAsync(message, routingKey, cancellationToken);
                messageIds.Add(id);
            }

            return messageIds;
        }

        /// <inheritdoc />
        public int GetPendingCount()
        {
            return _pendingMessages.Value?.Count ?? 0;
        }

        /// <inheritdoc />
        public void ClearPending()
        {
            var count = _pendingMessages.Value?.Count ?? 0;
            _pendingMessages.Value?.Clear();

            if (count > 0)
            {
                _logger.LogDebug("[TransactionalBus] Cleared {Count} pending messages", count);
            }
        }

        /// <summary>
        /// Flushes all pending messages to the outbox.
        /// Call this after the database transaction commits.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of messages flushed to the outbox.</returns>
        public async Task<int> FlushToOutboxAsync(CancellationToken cancellationToken = default)
        {
            var pending = _pendingMessages.Value;
            if (pending == null || pending.Count == 0)
            {
                return 0;
            }

            var count = pending.Count;

            try
            {
                await _outbox.AddRangeAsync(pending, cancellationToken);

                _logger.LogDebug(
                    "[TransactionalBus] Flushed {Count} messages to outbox",
                    count);

                pending.Clear();
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[TransactionalBus] Failed to flush {Count} messages to outbox",
                    count);
                throw;
            }
        }

        /// <summary>
        /// Gets the pending messages without flushing them.
        /// </summary>
        /// <returns>Read-only list of pending messages.</returns>
        public IReadOnlyList<TransactionalOutboxMessage> GetPendingMessages()
        {
            return _pendingMessages.Value?.AsReadOnly() ?? Array.Empty<TransactionalOutboxMessage>().ToList().AsReadOnly();
        }

        private void EnsurePendingList()
        {
            _pendingMessages.Value ??= new List<TransactionalOutboxMessage>();
        }

        private static string? GetCorrelationId(IDictionary<string, object> headers)
        {
            if (headers.TryGetValue("x-correlation-id", out var value) ||
                headers.TryGetValue("correlationId", out value) ||
                headers.TryGetValue("CorrelationId", out value))
            {
                return value?.ToString();
            }
            return null;
        }

        private static string? GetCausationId(IDictionary<string, object> headers)
        {
            if (headers.TryGetValue("x-causation-id", out var value) ||
                headers.TryGetValue("causationId", out value) ||
                headers.TryGetValue("CausationId", out value))
            {
                return value?.ToString();
            }
            return null;
        }

        private static byte GetPriority(IDictionary<string, object> headers)
        {
            if (headers.TryGetValue("x-priority", out var value) ||
                headers.TryGetValue("priority", out value) ||
                headers.TryGetValue("Priority", out value))
            {
                if (value is byte b)
                    return b;
                if (byte.TryParse(value?.ToString(), out var parsed))
                    return parsed;
            }
            return 0;
        }

        private static string? GetTenantId(IDictionary<string, object> headers)
        {
            if (headers.TryGetValue("x-tenant-id", out var value) ||
                headers.TryGetValue("tenantId", out value) ||
                headers.TryGetValue("TenantId", out value))
            {
                return value?.ToString();
            }
            return null;
        }
    }

    /// <summary>
    /// Configuration options for the transactional bus.
    /// </summary>
    public class TransactionalBusOptions
    {
        /// <summary>
        /// Default exchange name for messages.
        /// </summary>
        public string DefaultExchange { get; set; } = string.Empty;

        /// <summary>
        /// Custom routing key resolver. If null, uses message type name as routing key.
        /// </summary>
        public Func<Type, string>? RoutingKeyResolver { get; set; }

        /// <summary>
        /// Custom exchange resolver. If null, uses DefaultExchange.
        /// </summary>
        public Func<Type, string?>? ExchangeResolver { get; set; }

        /// <summary>
        /// Maximum number of pending messages before auto-flush warning.
        /// </summary>
        public int MaxPendingWarningThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets the routing key for a message type.
        /// </summary>
        internal string GetRoutingKey(Type messageType)
        {
            if (RoutingKeyResolver != null)
            {
                return RoutingKeyResolver(messageType);
            }

            // Default: use message type name in kebab-case
            return ToKebabCase(messageType.Name);
        }

        /// <summary>
        /// Gets the exchange for a message type.
        /// </summary>
        internal string? GetExchange(Type messageType)
        {
            if (ExchangeResolver != null)
            {
                return ExchangeResolver(messageType);
            }

            return DefaultExchange;
        }

        private static string ToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = new System.Text.StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        result.Append('-');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}

