//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents a strongly-typed operation with explicit input and output types.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the operation produces.</typeparam>
    public interface ITypedOperation<in TInput, TOutput>
    {
        /// <summary>
        /// Gets whether this operation is required even if previous operations failed.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Executes the operation with the given input.
        /// </summary>
        /// <param name="input">The input data for the operation.</param>
        /// <returns>The operation result containing the output or error information.</returns>
        IOperationResult<TOutput> Execute(TInput input);

        /// <summary>
        /// Performs rollback when an error occurs.
        /// </summary>
        /// <param name="input">The original input data.</param>
        void Rollback(TInput input);
    }

    /// <summary>
    /// Represents a strongly-typed operation that only receives input without producing output.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    public interface ITypedOperation<in TInput> : ITypedOperation<TInput, object>
    {
    }
}

