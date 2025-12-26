//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Configuration
{
    /// <summary>
    /// Factory for creating lazy-initialized providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory enables lazy initialization of expensive providers (e.g., those requiring
    /// external connections like Redis, Azure, AWS) to improve application startup time.
    /// </para>
    /// </remarks>
    internal static class LazyProviderFactory
    {
        /// <summary>
        /// Creates a lazy-initialized service factory.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="factory">The factory function to create the service.</param>
        /// <param name="enableLazyInit">Whether lazy initialization is enabled.</param>
        /// <returns>A function that returns the service instance.</returns>
        public static Func<TService> CreateLazyFactory<TService>(
            IServiceProvider serviceProvider,
            Func<IServiceProvider, TService> factory,
            bool enableLazyInit)
            where TService : class
        {
            if (!enableLazyInit)
            {
                // Eager initialization - create immediately
                var instance = factory(serviceProvider);
                return () => instance;
            }

            // Lazy initialization - create on first access
            Lazy<TService>? lazyInstance = null;
            var lockObject = new object();

            return () =>
            {
                if (lazyInstance == null)
                {
                    lock (lockObject)
                    {
                        if (lazyInstance == null)
                        {
                            lazyInstance = new Lazy<TService>(() => factory(serviceProvider), LazyThreadSafetyMode.ExecutionAndPublication);
                        }
                    }
                }

                return lazyInstance.Value;
            };
        }

        /// <summary>
        /// Registers a service with lazy initialization support.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">The factory function to create the service.</param>
        /// <param name="enableLazyInit">Whether lazy initialization is enabled.</param>
        /// <param name="lifetime">The service lifetime.</param>
        public static void RegisterLazyService<TService>(
            IServiceCollection services,
            Func<IServiceProvider, TService> factory,
            bool enableLazyInit,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TService : class
        {
            if (enableLazyInit)
            {
                // Register as factory that creates lazy instance
                services.Add(new ServiceDescriptor(
                    typeof(TService),
                    serviceProvider =>
                    {
                        var lazyFactory = CreateLazyFactory(serviceProvider, factory, true);
                        return lazyFactory();
                    },
                    lifetime));
            }
            else
            {
                // Register normally (eager initialization)
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(factory);
                        break;
                    case ServiceLifetime.Scoped:
                        services.AddScoped(factory);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(factory);
                        break;
                }
            }
        }
    }
}

