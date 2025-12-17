//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Application.Logic.Validation;
using System;
using System.Reflection;

namespace Mvp24Hours.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering validation services in the DI container.
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        #region [ IValidationService Registration ]

        /// <summary>
        /// Registers the IValidationService for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddValidationService&lt;CustomerDto&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddValidationService<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            return services.AddValidationService<T>(null, lifetime);
        }

        /// <summary>
        /// Registers the IValidationService for a specific type with configuration.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure validation service options.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddValidationService&lt;CustomerDto&gt;(options =>
        /// {
        ///     options.UseFluentValidation = true;
        ///     options.UseDataAnnotations = true;
        ///     options.UseCascadeValidation = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddValidationService<T>(
            this IServiceCollection services,
            Action<ValidationServiceOptions>? configureOptions,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            var options = new ValidationServiceOptions();
            configureOptions?.Invoke(options);

            services.Add(new ServiceDescriptor(
                typeof(IValidationService<T>),
                sp =>
                {
                    var validators = sp.GetServices<FluentValidation.IValidator<T>>();
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<ValidationService<T>>>();
                    return new ValidationService<T>(validators, sp, logger, options);
                },
                lifetime));

            // Also register ICascadeValidator
            services.Add(new ServiceDescriptor(
                typeof(ICascadeValidator<T>),
                sp => (ICascadeValidator<T>)sp.GetRequiredService<IValidationService<T>>(),
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers validation services for all types with FluentValidation validators.
        /// Scans assemblies for IValidator implementations and registers corresponding IValidationService.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationServices(
            this IServiceCollection services,
            Assembly[] assemblies,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddValidationServices(assemblies, null, lifetime);
        }

        /// <summary>
        /// Registers validation services for all types with FluentValidation validators.
        /// </summary>
        public static IServiceCollection AddValidationServices(
            this IServiceCollection services,
            Assembly[] assemblies,
            Action<ValidationServiceOptions>? configureOptions,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var options = new ValidationServiceOptions();
            configureOptions?.Invoke(options);

            var validatorInterfaceType = typeof(FluentValidation.IValidator<>);
            var validationServiceType = typeof(IValidationService<>);
            var cascadeValidatorType = typeof(ICascadeValidator<>);
            var validationServiceImplType = typeof(ValidationService<>);

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    var interfaces = type.GetInterfaces();

                    foreach (var @interface in interfaces)
                    {
                        if (!@interface.IsGenericType)
                            continue;

                        if (@interface.GetGenericTypeDefinition() != validatorInterfaceType)
                            continue;

                        // Get the type being validated
                        var validatedType = @interface.GetGenericArguments()[0];

                        // Register IValidationService<T> if not already registered
                        var serviceType = validationServiceType.MakeGenericType(validatedType);
                        var implType = validationServiceImplType.MakeGenericType(validatedType);
                        var cascadeType = cascadeValidatorType.MakeGenericType(validatedType);

                        services.TryAdd(new ServiceDescriptor(
                            serviceType,
                            sp =>
                            {
                                var validatorType = validatorInterfaceType.MakeGenericType(validatedType);
                                var validators = sp.GetServices(validatorType);

                                // Use reflection to create the service
                                var loggerType = typeof(Microsoft.Extensions.Logging.ILogger<>)
                                    .MakeGenericType(implType);
                                var logger = sp.GetService(loggerType);

                                return Activator.CreateInstance(
                                    implType,
                                    validators,
                                    sp,
                                    logger,
                                    options)!;
                            },
                            lifetime));

                        services.TryAdd(new ServiceDescriptor(
                            cascadeType,
                            sp => sp.GetRequiredService(serviceType),
                            lifetime));
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Registers validation services from the assembly containing the specified type.
        /// </summary>
        public static IServiceCollection AddValidationServicesFromAssemblyContaining<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddValidationServices(new[] { typeof(T).Assembly }, lifetime);
        }

        #endregion

        #region [ Validation Pipeline Registration ]

        /// <summary>
        /// Registers a validation pipeline for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configurePipeline">Action to configure the validation pipeline.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddValidationPipeline&lt;OrderDto&gt;(builder =>
        /// {
        ///     builder.AddStep&lt;NullCheckValidationStep&lt;OrderDto&gt;&gt;()
        ///            .UseFluentValidation()
        ///            .UseDataAnnotations()
        ///            .UseCascadeValidation();
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddValidationPipeline<T>(
            this IServiceCollection services,
            Action<IValidationPipelineBuilder<T>> configurePipeline,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            services.Add(new ServiceDescriptor(
                typeof(IValidationPipeline<T>),
                sp =>
                {
                    var builder = new ValidationPipelineBuilder<T>(sp);
                    configurePipeline(builder);
                    return builder.Build();
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Registers a default validation pipeline for a specific type.
        /// Includes FluentValidation, DataAnnotations, and CascadeValidation steps.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDefaultValidationPipeline<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            return services.AddValidationPipeline<T>(builder =>
            {
                builder.AddStep(new NullCheckValidationStep<T>())
                       .UseFluentValidation()
                       .UseDataAnnotations()
                       .UseCascadeValidation();
            }, lifetime);
        }

        /// <summary>
        /// Registers all validation step implementations as services.
        /// </summary>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">Service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationSteps<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            services.Add(new ServiceDescriptor(typeof(NullCheckValidationStep<T>), typeof(NullCheckValidationStep<T>), lifetime));
            services.Add(new ServiceDescriptor(typeof(FluentValidationStep<T>), typeof(FluentValidationStep<T>), lifetime));
            services.Add(new ServiceDescriptor(typeof(DataAnnotationValidationStep<T>), typeof(DataAnnotationValidationStep<T>), lifetime));
            services.Add(new ServiceDescriptor(typeof(CascadeValidationStep<T>), typeof(CascadeValidationStep<T>), lifetime));

            return services;
        }

        #endregion

        #region [ Complete Validation Setup ]

        /// <summary>
        /// Adds comprehensive validation support including FluentValidation validators,
        /// IValidationService, and validation pipelines.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan for validators.</param>
        /// <param name="configureOptions">Optional configuration for validation services.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursValidation(
        ///     new[] { typeof(CustomerValidator).Assembly },
        ///     options =>
        ///     {
        ///         options.UseFluentValidation = true;
        ///         options.UseCascadeValidation = true;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursValidation(
            this IServiceCollection services,
            Assembly[] assemblies,
            Action<ValidationServiceOptions>? configureOptions = null)
        {
            // Register FluentValidation validators
            services.AddMvp24HoursValidators(assemblies);

            // Register IValidationService for all validator types
            services.AddValidationServices(assemblies, configureOptions);

            return services;
        }

        /// <summary>
        /// Adds comprehensive validation support from the assembly containing the specified type.
        /// </summary>
        public static IServiceCollection AddMvp24HoursValidationFromAssemblyContaining<T>(
            this IServiceCollection services,
            Action<ValidationServiceOptions>? configureOptions = null)
        {
            return services.AddMvp24HoursValidation(new[] { typeof(T).Assembly }, configureOptions);
        }

        #endregion
    }
}

