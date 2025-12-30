//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Channels;

/// <summary>
/// Represents the read side of a channel.
/// </summary>
/// <typeparam name="T">The type of items in the channel.</typeparam>
public interface IChannelReader<T>
{
    /// <summary>
    /// Gets a value indicating whether the channel has been marked as complete.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the number of items available to read (may not be accurate for all implementations).
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Attempts to read an item from the channel without blocking.
    /// </summary>
    /// <param name="item">The item read, if successful.</param>
    /// <returns>True if an item was read; otherwise, false.</returns>
    bool TryRead(out T? item);

    /// <summary>
    /// Asynchronously reads an item from the channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The item read from the channel.</returns>
    ValueTask<T> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits until data is available to read.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if data is available; false if the channel is complete.</returns>
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an async enumerable for reading all items from the channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of items.</returns>
    IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads items from the channel in batches.
    /// </summary>
    /// <param name="batchSize">The maximum number of items per batch.</param>
    /// <param name="timeout">The maximum time to wait for a complete batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of item batches.</returns>
    IAsyncEnumerable<IReadOnlyList<T>> ReadBatchAsync(
        int batchSize,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to peek at the next item without removing it.
    /// </summary>
    /// <param name="item">The peeked item, if available.</param>
    /// <returns>True if an item was peeked; otherwise, false.</returns>
    bool TryPeek(out T? item);
}

