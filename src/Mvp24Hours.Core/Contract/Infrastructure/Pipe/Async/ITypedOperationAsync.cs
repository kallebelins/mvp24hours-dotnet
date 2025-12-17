//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents an asynchronous strongly-typed operation with explicit input and output types.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the operation produces.</typeparam>
    public interface ITypedOperationAsync<in TInput, TOutput>
    {
        /// <summary>
        /// Gets whether this operation is required even if previous operations failed.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Executes the operation asynchronously with the given input.
        /// </summary>
        /// <param name="input">The input data for the operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The operation result containing the output or error information.</returns>
        Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs rollback asynchronously when an error occurs.
        /// </summary>
        /// <param name="input">The original input data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RollbackAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an asynchronous strongly-typed operation that only receives input without producing output.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    public interface ITypedOperationAsync<in TInput> : ITypedOperationAsync<TInput, object>
    {
    }
}

