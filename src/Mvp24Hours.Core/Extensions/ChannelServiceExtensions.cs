//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Infrastructure.Channels;
using System;
using System.Threading.Channels;

namespace Mvp24Hours.Core.Extensions;

/// <summary>
/// Extension methods for registering Channel services in DI.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a fluent way to register Channel services
/// in the dependency injection container.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic registration
/// services.AddMvpChannels();
/// 
/// // Register specific channel types
/// services.AddBoundedChannel&lt;Order&gt;(100);
/// services.AddUnboundedChannel&lt;Event&gt;();
/// 
/// // Register channel with options
/// services.AddChannel&lt;Message&gt;(options =&gt;
/// {
///     options.Capacity = 500;
///     options.FullMode = BoundedChannelFullMode.DropOldest;
/// });
/// </code>
/// </example>
public static class ChannelServiceExtensions
{
    /// <summary>
    /// Adds the Channel factory and related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvpChannels(this IServiceCollection services)
    {
        services.TryAddSingleton<IChannelFactory, ChannelFactory>();
        return services;
    }

    /// <summary>
    /// Registers a bounded channel as a singleton.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="capacity">The maximum capacity.</param>
    /// <param name="fullMode">The behavior when the channel is full.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBoundedChannel<T>(
        this IServiceCollection services,
        int capacity,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
    {
        return services.AddChannel<T>(new MvpChannelOptions
        {
            IsBounded = true,
            Capacity = capacity,
            FullMode = fullMode
        });
    }

    /// <summary>
    /// Registers an unbounded channel as a singleton.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUnboundedChannel<T>(this IServiceCollection services)
    {
        return services.AddChannel<T>(MvpChannelOptions.Unbounded());
    }

    /// <summary>
    /// Registers a channel with the specified options as a singleton.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The channel options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChannel<T>(
        this IServiceCollection services,
        MvpChannelOptions options)
    {
        services.AddSingleton<IChannel<T>>(_ => new MvpChannel<T>(options));
        services.AddSingleton(sp => sp.GetRequiredService<IChannel<T>>().Reader);
        services.AddSingleton(sp => sp.GetRequiredService<IChannel<T>>().Writer);
        return services;
    }

    /// <summary>
    /// Registers a channel with configuration callback as a singleton.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChannel<T>(
        this IServiceCollection services,
        Action<MvpChannelOptions> configure)
    {
        var options = new MvpChannelOptions();
        configure(options);
        return services.AddChannel<T>(options);
    }

    /// <summary>
    /// Registers a keyed bounded channel.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The service key.</param>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyedBoundedChannel<T>(
        this IServiceCollection services,
        string key,
        int capacity)
    {
        services.AddKeyedSingleton<IChannel<T>>(key, (_, _) =>
            new MvpChannel<T>(MvpChannelOptions.Bounded(capacity)));
        return services;
    }

    /// <summary>
    /// Registers a high-throughput channel optimized for performance.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHighThroughputChannel<T>(
        this IServiceCollection services,
        int capacity = 1000)
    {
        return services.AddChannel<T>(MvpChannelOptions.HighThroughput(capacity));
    }

    /// <summary>
    /// Registers a channel that drops oldest items when full.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDropOldestChannel<T>(
        this IServiceCollection services,
        int capacity = 100)
    {
        return services.AddChannel<T>(MvpChannelOptions.DropOldest(capacity));
    }

    /// <summary>
    /// Registers a channel that drops write attempts when full.
    /// </summary>
    /// <typeparam name="T">The type of items in the channel.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="capacity">The maximum capacity.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDropWriteChannel<T>(
        this IServiceCollection services,
        int capacity = 100)
    {
        return services.AddChannel<T>(MvpChannelOptions.DropWrite(capacity));
    }
}

