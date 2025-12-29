//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Extensions
{
    /// <summary>
    /// Extension methods for registering MongoDB interceptors in the DI container.
    /// </summary>
    public static class MongoDbInterceptorExtensions
    {
        /// <summary>
        /// Adds the MongoDB interceptor pipeline to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDbContext(...)
        ///         .AddMongoDbInterceptorPipeline()
        ///         .AddMongoDbAuditInterceptor()
        ///         .AddMongoDbSoftDeleteInterceptor();
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbInterceptorPipeline(this IServiceCollection services)
        {
            services.TryAddScoped<IMongoDbInterceptorPipeline, MongoDbInterceptorPipeline>();
            return services;
        }

        /// <summary>
        /// Adds the audit interceptor that automatically populates audit fields.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="defaultUser">Default user when no user provider is available.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This interceptor sets CreatedAt/CreatedBy and ModifiedAt/ModifiedBy on entities
        /// implementing IAuditableEntity or IEntityLog interfaces.
        /// </remarks>
        public static IServiceCollection AddMongoDbAuditInterceptor(
            this IServiceCollection services,
            string defaultUser = "System")
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor>(sp =>
                new AuditInterceptor(
                    sp.GetService<ICurrentUserProvider>(),
                    sp.GetService<IClock>(),
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<AuditInterceptor>>(),
                    defaultUser));
            return services;
        }

        /// <summary>
        /// Adds the soft delete interceptor that converts physical deletes to soft deletes.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="defaultUser">Default user when no user provider is available.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This interceptor converts delete operations to updates for entities implementing
        /// ISoftDeletable or IEntityDateLog interfaces.
        /// </remarks>
        public static IServiceCollection AddMongoDbSoftDeleteInterceptor(
            this IServiceCollection services,
            string defaultUser = "System")
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor>(sp =>
                new SoftDeleteInterceptor(
                    sp.GetService<ICurrentUserProvider>(),
                    sp.GetService<IClock>(),
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<SoftDeleteInterceptor>>(),
                    defaultUser));
            return services;
        }

        /// <summary>
        /// Adds the command logger interceptor for detailed operation logging.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="slowOperationThreshold">Threshold for slow operation warnings.</param>
        /// <param name="logAllOperations">If true, logs all operations. If false, only logs slow operations.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbCommandLogger(
            this IServiceCollection services,
            TimeSpan? slowOperationThreshold = null,
            bool logAllOperations = true)
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor>(sp =>
                new CommandLogger(
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<CommandLogger>>(),
                    slowOperationThreshold,
                    logAllOperations));
            return services;
        }

        /// <summary>
        /// Adds the audit trail interceptor for comprehensive operation logging.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="logEntityData">If true, includes entity data in audit logs.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbAuditTrail(
            this IServiceCollection services,
            bool logEntityData = false)
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor>(sp =>
                new AuditTrailInterceptor(
                    sp.GetService<ICurrentUserProvider>(),
                    sp.GetService<IClock>(),
                    sp.GetService<Microsoft.Extensions.Logging.ILogger<AuditTrailInterceptor>>(),
                    logEntityData));
            return services;
        }

        /// <summary>
        /// Adds a custom interceptor to the MongoDB pipeline.
        /// </summary>
        /// <typeparam name="TInterceptor">The type of the interceptor.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbInterceptor<TInterceptor>(this IServiceCollection services)
            where TInterceptor : class, IMongoDbInterceptor
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor, TInterceptor>();
            return services;
        }

        /// <summary>
        /// Adds a custom interceptor instance to the MongoDB pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="interceptorFactory">Factory function to create the interceptor.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbInterceptor(
            this IServiceCollection services,
            Func<IServiceProvider, IMongoDbInterceptor> interceptorFactory)
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped(interceptorFactory);
            return services;
        }

        /// <summary>
        /// Adds all standard MongoDB interceptors (Audit, SoftDelete, CommandLogger, AuditTrail).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Configuration options for the interceptors.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddAllMongoDbInterceptors(options =>
        /// {
        ///     options.EnableAuditInterceptor = true;
        ///     options.EnableSoftDelete = true;
        ///     options.EnableCommandLogger = true;
        ///     options.LogSlowOperationsOnly = false;
        ///     options.SlowOperationThreshold = TimeSpan.FromSeconds(1);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddAllMongoDbInterceptors(
            this IServiceCollection services,
            Action<MongoDbInterceptorOptions> options = null)
        {
            var config = new MongoDbInterceptorOptions();
            options?.Invoke(config);

            services.AddMongoDbInterceptorPipeline();

            // Add tenant interceptor first (runs with lowest order)
            if (config.EnableTenantInterceptor)
            {
                services.AddMongoDbTenantInterceptor(
                    config.TenantValidateOnUpdate,
                    config.TenantValidateOnDelete,
                    config.TenantThrowOnMissing);
            }

            if (config.EnableAuditInterceptor)
            {
                services.AddMongoDbAuditInterceptor(config.DefaultUser);
            }

            if (config.EnableSoftDelete)
            {
                services.AddMongoDbSoftDeleteInterceptor(config.DefaultUser);
            }

            if (config.EnableCommandLogger)
            {
                services.AddMongoDbCommandLogger(
                    config.SlowOperationThreshold,
                    !config.LogSlowOperationsOnly);
            }

            if (config.EnableAuditTrail)
            {
                services.AddMongoDbAuditTrail(config.LogEntityDataInAuditTrail);
            }

            return services;
        }

        /// <summary>
        /// Adds the tenant interceptor for multi-tenancy support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="validateOnUpdate">If true, validates tenant ownership on updates.</param>
        /// <param name="validateOnDelete">If true, validates tenant ownership on deletes.</param>
        /// <param name="throwOnMissingTenant">If true, throws an exception when no tenant is set.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This interceptor automatically sets TenantId on insert for entities implementing
        /// <see cref="Core.Contract.Domain.Entity.ITenantEntity"/> and validates tenant ownership
        /// on update/delete operations.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// <list type="bullet">
        ///   <item>Register an <see cref="ITenantProvider"/> implementation</item>
        ///   <item>Entities must implement ITenantEntity or ITenantEntity&lt;T&gt;</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register tenant provider:
        /// services.AddScoped&lt;ITenantProvider&gt;(sp => 
        ///     new HttpHeaderTenantProvider(sp.GetRequiredService&lt;IHttpContextAccessor&gt;()));
        /// 
        /// // Register tenant interceptor:
        /// services.AddMongoDbTenantInterceptor();
        /// 
        /// // Or with configuration:
        /// services.AddMongoDbTenantInterceptor(
        ///     validateOnUpdate: true,
        ///     validateOnDelete: true,
        ///     throwOnMissingTenant: false);
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbTenantInterceptor(
            this IServiceCollection services,
            bool validateOnUpdate = true,
            bool validateOnDelete = true,
            bool throwOnMissingTenant = true)
        {
            services.AddMongoDbInterceptorPipeline();
            services.AddScoped<IMongoDbInterceptor>(sp =>
            {
                var tenantProvider = sp.GetService<ITenantProvider>();
                if (tenantProvider == null)
                {
                    // Use NoTenantProvider if no provider is registered
                    tenantProvider = NoTenantProvider.Instance;
                }

                return new TenantInterceptor(
                    tenantProvider,
                    validateOnUpdate,
                    validateOnDelete,
                    throwOnMissingTenant);
            });
            return services;
        }

        /// <summary>
        /// Adds the tenant interceptor with a specific tenant provider.
        /// </summary>
        /// <typeparam name="TTenantProvider">The type of tenant provider.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="validateOnUpdate">If true, validates tenant ownership on updates.</param>
        /// <param name="validateOnDelete">If true, validates tenant ownership on deletes.</param>
        /// <param name="throwOnMissingTenant">If true, throws an exception when no tenant is set.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMongoDbTenantInterceptor<TTenantProvider>(
            this IServiceCollection services,
            bool validateOnUpdate = true,
            bool validateOnDelete = true,
            bool throwOnMissingTenant = true)
            where TTenantProvider : class, ITenantProvider
        {
            // Register the tenant provider if not already registered
            services.TryAddScoped<ITenantProvider, TTenantProvider>();

            return services.AddMongoDbTenantInterceptor(
                validateOnUpdate,
                validateOnDelete,
                throwOnMissingTenant);
        }

        /// <summary>
        /// Registers the AsyncLocalTenantProvider for scenarios where tenant is set via AsyncLocal.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Use <see cref="AsyncLocalTenantProvider.SetCurrentTenant"/> to set the tenant at the
        /// beginning of a request/operation.
        /// </remarks>
        /// <example>
        /// <code>
        /// // In startup:
        /// services.AddMongoDbAsyncLocalTenantProvider();
        /// 
        /// // In middleware or controller:
        /// AsyncLocalTenantProvider.SetCurrentTenant("tenant-123");
        /// 
        /// // Operations will now use "tenant-123" as the tenant
        /// await repository.AddAsync(entity);
        /// </code>
        /// </example>
        public static IServiceCollection AddMongoDbAsyncLocalTenantProvider(this IServiceCollection services)
        {
            services.TryAddScoped<ITenantProvider>(sp => AsyncLocalTenantProvider.Instance);
            return services;
        }
    }

    /// <summary>
    /// Configuration options for MongoDB interceptors.
    /// </summary>
    public class MongoDbInterceptorOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to enable the audit interceptor.
        /// Default is true.
        /// </summary>
        public bool EnableAuditInterceptor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable soft delete.
        /// Default is true.
        /// </summary>
        public bool EnableSoftDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable command logging.
        /// Default is false for performance.
        /// </summary>
        public bool EnableCommandLogger { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable audit trail.
        /// Default is false.
        /// </summary>
        public bool EnableAuditTrail { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable the tenant interceptor.
        /// Default is false. Set to true to enable multi-tenancy support.
        /// </summary>
        public bool EnableTenantInterceptor { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to log only slow operations.
        /// Default is false (log all).
        /// </summary>
        public bool LogSlowOperationsOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the slow operation threshold.
        /// Default is 500ms.
        /// </summary>
        public TimeSpan SlowOperationThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the default user for audit fields.
        /// Default is "System".
        /// </summary>
        public string DefaultUser { get; set; } = "System";

        /// <summary>
        /// Gets or sets a value indicating whether to log entity data in the audit trail.
        /// Default is false for privacy.
        /// </summary>
        public bool LogEntityDataInAuditTrail { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate tenant ownership on update operations.
        /// Default is true.
        /// </summary>
        public bool TenantValidateOnUpdate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate tenant ownership on delete operations.
        /// Default is true.
        /// </summary>
        public bool TenantValidateOnDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to throw an exception when no tenant is set.
        /// Default is true.
        /// </summary>
        public bool TenantThrowOnMissing { get; set; } = true;
    }
}

