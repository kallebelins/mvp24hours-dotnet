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
using ParallelTasks = System.Threading.Tasks.Parallel;

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Parallel
{
    /// <summary>
    /// Represents a group of synchronous operations to be executed in parallel.
    /// </summary>
    public class ParallelOperationGroup : IParallelOperationGroup, IOperation
    {
        private readonly List<IOperation> _operations;

        /// <summary>
        /// Creates a new parallel operation group.
        /// </summary>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        public ParallelOperationGroup(
            IEnumerable<IOperation> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true)
        {
            _operations = operations?.ToList() ?? throw new ArgumentNullException(nameof(operations));
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            RequireAllSuccess = requireAllSuccess;
        }

        /// <inheritdoc />
        public IReadOnlyList<IOperation> Operations => _operations.AsReadOnly();

        /// <inheritdoc />
        public int? MaxDegreeOfParallelism { get; }

        /// <inheritdoc />
        public bool RequireAllSuccess { get; }

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <inheritdoc />
        public void Execute(IPipelineMessage input)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-execute-start", $"operations:{_operations.Count}");

            var exceptions = new List<Exception>();
            var parallelOptions = MaxDegreeOfParallelism.HasValue
                ? new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism.Value }
                : new System.Threading.Tasks.ParallelOptions();

            try
            {
                ParallelTasks.ForEach(_operations, parallelOptions, operation =>
                {
                    try
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-operation-start", $"operation:{operation.GetType().Name}");
                        operation.Execute(input);
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-operation-end", $"operation:{operation.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                        TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-parallel-operation-failure", ex);
                        
                        if (RequireAllSuccess)
                        {
                            throw; // Will be caught by Parallel.ForEach
                        }
                    }
                });
            }
            catch (AggregateException)
            {
                // Expected when RequireAllSuccess is true and an operation fails
            }

            if (exceptions.Count > 0)
            {
                if (RequireAllSuccess)
                {
                    throw new AggregateException("One or more parallel operations failed", exceptions);
                }
                // Store exceptions in message for later inspection
                input.AddContent("ParallelOperationExceptions", exceptions);
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-execute-end", $"operations:{_operations.Count}");
        }

        /// <inheritdoc />
        public void Rollback(IPipelineMessage input)
        {
            // Rollback all operations in reverse order
            foreach (var operation in _operations.Reverse<IOperation>())
            {
                try
                {
                    operation.Rollback(input);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-parallel-rollback-failure", ex);
                }
            }
        }
    }

    /// <summary>
    /// Represents a group of async operations to be executed in parallel.
    /// </summary>
    public class ParallelOperationGroupAsync : IParallelOperationGroupAsync, IOperationAsync
    {
        private readonly List<IOperationAsync> _operations;

        /// <summary>
        /// Creates a new async parallel operation group.
        /// </summary>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        public ParallelOperationGroupAsync(
            IEnumerable<IOperationAsync> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true)
        {
            _operations = operations?.ToList() ?? throw new ArgumentNullException(nameof(operations));
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            RequireAllSuccess = requireAllSuccess;
        }

        /// <inheritdoc />
        public IReadOnlyList<IOperationAsync> Operations => _operations.AsReadOnly();

        /// <inheritdoc />
        public int? MaxDegreeOfParallelism { get; }

        /// <inheritdoc />
        public bool RequireAllSuccess { get; }

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage message, CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-async-execute-start", $"operations:{_operations.Count}");

            var exceptions = new List<Exception>();

            if (MaxDegreeOfParallelism.HasValue)
            {
                using var semaphore = new SemaphoreSlim(MaxDegreeOfParallelism.Value);
                var tasks = _operations.Select(async operation =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ExecuteOperationAsync(operation, message, exceptions, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            else
            {
                var tasks = _operations.Select(operation =>
                    ExecuteOperationAsync(operation, message, exceptions, cancellationToken));

                await Task.WhenAll(tasks);
            }

            if (exceptions.Count > 0)
            {
                if (RequireAllSuccess)
                {
                    throw new AggregateException("One or more parallel operations failed", exceptions);
                }
                message.AddContent("ParallelOperationExceptions", exceptions);
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-async-execute-end", $"operations:{_operations.Count}");
        }

        private async Task ExecuteOperationAsync(
            IOperationAsync operation,
            IPipelineMessage message,
            List<Exception> exceptions,
            CancellationToken cancellationToken)
        {
            try
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-async-operation-start", $"operation:{operation.GetType().Name}");
                
                if (operation is IOperationAsyncWithCancellation operationWithCancellation)
                {
                    await operationWithCancellation.ExecuteAsync(message, cancellationToken);
                }
                else
                {
                    await operation.ExecuteAsync(message);
                }
                
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-parallel-async-operation-end", $"operation:{operation.GetType().Name}");
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
                TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-parallel-async-operation-failure", ex);
                
                if (RequireAllSuccess)
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        Task IOperationAsync.ExecuteAsync(IPipelineMessage input) => ExecuteAsync(input, CancellationToken.None);

        /// <inheritdoc />
        public async Task RollbackAsync(IPipelineMessage input)
        {
            foreach (var operation in _operations.Reverse<IOperationAsync>())
            {
                try
                {
                    await operation.RollbackAsync(input);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-parallel-async-rollback-failure", ex);
                }
            }
        }
    }
}

