//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Contract
{
    /// <summary>
    /// Factory interface for creating distributed lock instances.
    /// Provides a way to obtain <see cref="IDistributedLock"/> implementations
    /// configured for specific providers or scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The factory pattern allows for flexible configuration and provider selection
    /// without coupling consumers to specific implementations. Different providers
    /// (Redis, SQL Server, PostgreSQL, etc.) can be registered and selected by name
    /// or configuration.
    /// </para>
    /// <para>
    /// <strong>Provider Selection:</strong>
    /// Providers can be selected by name (e.g., "Redis", "SqlServer") or by default
    /// if only one provider is registered. The factory handles provider resolution
    /// and configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register providers
    /// services.AddDistributedLocking()
    ///     .AddRedisProvider("Redis", options => { ... })
    ///     .AddSqlServerProvider("SqlServer", options => { ... });
    /// 
    /// // Use factory
    /// var lockFactory = serviceProvider.GetRequiredService&lt;IDistributedLockFactory&gt;();
    /// var distributedLock = lockFactory.Create("Redis"); // or lockFactory.Create() for default
    /// 
    /// var result = await distributedLock.TryAcquireAsync("resource", options, cancellationToken);
    /// </code>
    /// </example>
    public interface IDistributedLockFactory
    {
        /// <summary>
        /// Creates a distributed lock instance using the default provider.
        /// </summary>
        /// <returns>
        /// An <see cref="IDistributedLock"/> instance configured with the default provider.
        /// </returns>
        /// <remarks>
        /// If multiple providers are registered, the first registered provider is used as default.
        /// If no providers are registered, this method throws an <see cref="InvalidOperationException"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no distributed lock providers are registered.
        /// </exception>
        IDistributedLock Create();

        /// <summary>
        /// Creates a distributed lock instance using the specified provider.
        /// </summary>
        /// <param name="providerName">
        /// The name of the provider to use. Must match a registered provider name.
        /// </param>
        /// <returns>
        /// An <see cref="IDistributedLock"/> instance configured with the specified provider.
        /// </returns>
        /// <remarks>
        /// Provider names are case-insensitive. If the specified provider is not found,
        /// this method throws an <see cref="ArgumentException"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified provider name is not registered.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="providerName"/> is <c>null</c> or empty.
        /// </exception>
        /// <example>
        /// <code>
        /// var redisLock = lockFactory.Create("Redis");
        /// var sqlLock = lockFactory.Create("SqlServer");
        /// </code>
        /// </example>
        IDistributedLock Create(string providerName);
    }
}

