//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.Streaming
{
    /// <summary>
    /// A pipeline implementation that supports streaming via IAsyncEnumerable.
    /// Processes items as they arrive and yields results immediately.
    /// </summary>
    /// <typeparam name="TInput">The type of input items.</typeparam>
    /// <typeparam name="TOutput">The type of output items.</typeparam>
    public class StreamingPipeline<TInput, TOutput> : IStreamingPipeline<TInput, TOutput>
    {
        private readonly List<Func<object?, CancellationToken, Task<IOperationResult<object>>>> _operations = [];
        private readonly ILogger<StreamingPipeline<TInput, TOutput>>? _logger;

        /// <summary>
        /// Creates a new streaming pipeline.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public StreamingPipeline(ILogger<StreamingPipeline<TInput, TOutput>>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool ContinueOnError { get; set; } = true;

        /// <inheritdoc/>
        public int MaxDegreeOfParallelism { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to preserve order of results when using parallel processing.
        /// </summary>
        public bool PreserveOrder { get; set; } = true;

        /// <inheritdoc/>
        public IStreamingPipeline<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperationAsync<TOpInput, TOpOutput> operation)
            where TOpInput : TInput
            where TOpOutput : TOutput
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operations.Add(async (input, ct) =>
            {
                var typedInput = (TOpInput?)input;
                var result = await operation.ExecuteAsync(typedInput!, ct);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            return this;
        }

        /// <summary>
        /// Adds a transformation function to the pipeline.
        /// </summary>
        /// <param name="transform">The transformation function.</param>
        /// <returns>The pipeline for chaining.</returns>
        public StreamingPipeline<TInput, TOutput> Add(Func<TInput, CancellationToken, Task<IOperationResult<TOutput>>> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            _operations.Add(async (input, ct) =>
            {
                var typedInput = (TInput?)input;
                var result = await transform(typedInput!, ct);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            return this;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IOperationResult<TOutput>> ExecuteStreamAsync(
            IAsyncEnumerable<TInput> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "streaming-pipeline-start");

            if (MaxDegreeOfParallelism <= 1)
            {
                // Sequential processing
                await foreach (var input in inputs.WithCancellation(cancellationToken))
                {
                    var result = await ProcessSingleItemAsync(input, cancellationToken);
                    
                    if (result.IsFailure && !ContinueOnError)
                    {
                        yield return result;
                        yield break;
                    }
                    
                    yield return result;
                }
            }
            else
            {
                // Parallel processing with bounded channel
                var channel = Channel.CreateBounded<IOperationResult<TOutput>>(
                    new BoundedChannelOptions(MaxDegreeOfParallelism * 2)
                    {
                        SingleWriter = false,
                        SingleReader = true,
                        FullMode = BoundedChannelFullMode.Wait
                    });

                var processingTask = ProcessInParallelAsync(inputs, channel.Writer, cancellationToken);

                await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return result;
                }

                await processingTask;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "streaming-pipeline-end");
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IOperationResult<TOutput>> ExecuteStreamAsync(
            IEnumerable<TInput> inputs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in ExecuteStreamAsync(ToAsyncEnumerable(inputs), cancellationToken))
            {
                yield return result;
            }
        }

        private async Task ProcessInParallelAsync(
            IAsyncEnumerable<TInput> inputs,
            ChannelWriter<IOperationResult<TOutput>> writer,
            CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);
            var tasks = new List<Task>();
            var shouldStop = false;

            try
            {
                await foreach (var input in inputs.WithCancellation(cancellationToken))
                {
                    if (shouldStop)
                        break;

                    await semaphore.WaitAsync(cancellationToken);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await ProcessSingleItemAsync(input, cancellationToken);
                            await writer.WriteAsync(result, cancellationToken);

                            if (result.IsFailure && !ContinueOnError)
                            {
                                shouldStop = true;
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                writer.Complete();
                semaphore.Dispose();
            }
        }

        private async Task<IOperationResult<TOutput>> ProcessSingleItemAsync(
            TInput input,
            CancellationToken cancellationToken)
        {
            try
            {
                object? currentValue = input;

                foreach (var operation in _operations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await operation(currentValue, cancellationToken);

                    if (result.IsFailure)
                    {
                        return OperationResult<TOutput>.Create(default, false, result.Messages);
                    }

                    currentValue = result.Value;
                }

                if (currentValue is TOutput typedOutput)
                {
                    return OperationResult<TOutput>.Success(typedOutput);
                }

                if (input is TOutput inputAsOutput)
                {
                    return OperationResult<TOutput>.Success(inputAsOutput);
                }

                return OperationResult<TOutput>.Create(default, true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing item in streaming pipeline");
                return OperationResult<TOutput>.Failure(ex);
            }
        }

        private static async IAsyncEnumerable<TInput> ToAsyncEnumerable(IEnumerable<TInput> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.CompletedTask; // Allow cooperative scheduling
            }
        }
    }
}

