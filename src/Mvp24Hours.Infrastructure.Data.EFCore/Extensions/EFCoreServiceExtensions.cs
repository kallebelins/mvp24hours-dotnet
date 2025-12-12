//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using System;

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursdbcontext-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursrepository-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursrepositoryasync-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hourstenantprovider-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hourstenantprovider-factory-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hourstenantinterceptor-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursmultitenancy-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursencryptionprovider-execute");

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
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcoreserviceextensions-addmvp24hoursencryptionprovider-factory-execute");

            services.Add(new ServiceDescriptor(typeof(IEncryptionProvider), factory, lifetime));
            return services;
        }

        #endregion
    }
}
