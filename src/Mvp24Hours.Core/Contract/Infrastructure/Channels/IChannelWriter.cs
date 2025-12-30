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
/// Represents the write side of a channel.
/// </summary>
/// <typeparam name="T">The type of items in the channel.</typeparam>
public interface IChannelWriter<T>
{
    /// <summary>
    /// Gets a value indicating whether the channel has been marked as complete.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Attempts to write an item to the channel without blocking.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <returns>True if the item was written; otherwise, false.</returns>
    bool TryWrite(T item);

    /// <summary>
    /// Asynchronously writes an item to the channel.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the item is written.</returns>
    ValueTask WriteAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits until space is available to write.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if space is available; false if the channel is complete.</returns>
    ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the channel as complete, signaling no more items will be written.
    /// </summary>
    /// <param name="error">Optional exception indicating an error condition.</param>
    /// <returns>True if the channel was successfully marked complete.</returns>
    bool TryComplete(Exception? error = null);

    /// <summary>
    /// Writes multiple items to the channel.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all items are written.</returns>
    ValueTask WriteManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple items to the channel from an async enumerable.
    /// </summary>
    /// <param name="items">The async enumerable of items to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all items are written.</returns>
    ValueTask WriteManyAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default);
}

