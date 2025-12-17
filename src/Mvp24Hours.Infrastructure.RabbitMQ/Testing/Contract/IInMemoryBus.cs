//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract
{
    /// <summary>
    /// In-memory message bus for unit testing.
    /// Provides message tracking and consumer simulation without requiring a real RabbitMQ connection.
    /// </summary>
    public interface IInMemoryBus : IMvpRabbitMQClient
    {
        /// <summary>
        /// Gets all published messages.
        /// </summary>
        IReadOnlyList<IPublishedMessage> PublishedMessages { get; }

        /// <summary>
        /// Gets all published messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>List of published messages of the specified type.</returns>
        IReadOnlyList<IPublishedMessage<TMessage>> GetPublishedMessages<TMessage>() where TMessage : class;

        /// <summary>
        /// Gets all consumed messages.
        /// </summary>
        IReadOnlyList<IConsumedMessage> ConsumedMessages { get; }

        /// <summary>
        /// Gets all consumed messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>List of consumed messages of the specified type.</returns>
        IReadOnlyList<IConsumedMessage<TMessage>> GetConsumedMessages<TMessage>() where TMessage : class;

        /// <summary>
        /// Simulates consuming a message by delivering it to registered consumers.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to consume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the consumption result.</returns>
        Task<ConsumeResult> ConsumeAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Simulates consuming a message with custom context configuration.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="message">The message to consume.</param>
        /// <param name="configureContext">Action to configure the consume context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the consumption result.</returns>
        Task<ConsumeResult> ConsumeAsync<TMessage>(
            TMessage message,
            Action<TestConsumeContextBuilder<TMessage>> configureContext,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Clears all published and consumed messages.
        /// </summary>
        void Clear();

        /// <summary>
        /// Clears only published messages.
        /// </summary>
        void ClearPublished();

        /// <summary>
        /// Clears only consumed messages.
        /// </summary>
        void ClearConsumed();

        /// <summary>
        /// Asserts that a message of the specified type was published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>True if at least one message of the type was published.</returns>
        bool WasPublished<TMessage>() where TMessage : class;

        /// <summary>
        /// Asserts that a message matching the predicate was published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="predicate">Predicate to match the message.</param>
        /// <returns>True if a matching message was published.</returns>
        bool WasPublished<TMessage>(Func<TMessage, bool> predicate) where TMessage : class;

        /// <summary>
        /// Asserts that a message of the specified type was consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>True if at least one message of the type was consumed.</returns>
        bool WasConsumed<TMessage>() where TMessage : class;

        /// <summary>
        /// Asserts that a message matching the predicate was consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="predicate">Predicate to match the message.</param>
        /// <returns>True if a matching message was consumed.</returns>
        bool WasConsumed<TMessage>(Func<TMessage, bool> predicate) where TMessage : class;

        /// <summary>
        /// Gets the count of published messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>Count of published messages.</returns>
        int PublishedCount<TMessage>() where TMessage : class;

        /// <summary>
        /// Gets the count of consumed messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>Count of consumed messages.</returns>
        int ConsumedCount<TMessage>() where TMessage : class;

        /// <summary>
        /// Configures the bus to simulate a timeout on the next consume operation.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        void SimulateTimeout(TimeSpan timeout);

        /// <summary>
        /// Configures the bus to simulate a failure on the next consume operation.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        void SimulateFailure(Exception exception);

        /// <summary>
        /// Configures the bus to simulate a delayed response.
        /// </summary>
        /// <param name="delay">The delay duration.</param>
        void SimulateDelay(TimeSpan delay);

        /// <summary>
        /// Resets all simulation configurations.
        /// </summary>
        void ResetSimulations();
    }
}

