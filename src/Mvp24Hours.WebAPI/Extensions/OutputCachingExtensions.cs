//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// Extension methods for configuring Output Caching in ASP.NET Core applications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Output caching is a server-side caching mechanism introduced in .NET 7 that stores
    /// HTTP responses and serves them directly without re-executing the endpoint logic.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item>Server-side response storage (not dependent on client cache headers)</item>
    /// <item>Tag-based invalidation for selective cache clearing</item>
    /// <item>Support for distributed cache backends (Redis)</item>
    /// <item>Policy-based configuration with named policies</item>
    /// <item>Vary-by support (query string, header, route values)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Differences from Response Caching:</strong>
    /// <list type="bullet">
    /// <item>Response Caching relies on HTTP cache headers (Cache-Control, Expires)</item>
    /// <item>Output Caching is controlled entirely server-side</item>
    /// <item>Output Caching supports programmatic cache invalidation</item>
    /// <item>Output Caching has better support for distributed scenarios</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class OutputCachingExtensions
    {
        #region ServiceCollection Extensions

        /// <summary>
        /// Adds output caching services with Mvp24Hours configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures output caching with support for:
        /// <list type="bullet">
        /// <item>Named policies (Default, Short, Medium, Long, NoCache)</item>
        /// <item>Tag-based invalidation</item>
        /// <item>Vary-by query string, header, and route values</item>
        /// <item>Optional Redis backend for distributed caching</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage with default options
        /// builder.Services.AddMvp24HoursOutputCache();
        /// 
        /// // With custom configuration
        /// builder.Services.AddMvp24HoursOutputCache(options =>
        /// {
        ///     options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
        ///     options.AddStandardPolicies();
        ///     options.AddPolicy("Products", p => p
        ///         .Expire(TimeSpan.FromMinutes(5))
        ///         .SetTags("products", "catalog")
        ///         .SetVaryByQuery("category", "page"));
        /// });
        /// 
        /// // With Redis backend
        /// builder.Services.AddMvp24HoursOutputCache(options =>
        /// {
        ///     options.UseDistributedCache = true;
        ///     options.RedisConnectionString = "localhost:6379";
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure output caching options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursOutputCache(
            this IServiceCollection services,
            Action<OutputCachingOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var mvpOptions = new OutputCachingOptions();
            configureOptions?.Invoke(mvpOptions);

            // Register options
            services.Configure<OutputCachingOptions>(opt =>
            {
                opt.Enabled = mvpOptions.Enabled;
                opt.DefaultExpirationTimeSpan = mvpOptions.DefaultExpirationTimeSpan;
                opt.MaximumBodySize = mvpOptions.MaximumBodySize;
                opt.SizeLimit = mvpOptions.SizeLimit;
                opt.UseDistributedCache = mvpOptions.UseDistributedCache;
                opt.RedisConnectionString = mvpOptions.RedisConnectionString;
                opt.RedisInstanceName = mvpOptions.RedisInstanceName;
                opt.RespectVaryHeader = mvpOptions.RespectVaryHeader;
                opt.UseCaseSensitivePaths = mvpOptions.UseCaseSensitivePaths;
                opt.DefaultPolicyName = mvpOptions.DefaultPolicyName;
                opt.VaryByQueryStringByDefault = mvpOptions.VaryByQueryStringByDefault;
                opt.CacheableMethods = mvpOptions.CacheableMethods;
                opt.CacheableContentTypes = mvpOptions.CacheableContentTypes;
                opt.CacheableStatusCodes = mvpOptions.CacheableStatusCodes;
                
                foreach (var path in mvpOptions.ExcludedPaths)
                {
                    opt.ExcludedPaths.Add(path);
                }

                foreach (var policy in mvpOptions.Policies)
                {
                    opt.Policies[policy.Key] = policy.Value;
                }
            });

            if (!mvpOptions.Enabled)
            {
                return services;
            }

            // Configure Redis if distributed cache is enabled
            if (mvpOptions.UseDistributedCache && !string.IsNullOrEmpty(mvpOptions.RedisConnectionString))
            {
                services.AddStackExchangeRedisOutputCache(options =>
                {
                    options.Configuration = mvpOptions.RedisConnectionString;
                    options.InstanceName = mvpOptions.RedisInstanceName;
                });
            }

            // Add output caching services
            services.AddOutputCache(options =>
            {
                options.MaximumBodySize = mvpOptions.MaximumBodySize;
                options.SizeLimit = mvpOptions.SizeLimit;
                options.UseCaseSensitivePaths = mvpOptions.UseCaseSensitivePaths;
                options.DefaultExpirationTimeSpan = mvpOptions.DefaultExpirationTimeSpan;

                // Add base policy that checks if caching is enabled
                options.AddBasePolicy(builder =>
                {
                    if (mvpOptions.VaryByQueryStringByDefault)
                    {
                        builder.SetVaryByQuery("*");
                    }
                });

                // Add named policies
                foreach (var policy in mvpOptions.Policies)
                {
                    options.AddPolicy(policy.Key, builder =>
                    {
                        ConfigurePolicy(builder, policy.Value);
                    });
                }

                // Add preset policies if not already defined
                if (!mvpOptions.Policies.ContainsKey("NoCache"))
                {
                    options.AddPolicy("NoCache", builder => builder.NoCache());
                }

                if (!mvpOptions.Policies.ContainsKey("Default"))
                {
                    options.AddPolicy("Default", builder =>
                    {
                        builder.Expire(mvpOptions.DefaultExpirationTimeSpan);
                    });
                }

                if (!mvpOptions.Policies.ContainsKey("Short"))
                {
                    options.AddPolicy("Short", builder =>
                    {
                        builder.Expire(TimeSpan.FromMinutes(1));
                        builder.Tag("short");
                    });
                }

                if (!mvpOptions.Policies.ContainsKey("Medium"))
                {
                    options.AddPolicy("Medium", builder =>
                    {
                        builder.Expire(TimeSpan.FromMinutes(10));
                        builder.Tag("medium");
                    });
                }

                if (!mvpOptions.Policies.ContainsKey("Long"))
                {
                    options.AddPolicy("Long", builder =>
                    {
                        builder.Expire(TimeSpan.FromHours(1));
                        builder.Tag("long");
                    });
                }

                if (!mvpOptions.Policies.ContainsKey("VeryLong"))
                {
                    options.AddPolicy("VeryLong", builder =>
                    {
                        builder.Expire(TimeSpan.FromHours(24));
                        builder.Tag("verylong");
                    });
                }

                // Add policy for authenticated requests (varies by Authorization header)
                if (!mvpOptions.Policies.ContainsKey("Authenticated"))
                {
                    options.AddPolicy("Authenticated", builder =>
                    {
                        builder.Expire(mvpOptions.DefaultExpirationTimeSpan);
                        builder.SetVaryByHeader("Authorization");
                        builder.Tag("authenticated");
                    });
                }

                // Add policy for API responses (varies by Accept header)
                if (!mvpOptions.Policies.ContainsKey("Api"))
                {
                    options.AddPolicy("Api", builder =>
                    {
                        builder.Expire(mvpOptions.DefaultExpirationTimeSpan);
                        builder.SetVaryByHeader("Accept", "Accept-Language");
                        builder.SetVaryByQuery("*");
                        builder.Tag("api");
                    });
                }
            });

            // Register cache invalidation service
            services.AddSingleton<IOutputCacheInvalidator, OutputCacheInvalidator>();

            return services;
        }

        /// <summary>
        /// Adds output caching with Redis backend for distributed scenarios.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that enables Redis-backed output caching.
        /// Useful for applications running on multiple instances (load-balanced environments).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursOutputCacheWithRedis(
        ///     "localhost:6379",
        ///     options =>
        ///     {
        ///         options.AddStandardPolicies();
        ///     });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="redisConnectionString">The Redis connection string.</param>
        /// <param name="configureOptions">Optional action to configure output caching options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursOutputCacheWithRedis(
            this IServiceCollection services,
            string redisConnectionString,
            Action<OutputCachingOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrEmpty(redisConnectionString);

            return services.AddMvp24HoursOutputCache(options =>
            {
                options.UseDistributedCache = true;
                options.RedisConnectionString = redisConnectionString;
                configureOptions?.Invoke(options);
            });
        }

        private static void ConfigurePolicy(OutputCachePolicyBuilder builder, OutputCachePolicyOptions policy)
        {
            if (policy.NoCache)
            {
                builder.NoCache();
                return;
            }

            if (policy.ExpirationTimeSpan.HasValue)
            {
                builder.Expire(policy.ExpirationTimeSpan.Value);
            }

            foreach (var tag in policy.Tags)
            {
                builder.Tag(tag);
            }

            if (policy.VaryByHeader.Count > 0)
            {
                builder.SetVaryByHeader(policy.VaryByHeader.ToArray());
            }

            if (policy.VaryByAllQueryKeys)
            {
                builder.SetVaryByQuery("*");
            }
            else if (policy.VaryByQueryKeys.Count > 0)
            {
                builder.SetVaryByQuery(policy.VaryByQueryKeys.ToArray());
            }

            if (policy.VaryByRouteValue.Count > 0)
            {
                builder.SetVaryByRouteValue(policy.VaryByRouteValue.ToArray());
            }

            if (policy.LockDuringPopulation)
            {
                builder.SetLocking(true);
            }
        }

        #endregion

        #region ApplicationBuilder Extensions

        /// <summary>
        /// Adds the output caching middleware to the request pipeline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware caches HTTP responses based on the configured policies.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added after CORS, authentication, and authorization middlewares,
        /// but before endpoint routing:
        /// <code>
        /// app.UseCors();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseMvp24HoursOutputCache();  // Add here
        /// app.MapControllers();
        /// </code>
        /// </para>
        /// <para>
        /// <strong>Note:</strong>
        /// Requires prior registration via <see cref="AddMvp24HoursOutputCache"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursOutputCache(options =>
        /// {
        ///     options.AddStandardPolicies();
        /// });
        /// 
        /// var app = builder.Build();
        /// 
        /// app.UseRouting();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseMvp24HoursOutputCache();
        /// app.MapControllers();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursOutputCache(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = builder.ApplicationServices.GetService<IOptions<OutputCachingOptions>>()?.Value;

            if (options == null || !options.Enabled)
            {
                return builder;
            }

            return builder.UseOutputCache();
        }

        /// <summary>
        /// Conditionally adds the output caching middleware.
        /// </summary>
        /// <example>
        /// <code>
        /// // Only enable output caching in production
        /// app.UseMvp24HoursOutputCache(app.Environment.IsProduction());
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="enabled">Whether to enable output caching.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursOutputCache(this IApplicationBuilder builder, bool enabled)
        {
            if (enabled)
            {
                return builder.UseMvp24HoursOutputCache();
            }
            return builder;
        }

        #endregion

        #region Endpoint Extensions

        /// <summary>
        /// Applies output caching to the endpoint with the specified policy.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This extension method applies a named output cache policy to an endpoint.
        /// Equivalent to using the [OutputCache(PolicyName = "...")] attribute.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// app.MapGet("/products", GetProducts)
        ///    .CacheOutputWithPolicy("Products");
        /// 
        /// app.MapGet("/products/{id}", GetProductById)
        ///    .CacheOutputWithPolicy("Short");
        /// </code>
        /// </example>
        /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="policyName">The cache policy name.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        public static TBuilder CacheOutputWithPolicy<TBuilder>(this TBuilder builder, string policyName)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(policyName);

            return builder.CacheOutput(policyName);
        }

        /// <summary>
        /// Applies output caching to the endpoint with custom configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This extension method allows inline configuration of output caching for an endpoint.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// app.MapGet("/products", GetProducts)
        ///    .CacheOutputFor(TimeSpan.FromMinutes(10), "products");
        /// 
        /// app.MapGet("/search", SearchProducts)
        ///    .CacheOutputFor(
        ///        TimeSpan.FromMinutes(5),
        ///        tags: new[] { "search", "products" },
        ///        varyByQuery: new[] { "q", "page", "size" });
        /// </code>
        /// </example>
        /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="duration">The cache duration.</param>
        /// <param name="tags">Optional cache tags for invalidation.</param>
        /// <param name="varyByQuery">Optional query string keys to vary by.</param>
        /// <param name="varyByHeader">Optional headers to vary by.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        public static TBuilder CacheOutputFor<TBuilder>(
            this TBuilder builder,
            TimeSpan duration,
            string[]? tags = null,
            string[]? varyByQuery = null,
            string[]? varyByHeader = null)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.CacheOutput(policy =>
            {
                policy.Expire(duration);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        policy.Tag(tag);
                    }
                }

                if (varyByQuery != null && varyByQuery.Length > 0)
                {
                    policy.SetVaryByQuery(varyByQuery);
                }

                if (varyByHeader != null && varyByHeader.Length > 0)
                {
                    policy.SetVaryByHeader(varyByHeader);
                }
            });
        }

        /// <summary>
        /// Disables output caching for the endpoint.
        /// </summary>
        /// <example>
        /// <code>
        /// app.MapPost("/orders", CreateOrder)
        ///    .NoCacheOutput();
        /// </code>
        /// </example>
        /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        public static TBuilder NoCacheOutput<TBuilder>(this TBuilder builder)
            where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.CacheOutput(policy => policy.NoCache());
        }

        #endregion
    }

    /// <summary>
    /// Service for programmatic cache invalidation.
    /// </summary>
    public interface IOutputCacheInvalidator
    {
        /// <summary>
        /// Evicts all cache entries with the specified tag.
        /// </summary>
        /// <param name="tag">The cache tag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EvictByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Evicts all cache entries with any of the specified tags.
        /// </summary>
        /// <param name="tags">The cache tags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EvictByTagsAsync(string[] tags, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of <see cref="IOutputCacheInvalidator"/>.
    /// </summary>
    internal sealed class OutputCacheInvalidator : IOutputCacheInvalidator
    {
        private readonly IOutputCacheStore _cacheStore;

        public OutputCacheInvalidator(IOutputCacheStore cacheStore)
        {
            _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        }

        public async Task EvictByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(tag);
            await _cacheStore.EvictByTagAsync(tag, cancellationToken);
        }

        public async Task EvictByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tags);

            foreach (var tag in tags)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    await _cacheStore.EvictByTagAsync(tag, cancellationToken);
                }
            }
        }
    }
}

