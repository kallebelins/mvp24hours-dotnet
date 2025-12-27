//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
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
        private readonly ILogger<ParallelOperationGroup>? _logger;

        /// <summary>
        /// Creates a new parallel operation group.
        /// </summary>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public ParallelOperationGroup(
            IEnumerable<IOperation> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true,
            ILogger<ParallelOperationGroup>? logger = null)
        {
            _operations = operations?.ToList() ?? throw new ArgumentNullException(nameof(operations));
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            RequireAllSuccess = requireAllSuccess;
            _logger = logger;
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
            _logger?.LogDebug("ParallelOperationGroup: Execute started with {OperationCount} operations", _operations.Count);

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
                        _logger?.LogDebug("ParallelOperationGroup: Operation '{OperationName}' started", operation.GetType().Name);
                        operation.Execute(input);
                        _logger?.LogDebug("ParallelOperationGroup: Operation '{OperationName}' finished", operation.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                        _logger?.LogError(ex, "ParallelOperationGroup: Operation '{OperationName}' failed", operation.GetType().Name);
                        
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

            _logger?.LogDebug("ParallelOperationGroup: Execute finished with {OperationCount} operations", _operations.Count);
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
                    _logger?.LogError(ex, "ParallelOperationGroup: Rollback failed");
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
        private readonly ILogger<ParallelOperationGroupAsync>? _logger;

        /// <summary>
        /// Creates a new async parallel operation group.
        /// </summary>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public ParallelOperationGroupAsync(
            IEnumerable<IOperationAsync> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true,
            ILogger<ParallelOperationGroupAsync>? logger = null)
        {
            _operations = operations?.ToList() ?? throw new ArgumentNullException(nameof(operations));
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            RequireAllSuccess = requireAllSuccess;
            _logger = logger;
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
            _logger?.LogDebug("ParallelOperationGroupAsync: ExecuteAsync started with {OperationCount} operations", _operations.Count);

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

            _logger?.LogDebug("ParallelOperationGroupAsync: ExecuteAsync finished with {OperationCount} operations", _operations.Count);
        }

        private async Task ExecuteOperationAsync(
            IOperationAsync operation,
            IPipelineMessage message,
            List<Exception> exceptions,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogDebug("ParallelOperationGroupAsync: Operation '{OperationName}' started", operation.GetType().Name);
                
                if (operation is IOperationAsyncWithCancellation operationWithCancellation)
                {
                    await operationWithCancellation.ExecuteAsync(message, cancellationToken);
                }
                else
                {
                    await operation.ExecuteAsync(message);
                }
                
                _logger?.LogDebug("ParallelOperationGroupAsync: Operation '{OperationName}' finished", operation.GetType().Name);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
                _logger?.LogError(ex, "ParallelOperationGroupAsync: Operation '{OperationName}' failed", operation.GetType().Name);
                
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
                    _logger?.LogError(ex, "ParallelOperationGroupAsync: RollbackAsync failed");
                }
            }
        }
    }
}

