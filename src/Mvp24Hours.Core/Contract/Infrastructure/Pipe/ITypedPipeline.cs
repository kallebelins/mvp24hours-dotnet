//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents a strongly-typed pipeline with explicit input and output types.
    /// </summary>
    /// <typeparam name="TInput">The type of input the pipeline receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the pipeline produces.</typeparam>
    public interface ITypedPipeline<TInput, TOutput>
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
        /// Executes the pipeline with the given input.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <returns>The result of the pipeline execution.</returns>
        IOperationResult<TOutput> Execute(TInput input);

        /// <summary>
        /// Adds a typed operation to the pipeline.
        /// </summary>
        /// <typeparam name="TOpInput">The operation input type.</typeparam>
        /// <typeparam name="TOpOutput">The operation output type.</typeparam>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The pipeline for chaining.</returns>
        ITypedPipeline<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperation<TOpInput, TOpOutput> operation)
            where TOpInput : TInput
            where TOpOutput : TOutput;

        /// <summary>
        /// Adds a transformation function to the pipeline.
        /// </summary>
        /// <param name="transform">The transformation function.</param>
        /// <returns>The pipeline for chaining.</returns>
        ITypedPipeline<TInput, TOutput> Add(Func<TInput, IOperationResult<TOutput>> transform);
    }
}

