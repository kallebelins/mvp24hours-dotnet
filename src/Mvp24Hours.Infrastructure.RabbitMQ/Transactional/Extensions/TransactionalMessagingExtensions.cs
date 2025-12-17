//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Extensions
{
    /// <summary>
    /// Extension methods for registering transactional messaging services.
    /// </summary>
    public static class TransactionalMessagingExtensions
    {
        /// <summary>
        /// Adds transactional messaging services with the in-memory outbox (for testing/development).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for the transactional bus.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// <list type="bullet">
        /// <item><see cref="InMemoryTransactionalOutbox"/> - In-memory outbox storage</item>
        /// <item><see cref="TransactionalBus"/> - The transactional bus implementation</item>
        /// <item><see cref="TransactionalEnlistment"/> - System.Transactions support</item>
        /// <item><see cref="TransactionalConsumeContextFactory"/> - Factory for transactional contexts</item>
        /// <item><see cref="OutboxPublisher"/> - Background service for publishing</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Warning:</strong> The in-memory outbox is not suitable for production use
        /// as messages will be lost on application restart.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvpRabbitMQ(...)
        ///         .AddTransactionalMessaging();
        /// </code>
        /// </example>
        public static IServiceCollection AddTransactionalMessaging(
            this IServiceCollection services,
            Action<TransactionalBusOptions>? configureOptions = null)
        {
            // Register in-memory outbox
            services.TryAddSingleton<ITransactionalOutbox, InMemoryTransactionalOutbox>();
            services.TryAddSingleton<InMemoryTransactionalOutboxOptions>();

            return services.AddTransactionalMessagingCore(configureOptions);
        }

        /// <summary>
        /// Adds transactional messaging services with a custom outbox implementation.
        /// </summary>
        /// <typeparam name="TOutbox">The custom outbox implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration for the transactional bus.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddMvpRabbitMQ(...)
        ///         .AddTransactionalMessaging&lt;EfCoreTransactionalOutbox&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddTransactionalMessaging<TOutbox>(
            this IServiceCollection services,
            Action<TransactionalBusOptions>? configureOptions = null)
            where TOutbox : class, ITransactionalOutbox
        {
            services.TryAddScoped<ITransactionalOutbox, TOutbox>();

            return services.AddTransactionalMessagingCore(configureOptions);
        }

        /// <summary>
        /// Adds transactional messaging services with a factory for the outbox.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="outboxFactory">Factory function for creating the outbox.</param>
        /// <param name="configureOptions">Optional configuration for the transactional bus.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTransactionalMessaging(
            this IServiceCollection services,
            Func<IServiceProvider, ITransactionalOutbox> outboxFactory,
            Action<TransactionalBusOptions>? configureOptions = null)
        {
            services.TryAddScoped(outboxFactory);

            return services.AddTransactionalMessagingCore(configureOptions);
        }

        /// <summary>
        /// Adds the in-memory outbox with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration for the in-memory outbox.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemoryOutbox(
            this IServiceCollection services,
            Action<InMemoryTransactionalOutboxOptions>? configureOptions = null)
        {
            var options = new InMemoryTransactionalOutboxOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.TryAddSingleton<ITransactionalOutbox, InMemoryTransactionalOutbox>();

            return services;
        }

        /// <summary>
        /// Configures the outbox publisher options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration for the outbox publisher.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureOutboxPublisher(
            this IServiceCollection services,
            Action<OutboxPublisherOptions> configureOptions)
        {
            var options = new OutboxPublisherOptions();
            configureOptions(options);
            services.AddSingleton(options);

            return services;
        }

        private static IServiceCollection AddTransactionalMessagingCore(
            this IServiceCollection services,
            Action<TransactionalBusOptions>? configureOptions = null)
        {
            // Configure options
            var busOptions = new TransactionalBusOptions();
            configureOptions?.Invoke(busOptions);
            services.TryAddSingleton(busOptions);

            // Register core services
            services.TryAddScoped<TransactionalBus>();
            services.TryAddScoped<ITransactionalBus>(sp => sp.GetRequiredService<TransactionalBus>());

            // Register transaction support
            services.TryAddScoped<ITransactionalEnlistment, TransactionalEnlistment>();

            // Register context factory
            services.TryAddScoped<ITransactionalConsumeContextFactory, TransactionalConsumeContextFactory>();

            // Register outbox publisher options if not already registered
            services.TryAddSingleton<OutboxPublisherOptions>();

            // Register the background publisher service
            services.AddHostedService<OutboxPublisher>();

            // Also register IOutboxPublisher to get status
            services.TryAddSingleton<IOutboxPublisher>(sp =>
            {
                // Get the hosted service instance
                var hostedServices = sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
                foreach (var service in hostedServices)
                {
                    if (service is OutboxPublisher publisher)
                    {
                        return publisher;
                    }
                }
                throw new InvalidOperationException("OutboxPublisher hosted service not found");
            });

            return services;
        }
    }
}

