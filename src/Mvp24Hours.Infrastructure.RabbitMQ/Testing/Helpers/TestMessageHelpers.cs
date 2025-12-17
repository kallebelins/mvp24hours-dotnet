//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Testing.Helpers
{
    /// <summary>
    /// Helper methods for testing message operations.
    /// </summary>
    public static class TestMessageHelpers
    {
        /// <summary>
        /// Simulates a timeout on the next consume operation.
        /// </summary>
        /// <param name="bus">The in-memory bus.</param>
        /// <param name="delayMilliseconds">Delay in milliseconds before timeout.</param>
        public static void SimulateTimeoutOnNextConsume(this IInMemoryBus bus, int delayMilliseconds = 100)
        {
            bus.SimulateTimeout(TimeSpan.FromMilliseconds(delayMilliseconds));
        }

        /// <summary>
        /// Simulates a failure on the next consume operation.
        /// </summary>
        /// <param name="bus">The in-memory bus.</param>
        /// <param name="message">The exception message.</param>
        public static void SimulateFailureOnNextConsume(this IInMemoryBus bus, string message = "Simulated failure")
        {
            bus.SimulateFailure(new Exception(message));
        }

        /// <summary>
        /// Simulates a specific exception on the next consume operation.
        /// </summary>
        /// <typeparam name="TException">The exception type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        /// <param name="message">The exception message.</param>
        public static void SimulateFailureOnNextConsume<TException>(this IInMemoryBus bus, string message = "Simulated failure")
            where TException : Exception
        {
            var exception = (TException?)Activator.CreateInstance(typeof(TException), message)
                ?? throw new InvalidOperationException($"Could not create exception of type {typeof(TException).Name}");
            bus.SimulateFailure(exception);
        }

        /// <summary>
        /// Simulates a delay on the next consume operation.
        /// </summary>
        /// <param name="bus">The in-memory bus.</param>
        /// <param name="delayMilliseconds">Delay in milliseconds.</param>
        public static void SimulateDelayOnNextConsume(this IInMemoryBus bus, int delayMilliseconds)
        {
            bus.SimulateDelay(TimeSpan.FromMilliseconds(delayMilliseconds));
        }

        /// <summary>
        /// Simulates network failure (connection lost).
        /// </summary>
        /// <param name="bus">The in-memory bus.</param>
        public static void SimulateNetworkFailure(this IInMemoryBus bus)
        {
            bus.SimulateFailure(new System.Net.Sockets.SocketException(10054)); // Connection reset
        }

        /// <summary>
        /// Simulates broker unavailable.
        /// </summary>
        /// <param name="bus">The in-memory bus.</param>
        public static void SimulateBrokerUnavailable(this IInMemoryBus bus)
        {
            bus.SimulateFailure(new InvalidOperationException("Broker is unavailable"));
        }

        /// <summary>
        /// Asserts that exactly one message of the type was published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        /// <returns>The published message.</returns>
        public static IPublishedMessage<TMessage> AssertSinglePublished<TMessage>(this IInMemoryBus bus) where TMessage : class
        {
            var messages = bus.GetPublishedMessages<TMessage>();
            if (messages.Count == 0)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} to be published, but none were published.");
            }
            if (messages.Count > 1)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} to be published, but {messages.Count} were published.");
            }
            return messages.First();
        }

        /// <summary>
        /// Asserts that exactly one message matching the predicate was published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        /// <param name="predicate">Predicate to match.</param>
        /// <returns>The published message.</returns>
        public static IPublishedMessage<TMessage> AssertSinglePublished<TMessage>(
            this IInMemoryBus bus,
            Func<TMessage, bool> predicate) where TMessage : class
        {
            var messages = bus.GetPublishedMessages<TMessage>()
                .Where(m => predicate(m.Message))
                .ToList();

            if (messages.Count == 0)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} matching the predicate to be published, but none matched.");
            }
            if (messages.Count > 1)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} matching the predicate to be published, but {messages.Count} matched.");
            }
            return messages.First();
        }

        /// <summary>
        /// Asserts that no messages of the type were published.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        public static void AssertNonePublished<TMessage>(this IInMemoryBus bus) where TMessage : class
        {
            var count = bus.PublishedCount<TMessage>();
            if (count > 0)
            {
                throw new InvalidOperationException($"Expected no messages of type {typeof(TMessage).Name} to be published, but {count} were published.");
            }
        }

        /// <summary>
        /// Asserts that exactly one message of the type was consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        /// <returns>The consumed message.</returns>
        public static IConsumedMessage<TMessage> AssertSingleConsumed<TMessage>(this IInMemoryBus bus) where TMessage : class
        {
            var messages = bus.GetConsumedMessages<TMessage>();
            if (messages.Count == 0)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} to be consumed, but none were consumed.");
            }
            if (messages.Count > 1)
            {
                throw new InvalidOperationException($"Expected exactly one message of type {typeof(TMessage).Name} to be consumed, but {messages.Count} were consumed.");
            }
            return messages.First();
        }

        /// <summary>
        /// Asserts that no messages of the type were consumed.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        public static void AssertNoneConsumed<TMessage>(this IInMemoryBus bus) where TMessage : class
        {
            var count = bus.ConsumedCount<TMessage>();
            if (count > 0)
            {
                throw new InvalidOperationException($"Expected no messages of type {typeof(TMessage).Name} to be consumed, but {count} were consumed.");
            }
        }

        /// <summary>
        /// Asserts that all consumed messages of the type were successful.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="bus">The in-memory bus.</param>
        public static void AssertAllConsumedSuccessfully<TMessage>(this IInMemoryBus bus) where TMessage : class
        {
            var messages = bus.GetConsumedMessages<TMessage>();
            var failed = messages.Where(m => !m.IsSuccess).ToList();
            
            if (failed.Count > 0)
            {
                var errors = string.Join(", ", failed.Select(f => f.Exception?.Message ?? "Unknown error"));
                throw new InvalidOperationException($"Expected all consumed messages to be successful, but {failed.Count} failed: {errors}");
            }
        }
    }
}

