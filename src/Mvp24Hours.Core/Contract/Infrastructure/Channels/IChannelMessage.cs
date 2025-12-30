//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Infrastructure.Channels;

/// <summary>
/// Represents a message that can be sent through a channel.
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
public interface IChannelMessage<out T>
{
    /// <summary>
    /// Gets the unique identifier for this message.
    /// </summary>
    Guid MessageId { get; }

    /// <summary>
    /// Gets the correlation ID for tracking related messages.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID (ID of the message that caused this one).
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Gets the message payload.
    /// </summary>
    T Payload { get; }

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets additional metadata associated with the message.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }
}

/// <summary>
/// Default implementation of <see cref="IChannelMessage{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the message payload.</typeparam>
public sealed record ChannelMessage<T> : IChannelMessage<T>
{
    /// <summary>
    /// Creates a new channel message with the specified payload.
    /// </summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="causationId">Optional causation ID.</param>
    /// <param name="metadata">Optional metadata.</param>
    public ChannelMessage(
        T payload,
        string? correlationId = null,
        string? causationId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        MessageId = Guid.NewGuid();
        Payload = payload;
        CorrelationId = correlationId;
        CausationId = causationId;
        CreatedAt = DateTimeOffset.UtcNow;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    /// <inheritdoc />
    public Guid MessageId { get; }

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public string? CausationId { get; init; }

    /// <inheritdoc />
    public T Payload { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }

    /// <summary>
    /// Creates a new channel message with just the payload.
    /// </summary>
    /// <param name="payload">The message payload.</param>
    /// <returns>A new channel message.</returns>
    public static ChannelMessage<T> Create(T payload) => new(payload);

    /// <summary>
    /// Creates a new channel message with correlation tracking.
    /// </summary>
    /// <param name="payload">The message payload.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="causationId">The causation ID.</param>
    /// <returns>A new channel message with tracking.</returns>
    public static ChannelMessage<T> CreateWithTracking(
        T payload,
        string correlationId,
        string? causationId = null)
        => new(payload, correlationId, causationId);
}

