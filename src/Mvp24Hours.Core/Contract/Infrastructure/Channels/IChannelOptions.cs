//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading.Channels;

namespace Mvp24Hours.Core.Contract.Infrastructure.Channels;

/// <summary>
/// Options for configuring an Mvp24Hours channel.
/// </summary>
/// <remarks>
/// Named MvpChannelOptions to avoid conflict with System.Threading.Channels.ChannelOptions.
/// </remarks>
public class MvpChannelOptions
{
    /// <summary>
    /// Gets or sets whether the channel is bounded (has a maximum capacity).
    /// Default is true for backpressure support.
    /// </summary>
    public bool IsBounded { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum capacity for bounded channels.
    /// Default is 100 items.
    /// </summary>
    public int Capacity { get; set; } = 100;

    /// <summary>
    /// Gets or sets the behavior when the channel is full.
    /// Default is Wait (block until space is available).
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Gets or sets whether to allow synchronous continuations.
    /// Default is false for better async behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this to true can improve performance in high-throughput scenarios,
    /// but may cause stack overflow in deeply nested operations.
    /// </para>
    /// </remarks>
    public bool AllowSynchronousContinuations { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the channel supports single reader.
    /// Default is false to support multiple consumers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this to true can improve performance when you guarantee only one consumer.
    /// </para>
    /// </remarks>
    public bool SingleReader { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the channel supports single writer.
    /// Default is false to support multiple producers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this to true can improve performance when you guarantee only one producer.
    /// </para>
    /// </remarks>
    public bool SingleWriter { get; set; } = false;

    /// <summary>
    /// Gets or sets the timeout for write operations when the channel is full.
    /// Default is null (wait indefinitely).
    /// </summary>
    public TimeSpan? WriteTimeout { get; set; }

    /// <summary>
    /// Gets or sets the timeout for read operations when the channel is empty.
    /// Default is null (wait indefinitely).
    /// </summary>
    public TimeSpan? ReadTimeout { get; set; }

    /// <summary>
    /// Creates default unbounded channel options.
    /// </summary>
    public static MvpChannelOptions Unbounded() => new()
    {
        IsBounded = false
    };

    /// <summary>
    /// Creates bounded channel options with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    /// <param name="fullMode">The behavior when full.</param>
    public static MvpChannelOptions Bounded(
        int capacity,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
        => new()
        {
            IsBounded = true,
            Capacity = capacity,
            FullMode = fullMode
        };

    /// <summary>
    /// Creates options optimized for high-throughput scenarios.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    public static MvpChannelOptions HighThroughput(int capacity = 1000) => new()
    {
        IsBounded = true,
        Capacity = capacity,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = false
    };

    /// <summary>
    /// Creates options for dropping oldest items when full.
    /// Useful for real-time data where latest is more important.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    public static MvpChannelOptions DropOldest(int capacity = 100) => new()
    {
        IsBounded = true,
        Capacity = capacity,
        FullMode = BoundedChannelFullMode.DropOldest
    };

    /// <summary>
    /// Creates options for dropping newest items when full.
    /// Useful when you want to preserve order of arrival.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    public static MvpChannelOptions DropNewest(int capacity = 100) => new()
    {
        IsBounded = true,
        Capacity = capacity,
        FullMode = BoundedChannelFullMode.DropNewest
    };

    /// <summary>
    /// Creates options that drop writes when full.
    /// Useful for fire-and-forget scenarios.
    /// </summary>
    /// <param name="capacity">The maximum capacity.</param>
    public static MvpChannelOptions DropWrite(int capacity = 100) => new()
    {
        IsBounded = true,
        Capacity = capacity,
        FullMode = BoundedChannelFullMode.DropWrite
    };
}

