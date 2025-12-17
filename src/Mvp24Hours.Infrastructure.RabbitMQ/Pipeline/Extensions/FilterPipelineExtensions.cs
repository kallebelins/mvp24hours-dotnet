//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring RabbitMQ filter pipeline.
    /// </summary>
    public static class FilterPipelineExtensions
    {
        /// <summary>
        /// Adds the RabbitMQ filter pipeline to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQFilters(
            this IServiceCollection services,
            Action<FilterPipelineOptions>? configure = null)
        {
            var options = new FilterPipelineOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.TryAddSingleton<IFilterPipelineExecutor, FilterPipelineExecutor>();

            // Register default filters based on options
            RegisterDefaultFilters(services, options);

            return services;
        }

        /// <summary>
        /// Adds the RabbitMQ filter pipeline with all default filters enabled.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRabbitMQFiltersWithDefaults(
            this IServiceCollection services)
        {
            return services.AddMvp24HoursRabbitMQFilters(options =>
            {
                options.EnableLoggingFilter = true;
                options.EnableExceptionHandlingFilter = true;
                options.EnableCorrelationFilter = true;
                options.EnableTelemetryFilter = true;
                options.EnableValidationFilter = true;
            });
        }

        /// <summary>
        /// Adds a consume filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddConsumeFilter<TFilter>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, IConsumeFilter
        {
            services.Add(new ServiceDescriptor(typeof(IConsumeFilter), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds a typed consume filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddConsumeFilter<TFilter, TMessage>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, IConsumeFilter<TMessage>
            where TMessage : class
        {
            services.Add(new ServiceDescriptor(typeof(IConsumeFilter<TMessage>), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds a publish filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPublishFilter<TFilter>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, IPublishFilter
        {
            services.Add(new ServiceDescriptor(typeof(IPublishFilter), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds a typed publish filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPublishFilter<TFilter, TMessage>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, IPublishFilter<TMessage>
            where TMessage : class
        {
            services.Add(new ServiceDescriptor(typeof(IPublishFilter<TMessage>), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds a send filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSendFilter<TFilter>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, ISendFilter
        {
            services.Add(new ServiceDescriptor(typeof(ISendFilter), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds a typed send filter to the service collection.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSendFilter<TFilter, TMessage>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TFilter : class, ISendFilter<TMessage>
            where TMessage : class
        {
            services.Add(new ServiceDescriptor(typeof(ISendFilter<TMessage>), typeof(TFilter), lifetime));
            services.Add(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
            return services;
        }

        /// <summary>
        /// Adds the logging filters (consume, publish, and send).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQLoggingFilters(this IServiceCollection services)
        {
            services.TryAddSingleton<IConsumeFilter, LoggingConsumeFilter>();
            services.TryAddSingleton<IPublishFilter, LoggingPublishFilter>();
            services.TryAddSingleton<ISendFilter, LoggingSendFilter>();
            return services;
        }

        /// <summary>
        /// Adds the exception handling filter.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQExceptionHandlingFilter(
            this IServiceCollection services,
            Action<ExceptionHandlingFilterOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            services.TryAddSingleton<IConsumeFilter, ExceptionHandlingConsumeFilter>();
            return services;
        }

        /// <summary>
        /// Adds the correlation filters (consume, publish, and send).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQCorrelationFilters(this IServiceCollection services)
        {
            services.TryAddSingleton<IConsumeFilter, CorrelationConsumeFilter>();
            services.TryAddSingleton<IPublishFilter, CorrelationPublishFilter>();
            services.TryAddSingleton<ISendFilter, CorrelationSendFilter>();
            return services;
        }

        /// <summary>
        /// Adds the telemetry filters (consume, publish, and send).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQTelemetryFilters(this IServiceCollection services)
        {
            services.TryAddSingleton<IConsumeFilter, TelemetryConsumeFilter>();
            services.TryAddSingleton<IPublishFilter, TelemetryPublishFilter>();
            services.TryAddSingleton<ISendFilter, TelemetrySendFilter>();
            return services;
        }

        /// <summary>
        /// Adds the validation filters.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQValidationFilters(
            this IServiceCollection services,
            Action<ValidationFilterOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            services.TryAddSingleton<IConsumeFilter, ValidationConsumeFilter>();
            services.TryAddSingleton<IPublishFilter, ValidationPublishFilter>();
            return services;
        }

        private static void RegisterDefaultFilters(IServiceCollection services, FilterPipelineOptions options)
        {
            // Order matters: Correlation → Telemetry → Validation → ExceptionHandling → Logging
            // (reverse order because filters are executed in registration order)
            
            if (options.EnableCorrelationFilter)
            {
                services.AddRabbitMQCorrelationFilters();
            }

            if (options.EnableTelemetryFilter)
            {
                services.AddRabbitMQTelemetryFilters();
            }

            if (options.EnableValidationFilter)
            {
                services.AddRabbitMQValidationFilters();
            }

            if (options.EnableExceptionHandlingFilter)
            {
                services.AddRabbitMQExceptionHandlingFilter();
            }

            if (options.EnableLoggingFilter)
            {
                services.AddRabbitMQLoggingFilters();
            }
        }
    }
}

