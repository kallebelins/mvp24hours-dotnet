//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Streaming
{
    /// <summary>
    /// Base class for operations that process items in a streaming fashion.
    /// </summary>
    /// <typeparam name="TInput">The type of input.</typeparam>
    /// <typeparam name="TOutput">The type of output.</typeparam>
    public abstract class StreamingOperationBase<TInput, TOutput> : IStreamingOperation<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets whether to continue processing when an item fails.
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets the buffer size for the stream.
        /// </summary>
        public int BufferSize { get; set; } = 1;

        /// <inheritdoc/>
        public abstract IAsyncEnumerable<TOutput> ProcessStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A streaming operation that transforms each input item to an output item.
    /// </summary>
    /// <typeparam name="TInput">The type of input.</typeparam>
    /// <typeparam name="TOutput">The type of output.</typeparam>
    public class TransformStreamingOperation<TInput, TOutput> : StreamingOperationBase<TInput, TOutput>
    {
        private readonly Func<TInput, CancellationToken, Task<TOutput>> _transform;

        /// <summary>
        /// Creates a new transform streaming operation.
        /// </summary>
        /// <param name="transform">The transformation function.</param>
        public TransformStreamingOperation(Func<TInput, CancellationToken, Task<TOutput>> transform)
        {
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<TOutput> ProcessStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                TOutput output;
                try
                {
                    output = await _transform(input, cancellationToken);
                }
                catch (Exception)
                {
                    if (!ContinueOnError)
                        throw;
                    continue;
                }

                yield return output;
            }
        }
    }

    /// <summary>
    /// A streaming operation that filters items based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    public class FilterStreamingOperation<T> : StreamingOperationBase<T, T>
    {
        private readonly Func<T, CancellationToken, Task<bool>> _predicate;

        /// <summary>
        /// Creates a new filter streaming operation.
        /// </summary>
        /// <param name="predicate">The filter predicate.</param>
        public FilterStreamingOperation(Func<T, CancellationToken, Task<bool>> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Creates a new filter streaming operation with a synchronous predicate.
        /// </summary>
        /// <param name="predicate">The filter predicate.</param>
        public FilterStreamingOperation(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _predicate = (item, _) => Task.FromResult(predicate(item));
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<T> ProcessStreamAsync(
            IAsyncEnumerable<T> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                bool shouldInclude;
                try
                {
                    shouldInclude = await _predicate(input, cancellationToken);
                }
                catch (Exception)
                {
                    if (!ContinueOnError)
                        throw;
                    continue;
                }

                if (shouldInclude)
                {
                    yield return input;
                }
            }
        }
    }

    /// <summary>
    /// A streaming operation that batches items together.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    public class BatchStreamingOperation<T> : StreamingOperationBase<T, IReadOnlyList<T>>
    {
        private readonly int _batchSize;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Creates a new batch streaming operation.
        /// </summary>
        /// <param name="batchSize">The maximum batch size.</param>
        /// <param name="timeout">Optional timeout after which a partial batch is yielded.</param>
        public BatchStreamingOperation(int batchSize, TimeSpan? timeout = null)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

            _batchSize = batchSize;
            _timeout = timeout ?? TimeSpan.MaxValue;
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<IReadOnlyList<T>> ProcessStreamAsync(
            IAsyncEnumerable<T> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(_batchSize);
            var lastYieldTime = DateTime.UtcNow;

            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                batch.Add(input);

                var elapsed = DateTime.UtcNow - lastYieldTime;
                var shouldYield = batch.Count >= _batchSize || elapsed >= _timeout;

                if (shouldYield && batch.Count > 0)
                {
                    yield return batch.ToArray();
                    batch.Clear();
                    lastYieldTime = DateTime.UtcNow;
                }
            }

            // Yield remaining items
            if (batch.Count > 0)
            {
                yield return batch.ToArray();
            }
        }
    }

    /// <summary>
    /// A streaming operation that flattens nested async enumerables.
    /// </summary>
    /// <typeparam name="TInput">The type of input containing the nested enumerable.</typeparam>
    /// <typeparam name="TOutput">The type of items in the nested enumerable.</typeparam>
    public class FlatMapStreamingOperation<TInput, TOutput> : StreamingOperationBase<TInput, TOutput>
    {
        private readonly Func<TInput, CancellationToken, IAsyncEnumerable<TOutput>> _flatMap;

        /// <summary>
        /// Creates a new flat map streaming operation.
        /// </summary>
        /// <param name="flatMap">The function that produces a stream of outputs for each input.</param>
        public FlatMapStreamingOperation(Func<TInput, CancellationToken, IAsyncEnumerable<TOutput>> flatMap)
        {
            _flatMap = flatMap ?? throw new ArgumentNullException(nameof(flatMap));
        }

        /// <inheritdoc/>
        public override async IAsyncEnumerable<TOutput> ProcessStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                IAsyncEnumerable<TOutput> innerStream;
                try
                {
                    innerStream = _flatMap(input, cancellationToken);
                }
                catch (Exception)
                {
                    if (!ContinueOnError)
                        throw;
                    continue;
                }

                await foreach (var output in innerStream.WithCancellation(cancellationToken))
                {
                    yield return output;
                }
            }
        }
    }
}

