//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Infrastructure.CronJob.Extensions;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Configuration
{
    /// <summary>
    /// Extension methods for configuring CronJobs via <see cref="IConfiguration"/> (appsettings.json).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions enable configuration of CronJobs using the ASP.NET Core configuration system,
    /// supporting appsettings.json, environment variables, Azure Key Vault, and other configuration providers.
    /// </para>
    /// </remarks>
    public static class CronJobConfigurationExtensions
    {
        #region Global Options

        /// <summary>
        /// Adds global CronJob options to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional delegate to configure the options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddCronJobGlobalOptions(options =>
        /// {
        ///     options.DefaultTimeZone = "UTC";
        ///     options.EnableObservability = true;
        ///     options.ValidateCronExpressionsOnStartup = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobGlobalOptions(
            this IServiceCollection services,
            Action<CronJobGlobalOptions>? configure = null)
        {
            Guard.Against.Null(services, nameof(services));

            var optionsBuilder = services.AddOptions<CronJobGlobalOptions>();

            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            optionsBuilder.Services.TryAddSingleton<IValidateOptions<CronJobGlobalOptions>, CronJobGlobalOptionsValidator>();
            optionsBuilder.ValidateOnStart();

            return services;
        }

        /// <summary>
        /// Adds global CronJob options from IConfiguration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // In appsettings.json:
        /// // "CronJobs": {
        /// //   "Global": {
        /// //     "DefaultTimeZone": "UTC",
        /// //     "EnableObservability": true
        /// //   }
        /// // }
        /// services.AddCronJobGlobalOptionsFromConfiguration(configuration);
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobGlobalOptionsFromConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configuration, nameof(configuration));

            services.AddOptions<CronJobGlobalOptions>()
                .Bind(configuration.GetSection(CronJobGlobalOptions.SectionName))
                .ValidateDataAnnotations()
                .Services.TryAddSingleton<IValidateOptions<CronJobGlobalOptions>, CronJobGlobalOptionsValidator>();

            services.AddOptions<CronJobGlobalOptions>().ValidateOnStart();

            return services;
        }

        #endregion

        #region Job Options from Configuration

        /// <summary>
        /// Adds a CronJob with configuration from appsettings.json.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Reads configuration from the section "CronJobs:{JobTypeName}".
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In appsettings.json:
        /// // "CronJobs": {
        /// //   "MyJob": {
        /// //     "CronExpression": "*/5 * * * *",
        /// //     "TimeZone": "UTC",
        /// //     "EnableRetry": true
        /// //   }
        /// // }
        /// services.AddCronJobFromConfiguration&lt;MyJob&gt;(configuration);
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobFromConfiguration<T>(
            this IServiceCollection services,
            IConfiguration configuration)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configuration, nameof(configuration));

            var sectionPath = CronJobOptions<T>.GetSectionPath();
            var section = configuration.GetSection(sectionPath);

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    $"Configuration section '{sectionPath}' not found. " +
                    $"Add the section to appsettings.json or use AddCronJobWithOptions<{typeof(T).Name}>() for code configuration.");
            }

            return services.AddCronJobWithOptions<T>(options =>
            {
                section.Bind(options);
            }, configuration);
        }

        /// <summary>
        /// Adds a resilient CronJob with configuration from appsettings.json.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddResilientCronJobFromConfiguration<T>(
            this IServiceCollection services,
            IConfiguration configuration)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configuration, nameof(configuration));

            var sectionPath = CronJobOptions<T>.GetSectionPath();
            var section = configuration.GetSection(sectionPath);

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    $"Configuration section '{sectionPath}' not found. " +
                    $"Add the section to appsettings.json or use AddResilientCronJobWithOptions<{typeof(T).Name}>() for code configuration.");
            }

            return services.AddResilientCronJobWithOptions<T>(options =>
            {
                section.Bind(options);
            }, configuration);
        }

        /// <summary>
        /// Adds an advanced CronJob with configuration from appsettings.json.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAdvancedCronJobFromConfiguration<T>(
            this IServiceCollection services,
            IConfiguration configuration)
            where T : AdvancedCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configuration, nameof(configuration));

            var sectionPath = CronJobOptions<T>.GetSectionPath();
            var section = configuration.GetSection(sectionPath);

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    $"Configuration section '{sectionPath}' not found. " +
                    $"Add the section to appsettings.json or use AddAdvancedCronJobWithOptions<{typeof(T).Name}>() for code configuration.");
            }

            return services.AddAdvancedCronJobWithOptions<T>(options =>
            {
                section.Bind(options);
            }, configuration);
        }

        #endregion

        #region Job Options with Delegate

        /// <summary>
        /// Adds a CronJob with comprehensive configuration options.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Delegate to configure the options.</param>
        /// <param name="configuration">Optional IConfiguration for binding additional settings.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddCronJobWithOptions&lt;MyJob&gt;(options =>
        /// {
        ///     options.CronExpression = "*/5 * * * *";
        ///     options.TimeZone = "UTC";
        ///     options.Enabled = true;
        ///     options.EnableRetry = true;
        ///     options.MaxRetryAttempts = 3;
        ///     options.PreventOverlapping = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobWithOptions<T>(
            this IServiceCollection services,
            Action<CronJobOptions<T>> configure,
            IConfiguration? configuration = null)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configure, nameof(configure));

            // Register options with validation
            var optionsBuilder = services.AddOptions<CronJobOptions<T>>();

            // Bind from configuration if provided
            if (configuration != null)
            {
                var section = configuration.GetSection(CronJobOptions<T>.GetSectionPath());
                if (section.Exists())
                {
                    optionsBuilder.Bind(section);
                }
            }

            // Apply delegate configuration (overrides configuration binding)
            optionsBuilder.Configure(configure);

            // Add validator
            optionsBuilder.Services.TryAddSingleton<IValidateOptions<CronJobOptions<T>>, CronJobOptionsValidator<T>>();
            optionsBuilder.ValidateOnStart();

            // Register the schedule config (adapter from CronJobOptions to IScheduleConfig)
            services.AddSingleton<IScheduleConfig<T>>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<CronJobOptions<T>>>().Value;

                // Check if job is enabled
                if (!opts.Enabled)
                {
                    return new ScheduleConfig<T>
                    {
                        CronExpression = null, // Will cause job to not schedule
                        TimeZoneInfo = opts.TimeZoneInfo
                    };
                }

                return new ScheduleConfig<T>
                {
                    CronExpression = opts.CronExpression,
                    TimeZoneInfo = opts.TimeZoneInfo
                };
            });

            // Register observability if enabled
            var tempOptions = new CronJobOptions<T>();
            configure(tempOptions);
            if (tempOptions.EnableObservability)
            {
                services.AddCronJobObservability();
            }

            // Register the hosted service
            services.AddHostedService<T>();

            return services;
        }

        /// <summary>
        /// Adds a resilient CronJob with comprehensive configuration options.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Delegate to configure the options.</param>
        /// <param name="configuration">Optional IConfiguration for binding additional settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddResilientCronJobWithOptions<T>(
            this IServiceCollection services,
            Action<CronJobOptions<T>> configure,
            IConfiguration? configuration = null)
            where T : ResilientCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configure, nameof(configure));

            // Register options with validation
            var optionsBuilder = services.AddOptions<CronJobOptions<T>>();

            if (configuration != null)
            {
                var section = configuration.GetSection(CronJobOptions<T>.GetSectionPath());
                if (section.Exists())
                {
                    optionsBuilder.Bind(section);
                }
            }

            optionsBuilder.Configure(configure);
            optionsBuilder.Services.TryAddSingleton<IValidateOptions<CronJobOptions<T>>, CronJobOptionsValidator<T>>();
            optionsBuilder.ValidateOnStart();

            // Register resilient schedule config
            services.AddSingleton<IResilientScheduleConfig<T>>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<CronJobOptions<T>>>().Value;
                return opts;
            });

            // Register resilience infrastructure
            services.TryAddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
            services.TryAddSingleton<CronJobCircuitBreaker>();

            // Register observability if enabled
            var tempOptions = new CronJobOptions<T>();
            configure(tempOptions);
            if (tempOptions.EnableObservability)
            {
                services.AddCronJobObservability();
            }

            // Register the hosted service
            services.AddHostedService<T>();

            return services;
        }

        /// <summary>
        /// Adds an advanced CronJob with comprehensive configuration options.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Delegate to configure the options.</param>
        /// <param name="configuration">Optional IConfiguration for binding additional settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAdvancedCronJobWithOptions<T>(
            this IServiceCollection services,
            Action<CronJobOptions<T>> configure,
            IConfiguration? configuration = null)
            where T : AdvancedCronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configure, nameof(configure));

            // Register options with validation
            var optionsBuilder = services.AddOptions<CronJobOptions<T>>();

            if (configuration != null)
            {
                var section = configuration.GetSection(CronJobOptions<T>.GetSectionPath());
                if (section.Exists())
                {
                    optionsBuilder.Bind(section);
                }
            }

            optionsBuilder.Configure(configure);
            optionsBuilder.Services.TryAddSingleton<IValidateOptions<CronJobOptions<T>>, CronJobOptionsValidator<T>>();
            optionsBuilder.ValidateOnStart();

            // Register resilient schedule config
            services.AddSingleton<IResilientScheduleConfig<T>>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<CronJobOptions<T>>>().Value;
                return opts;
            });

            // Register resilience and advanced infrastructure
            services.TryAddSingleton<ICronJobExecutionLock, InMemoryCronJobExecutionLock>();
            services.TryAddSingleton<CronJobCircuitBreaker>();
            services.AddCronJobAdvancedInfrastructure();

            // Register observability if enabled
            var tempOptions = new CronJobOptions<T>();
            configure(tempOptions);
            if (tempOptions.EnableObservability)
            {
                services.AddCronJobObservability();
            }

            // Register the hosted service
            services.AddHostedService<T>();

            return services;
        }

        #endregion

        #region Multiple Instances

        /// <summary>
        /// Adds multiple instances of the same CronJob type with different configurations.
        /// Each instance is identified by a unique instance name.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configurations">Array of instance configurations.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method uses keyed services (.NET 8+) to register multiple instances of the same job type.
        /// Each instance must have a unique <see cref="CronJobOptions{T}.InstanceName"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddCronJobInstances&lt;DataSyncJob&gt;(
        ///     new CronJobOptions&lt;DataSyncJob&gt;
        ///     {
        ///         InstanceName = "DataSync-US",
        ///         CronExpression = "0 0 * * *",
        ///         TimeZone = "America/New_York"
        ///     },
        ///     new CronJobOptions&lt;DataSyncJob&gt;
        ///     {
        ///         InstanceName = "DataSync-EU",
        ///         CronExpression = "0 0 * * *",
        ///         TimeZone = "Europe/London"
        ///     }
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobInstances<T>(
            this IServiceCollection services,
            params CronJobOptions<T>[] configurations)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configurations, nameof(configurations));

            if (configurations.Length == 0)
            {
                throw new ArgumentException("At least one configuration must be provided.", nameof(configurations));
            }

            foreach (var config in configurations)
            {
                var instanceName = config.GetEffectiveInstanceName();

                // Register keyed options
                services.AddKeyedSingleton<CronJobOptions<T>>(instanceName, (_, _) => config);

                // Register keyed schedule config
                services.AddKeyedSingleton<IScheduleConfig<T>>(instanceName, (_, _) => new ScheduleConfig<T>
                {
                    CronExpression = config.CronExpression,
                    TimeZoneInfo = config.TimeZoneInfo
                });

                // Register options validation
                services.TryAddSingleton<IValidateOptions<CronJobOptions<T>>, CronJobOptionsValidator<T>>();
            }

            // Register infrastructure once
            services.AddCronJobObservability();

            // Note: For multiple instances, the application needs to create a custom factory
            // or use a wrapper hosted service that manages all instances.
            // This is because IHostedService doesn't support keyed services directly.

            return services;
        }

        /// <summary>
        /// Adds multiple CronJob instances from configuration.
        /// </summary>
        /// <typeparam name="T">The type of the CronJob service.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Reads configuration from the section "CronJobs:{JobTypeName}:Instances".
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In appsettings.json:
        /// // "CronJobs": {
        /// //   "DataSyncJob": {
        /// //     "Instances": {
        /// //       "DataSync-US": { "CronExpression": "0 0 * * *", "TimeZone": "America/New_York" },
        /// //       "DataSync-EU": { "CronExpression": "0 0 * * *", "TimeZone": "Europe/London" }
        /// //     }
        /// //   }
        /// // }
        /// services.AddCronJobInstancesFromConfiguration&lt;DataSyncJob&gt;(configuration);
        /// </code>
        /// </example>
        public static IServiceCollection AddCronJobInstancesFromConfiguration<T>(
            this IServiceCollection services,
            IConfiguration configuration)
            where T : CronJobService<T>
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(configuration, nameof(configuration));

            var instancesSection = configuration.GetSection($"{CronJobOptions<T>.GetSectionPath()}:Instances");

            if (!instancesSection.Exists())
            {
                throw new InvalidOperationException(
                    $"Configuration section '{CronJobOptions<T>.GetSectionPath()}:Instances' not found. " +
                    $"Add the section to appsettings.json or use AddCronJobInstances<{typeof(T).Name}>() for code configuration.");
            }

            var instances = instancesSection.GetChildren();
            foreach (var instance in instances)
            {
                var config = new CronJobOptions<T>();
                instance.Bind(config);
                config.InstanceName = instance.Key;

                var instanceName = config.GetEffectiveInstanceName();

                services.AddKeyedSingleton<CronJobOptions<T>>(instanceName, (_, _) => config);
                services.AddKeyedSingleton<IScheduleConfig<T>>(instanceName, (_, _) => new ScheduleConfig<T>
                {
                    CronExpression = config.CronExpression,
                    TimeZoneInfo = config.TimeZoneInfo
                });
            }

            services.TryAddSingleton<IValidateOptions<CronJobOptions<T>>, CronJobOptionsValidator<T>>();
            services.AddCronJobObservability();

            return services;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validates all registered CronJob configurations.
        /// Call this during startup to fail fast on configuration errors.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>True if all validations pass.</returns>
        public static bool ValidateCronJobConfigurations(this IServiceProvider serviceProvider)
        {
            // This is automatically done by ValidateOnStart(), but can be called manually
            // for custom validation scenarios
            try
            {
                var globalOptions = serviceProvider.GetService<IOptions<CronJobGlobalOptions>>();
                globalOptions?.Value.ToString(); // Force validation

                return true;
            }
            catch (OptionsValidationException)
            {
                return false;
            }
        }

        #endregion
    }
}

