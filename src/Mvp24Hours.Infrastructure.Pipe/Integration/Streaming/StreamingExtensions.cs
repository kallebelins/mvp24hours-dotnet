//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Pipe.Integration.Streaming;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for streaming pipeline support.
    /// </summary>
    public static class StreamingExtensions
    {
        /// <summary>
        /// Adds a streaming pipeline to the service collection.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <param name="lifetime">The service lifetime.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStreamingPipeline<TInput, TOutput>(
            this IServiceCollection services,
            Action<StreamingPipeline<TInput, TOutput>>? configure = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.Add(new ServiceDescriptor(
                typeof(IStreamingPipeline<TInput, TOutput>),
                sp =>
                {
                    var logger = sp.GetService<ILogger<StreamingPipeline<TInput, TOutput>>>();
                    var pipeline = new StreamingPipeline<TInput, TOutput>(logger);
                    configure?.Invoke(pipeline);
                    return pipeline;
                },
                lifetime));

            return services;
        }

        /// <summary>
        /// Creates a new streaming pipeline.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>A new streaming pipeline.</returns>
        public static StreamingPipeline<TInput, TOutput> CreateStreamingPipeline<TInput, TOutput>(
            Action<StreamingPipeline<TInput, TOutput>>? configure = null)
        {
            var pipeline = new StreamingPipeline<TInput, TOutput>();
            configure?.Invoke(pipeline);
            return pipeline;
        }

        /// <summary>
        /// Creates a transform streaming operation.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="transform">The transformation function.</param>
        /// <returns>A transform streaming operation.</returns>
        public static TransformStreamingOperation<TInput, TOutput> CreateTransformOperation<TInput, TOutput>(
            Func<TInput, System.Threading.CancellationToken, System.Threading.Tasks.Task<TOutput>> transform)
        {
            return new TransformStreamingOperation<TInput, TOutput>(transform);
        }

        /// <summary>
        /// Creates a filter streaming operation.
        /// </summary>
        /// <typeparam name="T">The type of items.</typeparam>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>A filter streaming operation.</returns>
        public static FilterStreamingOperation<T> CreateFilterOperation<T>(Func<T, bool> predicate)
        {
            return new FilterStreamingOperation<T>(predicate);
        }

        /// <summary>
        /// Creates a batch streaming operation.
        /// </summary>
        /// <typeparam name="T">The type of items.</typeparam>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="timeout">Optional timeout for partial batches.</param>
        /// <returns>A batch streaming operation.</returns>
        public static BatchStreamingOperation<T> CreateBatchOperation<T>(int batchSize, TimeSpan? timeout = null)
        {
            return new BatchStreamingOperation<T>(batchSize, timeout);
        }
    }
}

