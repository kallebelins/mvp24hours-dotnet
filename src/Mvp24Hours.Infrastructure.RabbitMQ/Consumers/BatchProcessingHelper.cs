//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Consumers
{
    /// <summary>
    /// Helper class for processing batches of messages with various strategies.
    /// </summary>
    public static class BatchProcessingHelper
    {
        /// <summary>
        /// Process batch messages in parallel with a specified degree of parallelism.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="processFunc">The function to process each message.</param>
        /// <param name="maxDegreeOfParallelism">Maximum degree of parallelism. 0 = processor count.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of batch message results.</returns>
        public static async Task<IEnumerable<IBatchMessageResult>> ProcessInParallelAsync<TMessage>(
            IBatchConsumeContext<TMessage> context,
            Func<IBatchMessageItem<TMessage>, CancellationToken, Task<bool>> processFunc,
            int maxDegreeOfParallelism = 0,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            var results = new ConcurrentBag<IBatchMessageResult>();
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            var tasks = context.Messages.Select(async item =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var success = await processFunc(item, cancellationToken);
                    results.Add(success
                        ? BatchMessageResult.Ack(item.DeliveryTag)
                        : BatchMessageResult.Nack(item.DeliveryTag));
                }
                catch (Exception ex)
                {
                    results.Add(BatchMessageResult.Nack(item.DeliveryTag, requeue: true, errorMessage: ex.Message));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }

        /// <summary>
        /// Process batch messages sequentially.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="processFunc">The function to process each message.</param>
        /// <param name="stopOnFirstError">Whether to stop processing on the first error.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of batch message results.</returns>
        public static async Task<IEnumerable<IBatchMessageResult>> ProcessSequentiallyAsync<TMessage>(
            IBatchConsumeContext<TMessage> context,
            Func<IBatchMessageItem<TMessage>, CancellationToken, Task<bool>> processFunc,
            bool stopOnFirstError = false,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            var results = new List<IBatchMessageResult>();

            foreach (var item in context.Messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var success = await processFunc(item, cancellationToken);
                    results.Add(success
                        ? BatchMessageResult.Ack(item.DeliveryTag)
                        : BatchMessageResult.Nack(item.DeliveryTag));

                    if (!success && stopOnFirstError)
                    {
                        // Nack remaining messages
                        foreach (var remaining in context.Messages.SkipWhile(m => m.DeliveryTag <= item.DeliveryTag))
                        {
                            results.Add(BatchMessageResult.Nack(remaining.DeliveryTag, requeue: true,
                                errorMessage: "Processing stopped due to previous error"));
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(BatchMessageResult.Nack(item.DeliveryTag, requeue: true, errorMessage: ex.Message));

                    if (stopOnFirstError)
                    {
                        // Nack remaining messages
                        foreach (var remaining in context.Messages.SkipWhile(m => m.DeliveryTag <= item.DeliveryTag))
                        {
                            results.Add(BatchMessageResult.Nack(remaining.DeliveryTag, requeue: true,
                                errorMessage: "Processing stopped due to previous error"));
                        }
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Process batch messages with retry for failed items.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="processFunc">The function to process each message.</param>
        /// <param name="maxRetries">Maximum number of retries per message.</param>
        /// <param name="retryDelay">Delay between retries.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of batch message results.</returns>
        public static async Task<IEnumerable<IBatchMessageResult>> ProcessWithRetryAsync<TMessage>(
            IBatchConsumeContext<TMessage> context,
            Func<IBatchMessageItem<TMessage>, CancellationToken, Task<bool>> processFunc,
            int maxRetries = 3,
            TimeSpan? retryDelay = null,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            var delay = retryDelay ?? TimeSpan.FromMilliseconds(100);
            var results = new List<IBatchMessageResult>();

            foreach (var item in context.Messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retryCount = 0;
                var success = false;
                string? lastError = null;

                while (retryCount <= maxRetries && !success)
                {
                    try
                    {
                        success = await processFunc(item, cancellationToken);
                        if (!success)
                        {
                            lastError = "Processing returned false";
                            retryCount++;
                            if (retryCount <= maxRetries)
                            {
                                await Task.Delay(delay, cancellationToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.Message;
                        retryCount++;
                        if (retryCount <= maxRetries)
                        {
                            await Task.Delay(delay, cancellationToken);
                        }
                    }
                }

                results.Add(success
                    ? BatchMessageResult.Ack(item.DeliveryTag)
                    : BatchMessageResult.Nack(item.DeliveryTag, requeue: false, errorMessage: lastError));
            }

            return results;
        }

        /// <summary>
        /// Process batch messages as a transaction (all or nothing).
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="processFunc">The function to process the entire batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of batch message results (all ack or all nack).</returns>
        public static async Task<IEnumerable<IBatchMessageResult>> ProcessAsTransactionAsync<TMessage>(
            IBatchConsumeContext<TMessage> context,
            Func<IBatchConsumeContext<TMessage>, CancellationToken, Task<bool>> processFunc,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            try
            {
                var success = await processFunc(context, cancellationToken);
                return success
                    ? BatchMessageResult.AckAll(context)
                    : BatchMessageResult.NackAll(context, requeue: true, errorMessage: "Batch processing returned false");
            }
            catch (Exception ex)
            {
                return BatchMessageResult.NackAll(context, requeue: true, errorMessage: ex.Message);
            }
        }

        /// <summary>
        /// Group batch messages by a key and process each group.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <typeparam name="TKey">The grouping key type.</typeparam>
        /// <param name="context">The batch consume context.</param>
        /// <param name="keySelector">Function to select the grouping key.</param>
        /// <param name="processGroupFunc">Function to process each group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of batch message results.</returns>
        public static async Task<IEnumerable<IBatchMessageResult>> ProcessByGroupAsync<TMessage, TKey>(
            IBatchConsumeContext<TMessage> context,
            Func<IBatchMessageItem<TMessage>, TKey> keySelector,
            Func<TKey, IReadOnlyList<IBatchMessageItem<TMessage>>, CancellationToken, Task<bool>> processGroupFunc,
            CancellationToken cancellationToken = default)
            where TMessage : class
            where TKey : notnull
        {
            var results = new List<IBatchMessageResult>();
            var groups = context.Messages.GroupBy(keySelector).ToList();

            foreach (var group in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var groupItems = group.ToList();
                    var success = await processGroupFunc(group.Key, groupItems, cancellationToken);

                    foreach (var item in groupItems)
                    {
                        results.Add(success
                            ? BatchMessageResult.Ack(item.DeliveryTag)
                            : BatchMessageResult.Nack(item.DeliveryTag, requeue: true));
                    }
                }
                catch (Exception ex)
                {
                    foreach (var item in group)
                    {
                        results.Add(BatchMessageResult.Nack(item.DeliveryTag, requeue: true, errorMessage: ex.Message));
                    }
                }
            }

            return results;
        }
    }
}

