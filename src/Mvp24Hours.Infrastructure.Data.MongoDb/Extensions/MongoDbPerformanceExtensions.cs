//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.ConnectionPool;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring MongoDB performance features.
    /// </summary>
    public static class MongoDbPerformanceExtensions
    {
        /// <summary>
        /// Adds the MongoDB index manager service for automatic index creation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .AddMvp24HoursRepositoryAsync()
        ///         .AddMongoDbIndexManager();
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbIndexManager(this IServiceCollection services)
        {
            services.TryAddSingleton<IMongoDbIndexManager, MongoDbIndexManager>();
            return services;
        }

        /// <summary>
        /// Ensures all indexes are created for entities in the specified assembly.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assembly">The assembly containing entity types.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method should be called after AddMongoDbIndexManager and will execute
        /// during application startup via hosted service.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMongoDbIndexManager()
        ///         .EnsureMongoDbIndexes(typeof(Customer).Assembly);
        /// </code>
        /// </example>
        public static IServiceCollection EnsureMongoDbIndexes(
            this IServiceCollection services,
            Assembly assembly)
        {
            services.AddHostedService(sp =>
                new IndexCreationHostedService(sp, assembly));

            return services;
        }

        /// <summary>
        /// Configures advanced MongoDB connection pool settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options => { ... })
        ///         .ConfigureMongoDbConnectionPool(pool =>
        ///         {
        ///             pool.MinPoolSize = 10;
        ///             pool.MaxPoolSize = 200;
        ///             pool.WaitQueueTimeoutSeconds = 30;
        ///         });
        /// </code>
        /// </example>
        public static IServiceCollection ConfigureMongoDbConnectionPool(
            this IServiceCollection services,
            Action<MongoDbConnectionPoolOptions> configure)
        {
            services.Configure(configure);

            // Update MongoDB options to include connection pool settings
            services.PostConfigure<MongoDbOptions>(options =>
            {
                var poolOptions = new MongoDbConnectionPoolOptions();
                configure(poolOptions);

                // Apply pool settings via MongoDB options
                options.MaxConnectionPoolSize = poolOptions.MaxPoolSize;
                options.MinConnectionPoolSize = poolOptions.MinPoolSize;
                options.ConnectionTimeoutSeconds = poolOptions.ConnectTimeoutSeconds;
                options.SocketTimeoutSeconds = poolOptions.SocketTimeoutSeconds;
            });

            return services;
        }
    }

    /// <summary>
    /// Hosted service for creating MongoDB indexes on startup.
    /// </summary>
    internal class IndexCreationHostedService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Assembly _assembly;

        public IndexCreationHostedService(IServiceProvider serviceProvider, Assembly assembly)
        {
            _serviceProvider = serviceProvider;
            _assembly = assembly;
        }

        protected override async Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var indexManager = scope.ServiceProvider.GetService<IMongoDbIndexManager>();
            var context = scope.ServiceProvider.GetService<Mvp24HoursContext>();

            if (indexManager != null && context != null)
            {
                await indexManager.EnsureAllIndexesAsync(context.Database, _assembly, stoppingToken);
            }
        }
    }
}

