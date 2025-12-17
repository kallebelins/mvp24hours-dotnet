//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Strongly-typed batch message consumer interface.
    /// Implement this interface to consume batches of messages of a specific type.
    /// Batch consumers process multiple messages together for improved throughput.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to consume.</typeparam>
    /// <example>
    /// <code>
    /// public class OrderBatchConsumer : IBatchConsumer&lt;OrderMessage&gt;
    /// {
    ///     public async Task&lt;IEnumerable&lt;IBatchMessageResult&gt;&gt; ConsumeAsync(
    ///         IBatchConsumeContext&lt;OrderMessage&gt; context,
    ///         CancellationToken cancellationToken = default)
    ///     {
    ///         var results = new List&lt;BatchMessageResult&gt;();
    ///         foreach (var item in context.Messages)
    ///         {
    ///             try
    ///             {
    ///                 // Process each message
    ///                 await ProcessOrderAsync(item.Message);
    ///                 results.Add(BatchMessageResult.Ack(item.DeliveryTag));
    ///             }
    ///             catch (Exception ex)
    ///             {
    ///                 // Nack this specific message
    ///                 results.Add(BatchMessageResult.Nack(item.DeliveryTag, requeue: true));
    ///             }
    ///         }
    ///         return results;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBatchConsumer<TMessage> where TMessage : class
    {
        /// <summary>
        /// Handles the consumption of a batch of messages.
        /// </summary>
        /// <param name="context">The batch consume context containing the messages and metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// Returns a collection of results indicating the acknowledgment status for each message.
        /// If null or empty is returned, all messages are acknowledged.
        /// </returns>
        Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(
            IBatchConsumeContext<TMessage> context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result for a single message in a batch, indicating whether it should be acked or nacked.
    /// </summary>
    public interface IBatchMessageResult
    {
        /// <summary>
        /// Gets the delivery tag of the message.
        /// </summary>
        ulong DeliveryTag { get; }

        /// <summary>
        /// Gets whether the message was successfully processed.
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Gets whether the message should be requeued (only applicable when Success is false).
        /// </summary>
        bool Requeue { get; }

        /// <summary>
        /// Gets the optional error message if processing failed.
        /// </summary>
        string? ErrorMessage { get; }
    }
}

