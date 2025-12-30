//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Channels;

/// <summary>
/// Represents a channel that supports both reading and writing.
/// </summary>
/// <typeparam name="T">The type of items in the channel.</typeparam>
public interface IChannel<T> : IDisposable
{
    /// <summary>
    /// Gets the reader for this channel.
    /// </summary>
    IChannelReader<T> Reader { get; }

    /// <summary>
    /// Gets the writer for this channel.
    /// </summary>
    IChannelWriter<T> Writer { get; }

    /// <summary>
    /// Gets a value indicating whether the channel is completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the current item count in the channel.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the options used to configure this channel.
    /// </summary>
    MvpChannelOptions Options { get; }
}

/// <summary>
/// Factory for creating channels.
/// </summary>
public interface IChannelFactory
{
    /// <summary>
    /// Creates a new channel with the specified options.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="options">Channel options.</param>
    /// <returns>A new channel instance.</returns>
    IChannel<T> Create<T>(MvpChannelOptions? options = null);

    /// <summary>
    /// Creates a new unbounded channel.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <returns>A new unbounded channel.</returns>
    IChannel<T> CreateUnbounded<T>();

    /// <summary>
    /// Creates a new bounded channel with the specified capacity.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A new bounded channel.</returns>
    IChannel<T> CreateBounded<T>(int capacity);
}

