//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using System;
using System.Runtime.CompilerServices;

namespace Mvp24Hours.Extensions
{
    public static class EFCoreServiceExtensions
    {
        /// <summary>
        /// Add database context
        /// </summary>
        public static IServiceCollection AddMvp24HoursDbContext<TDbContext>(this IServiceCollection services,
            Func<IServiceProvider, TDbContext> dbFactory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where TDbContext : DbContext
        {
            if (dbFactory != null)
            {
                services.Add(new ServiceDescriptor(typeof(DbContext), dbFactory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(DbContext), typeof(TDbContext), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add repository
        /// </summary>
        public static IServiceCollection AddMvp24HoursRepository(this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type repository = null,
            Type unitOfWork = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(options => { });
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
            Action<EFCoreRepositoryOptions> options = null,
            Type repositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(options => { });
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

        #region Multi-tenancy

        /// <summary>
        /// Adds multi-tenancy support with the specified tenant provider.
        /// </summary>
        /// <typeparam name="TTenantProvider">The tenant provider implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the tenant provider for dependency injection.
        /// Use this in combination with tenant query filters in your DbContext.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursTenantProvider&lt;HttpHeaderTenantProvider&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursTenantProvider<TTenantProvider>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TTenantProvider : class, ITenantProvider
        {
            services.Add(new ServiceDescriptor(typeof(ITenantProvider), typeof(TTenantProvider), lifetime));
            return services;
        }

        /// <summary>
        /// Adds multi-tenancy support with a factory-based tenant provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory function to create the tenant provider.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursTenantProvider(sp =>
        /// {
        ///     var httpContext = sp.GetRequiredService&lt;IHttpContextAccessor&gt;();
        ///     return new HttpHeaderTenantProvider(httpContext);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursTenantProvider(
            this IServiceCollection services,
            Func<IServiceProvider, ITenantProvider> factory,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(typeof(ITenantProvider), factory, lifetime));
            return services;
        }

        /// <summary>
        /// Adds the tenant save changes interceptor for automatic TenantId assignment.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for the interceptor.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The interceptor automatically:
        /// <list type="bullet">
        /// <item>Sets TenantId on new entities</item>
        /// <item>Validates TenantId on modified entities</item>
        /// <item>Prevents cross-tenant data access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Remember to register the interceptor with your DbContext:
        /// <code>
        /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
        /// {
        ///     options.AddInterceptors(sp.GetRequiredService&lt;TenantSaveChangesInterceptor&gt;());
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMvp24HoursTenantInterceptor(
            this IServiceCollection services,
            Action<TenantInterceptorOptions> configureOptions = null)
        {
            var options = new TenantInterceptorOptions();
            configureOptions?.Invoke(options);

            services.AddScoped(sp =>
            {
                var tenantProvider = sp.GetRequiredService<ITenantProvider>();
                return new TenantSaveChangesInterceptor(tenantProvider, options);
            });

            return services;
        }

        /// <summary>
        /// Adds complete multi-tenancy support including provider and interceptor.
        /// </summary>
        /// <typeparam name="TTenantProvider">The tenant provider implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureInterceptor">Optional configuration for the interceptor.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This is a convenience method that registers both the tenant provider and interceptor.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursMultiTenancy&lt;HttpHeaderTenantProvider&gt;(options =>
        /// {
        ///     options.RequireTenant = true;
        ///     options.ValidateTenantOnModify = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursMultiTenancy<TTenantProvider>(
            this IServiceCollection services,
            Action<TenantInterceptorOptions> configureInterceptor = null)
            where TTenantProvider : class, ITenantProvider
        {
            services.AddMvp24HoursTenantProvider<TTenantProvider>();
            services.AddMvp24HoursTenantInterceptor(configureInterceptor);

            return services;
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Adds encryption provider for field-level encryption.
        /// </summary>
        /// <typeparam name="TEncryptionProvider">The encryption provider implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton (encryption providers are typically stateless).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursEncryptionProvider&lt;AesEncryptionProvider&gt;(ServiceLifetime.Singleton);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursEncryptionProvider<TEncryptionProvider>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TEncryptionProvider : class, IEncryptionProvider
        {
            services.Add(new ServiceDescriptor(typeof(IEncryptionProvider), typeof(TEncryptionProvider), lifetime));
            return services;
        }

        /// <summary>
        /// Adds encryption provider with a factory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory function to create the encryption provider.</param>
        /// <param name="lifetime">The service lifetime. Default is Singleton.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursEncryptionProvider(sp =>
        /// {
        ///     var config = sp.GetRequiredService&lt;IConfiguration&gt;();
        ///     var key = config["Encryption:Key"];
        ///     return AesEncryptionProvider.CreateFromKey(key);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursEncryptionProvider(
            this IServiceCollection services,
            Func<IServiceProvider, IEncryptionProvider> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.Add(new ServiceDescriptor(typeof(IEncryptionProvider), factory, lifetime));
            return services;
        }

        #endregion

        #region Streaming Repository

        /// <summary>
        /// Adds streaming repository with IAsyncEnumerable support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Repository configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The streaming repository extends the standard async repository with methods
        /// that return <see cref="IAsyncEnumerable{T}"/> for efficient streaming of large result sets.
        /// </para>
        /// <para>
        /// Use streaming for:
        /// <list type="bullet">
        /// <item>Large data exports</item>
        /// <item>ETL processes</item>
        /// <item>Background batch processing</item>
        /// <item>Memory-constrained environments</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursStreamingRepositoryAsync(options =>
        /// {
        ///     options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ///     options.EnableQueryTags = true;
        ///     options.StreamingBufferSize = 100;
        /// });
        /// 
        /// // Usage
        /// await foreach (var customer in repository.StreamAllAsync())
        /// {
        ///     await ProcessCustomerAsync(customer);
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursStreamingRepositoryAsync(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type streamingRepositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(opt => { });
            }

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            if (streamingRepositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IStreamingRepositoryAsync<>), streamingRepositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IStreamingRepositoryAsync<>), typeof(StreamingRepositoryAsync<>), lifetime));
            }

