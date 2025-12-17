//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// Provides a fluent, type-safe chain of operations that can be composed and executed.
    /// </summary>
    /// <typeparam name="TInput">The input type of the chain.</typeparam>
    /// <typeparam name="TOutput">The current output type of the chain.</typeparam>
    public sealed class OperationChain<TInput, TOutput>
    {
        private readonly List<Func<object?, IOperationResult<object>>> _syncOperations = [];
        private readonly List<Func<object?, CancellationToken, Task<IOperationResult<object>>>> _asyncOperations = [];
        private readonly bool _isAsync;

        internal OperationChain(bool isAsync = false)
        {
            _isAsync = isAsync;
        }

        internal OperationChain(
            List<Func<object?, IOperationResult<object>>> syncOperations,
            List<Func<object?, CancellationToken, Task<IOperationResult<object>>>> asyncOperations,
            bool isAsync)
        {
            _syncOperations = new List<Func<object?, IOperationResult<object>>>(syncOperations);
            _asyncOperations = new List<Func<object?, CancellationToken, Task<IOperationResult<object>>>>(asyncOperations);
            _isAsync = isAsync;
        }

        /// <summary>
        /// Chains a transformation that converts the current output to a new type.
        /// </summary>
        /// <typeparam name="TNext">The new output type.</typeparam>
        /// <param name="transform">The transformation function.</param>
        /// <returns>A new chain with the transformed output type.</returns>
        public OperationChain<TInput, TNext> Then<TNext>(Func<TOutput, TNext> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var newChain = new OperationChain<TInput, TNext>(_syncOperations, _asyncOperations, _isAsync);

            newChain._syncOperations.Add(input =>
            {
                try
                {
                    var typedInput = (TOutput?)input;
                    var result = transform(typedInput!);
                    return OperationResult<object>.Success(result!);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            return newChain;
        }

        /// <summary>
        /// Chains a transformation that returns an operation result.
        /// </summary>
        /// <typeparam name="TNext">The new output type.</typeparam>
        /// <param name="transform">The transformation function returning an operation result.</param>
        /// <returns>A new chain with the transformed output type.</returns>
        public OperationChain<TInput, TNext> Then<TNext>(Func<TOutput, IOperationResult<TNext>> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var newChain = new OperationChain<TInput, TNext>(_syncOperations, _asyncOperations, _isAsync);

            newChain._syncOperations.Add(input =>
            {
                try
                {
                    var typedInput = (TOutput?)input;
                    var result = transform(typedInput!);
                    return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            return newChain;
        }

        /// <summary>
        /// Chains a typed operation.
        /// </summary>
        /// <typeparam name="TNext">The output type of the operation.</typeparam>
        /// <param name="operation">The typed operation to chain.</param>
        /// <returns>A new chain with the operation's output type.</returns>
        public OperationChain<TInput, TNext> Then<TNext>(ITypedOperation<TOutput, TNext> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var newChain = new OperationChain<TInput, TNext>(_syncOperations, _asyncOperations, _isAsync);

            newChain._syncOperations.Add(input =>
            {
                var typedInput = (TOutput?)input;
                var result = operation.Execute(typedInput!);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            return newChain;
        }

        /// <summary>
        /// Chains an async transformation.
        /// </summary>
        /// <typeparam name="TNext">The new output type.</typeparam>
        /// <param name="transform">The async transformation function.</param>
        /// <returns>A new async chain with the transformed output type.</returns>
        public OperationChain<TInput, TNext> ThenAsync<TNext>(Func<TOutput, CancellationToken, Task<TNext>> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var newChain = new OperationChain<TInput, TNext>(_syncOperations, _asyncOperations, isAsync: true);

            newChain._asyncOperations.Add(async (input, ct) =>
            {
                try
                {
                    var typedInput = (TOutput?)input;
                    var result = await transform(typedInput!, ct);
                    return OperationResult<object>.Success(result!);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            return newChain;
        }

        /// <summary>
        /// Chains an async typed operation.
        /// </summary>
        /// <typeparam name="TNext">The output type of the operation.</typeparam>
        /// <param name="operation">The async typed operation to chain.</param>
        /// <returns>A new async chain with the operation's output type.</returns>
        public OperationChain<TInput, TNext> ThenAsync<TNext>(ITypedOperationAsync<TOutput, TNext> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var newChain = new OperationChain<TInput, TNext>(_syncOperations, _asyncOperations, isAsync: true);

            newChain._asyncOperations.Add(async (input, ct) =>
            {
                var typedInput = (TOutput?)input;
                var result = await operation.ExecuteAsync(typedInput!, ct);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            return newChain;
        }

        /// <summary>
        /// Adds a side-effect action that doesn't change the type.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The same chain for continued chaining.</returns>
        public OperationChain<TInput, TOutput> Tap(Action<TOutput> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _syncOperations.Add(input =>
            {
                try
                {
                    var typedInput = (TOutput?)input;
                    action(typedInput!);
                    return OperationResult<object>.Success(input!);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            return this;
        }

        /// <summary>
        /// Adds a conditional branch to the chain.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="thenChain">The chain to execute if condition is true.</param>
        /// <returns>The chain for continued chaining.</returns>
        public OperationChain<TInput, TOutput> When(
            Func<TOutput, bool> condition,
            Func<OperationChain<TOutput, TOutput>, OperationChain<TOutput, TOutput>> thenChain)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (thenChain == null)
                throw new ArgumentNullException(nameof(thenChain));

            _syncOperations.Add(input =>
            {
                var typedInput = (TOutput?)input;
                
                if (condition(typedInput!))
                {
                    var branchChain = thenChain(OperationChain.Start<TOutput>());
                    return branchChain.ExecuteInternal(typedInput!);
                }

                return OperationResult<object>.Success(input!);
            });

            return this;
        }

        /// <summary>
        /// Finalizes the chain and executes it with the given input.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <returns>The final result of the chain.</returns>
        public IOperationResult<TOutput> Finally(TInput input)
        {
            if (_isAsync || _asyncOperations.Count > 0)
            {
                throw new InvalidOperationException("This chain contains async operations. Use FinallyAsync instead.");
            }

            var result = ExecuteInternal(input);

            if (result.IsFailure)
            {
                return OperationResult<TOutput>.Failure(result.Messages);
            }

            if (result.Value is TOutput typedOutput)
            {
                return OperationResult<TOutput>.Success(typedOutput, result.Messages);
            }

            return OperationResult<TOutput>.Create(default, result.IsSuccess, result.Messages);
        }

        /// <summary>
        /// Finalizes the chain and executes it asynchronously with the given input.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The final result of the chain.</returns>
        public async Task<IOperationResult<TOutput>> FinallyAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var result = await ExecuteInternalAsync(input, cancellationToken);

            if (result.IsFailure)
            {
                return OperationResult<TOutput>.Failure(result.Messages);
            }

            if (result.Value is TOutput typedOutput)
            {
                return OperationResult<TOutput>.Success(typedOutput, result.Messages);
            }

            return OperationResult<TOutput>.Create(default, result.IsSuccess, result.Messages);
        }

        internal IOperationResult<object> ExecuteInternal(object? input)
        {
            object? currentValue = input;
            var allMessages = new List<Core.Contract.ValueObjects.Logic.IMessageResult>();

            foreach (var operation in _syncOperations)
            {
                var result = operation(currentValue);
                allMessages.AddRange(result.Messages);

                if (result.IsFailure)
                {
                    return OperationResult<object>.Failure(allMessages);
                }

                currentValue = result.Value;
            }

            return OperationResult<object>.Success(currentValue!, allMessages);
        }

        internal async Task<IOperationResult<object>> ExecuteInternalAsync(object? input, CancellationToken cancellationToken)
        {
            object? currentValue = input;
            var allMessages = new List<Core.Contract.ValueObjects.Logic.IMessageResult>();

            // Execute sync operations first
            foreach (var operation in _syncOperations)
            {
                var result = operation(currentValue);
                allMessages.AddRange(result.Messages);

                if (result.IsFailure)
                {
                    return OperationResult<object>.Failure(allMessages);
                }

                currentValue = result.Value;
            }

            // Then execute async operations
            foreach (var operation in _asyncOperations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await operation(currentValue, cancellationToken);
                allMessages.AddRange(result.Messages);

                if (result.IsFailure)
                {
                    return OperationResult<object>.Failure(allMessages);
                }

                currentValue = result.Value;
            }

            return OperationResult<object>.Success(currentValue!, allMessages);
        }
    }

    /// <summary>
    /// Static factory class for creating operation chains.
    /// </summary>
    public static class OperationChain
    {
        /// <summary>
        /// Starts a new operation chain with the specified input type.
        /// </summary>
        /// <typeparam name="T">The input and initial output type.</typeparam>
        /// <returns>A new operation chain.</returns>
        public static OperationChain<T, T> Start<T>()
        {
            return new OperationChain<T, T>();
        }

        /// <summary>
        /// Creates an operation chain starting with a transformation.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="initialTransform">The initial transformation.</param>
        /// <returns>A new operation chain.</returns>
        public static OperationChain<TInput, TOutput> Pipe<TInput, TOutput>(Func<TInput, TOutput> initialTransform)
        {
            if (initialTransform == null)
                throw new ArgumentNullException(nameof(initialTransform));

            return Start<TInput>().Then(initialTransform);
        }
    }
}

