//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Logic;
using System;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering bulk operation services in the DI container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions simplify the registration of bulk command services with their
    /// corresponding interfaces, supporting both entity and DTO-based services.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register bulk service for entities
    /// services.AddBulkCommandService&lt;ICustomerBulkService, CustomerBulkService&gt;();
    /// 
    /// // Register bulk service with DTO support
    /// services.AddBulkCommandServiceWithDto&lt;ICustomerBulkService, CustomerBulkService, CustomerDto&gt;();
    /// 
    /// // Register bulk service with separate Create/Update DTOs
    /// services.AddBulkCommandServiceWithSeparateDtos&lt;
    ///     ICustomerBulkService, 
    ///     CustomerBulkService, 
    ///     CreateCustomerDto, 
    ///     UpdateCustomerDto&gt;();
    /// </code>
    /// </example>
    public static class BulkServiceCollectionExtensions
    {
        #region [ Entity-based Bulk Services ]

        /// <summary>
        /// Registers a bulk command service for entities with scoped lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddBulkCommandService&lt;ICustomerBulkService, CustomerBulkService&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddBulkCommandService<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddScoped<TService, TImplementation>();
            return services;
        }

        /// <summary>
        /// Registers a bulk command service for entities with configurable lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddBulkCommandService&lt;ICustomerBulkService, CustomerBulkService&gt;(ServiceLifetime.Transient);
        /// </code>
        /// </example>
        public static IServiceCollection AddBulkCommandService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
            return services;
        }

        /// <summary>
        /// Registers a bulk command service for entities with a factory method.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">Factory method to create the service instance.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddBulkCommandService&lt;ICustomerBulkService&gt;(sp =>
        /// {
        ///     var dbContext = sp.GetRequiredService&lt;MyDbContext&gt;();
        ///     var validator = sp.GetService&lt;IValidator&lt;Customer&gt;&gt;();
        ///     return new CustomerBulkService(dbContext, validator);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddBulkCommandService<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> factory)
            where TService : class
        {
            services.AddScoped(factory);
            return services;
        }

        #endregion

        #region [ DTO-based Bulk Services ]

        /// <summary>
        /// Registers a bulk command service with DTO support with scoped lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddBulkCommandServiceWithDto&lt;ICustomerBulkService, CustomerBulkService&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddBulkCommandServiceWithDto<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddScoped<TService, TImplementation>();
            return services;
        }

        /// <summary>
        /// Registers a bulk command service with DTO support with configurable lifetime.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddBulkCommandServiceWithDto<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
            return services;
        }

        #endregion

        #region [ Interface Registration ]

        /// <summary>
        /// Registers the <see cref="IBulkCommandServiceAsync{TEntity}"/> interface for a specific entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the implementation as the <see cref="IBulkCommandServiceAsync{TEntity}"/>
        /// interface, allowing injection via the interface.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register interface
        /// services.AddBulkCommandServiceAsync&lt;Customer, CustomerBulkService&gt;();
        /// 
        /// // Inject via interface
        /// public class CustomerController
        /// {
        ///     private readonly IBulkCommandServiceAsync&lt;Customer&gt; _bulkService;
        ///     
        ///     public CustomerController(IBulkCommandServiceAsync&lt;Customer&gt; bulkService)
        ///     {
        ///         _bulkService = bulkService;
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IServiceCollection AddBulkCommandServiceAsync<TEntity, TImplementation>(
            this IServiceCollection services)
            where TEntity : class
            where TImplementation : class, IBulkCommandServiceAsync<TEntity>
        {
            services.AddScoped<IBulkCommandServiceAsync<TEntity>, TImplementation>();
            return services;
        }

        /// <summary>
        /// Registers the <see cref="IBulkCommandServiceWithDtoAsync{TDto}"/> interface for a specific DTO type.
        /// </summary>
        /// <typeparam name="TDto">The DTO type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddBulkCommandServiceWithDtoAsync<TDto, TImplementation>(
            this IServiceCollection services)
            where TDto : class
            where TImplementation : class, IBulkCommandServiceWithDtoAsync<TDto>
        {
            services.AddScoped<IBulkCommandServiceWithDtoAsync<TDto>, TImplementation>();
            return services;
        }

        /// <summary>
        /// Registers the <see cref="IBulkCommandServiceWithSeparateDtosAsync{TCreateDto, TUpdateDto}"/>
        /// interface for specific DTO types.
        /// </summary>
        /// <typeparam name="TCreateDto">The create DTO type.</typeparam>
        /// <typeparam name="TUpdateDto">The update DTO type.</typeparam>
        /// <typeparam name="TImplementation">The service implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddBulkCommandServiceWithSeparateDtosAsync<TCreateDto, TUpdateDto, TImplementation>(
            this IServiceCollection services)
            where TCreateDto : class
            where TUpdateDto : class
            where TImplementation : class, IBulkCommandServiceWithSeparateDtosAsync<TCreateDto, TUpdateDto>
        {
            services.AddScoped<IBulkCommandServiceWithSeparateDtosAsync<TCreateDto, TUpdateDto>, TImplementation>();
            return services;
        }

        #endregion
    }
}

