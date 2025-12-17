//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Composition
{
    /// <summary>
    /// Represents a sub-pipeline scope that groups operations together.
    /// </summary>
    public class SubPipelineOperation : IOperation
    {
        private readonly List<IOperation> _operations = new();
        private readonly List<IOperation> _executedOperations = new();

        /// <summary>
        /// Creates a new sub-pipeline operation.
        /// </summary>
        /// <param name="name">Optional name for this scope.</param>
        public SubPipelineOperation(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of this sub-pipeline scope.
        /// </summary>
        public string? Name { get; }

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <summary>
        /// Gets the operations in this sub-pipeline.
        /// </summary>
        public IReadOnlyList<IOperation> Operations => _operations.AsReadOnly();

        /// <summary>
        /// Adds an operation to this sub-pipeline.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>This sub-pipeline for chaining.</returns>
        public SubPipelineOperation Add(IOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            
            _operations.Add(operation);
            return this;
        }

        /// <inheritdoc />
        public void Execute(IPipelineMessage input)
        {
            var scopeName = Name ?? "anonymous";
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-execute-start", $"scope:{scopeName},operations:{_operations.Count}");
            _executedOperations.Clear();

            try
            {
                foreach (var operation in _operations)
                {
                    if (input.IsLocked && !operation.IsRequired)
                        continue;

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-operation-start", $"scope:{scopeName},operation:{operation.GetType().Name}");
                    try
                    {
                        operation.Execute(input);
                        _executedOperations.Add(operation);
                    }
                    finally
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-operation-end", $"scope:{scopeName},operation:{operation.GetType().Name}");
                    }

                    if (input.IsFaulty)
                        break;
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-execute-end", $"scope:{scopeName}");
            }
        }

        /// <inheritdoc />
        public void Rollback(IPipelineMessage input)
        {
            foreach (var operation in _executedOperations.Reverse<IOperation>())
            {
                try
                {
                    operation.Rollback(input);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-subpipeline-rollback-failure", ex);
                }
            }
        }
    }

    /// <summary>
    /// Async version of sub-pipeline operation.
    /// </summary>
    public class SubPipelineOperationAsync : IOperationAsync
    {
        private readonly List<IOperationAsync> _operations = new();
        private readonly List<IOperationAsync> _executedOperations = new();

        /// <summary>
        /// Creates a new async sub-pipeline operation.
        /// </summary>
        /// <param name="name">Optional name for this scope.</param>
        public SubPipelineOperationAsync(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of this sub-pipeline scope.
        /// </summary>
        public string? Name { get; }

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <summary>
        /// Gets the operations in this sub-pipeline.
        /// </summary>
        public IReadOnlyList<IOperationAsync> Operations => _operations.AsReadOnly();

        /// <summary>
        /// Adds an operation to this sub-pipeline.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>This sub-pipeline for chaining.</returns>
        public SubPipelineOperationAsync Add(IOperationAsync operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            
            _operations.Add(operation);
            return this;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage input)
        {
            await ExecuteAsync(input, CancellationToken.None);
        }

        /// <summary>
        /// Executes the sub-pipeline with cancellation support.
        /// </summary>
        public async Task ExecuteAsync(IPipelineMessage input, CancellationToken cancellationToken)
        {
            var scopeName = Name ?? "anonymous";
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-async-execute-start", $"scope:{scopeName},operations:{_operations.Count}");
            _executedOperations.Clear();

            try
            {
                foreach (var operation in _operations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (input.IsLocked && !operation.IsRequired)
                        continue;

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-async-operation-start", $"scope:{scopeName},operation:{operation.GetType().Name}");
                    try
                    {
                        if (operation is IOperationAsyncWithCancellation operationWithCancellation)
                        {
                            await operationWithCancellation.ExecuteAsync(input, cancellationToken);
                        }
                        else
                        {
                            await operation.ExecuteAsync(input);
                        }
                        _executedOperations.Add(operation);
                    }
                    finally
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-async-operation-end", $"scope:{scopeName},operation:{operation.GetType().Name}");
                    }

                    if (input.IsFaulty)
                        break;
                }
            }
            finally
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-subpipeline-async-execute-end", $"scope:{scopeName}");
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(IPipelineMessage input)
        {
            foreach (var operation in _executedOperations.Reverse<IOperationAsync>())
            {
                try
                {
                    await operation.RollbackAsync(input);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-subpipeline-async-rollback-failure", ex);
                }
            }
        }
    }
}

