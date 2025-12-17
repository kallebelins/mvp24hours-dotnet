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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional
{
    /// <summary>
    /// In-memory implementation of the transactional outbox for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This implementation stores messages in memory,
    /// so messages will be lost if the application restarts.
    /// For production use, implement a persistent outbox using a database.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Unit testing transactional messaging logic</item>
    /// <item>Integration tests without database setup</item>
    /// <item>Development and debugging</item>
    /// <item>Proof of concept implementations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register for testing
    /// services.AddSingleton&lt;ITransactionalOutbox, InMemoryTransactionalOutbox&gt;();
    /// 
    /// // Use in tests
    /// var outbox = new InMemoryTransactionalOutbox();
    /// var message = new TransactionalOutboxMessage { ... };
    /// await outbox.AddAsync(message);
    /// 
    /// var pending = await outbox.GetPendingAsync();
    /// Assert.Single(pending);
    /// </code>
    /// </example>
    public sealed class InMemoryTransactionalOutbox : ITransactionalOutbox
    {
        private readonly ConcurrentDictionary<Guid, TransactionalOutboxMessage> _messages = new();
        private readonly ILogger<InMemoryTransactionalOutbox>? _logger;
        private readonly InMemoryTransactionalOutboxOptions _options;

        /// <summary>
        /// Creates a new instance of the in-memory transactional outbox.
        /// </summary>
        /// <param name="logger">Optional logger for recording operations.</param>
        /// <param name="options">Optional configuration options.</param>
        public InMemoryTransactionalOutbox(
            ILogger<InMemoryTransactionalOutbox>? logger = null,
            InMemoryTransactionalOutboxOptions? options = null)
        {
            _logger = logger;
            _options = options ?? new InMemoryTransactionalOutboxOptions();
        }

        /// <inheritdoc />
        public Task AddAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _messages.TryAdd(message.Id, message);

            _logger?.LogDebug(
                "[InMemoryOutbox] Added message {MessageId} of type {MessageType}",
                message.Id,
                message.MessageType);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task AddRangeAsync(IEnumerable<TransactionalOutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var count = 0;
            foreach (var message in messages)
            {
                _messages.TryAdd(message.Id, message);
                count++;
            }

            _logger?.LogDebug("[InMemoryOutbox] Added {Count} messages", count);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TransactionalOutboxMessage>> GetPendingAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var pending = _messages.Values
                .Where(m => m.Status == TransactionalOutboxStatus.Pending ||
                           (m.Status == TransactionalOutboxStatus.Failed && 
                            (m.NextRetryAt == null || m.NextRetryAt <= now)))
                .Where(m => m.ScheduledAt == null || m.ScheduledAt <= now)
                .OrderBy(m => m.Priority)
                    .ThenBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToList();

            _logger?.LogDebug("[InMemoryOutbox] Retrieved {Count} pending messages", pending.Count);

            return Task.FromResult<IReadOnlyList<TransactionalOutboxMessage>>(pending);
        }

        /// <inheritdoc />
        public Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.Status = TransactionalOutboxStatus.Published;
                message.PublishedAt = DateTime.UtcNow;
                message.LastError = null;

                _logger?.LogDebug("[InMemoryOutbox] Marked message {MessageId} as published", messageId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                message.RetryCount++;
                message.LastError = error;

                if (message.RetryCount >= _options.MaxRetryCount)
                {
                    message.Status = TransactionalOutboxStatus.DeadLetter;
                    _logger?.LogWarning(
                        "[InMemoryOutbox] Message {MessageId} moved to dead letter after {RetryCount} retries. Error: {Error}",
                        messageId,
                        message.RetryCount,
                        error);
                }
                else
                {
                    message.Status = TransactionalOutboxStatus.Failed;
                    // Exponential backoff: 2^retryCount seconds (1s, 2s, 4s, 8s, ...)
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, message.RetryCount));
                    message.NextRetryAt = DateTime.UtcNow.Add(delay);

                    _logger?.LogWarning(
                        "[InMemoryOutbox] Message {MessageId} failed (attempt {RetryCount}), next retry at {NextRetryAt}. Error: {Error}",
                        messageId,
                        message.RetryCount,
                        message.NextRetryAt,
                        error);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            var count = _messages.Values.Count(m =>
                m.Status == TransactionalOutboxStatus.Pending ||
                m.Status == TransactionalOutboxStatus.Failed);

            return Task.FromResult(count);
        }

        /// <inheritdoc />
        public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            var toRemove = _messages.Values
                .Where(m => m.Status == TransactionalOutboxStatus.Published &&
                           m.PublishedAt.HasValue &&
                           m.PublishedAt.Value < olderThan)
                .Select(m => m.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                _messages.TryRemove(id, out _);
            }

            _logger?.LogDebug("[InMemoryOutbox] Cleaned up {Count} old messages", toRemove.Count);

            return Task.FromResult(toRemove.Count);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TransactionalOutboxMessage>> GetDeadLettersAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            var deadLetters = _messages.Values
                .Where(m => m.Status == TransactionalOutboxStatus.DeadLetter)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToList();

            return Task.FromResult<IReadOnlyList<TransactionalOutboxMessage>>(deadLetters);
        }

        /// <summary>
        /// Gets all messages for testing purposes.
        /// </summary>
        /// <returns>All messages in the outbox.</returns>
        public IReadOnlyList<TransactionalOutboxMessage> GetAll()
        {
            return _messages.Values.ToList();
        }

        /// <summary>
        /// Clears all messages for testing purposes.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _logger?.LogDebug("[InMemoryOutbox] Cleared all messages");
        }

        /// <summary>
        /// Gets a specific message by ID for testing purposes.
        /// </summary>
        /// <param name="messageId">The message ID to find.</param>
        /// <returns>The message if found, null otherwise.</returns>
        public TransactionalOutboxMessage? GetById(Guid messageId)
        {
            _messages.TryGetValue(messageId, out var message);
            return message;
        }
    }

    /// <summary>
    /// Configuration options for the in-memory transactional outbox.
    /// </summary>
    public class InMemoryTransactionalOutboxOptions
    {
        /// <summary>
        /// Maximum number of retry attempts before moving to dead letter.
        /// Default is 3.
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
    }
}

