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
    /// Marker interface for operations that support timeout configuration.
    /// </summary>
    public interface IOperationWithTimeout
    {
        /// <summary>
        /// Gets the timeout duration for this operation.
        /// Returns null to use the default pipeline timeout.
        /// </summary>
        TimeSpan? Timeout { get; }
    }

    /// <summary>
    /// Extended async operation interface with cancellation token support.
    /// </summary>
    public interface IOperationAsyncWithCancellation : IOperationAsync
    {
        /// <summary>
        /// Perform an operation with cancellation support.
        /// </summary>
        /// <param name="input">The pipeline message.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(IPipelineMessage input, CancellationToken cancellationToken);

        /// <summary>
        /// Perform rollback with cancellation support.
        /// </summary>
        /// <param name="input">The pipeline message.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the rollback.</param>
        /// <returns>A task representing the asynchronous rollback.</returns>
        Task RollbackAsync(IPipelineMessage input, CancellationToken cancellationToken);

        /// <summary>
        /// Default implementation - calls the base ExecuteAsync
        /// </summary>
        Task IOperationAsync.ExecuteAsync(IPipelineMessage input) => ExecuteAsync(input, CancellationToken.None);

        /// <summary>
        /// Default implementation - calls the base RollbackAsync
        /// </summary>
        Task IOperationAsync.RollbackAsync(IPipelineMessage input) => RollbackAsync(input, CancellationToken.None);
    }
}

