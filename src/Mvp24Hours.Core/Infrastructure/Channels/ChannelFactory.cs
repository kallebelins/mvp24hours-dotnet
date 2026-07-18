//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;

namespace Mvp24Hours.Core.Infrastructure.Channels;

/// <summary>
/// Factory for creating channels with logging and observability.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides a centralized way to create channels with consistent
/// configuration and optional logging. It's designed to be registered in DI
/// and used throughout the application.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddSingleton&lt;IChannelFactory, ChannelFactory&gt;();
/// 
/// // Use in services
/// public class OrderProcessor
/// {
///     private readonly IChannel&lt;Order&gt; _orderChannel;
/// 
///     public OrderProcessor(IChannelFactory channelFactory)
///     {
///         _orderChannel = channelFactory.CreateBounded&lt;Order&gt;(100);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// Creates a new channel factory.
/// </remarks>
/// <param name="logger">Optional logger for channel operations.</param>
public sealed class ChannelFactory(ILogger<ChannelFactory>? logger = null) : IChannelFactory
{
    private readonly ILogger<ChannelFactory>? _logger = logger;

    /// <inheritdoc />
    public IChannel<T> Create<T>(MvpChannelOptions? options = null)
    {
        MvpChannelOptions effectiveOptions = options ?? new MvpChannelOptions();

        _logger?.LogDebug(
            "Creating channel of type {Type}, bounded={IsBounded}, capacity={Capacity}",
            typeof(T).Name,
            effectiveOptions.IsBounded,
            effectiveOptions.Capacity);

        return new MvpChannel<T>(effectiveOptions);
    }

    /// <inheritdoc />
    public IChannel<T> CreateUnbounded<T>()
    {
        _logger?.LogDebug("Creating unbounded channel of type {Type}", typeof(T).Name);
        return new MvpChannel<T>(MvpChannelOptions.Unbounded());
    }

    /// <inheritdoc />
    public IChannel<T> CreateBounded<T>(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0.");
        }

        _logger?.LogDebug(
            "Creating bounded channel of type {Type}, capacity={Capacity}",
            typeof(T).Name,
            capacity);

        return new MvpChannel<T>(MvpChannelOptions.Bounded(capacity));
    }
}

/// <summary>
/// Static helper for creating channels without DI.
/// </summary>
public static class Channels
{
    /// <summary>
    /// Creates a new channel with the specified options.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="options">Channel options.</param>
    /// <returns>A new channel instance.</returns>
    public static IChannel<T> Create<T>(MvpChannelOptions? options = null)
    {
        return new MvpChannel<T>(options);
    }

    /// <summary>
    /// Creates a new unbounded channel.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <returns>A new unbounded channel.</returns>
    public static IChannel<T> CreateUnbounded<T>()
    {
        return new MvpChannel<T>(MvpChannelOptions.Unbounded());
    }

    /// <summary>
    /// Creates a new bounded channel with the specified capacity.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A new bounded channel.</returns>
    public static IChannel<T> CreateBounded<T>(int capacity)
    {
        return new MvpChannel<T>(MvpChannelOptions.Bounded(capacity));
    }

    /// <summary>
    /// Creates a channel optimized for high-throughput scenarios.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A high-throughput channel.</returns>
    public static IChannel<T> CreateHighThroughput<T>(int capacity = 1000)
    {
        return new MvpChannel<T>(MvpChannelOptions.HighThroughput(capacity));
    }

    /// <summary>
    /// Creates a channel that drops oldest items when full.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A drop-oldest channel.</returns>
    public static IChannel<T> CreateDropOldest<T>(int capacity = 100)
    {
        return new MvpChannel<T>(MvpChannelOptions.DropOldest(capacity));
    }

    /// <summary>
    /// Creates a channel that drops newest items when full.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>A drop-newest channel.</returns>
    public static IChannel<T> CreateDropNewest<T>(int capacity = 100)
    {
        return new MvpChannel<T>(MvpChannelOptions.DropNewest(capacity));
    }
}

