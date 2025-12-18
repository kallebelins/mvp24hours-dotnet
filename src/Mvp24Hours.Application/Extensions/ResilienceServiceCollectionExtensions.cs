//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using System;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering resilience services.
    /// </summary>
    public static class ResilienceServiceCollectionExtensions
    {
        /// <summary>
        /// Adds resilience services (exception mapper, error localizer) to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure exception mapping options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpResilience(options =>
        /// {
        ///     options.IncludeExceptionDetails = env.IsDevelopment();
        ///     options.LogServerErrors = true;
        ///     
        ///     // Add custom mapping
        ///     options.AddMapping&lt;MyCustomException&gt;(
        ///         ResultStatusCode.DomainRuleViolation,
        ///         "CUSTOM.ERROR");
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpResilience(
            this IServiceCollection services,
            Action<ExceptionMappingOptions>? configureOptions = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ExceptionMappingOptions>(_ => { });
            }

            // Register services
            services.AddSingleton<IExceptionToResultMapper, ExceptionToResultMapper>();
            services.AddSingleton<IErrorMessageLocalizer, DefaultErrorMessageLocalizer>();

            return services;
        }

        /// <summary>
        /// Adds resilience services with a custom error message localizer.
        /// </summary>
        /// <typeparam name="TLocalizer">The localizer implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure exception mapping options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpResilience<TLocalizer>(
            this IServiceCollection services,
            Action<ExceptionMappingOptions>? configureOptions = null)
            where TLocalizer : class, IErrorMessageLocalizer
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ExceptionMappingOptions>(_ => { });
            }

            // Register services
            services.AddSingleton<IExceptionToResultMapper, ExceptionToResultMapper>();
            services.AddSingleton<IErrorMessageLocalizer, TLocalizer>();

            return services;
        }

        /// <summary>
        /// Adds resilience services with custom implementations.
        /// </summary>
        /// <typeparam name="TMapper">The exception mapper implementation type.</typeparam>
        /// <typeparam name="TLocalizer">The localizer implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure exception mapping options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpResilience<TMapper, TLocalizer>(
            this IServiceCollection services,
            Action<ExceptionMappingOptions>? configureOptions = null)
            where TMapper : class, IExceptionToResultMapper
            where TLocalizer : class, IErrorMessageLocalizer
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ExceptionMappingOptions>(_ => { });
            }

            // Register services
            services.AddSingleton<IExceptionToResultMapper, TMapper>();
            services.AddSingleton<IErrorMessageLocalizer, TLocalizer>();

            return services;
        }
    }
}

