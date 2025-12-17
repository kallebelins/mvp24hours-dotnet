//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// A strongly-typed pipeline implementation that provides type-safe operation chaining.
    /// </summary>
    /// <typeparam name="TInput">The type of input the pipeline receives.</typeparam>
    /// <typeparam name="TOutput">The type of output the pipeline produces.</typeparam>
    public class TypedPipeline<TInput, TOutput> : ITypedPipeline<TInput, TOutput>
    {
        private readonly List<Func<object?, IOperationResult<object>>> _operations = [];
        private readonly List<Action<object?>> _rollbacks = [];
        private readonly List<object?> _executedInputs = [];

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
        public ITypedPipeline<TInput, TOutput> Add<TOpInput, TOpOutput>(ITypedOperation<TOpInput, TOpOutput> operation)
            where TOpInput : TInput
            where TOpOutput : TOutput
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operations.Add(input =>
            {
                var typedInput = (TOpInput?)input;
                var result = operation.Execute(typedInput!);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            _rollbacks.Add(input =>
            {
                if (input is TOpInput typedInput)
                {
                    operation.Rollback(typedInput);
                }
            });

            return this;
        }

        /// <inheritdoc/>
        public ITypedPipeline<TInput, TOutput> Add(Func<TInput, IOperationResult<TOutput>> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            _operations.Add(input =>
            {
                var typedInput = (TInput?)input;
                var result = transform(typedInput!);
                return OperationResult<object>.Create(result.Value, result.IsSuccess, result.Messages);
            });

            _rollbacks.Add(_ => { }); // No rollback for lambda operations

            return this;
        }

        /// <summary>
        /// Adds an operation using an action that modifies the input in place.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="isRequired">Whether this operation is required.</param>
        /// <returns>The pipeline for chaining.</returns>
        public TypedPipeline<TInput, TOutput> Add(Action<TInput> action, bool isRequired = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _operations.Add(input =>
            {
                try
                {
                    var typedInput = (TInput?)input;
                    action(typedInput!);
                    return OperationResult<object>.Success(input!);
                }
                catch (Exception ex)
                {
                    return OperationResult<object>.Failure(ex);
                }
            });

            _rollbacks.Add(_ => { });

            return this;
        }

        /// <inheritdoc/>
        public IOperationResult<TOutput> Execute(TInput input)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-execute-start");
            _executedInputs.Clear();

            try
            {
                object? currentValue = input;
                var allMessages = new List<IMessageResult>();

                foreach (var operation in _operations)
                {
                    _executedInputs.Add(currentValue);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-operation-start");
                    IOperationResult<object> result;

                    try
                    {
                        result = operation(currentValue);
                    }
                    catch (Exception ex)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Error, "typed-pipeline-operation-error", ex);
                        result = OperationResult<object>.Failure(ex);
                    }

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-operation-end");

                    allMessages.AddRange(result.Messages);

                    if (result.IsFailure)
                    {
                        if (ForceRollbackOnFailure)
                        {
                            ExecuteRollback();
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
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-execute-end");
            }
        }

        private void ExecuteRollback()
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-rollback-start");

            for (int i = _executedInputs.Count - 1; i >= 0; i--)
            {
                try
                {
                    _rollbacks[i](_executedInputs[i]);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "typed-pipeline-rollback-error", ex);
                }
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "typed-pipeline-rollback-end");
        }
    }
}

