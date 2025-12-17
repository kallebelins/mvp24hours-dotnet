//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract
{
    /// <summary>
    /// Test harness for integration testing of RabbitMQ consumers and messaging.
    /// Provides a controlled environment for testing message flows.
    /// </summary>
    public interface ITestHarness : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets the in-memory bus for message tracking.
        /// </summary>
        IInMemoryBus Bus { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets all published messages.
        /// </summary>
        IReadOnlyList<IPublishedMessage> Published { get; }

        /// <summary>
        /// Gets all consumed messages.
        /// </summary>
        IReadOnlyList<IConsumedMessage> Consumed { get; }

        /// <summary>
        /// Starts the test harness and initializes all consumers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the test harness.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a message for testing.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="routingKey">Optional routing key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<string> PublishAsync<TMessage>(
            TMessage message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Publishes a message and waits for it to be consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to publish.</param>
        /// <param name="timeout">Maximum time to wait for consumption.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The consumed message information.</returns>
        Task<IConsumedMessage<TMessage>> PublishAndWaitAsync<TMessage>(
            TMessage message,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Sends a request and waits for a response.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="timeout">Maximum time to wait for response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response.</returns>
        Task<Response<TResponse>> RequestAsync<TRequest, TResponse>(
            TRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Gets a consumer harness for a specific consumer type.
        /// </summary>
        /// <typeparam name="TConsumer">The consumer type.</typeparam>
        /// <returns>The consumer harness.</returns>
        IConsumerHarness<TConsumer> GetConsumerHarness<TConsumer>() where TConsumer : class;

        /// <summary>
        /// Waits for a message of the specified type to be published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <param name="predicate">Optional predicate to filter messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The published message.</returns>
        Task<IPublishedMessage<TMessage>> WaitForPublishAsync<TMessage>(
            TimeSpan? timeout = null,
            Func<TMessage, bool>? predicate = null,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Waits for a message of the specified type to be consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="timeout">Maximum time to wait.</param>
        /// <param name="predicate">Optional predicate to filter messages.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The consumed message.</returns>
        Task<IConsumedMessage<TMessage>> WaitForConsumeAsync<TMessage>(
            TimeSpan? timeout = null,
            Func<TMessage, bool>? predicate = null,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Clears all tracked messages.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Consumer-specific test harness for testing individual consumers.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    public interface IConsumerHarness<TConsumer> where TConsumer : class
    {
        /// <summary>
        /// Gets the consumer instance.
        /// </summary>
        TConsumer Consumer { get; }

        /// <summary>
        /// Gets all messages consumed by this consumer.
        /// </summary>
        IReadOnlyList<IConsumedMessage> Consumed { get; }

        /// <summary>
        /// Sends a message directly to this consumer.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The consumption result.</returns>
        Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Sends a message with custom context to this consumer.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="configureContext">Action to configure the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The consumption result.</returns>
        Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            Action<TestConsumeContextBuilder<TMessage>> configureContext,
            CancellationToken cancellationToken = default) where TMessage : class;
    }
}

