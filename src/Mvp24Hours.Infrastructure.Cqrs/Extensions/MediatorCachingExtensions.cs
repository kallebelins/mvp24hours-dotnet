//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Mvp24Hours.Infrastructure.Cqrs.Extensions;

/// <summary>
/// Extension methods for configuring caching with the Mediator.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for setting up caching infrastructure
/// required by behaviors like <c>CachingBehavior</c> and <c>IdempotencyBehavior</c>.
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// <list type="bullet">
/// <item>For Redis: Install <c>Mvp24Hours.Infrastructure.Caching.Redis</c> package</item>
/// <item>For Memory: Uses built-in <c>Microsoft.Extensions.Caching.Memory</c></item>
/// </list>
/// </para>
/// </remarks>
public static class MediatorCachingExtensions
{
    /// <summary>
    /// Adds in-memory distributed cache for the Mediator.
    /// Suitable for development and single-instance deployments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> In-memory cache is not shared between instances.
    /// For multi-instance deployments, use Redis with <c>AddMediatorRedisCache</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMediatorMemoryCache();
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.RegisterCachingBehavior = true;
    ///     options.RegisterIdempotencyBehavior = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMediatorMemoryCache(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        return services;
    }

    /// <summary>
    /// Adds Redis distributed cache for the Mediator.
    /// Required for multi-instance deployments with idempotency.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="instanceName">Optional instance name prefix for keys.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures Redis as the distributed cache provider,
    /// enabling shared caching and idempotency across multiple application instances.
    /// </para>
    /// <para>
    /// <strong>Connection String Examples:</strong>
    /// <list type="bullet">
    /// <item><c>localhost:6379</c> - Local Redis</item>
    /// <item><c>redis-server:6379,password=secret</c> - Redis with password</item>
    /// <item><c>redis-server:6379,ssl=true,abortConnect=false</c> - Azure Redis</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic Redis setup
    /// services.AddMediatorRedisCache("localhost:6379");
    /// 
    /// // With custom instance name
    /// services.AddMediatorRedisCache("localhost:6379", "myapp");
    /// 
    /// // Then configure Mediator with caching behaviors
    /// services.AddMvpMediator(options =>
    /// {
    ///     options.RegisterHandlersFromAssemblyContaining&lt;Program&gt;();
    ///     options.RegisterCachingBehavior = true;
    ///     options.RegisterIdempotencyBehavior = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMediatorRedisCache(
        this IServiceCollection services,
        string connectionString,
        string? instanceName = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), 
                "Redis connection string is required.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = instanceName ?? "mvp24mediator:";
        });

        return services;
    }

    /// <summary>
    /// Adds Redis distributed cache with advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure Redis options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMediatorRedisCache(options =>
    /// {
    ///     options.Configuration = "localhost:6379";
    ///     options.InstanceName = "myapp:";
    ///     options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    ///     {
    ///         ConnectTimeout = 5000,
    ///         SyncTimeout = 5000,
    ///         AbortOnConnectFail = false
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMediatorRedisCache(
        this IServiceCollection services,
        Action<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions> configure)
    {
        services.AddStackExchangeRedisCache(configure);
        return services;
    }
}

/// <summary>
/// Configuration options for Mediator caching.
/// </summary>
public sealed class MediatorCacheOptions
{
    /// <summary>
    /// Gets or sets the default cache duration for query responses.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan DefaultQueryCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the default idempotency duration.
    /// Default is 24 hours.
    /// </summary>
    public TimeSpan DefaultIdempotencyDuration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the prefix for all cache keys.
    /// Default is "mvp24mediator:".
    /// </summary>
    public string KeyPrefix { get; set; } = "mvp24mediator:";

    /// <summary>
    /// Gets or sets whether to use sliding expiration for cached items.
    /// Default is false (absolute expiration).
    /// </summary>
    public bool UseSlidingExpiration { get; set; }
}