            // Also register as IRepositoryAsync for compatibility
            services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(StreamingRepositoryAsync<>), lifetime));

            return services;
        }

        #endregion

        #region Bulk Operations Repository

        /// <summary>
        /// Adds repository with high-performance bulk operations support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Repository configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The bulk operations repository extends the standard async repository with methods
        /// for high-performance operations on large datasets:
        /// <list type="bullet">
        /// <item><strong>BulkInsertAsync</strong> - Insert thousands of entities efficiently</item>
        /// <item><strong>BulkUpdateAsync</strong> - Update entities by primary key</item>
        /// <item><strong>BulkDeleteAsync</strong> - Delete entities by primary key</item>
        /// <item><strong>ExecuteUpdateAsync</strong> - Update entities matching a condition (.NET 7+)</item>
        /// <item><strong>ExecuteDeleteAsync</strong> - Delete entities matching a condition (.NET 7+)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use bulk operations when:
        /// <list type="bullet">
        /// <item>Processing thousands of entities</item>
        /// <item>Performance is critical</item>
        /// <item>You don't need change tracking</item>
        /// <item>You don't need entity events/interceptors</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursBulkOperationsRepositoryAsync(options =>
        /// {
        ///     options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
        /// });
        /// 
        /// // Usage in service
        /// public class ImportService
        /// {
        ///     private readonly IBulkOperationsRepositoryAsync&lt;Customer&gt; _repository;
        ///     
        ///     public async Task ImportCustomers(IList&lt;Customer&gt; customers)
        ///     {
        ///         var result = await _repository.BulkInsertAsync(customers, new BulkOperationOptions
        ///         {
        ///             BatchSize = 5000,
        ///             ProgressCallback = (processed, total) => 
        ///                 Console.WriteLine($"Progress: {processed}/{total}")
        ///         });
        ///         
        ///         Console.WriteLine($"Inserted {result.RowsAffected} rows in {result.ElapsedTime}");
        ///     }
        ///     
        ///     public async Task CleanupOldData()
        ///     {
        ///         // Delete all records older than 5 years
        ///         var deleted = await _repository.ExecuteDeleteAsync(
        ///             c => c.CreatedAt &lt; DateTime.UtcNow.AddYears(-5)
        ///         );
        ///         Console.WriteLine($"Deleted {deleted} old records");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursBulkOperationsRepositoryAsync(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type bulkOperationsRepositoryAsync = null,
            Type unitOfWorkAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(opt => { });
            }

            if (unitOfWorkAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), unitOfWorkAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IUnitOfWorkAsync), typeof(UnitOfWorkAsync), lifetime));
            }

            if (bulkOperationsRepositoryAsync != null)
            {
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsRepositoryAsync<>), bulkOperationsRepositoryAsync, lifetime));
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsAsync<>), bulkOperationsRepositoryAsync, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsRepositoryAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
                services.Add(new ServiceDescriptor(typeof(IBulkOperationsAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));
            }

            // Also register as IRepositoryAsync for compatibility
            services.Add(new ServiceDescriptor(typeof(IRepositoryAsync<>), typeof(BulkOperationsRepositoryAsync<>), lifetime));

            return services;
        }

        #endregion

        #region Read-Only Repository (CQRS Support)

        /// <summary>
        /// Adds read-only repository with Specification Pattern support.
        /// Ideal for CQRS query handlers and read-only scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Repository configuration options.</param>
        /// <param name="readOnlyRepository">Custom read-only repository implementation type.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The read-only repository provides:
        /// <list type="bullet">
        /// <item><strong>Specification Pattern</strong> - Expressive, reusable queries</item>
        /// <item><strong>Keyset Pagination</strong> - Efficient cursor-based pagination</item>
        /// <item><strong>No Tracking by Default</strong> - Better read performance</item>
        /// <item><strong>No Command Methods</strong> - Enforces read-only access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use in CQRS architecture to separate read and write concerns:
        /// <list type="bullet">
        /// <item>Query handlers use <see cref="IReadOnlyRepository{T}"/></item>
        /// <item>Command handlers use <see cref="IRepository{T}"/></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Startup.cs
        /// services.AddMvp24HoursReadOnlyRepository(options =>
        /// {
        ///     options.MaxQtyByQueryPage = 100;
        /// });
        /// 
        /// // Query Handler
        /// public class GetActiveCustomersHandler
        /// {
        ///     private readonly IReadOnlyRepository&lt;Customer&gt; _repository;
        ///     
        ///     public IList&lt;Customer&gt; Handle(GetActiveCustomersQuery query)
        ///     {
        ///         var spec = new ActiveCustomerSpecification();
        ///         return _repository.GetBySpecification(spec);
        ///     }
        /// }
        /// 
        /// // Keyset pagination
        /// var page = _repository.GetByKeysetPagination(
        ///     clause: c => c.IsActive,
        ///     keySelector: c => c.Id,
        ///     lastKey: null,
        ///     pageSize: 20);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOnlyRepository(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type readOnlyRepository = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(opt => { });
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
        /// Adds asynchronous read-only repository with Specification Pattern support.
        /// Ideal for CQRS query handlers and read-only scenarios.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Repository configuration options.</param>
        /// <param name="readOnlyRepositoryAsync">Custom read-only repository implementation type.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The async read-only repository provides:
        /// <list type="bullet">
        /// <item><strong>Specification Pattern</strong> - Expressive, reusable queries</item>
        /// <item><strong>Keyset Pagination</strong> - Efficient cursor-based pagination</item>
        /// <item><strong>No Tracking by Default</strong> - Better read performance</item>
        /// <item><strong>Full Async Support</strong> - With CancellationToken</item>
        /// <item><strong>No Command Methods</strong> - Enforces read-only access</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Startup.cs
        /// services.AddMvp24HoursReadOnlyRepositoryAsync(options =>
        /// {
        ///     options.MaxQtyByQueryPage = 100;
        /// });
        /// 
        /// // Query Handler
        /// public class GetActiveCustomersHandler
        /// {
        ///     private readonly IReadOnlyRepositoryAsync&lt;Customer&gt; _repository;
        ///     
        ///     public async Task&lt;IList&lt;Customer&gt;&gt; HandleAsync(GetActiveCustomersQuery query)
        ///     {
        ///         var spec = new ActiveCustomerSpecification();
        ///         return await _repository.GetBySpecificationAsync(spec);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOnlyRepositoryAsync(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            Type readOnlyRepositoryAsync = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<EFCoreRepositoryOptions>(opt => { });
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
        /// Adds both read-only and full repositories for complete CQRS support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Repository configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Registers both repository types for CQRS architecture:
        /// <list type="bullet">
        /// <item><see cref="IReadOnlyRepositoryAsync{T}"/> for query handlers</item>
        /// <item><see cref="IRepositoryAsync{T}"/> for command handlers</item>
        /// <item><see cref="IUnitOfWorkAsync"/> for transaction management</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursCqrsRepositories(options =>
        /// {
        ///     options.MaxQtyByQueryPage = 100;
        /// });
        /// 
        /// // Query handler (read-only)
        /// public class GetCustomerQueryHandler
        /// {
        ///     private readonly IReadOnlyRepositoryAsync&lt;Customer&gt; _repository;
        ///     // ...
        /// }
        /// 
        /// // Command handler (full access)
        /// public class CreateCustomerCommandHandler
        /// {
        ///     private readonly IRepositoryAsync&lt;Customer&gt; _repository;
        ///     private readonly IUnitOfWorkAsync _unitOfWork;
        ///     // ...
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursCqrsRepositories(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> options = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            // Register read-only repository for queries
            services.AddMvp24HoursReadOnlyRepositoryAsync(options, lifetime: lifetime);

            // Register full repository for commands
            services.AddMvp24HoursRepositoryAsync(options, lifetime: lifetime);

            return services;
        }

        #endregion

        #region Performance Options

        /// <summary>
        /// Adds repository with optimized performance settings for read-heavy workloads.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="additionalOptions">Additional configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Pre-configures the repository with settings optimized for read performance:
        /// <list type="bullet">
        /// <item>NoTracking by default</item>
        /// <item>Split queries enabled</item>
        /// <item>Query tags enabled for profiling</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursReadOptimizedRepository(options =>
        /// {
        ///     options.MaxQtyByQueryPage = 200;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursReadOptimizedRepository(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> additionalOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursRepositoryAsync(options =>
            {
                // Optimized defaults for read performance
                options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
                options.UseSplitQueries = true;
                options.EnableQueryTags = true;
                options.SlowQueryThresholdMs = 500;

                // Apply additional customizations
                additionalOptions?.Invoke(options);
            }, lifetime: lifetime);
        }

        /// <summary>
        /// Adds repository with optimized performance settings for write-heavy workloads.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="additionalOptions">Additional configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Pre-configures the repository with settings optimized for write performance:
        /// <list type="bullet">
        /// <item>Tracking enabled by default (required for updates)</item>
        /// <item>Single queries (reduces round trips)</item>
        /// <item>Query tags disabled (reduces overhead)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursWriteOptimizedRepository(options =>
        /// {
        ///     options.TransactionIsolationLevel = IsolationLevel.ReadCommitted;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursWriteOptimizedRepository(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> additionalOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursRepositoryAsync(options =>
            {
                // Optimized defaults for write performance
                options.DefaultTrackingBehavior = QueryTrackingBehavior.TrackAll;
                options.UseSplitQueries = false;
                options.EnableQueryTags = false;

                // Apply additional customizations
                additionalOptions?.Invoke(options);
            }, lifetime: lifetime);
        }

        /// <summary>
        /// Adds repository configured for development with detailed logging and profiling.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="additionalOptions">Additional configuration options.</param>
        /// <param name="lifetime">The service lifetime. Default is Scoped.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Pre-configures the repository with settings useful during development:
        /// <list type="bullet">
        /// <item>Query tags enabled with detailed information</item>
        /// <item>Sensitive data logging enabled</item>
        /// <item>Lower slow query threshold for optimization</item>
        /// </list>
        /// </para>
        /// <para>
        /// ⚠️ <strong>Warning</strong>: Do not use in production as it exposes sensitive data in logs.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Development environment only
        /// if (env.IsDevelopment())
        /// {
        ///     services.AddMvp24HoursDevRepository();
        /// }
        /// else
        /// {
        ///     services.AddMvp24HoursReadOptimizedRepository();
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursDevRepository(
            this IServiceCollection services,
            Action<EFCoreRepositoryOptions> additionalOptions = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursRepositoryAsync(options =>
            {
                // Development-friendly defaults
                options.DefaultTrackingBehavior = QueryTrackingBehavior.TrackAll;
                options.UseSplitQueries = true;
                options.EnableQueryTags = true;
                options.QueryTagPrefix = "Mvp24Hours-Dev";
                options.EnableSensitiveDataLogging = true;
                options.SlowQueryThresholdMs = 200; // Lower threshold for profiling

                // Apply additional customizations
                additionalOptions?.Invoke(options);
            }, lifetime: lifetime);
        }

        #endregion
    }
}
