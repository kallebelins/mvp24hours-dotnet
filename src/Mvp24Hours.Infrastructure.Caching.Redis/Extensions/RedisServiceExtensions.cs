//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Reflection;

namespace Mvp24Hours.Extensions
{
    public static class RedisServiceExtensions
    {
        /// <summary>
        /// See settings at: https://stackexchange.github.io/StackExchange.Redis/Configuration.html
        /// </summary>
        public static IServiceCollection AddMvp24HoursCachingRedis(this IServiceCollection services,
            ConfigurationOptions configurationOptions,
            string instanceName = null)
        {
            if (configurationOptions == null)
            {
                throw new ArgumentNullException(nameof(configurationOptions), "Configuration options is required.");
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = configurationOptions;
                options.InstanceName = $"{(instanceName ?? Assembly.GetEntryAssembly().GetName().Name.Replace(".", "_"))}".ToLower();
            });

            return services;
        }

        /// <summary>
        /// See settings at: https://stackexchange.github.io/StackExchange.Redis/Configuration.html
        /// </summary>
        public static IServiceCollection AddMvp24HoursCachingRedis(this IServiceCollection services,
            string connectionString,
            string instanceName = null)
        {
            if (!connectionString.HasValue())
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection strings is required.");
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = $"{instanceName ?? Assembly.GetEntryAssembly().GetName().Name.Replace(".", "_")}_".ToLower();
            });

            return services;
        }
    }
}
