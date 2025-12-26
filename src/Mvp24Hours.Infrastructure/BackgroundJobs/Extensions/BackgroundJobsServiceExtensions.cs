//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Extensions
{
    /// <summary>
    /// Extension methods for configuring background job services with dependency injection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods provide a convenient way to register background job providers
    /// and configure the job scheduler infrastructure. Multiple providers can be registered,
    /// and the default provider can be specified.
    /// </para>
    /// </remarks>
    public static class BackgroundJobsServiceExtensions
    {
        /// <summary>
        /// Adds background job services to the service collection with provider selection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for selecting and configuring the provider.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the background job infrastructure. You must specify a provider
        /// using one of the provider-specific methods:
        /// - <see cref="AddInMemoryBackgroundJobs"/>
        /// - <see cref="AddHangfireBackgroundJobs"/>
        /// - <see cref="AddQuartzBackgroundJobs"/>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddBackgroundJobs(builder =>
        /// {
        ///     builder.AddInMemoryProvider(); // For testing
        ///     // or
        ///     builder.AddHangfireProvider(options => { ... }); // For production
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBackgroundJobs(
            this IServiceCollection services,
            Action<IBackgroundJobsBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new BackgroundJobsBuilder(services);
            configure(builder);

            // Register the scheduler based on selected provider
            builder.Build();

            return services;
        }

        /// <summary>
        /// Adds in-memory background job provider (for testing and development).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This provider stores jobs in memory and executes them synchronously or asynchronously.
        /// Jobs are lost when the application restarts. Suitable for:
        /// - Unit testing
        /// - Integration testing
        /// - Development environments
        /// - Prototyping
        /// </para>
        /// <para>
        /// <strong>Not suitable for production use.</strong> Use <see cref="AddHangfireBackgroundJobs"/>
        /// or <see cref="AddQuartzBackgroundJobs"/> for production scenarios.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddInMemoryBackgroundJobs();
        /// </code>
        /// </example>
        public static IServiceCollection AddInMemoryBackgroundJobs(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IJobScheduler>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<InMemoryJobProvider>>();
                return new InMemoryJobProvider(serviceProvider, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Hangfire background job provider (for production use).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Hangfire options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This provider uses Hangfire for persistent, distributed background job processing.
        /// Requires Hangfire NuGet packages and database configuration.
        /// </para>
        /// <para>
        /// <strong>Required NuGet packages:</strong>
        /// - Hangfire.Core
        /// - Hangfire.AspNetCore
        /// - Hangfire.SqlServer (or Hangfire.PostgreSql, Hangfire.MySql, etc.)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHangfireBackgroundJobs(options =>
        /// {
        ///     options.ConnectionString = "Server=...;Database=Hangfire;...";
        ///     options.StorageProvider = HangfireStorageProvider.SqlServer;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddHangfireBackgroundJobs(
            this IServiceCollection services,
            Action<HangfireJobOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new HangfireJobOptions();
            configure?.Invoke(options);

            services.TryAddSingleton<Microsoft.Extensions.Options.IOptions<HangfireJobOptions>>(
                Microsoft.Extensions.Options.Options.Create(options));

            // Register HangfireJobProvider
            services.TryAddSingleton<IJobScheduler>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<HangfireJobProvider>>();
                var optionsSnapshot = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HangfireJobOptions>>();
                return new HangfireJobProvider(serviceProvider, optionsSnapshot, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds Quartz.NET background job provider (for production use).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Quartz options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This provider uses Quartz.NET for persistent, distributed background job processing.
        /// Requires Quartz NuGet packages and database configuration.
        /// </para>
        /// <para>
        /// <strong>Required NuGet packages:</strong>
        /// - Quartz
        /// - Quartz.Serialization.Json (optional, for JSON serialization)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddQuartzBackgroundJobs(options =>
        /// {
        ///     options.ConnectionString = "Server=...;Database=Quartz;...";
        ///     options.StorageProvider = QuartzStorageProvider.SqlServer;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddQuartzBackgroundJobs(
            this IServiceCollection services,
            Action<QuartzJobOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new QuartzJobOptions();
            configure?.Invoke(options);

            services.TryAddSingleton<Microsoft.Extensions.Options.IOptions<QuartzJobOptions>>(
                Microsoft.Extensions.Options.Options.Create(options));

            // Register QuartzJobProvider
            services.TryAddSingleton<IJobScheduler>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<QuartzJobProvider>>();
                var optionsSnapshot = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuartzJobOptions>>();
                return new QuartzJobProvider(serviceProvider, optionsSnapshot, logger);
            });

            return services;
        }
    }

    /// <summary>
    /// Builder interface for configuring background job providers.
    /// </summary>
    public interface IBackgroundJobsBuilder
    {
        /// <summary>
        /// Gets the service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Adds the in-memory provider.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        IBackgroundJobsBuilder AddInMemoryProvider();

        /// <summary>
        /// Adds the Hangfire provider.
        /// </summary>
        /// <param name="configure">Configuration action for Hangfire options.</param>
        /// <returns>The builder for chaining.</returns>
        IBackgroundJobsBuilder AddHangfireProvider(Action<HangfireJobOptions>? configure = null);

        /// <summary>
        /// Adds the Quartz.NET provider.
        /// </summary>
        /// <param name="configure">Configuration action for Quartz options.</param>
        /// <returns>The builder for chaining.</returns>
        IBackgroundJobsBuilder AddQuartzProvider(Action<QuartzJobOptions>? configure = null);
    }

    /// <summary>
    /// Default implementation of <see cref="IBackgroundJobsBuilder"/>.
    /// </summary>
    internal class BackgroundJobsBuilder : IBackgroundJobsBuilder
    {
        private string? _selectedProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobsBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public BackgroundJobsBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }

        /// <inheritdoc />
        public IBackgroundJobsBuilder AddInMemoryProvider()
        {
            Services.AddInMemoryBackgroundJobs();
            _selectedProvider = "InMemory";
            return this;
        }

        /// <inheritdoc />
        public IBackgroundJobsBuilder AddHangfireProvider(Action<HangfireJobOptions>? configure = null)
        {
            Services.AddHangfireBackgroundJobs(configure);
            _selectedProvider = "Hangfire";
            return this;
        }

        /// <inheritdoc />
        public IBackgroundJobsBuilder AddQuartzProvider(Action<QuartzJobOptions>? configure = null)
        {
            Services.AddQuartzBackgroundJobs(configure);
            _selectedProvider = "Quartz";
            return this;
        }

        /// <summary>
        /// Builds and finalizes the configuration.
        /// </summary>
        internal void Build()
        {
            if (string.IsNullOrWhiteSpace(_selectedProvider))
            {
                throw new InvalidOperationException(
                    "No background job provider was selected. Call one of: AddInMemoryProvider(), AddHangfireProvider(), or AddQuartzProvider().");
            }
        }
    }
}

