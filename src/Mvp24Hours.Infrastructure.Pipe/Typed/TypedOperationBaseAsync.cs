//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// Base class for strongly-typed asynchronous operations.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the operation produces.</typeparam>
    public abstract class TypedOperationBaseAsync<TInput, TOutput> : ITypedOperationAsync<TInput, TOutput>
    {
        /// <inheritdoc/>
        public virtual bool IsRequired => false;

        /// <inheritdoc/>
        public abstract Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public virtual Task RollbackAsync(TInput input, CancellationToken cancellationToken = default)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a successful result with the given value.
        /// </summary>
        protected static OperationResult<TOutput> Success(TOutput value) => OperationResult<TOutput>.Success(value);

        /// <summary>
        /// Creates a failed result with the given error message.
        /// </summary>
        protected static OperationResult<TOutput> Failure(string errorMessage) => OperationResult<TOutput>.Failure(errorMessage);

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        protected static OperationResult<TOutput> Failure(Exception exception) => OperationResult<TOutput>.Failure(exception);

        /// <summary>
        /// Creates a completed task with a successful result.
        /// </summary>
        protected static Task<IOperationResult<TOutput>> SuccessAsync(TOutput value) 
            => Task.FromResult<IOperationResult<TOutput>>(OperationResult<TOutput>.Success(value));

        /// <summary>
        /// Creates a completed task with a failed result.
        /// </summary>
        protected static Task<IOperationResult<TOutput>> FailureAsync(string errorMessage) 
            => Task.FromResult<IOperationResult<TOutput>>(OperationResult<TOutput>.Failure(errorMessage));
    }

    /// <summary>
    /// Base class for strongly-typed asynchronous operations without output.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    public abstract class TypedOperationBaseAsync<TInput> : TypedOperationBaseAsync<TInput, object>
    {
        /// <summary>
        /// Executes the operation logic asynchronously.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected abstract Task ExecuteCoreAsync(TInput input, CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public override async Task<IOperationResult<object>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            try
            {
                await ExecuteCoreAsync(input, cancellationToken);
                return OperationResult<object>.Success(new object());
            }
            catch (Exception ex)
            {
                return OperationResult<object>.Failure(ex);
            }
        }
    }
}

