//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering application services and AutoMapper profiles.
    /// </summary>
    public static class ApplicationServiceCollectionExtensions
    {
        #region [ AutoMapper Extensions ]

        /// <summary>
        /// Adds AutoMapper with profile scanning from the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for AutoMapper profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMapper(typeof(CustomerProfile).Assembly);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMapper(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddMvp24HoursAutoMapper(null, assemblies);
        }

        /// <summary>
        /// Adds AutoMapper with profile scanning from assemblies containing the specified types.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="profileAssemblyMarkerTypes">Types used to identify assemblies to scan for profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMapperFromAssemblyContaining&lt;CustomerProfile&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMapperFromAssemblyContaining<T>(
            this IServiceCollection services)
        {
            return services.AddMvp24HoursAutoMapper(typeof(T).Assembly);
        }

        /// <summary>
        /// Adds AutoMapper with profile scanning from assemblies containing the specified types.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="profileAssemblyMarkerTypes">Types used to identify assemblies to scan for profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMapperFromAssemblyContaining(typeof(CustomerProfile), typeof(OrderProfile));
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMapperFromAssemblyContaining(
            this IServiceCollection services,
            params Type[] profileAssemblyMarkerTypes)
        {
            var assemblies = profileAssemblyMarkerTypes
                .Select(t => t.Assembly)
                .Distinct()
                .ToArray();

            return services.AddMvp24HoursAutoMapper(assemblies);
        }

        /// <summary>
        /// Adds AutoMapper with profile scanning from the specified assemblies and optional configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configAction">Optional configuration action for MapperConfiguration.</param>
        /// <param name="assemblies">The assemblies to scan for AutoMapper profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMapper(
        ///     cfg => cfg.AllowNullDestinationValues = true,
        ///     typeof(CustomerProfile).Assembly
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMapper(
            this IServiceCollection services,
            Action<IMapperConfigurationExpression>? configAction,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified for AutoMapper profile scanning.", nameof(assemblies));
            }

            // Find all profiles in the specified assemblies
            var profileTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                .ToList();

            // Create mapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                // Apply custom configuration if provided
                configAction?.Invoke(cfg);

                // Add all discovered profiles
                foreach (var profileType in profileTypes)
                {
                    cfg.AddProfile(profileType);
                }
            });

            // Register IMapper as singleton
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }

        /// <summary>
        /// Adds AutoMapper using the built-in AddAutoMapper extension with assembly scanning.
        /// This method uses AutoMapper's native DI integration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for AutoMapper profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAutoMapperNative(typeof(CustomerProfile).Assembly);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursAutoMapperNative(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified for AutoMapper profile scanning.", nameof(assemblies));
            }

            // Find all profiles in the specified assemblies (same as AddMvp24HoursAutoMapper but no custom config)
            var profileTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
                .ToList();

            // Create mapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                // Add all discovered profiles
                foreach (var profileType in profileTypes)
                {
                    cfg.AddProfile(profileType);
                }
            });

            // Register IMapper as singleton
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }

        #endregion

        #region [ Application Service Extensions ]

        /// <summary>
        /// Scans the specified assemblies and registers all application services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for application services.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method scans for classes implementing:
        /// <list type="bullet">
        /// <item><see cref="IApplicationService{TEntity}"/></item>
        /// <item><see cref="IApplicationServiceAsync{TEntity}"/></item>
        /// <item><see cref="IApplicationServiceWithDto{TEntity,TDto}"/></item>
        /// <item><see cref="IApplicationServiceWithDtoAsync{TEntity,TDto}"/></item>
        /// <item><see cref="IApplicationServiceWithSeparateDtos{TEntity,TDto,TCreateDto,TUpdateDto}"/></item>
        /// <item><see cref="IApplicationServiceWithSeparateDtosAsync{TEntity,TDto,TCreateDto,TUpdateDto}"/></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplicationServices(typeof(CustomerService).Assembly);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplicationServices(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddMvp24HoursApplicationServices(ServiceLifetime.Scoped, assemblies);
        }

        /// <summary>
        /// Scans the specified assemblies and registers all application services with the specified lifetime.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="assemblies">The assemblies to scan for application services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursApplicationServices(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
            }

            var applicationServiceInterfaces = new[]
            {
                typeof(IApplicationService<>),
                typeof(IApplicationServiceAsync<>),
                typeof(IApplicationServiceWithDto<,>),
                typeof(IApplicationServiceWithDtoAsync<,>),
                typeof(IApplicationServiceWithSeparateDtos<,,,>),
                typeof(IApplicationServiceWithSeparateDtosAsync<,,,>),
                typeof(IReadOnlyApplicationService<>),
                typeof(IReadOnlyApplicationServiceAsync<>),
                typeof(IReadOnlyApplicationServiceWithDto<,>),
                typeof(IReadOnlyApplicationServiceWithDtoAsync<,>),
                typeof(IReadOnlyApplicationServiceWithSeparateDtos<,>),
                typeof(IReadOnlyApplicationServiceWithSeparateDtosAsync<,>)
            };

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .ToList();

                foreach (var type in types)
                {
                    var interfaces = type.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   applicationServiceInterfaces.Contains(i.GetGenericTypeDefinition()))
                        .ToList();

                    foreach (var @interface in interfaces)
                    {
                        services.Add(new ServiceDescriptor(@interface, type, lifetime));
                    }

                    // Also register by concrete type if it implements any application service interface
                    if (interfaces.Any())
                    {
                        services.Add(new ServiceDescriptor(type, type, lifetime));
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Scans the assembly containing the specified type and registers all application services.
        /// </summary>
        /// <typeparam name="T">A type in the assembly to scan.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplicationServicesFromAssemblyContaining&lt;CustomerService&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplicationServicesFromAssemblyContaining<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursApplicationServices(lifetime, typeof(T).Assembly);
        }

        /// <summary>
        /// Registers a specific application service type.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplicationService&lt;ICustomerService, CustomerService&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplicationService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
            return services;
        }

        #endregion

        #region [ Complete Setup Extensions ]

        /// <summary>
        /// Adds all Mvp24Hours Application services including AutoMapper with profile scanning.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for services and profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplication(
        ///     typeof(CustomerService).Assembly,
        ///     typeof(CustomerProfile).Assembly
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplication(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddMvp24HoursApplication(null, assemblies);
        }

        /// <summary>
        /// Adds all Mvp24Hours Application services including AutoMapper with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="autoMapperConfig">Optional AutoMapper configuration action.</param>
        /// <param name="assemblies">The assemblies to scan for services and profiles.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplication(
        ///     cfg => cfg.AllowNullDestinationValues = true,
        ///     typeof(CustomerService).Assembly
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplication(
            this IServiceCollection services,
            Action<IMapperConfigurationExpression>? autoMapperConfig,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
            }

            // Register AutoMapper
            services.AddMvp24HoursAutoMapper(autoMapperConfig, assemblies);

            // Register Application Services
            services.AddMvp24HoursApplicationServices(assemblies);

            return services;
        }

        /// <summary>
        /// Adds all Mvp24Hours Application services from the assembly containing the specified type.
        /// </summary>
        /// <typeparam name="T">A type in the assembly to scan.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApplicationFromAssemblyContaining&lt;CustomerService&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursApplicationFromAssemblyContaining<T>(
            this IServiceCollection services)
        {
            return services.AddMvp24HoursApplication(typeof(T).Assembly);
        }

        #endregion

        #region [ Validation Extensions ]

        /// <summary>
        /// Scans the specified assemblies and registers all FluentValidation validators.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies to scan for validators.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursValidators(typeof(CustomerValidator).Assembly);
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursValidators(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddMvp24HoursValidators(ServiceLifetime.Scoped, assemblies);
        }

        /// <summary>
        /// Scans the specified assemblies and registers all FluentValidation validators with the specified lifetime.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="assemblies">The assemblies to scan for validators.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursValidators(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
            }

            var validatorInterfaceType = typeof(FluentValidation.IValidator<>);

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .ToList();

                foreach (var type in types)
                {
                    var interfaces = type.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == validatorInterfaceType)
                        .ToList();

                    foreach (var @interface in interfaces)
                    {
                        services.Add(new ServiceDescriptor(@interface, type, lifetime));
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Scans the assembly containing the specified type and registers all FluentValidation validators.
        /// </summary>
        /// <typeparam name="T">A type in the assembly to scan.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursValidatorsFromAssemblyContaining&lt;CustomerValidator&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursValidatorsFromAssemblyContaining<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddMvp24HoursValidators(lifetime, typeof(T).Assembly);
        }

        #endregion
    }
}

