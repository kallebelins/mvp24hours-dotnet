//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.ChangeStreams;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.GridFS;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Sharding;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Transactions;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced
{
    /// <summary>
    /// Extension methods for registering MongoDB advanced features in DI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions register the following advanced MongoDB features:
    /// <list type="bullet">
    ///   <item>Transaction management</item>
    ///   <item>GridFS file storage</item>
    ///   <item>Change Streams for real-time events</item>
    ///   <item>Full-text search</item>
    ///   <item>Time Series collections</item>
    ///   <item>Capped collections</item>
    ///   <item>Geospatial queries</item>
    ///   <item>Schema validation</item>
    ///   <item>Sharding management</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class MongoDbAdvancedExtensions
    {
        /// <summary>
        /// Adds all MongoDB advanced services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbAdvanced(this IServiceCollection services)
        {
            services.AddMvpMongoDbTransactions();
            services.AddMvpMongoDbGridFs();
            services.AddMvpMongoDbSchemaValidation();
            services.AddMvpMongoDbSharding();

            return services;
        }

        /// <summary>
        /// Adds MongoDB transaction management services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for transaction options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbTransactions(
            this IServiceCollection services,
            Action<MongoDbTransactionOptions> configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            services.AddScoped<IMongoDbTransactionManager>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                var options = configureOptions != null ? new MongoDbTransactionOptions() : null;
                configureOptions?.Invoke(options);
                return new MongoDbTransactionManager(client, options: options);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB GridFS file storage services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbGridFs(this IServiceCollection services)
        {
            services.AddScoped<IMongoDbGridFsService>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return new MongoDbGridFsService(database);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Change Stream services for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbChangeStream<TDocument>(
            this IServiceCollection services,
            string collectionName = null)
        {
            services.AddScoped<IMongoDbChangeStreamService<TDocument>>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<TDocument>(collectionName ?? typeof(TDocument).Name);
                return new MongoDbChangeStreamService<TDocument>(collection);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB full-text search services for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbTextSearch<TDocument>(
            this IServiceCollection services,
            string collectionName = null)
        {
            services.AddScoped<IMongoDbTextSearchService<TDocument>>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<TDocument>(collectionName ?? typeof(TDocument).Name);
                return new MongoDbTextSearchService<TDocument>(collection);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Time Series services for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="timeField">The time field name.</param>
        /// <param name="metaField">The metadata field name (optional).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbTimeSeries<TDocument>(
            this IServiceCollection services,
            string collectionName,
            string timeField,
            string metaField = null)
        {
            services.AddScoped<IMongoDbTimeSeriesService<TDocument>>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return new MongoDbTimeSeriesService<TDocument>(database, collectionName, timeField, metaField);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Capped Collection services for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbCappedCollection<TDocument>(
            this IServiceCollection services,
            string collectionName)
        {
            services.AddScoped<IMongoDbCappedCollectionService<TDocument>>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return new MongoDbCappedCollectionService<TDocument>(database, collectionName);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Geospatial services for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbGeospatial<TDocument>(
            this IServiceCollection services,
            string collectionName = null)
        {
            services.AddScoped<IMongoDbGeospatialService<TDocument>>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                var collection = database.GetCollection<TDocument>(collectionName ?? typeof(TDocument).Name);
                return new MongoDbGeospatialService<TDocument>(collection);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Schema Validation services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbSchemaValidation(this IServiceCollection services)
        {
            services.AddScoped<IMongoDbSchemaValidationService>(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return new MongoDbSchemaValidationService(database);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB Sharding management services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpMongoDbSharding(this IServiceCollection services)
        {
            services.AddScoped<IMongoDbShardingService>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return new MongoDbShardingService(client);
            });

            return services;
        }
    }
}

