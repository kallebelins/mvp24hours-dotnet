//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Streaming
{
    /// <summary>
    /// Represents a pipeline that processes items as an async stream.
    /// </summary>
    /// <typeparam name="TInput">The type of input items.</typeparam>
    /// <typeparam name="TOutput">The type of output items.</typeparam>
    public interface IStreamingPipeline<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets whether to continue processing remaining items when one fails.
        /// </summary>
        bool ContinueOnError { get; set; }

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for processing items.
        /// A value of 1 means sequential processing.
        /// </summary>
        int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Executes the pipeline on a stream of inputs and yields results as they complete.
        /// </summary>
        /// <param name="inputs">The async enumerable of inputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of operation results.</returns>
        IAsyncEnumerable<IOperationResult<TOutput>> ExecuteStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the pipeline on a synchronous collection and yields results as they complete.
        /// </summary>
        /// <param name="inputs">The collection of inputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of operation results.</returns>
        IAsyncEnumerable<IOperationResult<TOutput>> ExecuteStreamAsync(
            IEnumerable<TInput> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an operation to the streaming pipeline.
        /// </summary>
        /// <typeparam name="TOpInput">The operation input type.</typeparam>
        /// <typeparam name="TOpOutput">The operation output type.</typeparam>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The pipeline for chaining.</returns>
        IStreamingPipeline<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperationAsync<TOpInput, TOpOutput> operation)
            where TOpInput : TInput
            where TOpOutput : TOutput;
    }

    /// <summary>
    /// Represents a streaming operation that processes items one at a time.
    /// </summary>
    /// <typeparam name="TInput">The type of input.</typeparam>
    /// <typeparam name="TOutput">The type of output.</typeparam>
    public interface IStreamingOperation<TInput, TOutput>
    {
        /// <summary>
        /// Processes a stream of inputs and yields outputs.
        /// </summary>
        /// <param name="inputs">The async enumerable of inputs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of outputs.</returns>
        IAsyncEnumerable<TOutput> ProcessStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            CancellationToken cancellationToken = default);
    }
}

