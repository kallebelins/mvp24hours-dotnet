//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents an asynchronous strongly-typed pipeline with explicit input and output types.
    /// </summary>
    /// <typeparam name="TInput">The type of input the pipeline receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the pipeline produces.</typeparam>
    public interface ITypedPipelineAsync<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets whether the pipeline should break execution when an operation fails.
        /// </summary>
        bool IsBreakOnFail { get; set; }

        /// <summary>
        /// Gets or sets whether the pipeline should force rollback on failure.
        /// </summary>
        bool ForceRollbackOnFailure { get; set; }

        /// <summary>
        /// Gets or sets whether exceptions should propagate after handling.
        /// </summary>
        bool AllowPropagateException { get; set; }

        /// <summary>
        /// Executes the pipeline asynchronously with the given input.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the pipeline execution.</returns>
        Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an async typed operation to the pipeline.
        /// </summary>
        /// <typeparam name="TOpInput">The operation input type.</typeparam>
        /// <typeparam name="TOpOutput">The operation output type.</typeparam>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The pipeline for chaining.</returns>
        ITypedPipelineAsync<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperationAsync<TOpInput, TOpOutput> operation)
            where TOpInput : TInput
            where TOpOutput : TOutput;

        /// <summary>
        /// Adds an async transformation function to the pipeline.
        /// </summary>
        /// <param name="transform">The async transformation function.</param>
        /// <returns>The pipeline for chaining.</returns>
        ITypedPipelineAsync<TInput, TOutput> Add(Func<TInput, CancellationToken, Task<IOperationResult<TOutput>>> transform);
    }
}

