//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Messages
{
    /// <summary>
    /// Implementation of a message envelope with typed payload.
    /// </summary>
    /// <typeparam name="TPayload">The type of the message payload.</typeparam>
    public class Message<TPayload> : IMessage<TPayload>
    {
        /// <inheritdoc />
        public string MessageId { get; init; } = Guid.NewGuid().ToString();

        /// <inheritdoc />
        public string? CorrelationId { get; init; }

        /// <inheritdoc />
        public string? CausationId { get; init; }

        /// <inheritdoc />
        public string MessageType { get; init; } = typeof(TPayload).AssemblyQualifiedName ?? typeof(TPayload).Name;

        /// <inheritdoc />
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public IDictionary<string, object> Headers { get; init; } = new Dictionary<string, object>();

        /// <inheritdoc />
        public string? SourceApplication { get; init; }

        /// <inheritdoc />
        public string ContentType { get; init; } = "application/json";

        /// <inheritdoc />
        public TPayload Payload { get; init; } = default!;

        /// <summary>
        /// Creates an empty message.
        /// </summary>
        public Message() { }

        /// <summary>
        /// Creates a message with the specified payload.
        /// </summary>
        /// <param name="payload">The message payload.</param>
        public Message(TPayload payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// Creates a message with the specified payload and correlation ID.
        /// </summary>
        /// <param name="payload">The message payload.</param>
        /// <param name="correlationId">The correlation ID.</param>
        public Message(TPayload payload, string? correlationId) : this(payload)
        {
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Creates a new message from a payload with optional metadata.
        /// </summary>
        public static Message<TPayload> Create(
            TPayload payload,
            string? correlationId = null,
            string? causationId = null,
            string? sourceApplication = null,
            IDictionary<string, object>? headers = null)
        {
            return new Message<TPayload>
            {
                Payload = payload,
                CorrelationId = correlationId,
                CausationId = causationId,
                SourceApplication = sourceApplication,
                Headers = headers ?? new Dictionary<string, object>()
            };
        }
    }
}

