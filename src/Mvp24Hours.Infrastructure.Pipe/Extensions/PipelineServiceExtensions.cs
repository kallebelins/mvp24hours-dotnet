//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Configuration;
using Mvp24Hours.Infrastructure.Pipe.ExceptionMapping;
using Mvp24Hours.Infrastructure.Pipe.Middleware;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using Mvp24Hours.Infrastructure.Pipe.Validation;
using System;

namespace Mvp24Hours.Extensions
{
    public static class PipelineServiceExtensions
    {
        /// <summary>
        /// Add pipeline engine
        /// </summary>
        public static IServiceCollection AddMvp24HoursPipeline(this IServiceCollection services,
            Action<PipelineOptions>? options = null,
            Func<IServiceProvider, IPipeline>? factory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<PipelineOptions>(options =>
                {
                    options.IsBreakOnFail = false;
                    options.ForceRollbackOnFalure = false;
                });
            }

            if (factory != null)
            {
                services.Add(new ServiceDescriptor(typeof(IPipeline), factory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IPipeline), typeof(Pipeline), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add pipeline engine async
        /// </summary>
        public static IServiceCollection AddMvp24HoursPipelineAsync(this IServiceCollection services,
            Action<PipelineAsyncOptions>? options = null,
            Func<IServiceProvider, IPipelineAsync>? factory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<PipelineAsyncOptions>(options =>
                {
                    options.IsBreakOnFail = false;
                    options.ForceRollbackOnFalure = false;
                });
            }

            if (factory != null)
            {
                services.Add(new ServiceDescriptor(typeof(IPipelineAsync), factory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IPipelineAsync), typeof(PipelineAsync), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Adds default pipeline exception mapper.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineExceptionMapper(
            this IServiceCollection services,
            Action<DefaultPipelineExceptionMapper>? configure = null)
        {
            var mapper = new DefaultPipelineExceptionMapper();
            configure?.Invoke(mapper);
            services.TryAddSingleton<IPipelineExceptionMapper>(mapper);
            return services;
        }

        /// <summary>
        /// Adds default pipeline validator.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineValidator(
            this IServiceCollection services,
            Action<DefaultPipelineValidator>? configure = null)
        {
            var validator = new DefaultPipelineValidator();
            configure?.Invoke(validator);
            services.TryAddSingleton<IPipelineValidator>(validator);
            return services;
        }

        /// <summary>
        /// Adds logging middleware for pipeline operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineLoggingMiddleware(this IServiceCollection services)
        {
            services.TryAddSingleton<IPipelineMiddleware, LoggingPipelineMiddleware>();
            services.TryAddSingleton<IPipelineMiddlewareSync, LoggingPipelineMiddlewareSync>();
            return services;
        }

        /// <summary>
        /// Adds timeout middleware for pipeline operations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="defaultTimeout">Default timeout for operations.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineTimeoutMiddleware(
            this IServiceCollection services,
            TimeSpan defaultTimeout)
        {
            services.TryAddSingleton<IPipelineMiddleware>(new TimeoutPipelineMiddleware(defaultTimeout));
            return services;
        }

        /// <summary>
        /// Adds a custom pipeline middleware.
        /// </summary>
        /// <typeparam name="TMiddleware">The middleware type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineMiddleware<TMiddleware>(this IServiceCollection services)
            where TMiddleware : class, IPipelineMiddleware
        {
            services.AddSingleton<IPipelineMiddleware, TMiddleware>();
            return services;
        }

        /// <summary>
        /// Adds a custom sync pipeline middleware.
        /// </summary>
        /// <typeparam name="TMiddleware">The middleware type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineMiddlewareSync<TMiddleware>(this IServiceCollection services)
            where TMiddleware : class, IPipelineMiddlewareSync
        {
            services.AddSingleton<IPipelineMiddlewareSync, TMiddleware>();
            return services;
        }

        /// <summary>
        /// Adds a typed pipeline to the service collection.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTypedPipeline<TInput, TOutput>(
            this IServiceCollection services,
            Action<TypedPipeline<TInput, TOutput>>? configure = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(
                typeof(ITypedPipeline<TInput, TOutput>),
                sp =>
                {
                    var pipeline = new TypedPipeline<TInput, TOutput>();
                    configure?.Invoke(pipeline);
                    return pipeline;
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Adds an async typed pipeline to the service collection.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <param name="lifetime">The service lifetime (default: Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTypedPipelineAsync<TInput, TOutput>(
            this IServiceCollection services,
            Action<TypedPipelineAsync<TInput, TOutput>>? configure = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(
                typeof(ITypedPipelineAsync<TInput, TOutput>),
                sp =>
                {
                    var pipeline = new TypedPipelineAsync<TInput, TOutput>();
                    configure?.Invoke(pipeline);
                    return pipeline;
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Adds a typed operation to the service collection.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTypedOperation<TOperation, TInput, TOutput>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TOperation : class, ITypedOperation<TInput, TOutput>
        {
            services.Add(new ServiceDescriptor(typeof(ITypedOperation<TInput, TOutput>), typeof(TOperation), lifetime));
            return services;
        }

        /// <summary>
        /// Adds an async typed operation to the service collection.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTypedOperationAsync<TOperation, TInput, TOutput>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TOperation : class, ITypedOperationAsync<TInput, TOutput>
        {
            services.Add(new ServiceDescriptor(typeof(ITypedOperationAsync<TInput, TOutput>), typeof(TOperation), lifetime));
            return services;
        }
    }
}
