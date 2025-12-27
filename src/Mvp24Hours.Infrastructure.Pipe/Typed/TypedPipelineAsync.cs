//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// An asynchronous strongly-typed pipeline implementation that provides type-safe operation chaining.
    /// </summary>
    /// <typeparam name="TInput">The type of input the pipeline receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the pipeline produces.</typeparam>
    public class TypedPipelineAsync<TInput, TOutput> : ITypedPipelineAsync<TInput, TOutput>
    {
        private readonly List<Func<object?, CancellationToken, Task<IOperationResult<object>>>> _operations = [];
        private readonly List<Func<object?, CancellationToken, Task>> _rollbacks = [];
        private readonly List<object?> _executedInputs = [];
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new instance of TypedPipelineAsync.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public TypedPipelineAsync(ILogger<TypedPipelineAsync<TInput, TOutput>>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool IsBreakOnFail { get; set; }

        /// <inheritdoc/>
        public bool ForceRollbackOnFailure { get; set; }

        /// <inheritdoc/>
        public bool AllowPropagateException { get; set; }

        /// <summary>
        /// Gets the number of operations in the pipeline.
        /// </summary>
        public int OperationCount => _operations.Count;

        /// <inheritdoc/>
        public ITypedPipelineAsync<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperationAsync<TOpInput, TOpOutput> operation)
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

            _rollbacks.Add(async (input, ct) =>
            {
                if (input is TOpInput typedInput)
                {
                    await operation.RollbackAsync(typedInput, ct);
                }
            });

            return this;
        }

        /// <inheritdoc/>
        public ITypedPipelineAsync<TInput, TOutput> Add(Func<TInput, CancellationToken, Task<IOperationResult<TOutput>>> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            _operations.Add(async (input, ct) =>
            {
                var typedInput = (TInput?)input;
                var result = await transform(typedInput!, ct);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            _rollbacks.Add((_, _) => Task.CompletedTask); // No rollback for lambda operations

            return this;
        }

        /// <summary>
        /// Adds an async operation using a function that takes input and cancellation token.
        /// </summary>
        /// <param name="action">The async action to execute.</param>
        /// <param name="isRequired">Whether this operation is required.</param>
        /// <returns>The pipeline for chaining.</returns>
        public TypedPipelineAsync<TInput, TOutput> Add(Func<TInput, CancellationToken, Task> action, bool isRequired = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _operations.Add(async (input, ct) =>
            {
                try
                {
                    var typedInput = (TInput?)input;
                    await action(typedInput!, ct);
                    return OperationResult<object>.Success(input!);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            _rollbacks.Add((_, _) => Task.CompletedTask);

            return this;
        }

        /// <inheritdoc/>
        public async Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("TypedPipelineAsync: ExecuteAsync started");
            _executedInputs.Clear();

            try
            {
                object? currentValue = input;
                var allMessages = new List<IMessageResult>();

                foreach (var operation in _operations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _executedInputs.Add(currentValue);

                    _logger?.LogDebug("TypedPipelineAsync: Operation started");
                    IOperationResult<object> result;

                    try
                    {
                        result = await operation(currentValue, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "TypedPipelineAsync: Operation error");
                        result = OperationResult<object>.Failure(ex);
                    }

                    _logger?.LogDebug("TypedPipelineAsync: Operation completed");

                    allMessages.AddRange(result.Messages);

                    if (result.IsFailure)
                    {
                        if (ForceRollbackOnFailure)
                        {
                            await ExecuteRollbackAsync(cancellationToken);
                        }

                        var failureResult = OperationResult<TOutput>.Failure(allMessages);

                        if (AllowPropagateException && allMessages.Any(m => m.Type == MessageType.Error))
                        {
                            throw new InvalidOperationException(failureResult.ErrorMessage);
                        }

                        if (IsBreakOnFail)
                        {
                            return failureResult;
                        }
                    }
                    else
                    {
                        currentValue = result.Value;
                    }
                }

                // Try to cast final value to TOutput
                if (currentValue is TOutput typedOutput)
                {
                    return OperationResult<TOutput>.Success(typedOutput, allMessages);
                }

                // If no operations produced output, try casting input
                if (input is TOutput inputAsOutput)
                {
                    return OperationResult<TOutput>.Success(inputAsOutput, allMessages);
                }

                return OperationResult<TOutput>.Create(default, true, allMessages);
            }
            finally
            {
                _logger?.LogDebug("TypedPipelineAsync: ExecuteAsync completed");
            }
        }

        private async Task ExecuteRollbackAsync(CancellationToken cancellationToken)
        {
            _logger?.LogDebug("TypedPipelineAsync: Rollback started");

            for (int i = _executedInputs.Count - 1; i >= 0; i--)
            {
                try
                {
                    await _rollbacks[i](_executedInputs[i], cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "TypedPipelineAsync: Rollback error");
                }
            }

            _logger?.LogDebug("TypedPipelineAsync: Rollback completed");
        }
    }
}

