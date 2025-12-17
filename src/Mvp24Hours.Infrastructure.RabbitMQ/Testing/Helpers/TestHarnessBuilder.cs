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
using System.Reflection;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Helpers
{
    /// <summary>
    /// Builder for creating test harnesses with custom configurations.
    /// </summary>
    public class TestHarnessBuilder
    {
        private readonly IServiceCollection _services;
        private readonly List<Type> _consumerTypes = new();
        private readonly List<Type> _requestHandlerTypes = new();

        /// <summary>
        /// Creates a new test harness builder.
        /// </summary>
        public TestHarnessBuilder()
        {
            _services = new ServiceCollection();
        }

        /// <summary>
        /// Adds a service to the test harness.
        /// </summary>
        public TestHarnessBuilder AddService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _services.AddScoped<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// Adds a service to the test harness.
        /// </summary>
        public TestHarnessBuilder AddService<TService>(TService instance) where TService : class
        {
            _services.AddSingleton(instance);
            return this;
        }

        /// <summary>
        /// Adds a service factory to the test harness.
        /// </summary>
        public TestHarnessBuilder AddService<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            _services.AddScoped(factory);
            return this;
        }

        /// <summary>
        /// Adds a consumer to the test harness.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        public TestHarnessBuilder AddConsumer<TConsumer>() where TConsumer : class
        {
            var consumerType = typeof(TConsumer);
            _consumerTypes.Add(consumerType);
            _services.AddScoped(consumerType);

            // Register consumer for its message type
            var interfaces = consumerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

            foreach (var iface in interfaces)
            {
                _services.AddScoped(iface, consumerType);
            }

            return this;
        }

        /// <summary>
        /// Adds a request handler to the test harness.
        /// </summary>
        /// <typeparam name="THandler">The handler type.</typeparam>
        public TestHarnessBuilder AddRequestHandler<THandler>() where THandler : class
        {
            var handlerType = typeof(THandler);
            _requestHandlerTypes.Add(handlerType);
            _services.AddScoped(handlerType);

            // Register handler for its request/response types
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            foreach (var iface in interfaces)
            {
                _services.AddScoped(iface, handlerType);
            }

            return this;
        }

        /// <summary>
        /// Adds consumers from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        public TestHarnessBuilder AddConsumersFromAssembly(Assembly assembly)
        {
            var consumerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>)));

            foreach (var consumerType in consumerTypes)
            {
                _consumerTypes.Add(consumerType);
                _services.AddScoped(consumerType);

                var interfaces = consumerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

                foreach (var iface in interfaces)
                {
                    _services.AddScoped(iface, consumerType);
                }
            }

            return this;
        }

        /// <summary>
        /// Adds consumers from the assembly containing the specified type.
        /// </summary>
        /// <typeparam name="T">A type from the assembly.</typeparam>
        public TestHarnessBuilder AddConsumersFromAssemblyContaining<T>()
        {
            return AddConsumersFromAssembly(typeof(T).Assembly);
        }

        /// <summary>
        /// Configures services using an action.
        /// </summary>
        /// <param name="configure">Action to configure services.</param>
        public TestHarnessBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
            configure(_services);
            return this;
        }

        /// <summary>
        /// Adds the in-memory bus to the services.
        /// </summary>
        public TestHarnessBuilder UseInMemoryBus()
        {
            _services.AddSingleton<IInMemoryBus>(sp => new InMemoryBus(sp));
            _services.AddSingleton<IMvpRabbitMQClient>(sp => sp.GetRequiredService<IInMemoryBus>() as IMvpRabbitMQClient 
                ?? throw new InvalidOperationException("InMemoryBus not found"));
            return this;
        }

        /// <summary>
        /// Builds the test harness.
        /// </summary>
        /// <returns>A configured test harness.</returns>
        public ITestHarness Build()
        {
            var serviceProvider = _services.BuildServiceProvider();
            return new TestHarness(serviceProvider);
        }

        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        public static TestHarnessBuilder Create() => new();
    }
}

