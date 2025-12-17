//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Extensions
{
    /// <summary>
    /// Extension methods for configuring RabbitMQ testing services.
    /// </summary>
    public static class TestingServiceExtensions
    {
        /// <summary>
        /// Adds the in-memory RabbitMQ bus for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemoryRabbitMQ(this IServiceCollection services)
        {
            services.AddSingleton<IInMemoryBus>(sp => new InMemoryBus(sp));
            services.AddSingleton<IMvpRabbitMQClient>(sp => 
                sp.GetRequiredService<IInMemoryBus>() as IMvpRabbitMQClient
                ?? throw new InvalidOperationException("InMemoryBus not registered"));
            
            return services;
        }

        /// <summary>
        /// Adds the test harness for integration testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQTestHarness(this IServiceCollection services)
        {
            services.AddInMemoryRabbitMQ();
            services.AddSingleton<ITestHarness>(sp => new TestHarness(sp));
            
            return services;
        }

        /// <summary>
        /// Adds the test harness with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure the test harness.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRabbitMQTestHarness(
            this IServiceCollection services,
            Action<TestHarnessOptions> configure)
        {
            var options = new TestHarnessOptions();
            configure(options);

            services.AddInMemoryRabbitMQ();

            if (options.AutoRegisterConsumers)
            {
                // Auto-register consumers from specified assemblies
                foreach (var assembly in options.ConsumerAssemblies)
                {
                    var consumerTypes = assembly.GetTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface)
                        .Where(t => t.GetInterfaces().Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>)));

                    foreach (var consumerType in consumerTypes)
                    {
                        services.AddScoped(consumerType);

                        var interfaces = consumerType.GetInterfaces()
                            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

                        foreach (var iface in interfaces)
                        {
                            services.AddScoped(iface, consumerType);
                        }
                    }
                }
            }

            services.AddSingleton<ITestHarness>(sp => new TestHarness(sp));

            return services;
        }

        /// <summary>
        /// Replaces the RabbitMQ client with the in-memory bus for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ReplaceRabbitMQWithInMemory(this IServiceCollection services)
        {
            // Remove existing IMvpRabbitMQClient registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IMvpRabbitMQClient))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory implementation
            services.AddInMemoryRabbitMQ();

            return services;
        }

        /// <summary>
        /// Adds a test consumer to the service collection.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTestConsumer<TConsumer>(this IServiceCollection services)
            where TConsumer : class
        {
            var consumerType = typeof(TConsumer);
            services.AddScoped(consumerType);

            var interfaces = consumerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, consumerType);
            }

            return services;
        }

        /// <summary>
        /// Adds a test request handler to the service collection.
        /// </summary>
        /// <typeparam name="THandler">The handler type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTestRequestHandler<THandler>(this IServiceCollection services)
            where THandler : class
        {
            var handlerType = typeof(THandler);
            services.AddScoped(handlerType);

            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, handlerType);
            }

            return services;
        }
    }

    /// <summary>
    /// Options for configuring the test harness.
    /// </summary>
    public class TestHarnessOptions
    {
        /// <summary>
        /// Gets or sets whether to auto-register consumers from assemblies.
        /// </summary>
        public bool AutoRegisterConsumers { get; set; } = false;

        /// <summary>
        /// Gets or sets the assemblies to scan for consumers.
        /// </summary>
        public List<System.Reflection.Assembly> ConsumerAssemblies { get; set; } = new();

        /// <summary>
        /// Adds an assembly to scan for consumers.
        /// </summary>
        public TestHarnessOptions AddConsumersFromAssembly(System.Reflection.Assembly assembly)
        {
            AutoRegisterConsumers = true;
            ConsumerAssemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// Adds the assembly containing the specified type.
        /// </summary>
        public TestHarnessOptions AddConsumersFromAssemblyContaining<T>()
        {
            return AddConsumersFromAssembly(typeof(T).Assembly);
        }
    }
}

