//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Extension methods for registering scheduled command services.
    /// </summary>
    public static class SchedulingExtensions
    {
        /// <summary>
        /// Adds scheduled command services with in-memory store.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Optional options configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMvpScheduledCommands(
            this IServiceCollection services,
            Action<ScheduledCommandOptions>? configureOptions = null)
        {
            return services.AddMvpScheduledCommands<InMemoryScheduledCommandStore>(configureOptions);
        }

        /// <summary>
        /// Adds scheduled command services with a custom store.
        /// </summary>
        /// <typeparam name="TStore">The store implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Optional options configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMvpScheduledCommands<TStore>(
            this IServiceCollection services,
            Action<ScheduledCommandOptions>? configureOptions = null)
            where TStore : class, IScheduledCommandStore
        {
            var options = new ScheduledCommandOptions();
            configureOptions?.Invoke(options);

            // Register store
            services.AddSingleton<IScheduledCommandStore, TStore>();

            // Register scheduler
            services.AddScoped<ICommandScheduler, CommandScheduler>();

            // Register options
            services.AddSingleton(options);

            // Register hosted service if enabled
            if (options.Enabled)
            {
                services.AddHostedService<ScheduledCommandHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds scheduled command services with a custom store instance.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="store">The store instance</param>
        /// <param name="configureOptions">Optional options configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMvpScheduledCommands(
            this IServiceCollection services,
            IScheduledCommandStore store,
            Action<ScheduledCommandOptions>? configureOptions = null)
        {
            var options = new ScheduledCommandOptions();
            configureOptions?.Invoke(options);

            // Register store instance
            services.AddSingleton(store);

            // Register scheduler
            services.AddScoped<ICommandScheduler, CommandScheduler>();

            // Register options
            services.AddSingleton(options);

            // Register hosted service if enabled
            if (options.Enabled)
            {
                services.AddHostedService<ScheduledCommandHostedService>();
            }

            return services;
        }

        /// <summary>
        /// Adds only the command scheduler without the background processor.
        /// Useful when you want to process commands manually or in a different way.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMvpCommandSchedulerOnly(this IServiceCollection services)
        {
            services.AddSingleton<IScheduledCommandStore, InMemoryScheduledCommandStore>();
            services.AddScoped<ICommandScheduler, CommandScheduler>();
            return services;
        }

        /// <summary>
        /// Adds only the command scheduler with a custom store.
        /// </summary>
        /// <typeparam name="TStore">The store implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMvpCommandSchedulerOnly<TStore>(this IServiceCollection services)
            where TStore : class, IScheduledCommandStore
        {
            services.AddSingleton<IScheduledCommandStore, TStore>();
            services.AddScoped<ICommandScheduler, CommandScheduler>();
            return services;
        }
    }
}

