//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// Base class for strongly-typed synchronous operations.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the operation produces.</typeparam>
    public abstract class TypedOperationBase<TInput, TOutput> : ITypedOperation<TInput, TOutput>
    {
        /// <inheritdoc/>
        public virtual bool IsRequired => false;

        /// <inheritdoc/>
        public abstract IOperationResult<TOutput> Execute(TInput input);

        /// <inheritdoc/>
        public virtual void Rollback(TInput input)
        {
            // Default implementation does nothing
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
    }

    /// <summary>
    /// Base class for strongly-typed synchronous operations without output.
    /// </summary>
    /// <typeparam name="TInput">The type of input the operation receives.</typeparam>
    public abstract class TypedOperationBase<TInput> : TypedOperationBase<TInput, object>
    {
        /// <summary>
        /// Executes the operation logic.
        /// </summary>
        /// <param name="input">The input data.</param>
        protected abstract void ExecuteCore(TInput input);

        /// <inheritdoc/>
        public override IOperationResult<object> Execute(TInput input)
        {
            try
            {
                ExecuteCore(input);
                return OperationResult<object>.Success(new object());
            }
            catch (Exception ex)
            {
                return OperationResult<object>.Failure(ex);
            }
        }
    }
}

