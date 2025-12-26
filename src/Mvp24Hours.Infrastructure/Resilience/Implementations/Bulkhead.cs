//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Contract;
using Mvp24Hours.Infrastructure.Resilience.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Implementations
{
    /// <summary>
    /// Generic implementation of bulkhead pattern for limiting concurrent operations.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <remarks>
    /// <para>
    /// This implementation uses SemaphoreSlim to limit the number of concurrent operations.
    /// When the limit is reached, new operations will wait until a slot becomes available.
    /// </para>
    /// </remarks>
    public class Bulkhead<TResult> : IBulkhead<TResult>
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;
        private int _currentConcurrency;
        private int _queuedOperations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bulkhead{TResult}"/> class.
        /// </summary>
        /// <param name="maxConcurrency">Maximum number of concurrent operations allowed.</param>
        /// <param name="maxQueueLength">Maximum number of operations that can wait in queue. Default is 0 (no queue).</param>
        public Bulkhead(int maxConcurrency, int maxQueueLength = 0)
        {
            if (maxConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be greater than 0.");
            }

            _maxConcurrency = maxConcurrency;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        /// <inheritdoc/>
        public int MaxConcurrency => _maxConcurrency;

        /// <inheritdoc/>
        public int CurrentConcurrency => _currentConcurrency;

        /// <inheritdoc/>
        public int QueuedOperations => _queuedOperations;

        /// <inheritdoc/>
        public async Task<TResult> ExecuteAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync((_, ct) => operation(ct), null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TResult> ExecuteAsync(
            Func<object?, CancellationToken, Task<TResult>> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            // Try to acquire semaphore
            if (!await _semaphore.WaitAsync(TimeSpan.Zero, cancellationToken))
            {
                Interlocked.Increment(ref _queuedOperations);
                try
                {
                    // Wait for a slot to become available
                    await _semaphore.WaitAsync(cancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref _queuedOperations);
                }
            }

            Interlocked.Increment(ref _currentConcurrency);

            try
            {
                return await operation(context, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Generic implementation of bulkhead pattern for operations without return values.
    /// </summary>
    public class Bulkhead : IBulkhead
    {
        private readonly Bulkhead<object?> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bulkhead"/> class.
        /// </summary>
        /// <param name="maxConcurrency">Maximum number of concurrent operations allowed.</param>
        /// <param name="maxQueueLength">Maximum number of operations that can wait in queue. Default is 0 (no queue).</param>
        public Bulkhead(int maxConcurrency, int maxQueueLength = 0)
        {
            _inner = new Bulkhead<object?>(maxConcurrency, maxQueueLength);
        }

        /// <inheritdoc/>
        public int MaxConcurrency => _inner.MaxConcurrency;

        /// <inheritdoc/>
        public int CurrentConcurrency => _inner.CurrentConcurrency;

        /// <inheritdoc/>
        public int QueuedOperations => _inner.QueuedOperations;

        /// <inheritdoc/>
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync((_, ct) => operation(ct), null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(
            Func<object?, CancellationToken, Task> operation,
            object? context = null,
            CancellationToken cancellationToken = default)
        {
            await _inner.ExecuteAsync(
                async (ctx, ct) =>
                {
                    await operation(ctx, ct);
                    return (object?)null;
                },
                context,
                cancellationToken);
        }
    }
}

