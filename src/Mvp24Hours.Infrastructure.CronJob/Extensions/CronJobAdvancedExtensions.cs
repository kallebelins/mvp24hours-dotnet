//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Control;
using Mvp24Hours.Infrastructure.CronJob.Dependencies;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Extensions
{
    /// <summary>
    /// Extension methods for registering advanced CronJob services.
    /// </summary>
    public static class CronJobAdvancedExtensions
    {
        /// <summary>
        /// Adds all advanced CronJob infrastructure services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Optional configuration callback.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers:
        /// <list type="bullet">
        /// <item>ICronJobContextAccessor - For accessing execution context</item>
        /// <item>ICronJobStateStore - For persisting job state</item>
        /// <item>ICronJobController - For pause/resume control</item>
        /// <item>ICronJobDependencyTracker - For job dependencies</item>
        /// <item>ICronJobEventDispatcher - For lifecycle events</item>
        /// <item>IDistributedCronJobLock - For distributed locking</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddCronJobAdvancedInfrastructure(options =>
        /// {
        ///     options.UseDistributedLocking = true;
        ///     options.UseStatePersistence = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobAdvancedInfrastructure(
            this IServiceCollection services,
            Action<CronJobAdvancedOptions>? options = null)
        {
            Guard.Against.Null(services, nameof(services));

            var config = new CronJobAdvancedOptions();
            options?.Invoke(config);

            // Context accessor
            services.TryAddSingleton<ICronJobContextAccessor, CronJobContextAccessor>();

            // State store
            if (config.UseStatePersistence)
            {
                services.TryAddSingleton<ICronJobStateStore, InMemoryCronJobStateStore>();
            }

            // Controller
            if (config.UseController)
            {
                services.TryAddSingleton<ICronJobController, CronJobController>();
            }

            // Dependency tracker
            if (config.UseDependencies)
            {
                services.TryAddSingleton<ICronJobDependencyTracker, InMemoryCronJobDependencyTracker>();
            }

            // Event dispatcher
            if (config.UseEventHandlers)
            {
                services.TryAddSingleton<ICronJobEventDispatcher, CronJobEventDispatcher>();
            }

            // Distributed locking
            if (config.UseDistributedLocking)
            {
                services.TryAddSingleton<IDistributedCronJobLock, InMemoryDistributedCronJobLock>();
            }

            return services;
        }

        /// <summary>
        /// Adds a custom state store implementation.
        /// </summary>
        /// <typeparam name="TStore">The state store implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCronJobStateStore<TStore>(this IServiceCollection services)
            where TStore : class, ICronJobStateStore
        {
            Guard.Against.Null(services, nameof(services));
            services.AddSingleton<ICronJobStateStore, TStore>();
            return services;
        }

        /// <summary>
        /// Adds a custom distributed lock implementation.
        /// </summary>
        /// <typeparam name="TLock">The distributed lock implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCronJobDistributedLock<TLock>(this IServiceCollection services)
            where TLock : class, IDistributedCronJobLock
        {
            Guard.Against.Null(services, nameof(services));
            services.AddSingleton<IDistributedCronJobLock, TLock>();
            return services;
        }

        /// <summary>
        /// Adds an event handler for CronJob lifecycle events.
        /// </summary>
        /// <typeparam name="THandler">The event handler type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCronJobEventHandler<THandler>(this IServiceCollection services)
            where THandler : class, ICronJobEventHandler
        {
            Guard.Against.Null(services, nameof(services));

            var handlerType = typeof(THandler);

            // Register for each interface the handler implements
            if (typeof(ICronJobStartingHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobStartingHandler), handlerType);
            }
            if (typeof(ICronJobCompletedHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobCompletedHandler), handlerType);
            }
            if (typeof(ICronJobFailedHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobFailedHandler), handlerType);
            }
            if (typeof(ICronJobCancelledHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobCancelledHandler), handlerType);
            }
            if (typeof(ICronJobRetryHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobRetryHandler), handlerType);
            }
            if (typeof(ICronJobSkippedHandler).IsAssignableFrom(handlerType))
            {
                services.AddSingleton(typeof(ICronJobSkippedHandler), handlerType);
            }

            return services;
        }

        /// <summary>
        /// Registers a job dependency.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dependency">The dependency to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCronJobDependency(
            this IServiceCollection services,
            ICronJobDependency dependency)
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(dependency, nameof(dependency));

            // Register initialization action
            services.AddSingleton(sp =>
            {
                var tracker = sp.GetRequiredService<ICronJobDependencyTracker>();
                tracker.RegisterDependency(dependency);
                return dependency;
            });

            return services;
        }

        /// <summary>
        /// Configures a job dependency using a builder.
        /// </summary>
        /// <typeparam name="TDependentJob">The dependent job type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration callback.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddCronJobDependency&lt;ReportJob&gt;(builder =>
        /// {
        ///     builder.DependsOn&lt;DataProcessingJob&gt;()
        ///            .WithSuccessRequired()
        ///            .WithMaxAge(TimeSpan.FromHours(1));
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobDependency<TDependentJob>(
            this IServiceCollection services,
            Action<CronJobDependencyBuilder> configure)
            where TDependentJob : class
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configure, nameof(configure));

            var builder = CronJobDependency.For(typeof(TDependentJob).Name);
            configure(builder);

            return services.AddCronJobDependency(builder.Build());
        }
    }

    /// <summary>
    /// Options for configuring advanced CronJob infrastructure.
    /// </summary>
    public sealed class CronJobAdvancedOptions
    {
        /// <summary>
        /// Gets or sets whether to use state persistence.
        /// Default is true.
        /// </summary>
        public bool UseStatePersistence { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use the job controller.
        /// Default is true.
        /// </summary>
        public bool UseController { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use job dependencies.
        /// Default is true.
        /// </summary>
        public bool UseDependencies { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use event handlers.
        /// Default is true.
        /// </summary>
        public bool UseEventHandlers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use distributed locking.
        /// Default is false (single-instance only).
        /// </summary>
        public bool UseDistributedLocking { get; set; } = false;
    }
}

