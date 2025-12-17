//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Saga;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Saga.Extensions
{
    /// <summary>
    /// Extension methods for integrating CQRS sagas with RabbitMQ.
    /// </summary>
    public static class CqrsSagaServiceExtensions
    {
        /// <summary>
        /// Adds CQRS saga integration with RabbitMQ.
        /// Enables CQRS sagas to be triggered and driven by RabbitMQ messages.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TSaga">The type of CQRS saga.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddCqrsSagaRabbitMQIntegration&lt;OrderSagaData, OrderSaga&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddCqrsSagaRabbitMQIntegration<TData, TSaga>(this IServiceCollection services)
            where TData : class
            where TSaga : SagaBase<TData>
        {
            // Register the CQRS saga adapter
            services.TryAddScoped<CqrsSagaAdapter<TData, TSaga>>();

            return services;
        }

        /// <summary>
        /// Adds a CQRS saga message consumer that starts a saga from a RabbitMQ message.
        /// </summary>
        /// <typeparam name="TData">The type of saga data.</typeparam>
        /// <typeparam name="TSaga">The type of CQRS saga.</typeparam>
        /// <typeparam name="TMessage">The type of message that starts the saga.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="dataFactory">Factory to create saga data from the message.</param>
        /// <param name="correlationIdExtractor">Optional correlation ID extractor.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddCqrsSagaConsumer&lt;OrderSagaData, OrderSaga, OrderCreatedEvent&gt;(
        ///     msg => new OrderSagaData { OrderId = msg.OrderId },
        ///     msg => msg.OrderId);
        /// </code>
        /// </example>
        public static IServiceCollection AddCqrsSagaConsumer<TData, TSaga, TMessage>(
            this IServiceCollection services,
            Func<TMessage, TData> dataFactory,
            Func<TMessage, Guid>? correlationIdExtractor = null)
            where TData : class
            where TSaga : SagaBase<TData>
            where TMessage : class
        {
            // Ensure adapter is registered
            services.AddCqrsSagaRabbitMQIntegration<TData, TSaga>();

            // Register the consumer
            services.AddScoped<CqrsSagaMessageConsumer<TData, TSaga, TMessage>>(sp =>
            {
                var adapter = sp.GetRequiredService<CqrsSagaAdapter<TData, TSaga>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<CqrsSagaMessageConsumer<TData, TSaga, TMessage>>>();
                return new CqrsSagaMessageConsumer<TData, TSaga, TMessage>(
                    adapter, dataFactory, correlationIdExtractor, logger);
            });

            return services;
        }
    }
}

