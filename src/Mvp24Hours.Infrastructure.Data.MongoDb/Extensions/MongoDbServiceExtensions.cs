//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for registering MongoDB services in the DI container.
    /// </summary>
    public static class MongoDbServiceExtensions
    {
        /// <summary>
        /// Add database context services
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext(this IServiceCollection services,
            Action<MongoDbOptions> options = null,
            Func<IServiceProvider, Mvp24HoursContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursDbContext<Mvp24HoursContext>(options, dbFactory, lifetime);
        }

        /// <summary>
        /// Add database context services
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext<DbContext>(this IServiceCollection services,
            Action<MongoDbOptions> options = null,
            Func<IServiceProvider, DbContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where DbContext : Mvp24HoursContext
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<MongoDbOptions>(options => { });
            }

            if (dbFactory != null)
            {
                services.Add(new ServiceDescriptor(typeof(Mvp24HoursContext), dbFactory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(Mvp24HoursContext), typeof(DbContext), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepository(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type repository = null,
            Type unitOfWork = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(repositoryOptions => { });
            }

            if (unitOfWork != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWork), unitOfWork, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWork), typeof(UnitOfWork), lifetime));
            }

            if (repository != null)
            {
                services.Add(new ServiceDescriptor(typeof(IRepository<>), repository, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(Repository<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepositoryAsync(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type repositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(repositoryOptions => { });
            }

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            if (repositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), repositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(RepositoryAsync<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add async repository with interceptor support for auditing, soft delete, and logging.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="unitOfWorkAsync">Custom UnitOfWork implementation type (optional).</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="RepositoryAsyncWithInterceptors{T}"/> which supports
        /// the MongoDB interceptor pipeline for automatic audit fields, soft delete, and logging.
        /// </para>
        /// <para>
        /// After calling this method, add interceptors using the extension methods:
        /// <code>
        /// services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        ///         .AddMongoDbAuditInterceptor()
        ///         .AddMongoDbSoftDeleteInterceptor()
        ///         .AddMongoDbCommandLogger();
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic setup with all standard interceptors:
        /// services.AddMvp24HoursDbContext(options => 
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursRepositoryAsyncWithInterceptors()
        /// .AddAllMongoDbInterceptors(options =>
        /// {
        ///     options.EnableAuditInterceptor = true;
        ///     options.EnableSoftDelete = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursRepositoryAsyncWithInterceptors(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(options => { });
            }

            // Register the interceptor pipeline (if not already registered)
            services.TryAddScoped<IMongoDbInterceptorPipeline, MongoDbInterceptorPipeline>();

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            // Register the repository with interceptor support
            services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(RepositoryAsyncWithInterceptors<>), lifetime));

            return services;
        }

        /// <summary>
        /// Add bulk operations repository for high-performance batch operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="bulkOperationsRepositoryAsync">Custom bulk operations repository type (optional).</param>
        /// <param name="unitOfWorkAsync">Custom UnitOfWork implementation type (optional).</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="BulkOperationsRepositoryAsync{T}"/> which provides
        /// optimized MongoDB bulk operations:
        /// <list type="bullet">
        ///   <item><b>BulkInsertAsync</b> - Uses InsertMany for efficient multi-document inserts</item>
        ///   <item><b>BulkUpdateAsync</b> - Uses BulkWrite with ReplaceOne for batch updates</item>
        ///   <item><b>BulkDeleteAsync</b> - Uses BulkWrite with DeleteOne for batch deletes</item>
        ///   <item><b>UpdateManyAsync</b> - Updates all documents matching a filter</item>
        ///   <item><b>DeleteManyAsync</b> - Deletes all documents matching a filter</item>
        /// </list>
        /// </para>
        /// <para>
        /// MongoDB-specific features supported:
        /// <list type="bullet">
        ///   <item><b>Ordered vs Unordered</b> - Control execution order and error handling</item>
        ///   <item><b>Write Concern</b> - Configure durability guarantees</item>
        ///   <item><b>Bypass Validation</b> - Skip server-side document validation</item>
        ///   <item><b>Progress Callback</b> - Monitor progress of long-running operations</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register bulk operations repository
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursBulkOperationsRepositoryAsync();
        /// 
        /// // Use in service
        /// public class ImportService
        /// {
        ///     private readonly IBulkOperationsMongoDbAsync&lt;Customer&gt; _repository;
        ///     
        ///     public async Task ImportAsync(IList&lt;Customer&gt; customers)
        ///     {
        ///         var options = new MongoDbBulkOperationOptions
        ///         {
        ///             IsOrdered = false, // Better performance
        ///             BatchSize = 5000,
        ///             ProgressCallback = (processed, total) =&gt; 
        ///                 Console.WriteLine($"Progress: {processed}/{total}")
        ///         };
        ///         
        ///         var result = await _repository.BulkInsertAsync(customers, options);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursBulkOperationsRepositoryAsync(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type bulkOperationsRepositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(options => { });
            }

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.TryAdd(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            if (bulkOperationsRepositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsMongoDbAsync<>), bulkOperationsRepositoryAsync, lifetime));
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsAsync<>), bulkOperationsRepositoryAsync, lifetime));
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), bulkOperationsRepositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsMongoDbAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
                services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add bulk operations repository with interceptor support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="unitOfWorkAsync">Custom UnitOfWork implementation type (optional).</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method combines bulk operations with the interceptor pipeline,
        /// enabling automatic audit fields, soft delete, and logging alongside
        /// high-performance bulk operations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursBulkOperationsRepositoryAsyncWithInterceptors()
        /// .AddMongoDbAuditInterceptor()
        /// .AddMongoDbSoftDeleteInterceptor();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursBulkOperationsRepositoryAsyncWithInterceptors(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(options => { });
            }

            // Register the interceptor pipeline (if not already registered)
            services.TryAddScoped<IMongoDbInterceptorPipeline, MongoDbInterceptorPipeline>();

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.TryAdd(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            // Register the bulk operations repository
            services.Add(new ServiceDescriptor(typeof(IBulkOperationsMongoDbAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
            services.Add(new ServiceDescriptor(typeof(IBulkOperationsAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
            services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));

            return services;
        }

        /// <summary>
        /// Add read-only repository with Specification Pattern support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="readOnlyRepository">Custom read-only repository type (optional).</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="ReadOnlyRepository{T}"/> which provides
        /// query-only access to entities with full Specification Pattern support:
        /// <list type="bullet">
        ///   <item><b>GetBySpecification</b> - Retrieve entities matching a specification</item>
        ///   <item><b>CountBySpecification</b> - Count entities matching a specification</item>
        ///   <item><b>AnyBySpecification</b> - Check if any entities match a specification</item>
        ///   <item><b>GetByKeysetPagination</b> - Efficient cursor-based pagination</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use this for CQRS query handlers and read-only scenarios where write operations
        /// should not be available.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register read-only repository
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursReadOnlyRepository();
        /// 
        /// // Use in query handler
        /// public class GetActiveCustomersQueryHandler
        /// {
        ///     private readonly IReadOnlyRepository&lt;Customer&gt; _repository;
        ///     
        ///     public IList&lt;Customer&gt; Handle(GetActiveCustomersQuery query)
        ///     {
        ///         var spec = new ActiveCustomerSpecification();
        ///         return _repository.GetBySpecification(spec);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOnlyRepository(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type readOnlyRepository = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(options => { });
            }

            if (readOnlyRepository != null)
            {
                services.Add(new ServiceDescriptor(typeof(IReadOnlyRepository<>), readOnlyRepository, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add async read-only repository with Specification Pattern support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="readOnlyRepositoryAsync">Custom async read-only repository type (optional).</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers <see cref="ReadOnlyRepositoryAsync{T}"/> which provides
        /// async query-only access to entities with full Specification Pattern support:
        /// <list type="bullet">
        ///   <item><b>GetBySpecificationAsync</b> - Retrieve entities matching a specification</item>
        ///   <item><b>CountBySpecificationAsync</b> - Count entities matching a specification</item>
        ///   <item><b>AnyBySpecificationAsync</b> - Check if any entities match a specification</item>
        ///   <item><b>GetByKeysetPaginationAsync</b> - Efficient cursor-based pagination</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use this for CQRS query handlers and read-only scenarios where write operations
        /// should not be available. Prefer async methods for better scalability.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register async read-only repository
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursReadOnlyRepositoryAsync();
        /// 
        /// // Use in async query handler
        /// public class GetActiveCustomersQueryHandler
        /// {
        ///     private readonly IReadOnlyRepositoryAsync&lt;Customer&gt; _repository;
        ///     
        ///     public async Task&lt;IList&lt;Customer&gt;&gt; HandleAsync(GetActiveCustomersQuery query, CancellationToken ct)
        ///     {
        ///         var spec = new ActiveCustomerSpecification();
        ///         return await _repository.GetBySpecificationAsync(spec, ct);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOnlyRepositoryAsync(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            Type readOnlyRepositoryAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (repositoryOptions != null)
            {
                services.Configure(repositoryOptions);
            }
            else
            {
                services.Configure<MongoDbRepositoryOptions>(options => { });
            }

            if (readOnlyRepositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IReadOnlyRepositoryAsync<>), readOnlyRepositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IReadOnlyRepositoryAsync<>), typeof(ReadOnlyRepositoryAsync<>), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add both sync and async read-only repositories with Specification Pattern support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="repositoryOptions">Repository configuration options.</param>
        /// <param name="lifetime">Service lifetime. Defaults to Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers both <see cref="ReadOnlyRepository{T}"/> and 
        /// <see cref="ReadOnlyRepositoryAsync{T}"/> for complete read-only access.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(options =>
        /// {
        ///     options.DatabaseName = "MyDatabase";
        ///     options.ConnectionString = "mongodb://localhost:27017";
        /// })
        /// .AddMvp24HoursReadOnlyRepositories();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOnlyRepositories(this IServiceCollection services,
            Action<MongoDbRepositoryOptions> repositoryOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services
                .AddMvp24HoursReadOnlyRepository(repositoryOptions, lifetime: lifetime)
                .AddMvp24HoursReadOnlyRepositoryAsync(repositoryOptions, lifetime: lifetime);
        }
    }
}
