//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Priority
{
    /// <summary>
    /// A pipeline that automatically sorts and executes operations by priority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Operations with higher priority values execute first.
    /// Operations can specify priority via:
    /// <list type="bullet">
    /// <item>Implementing <see cref="IPrioritizedOperation"/></item>
    /// <item>Using <see cref="OperationPriorityAttribute"/></item>
    /// <item>Wrapping with <see cref="PrioritizedOperation{T}"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pipeline = new PriorityPipeline();
    /// 
    /// // These will be automatically sorted by priority
    /// pipeline.Add(new LoggingOperation(), PriorityLevel.Lowest);
    /// pipeline.Add(new ValidationOperation(), PriorityLevel.Critical);
    /// pipeline.Add(new ProcessingOperation(), PriorityLevel.Normal);
    /// 
    /// // Execution order: Validation (150) -> Processing (50) -> Logging (0)
    /// pipeline.Execute(message);
    /// </code>
    /// </example>
    public class PriorityPipeline
    {
        private readonly List<PrioritizedOperation<IOperation>> _operations = [];
        private readonly List<PrioritizedOperation<IOperationAsync>> _asyncOperations = [];
        private bool _needsSorting = true;

        /// <summary>
        /// Gets or sets whether to break on fail.
        /// </summary>
        public bool IsBreakOnFail { get; set; }

        /// <summary>
        /// Gets or sets whether to allow exception propagation.
        /// </summary>
        public bool AllowPropagateException { get; set; }

        /// <summary>
        /// Adds a synchronous operation with auto-detected priority.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline Add(IOperation operation)
        {
            var priority = OperationPriorityHelper.GetPriority(operation);
            var group = OperationPriorityHelper.GetGroup(operation);
            _operations.Add(new PrioritizedOperation<IOperation>(operation, priority, group));
            _needsSorting = true;
            return this;
        }

        /// <summary>
        /// Adds a synchronous operation with explicit priority.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <param name="priority">The priority value.</param>
        /// <param name="group">Optional group name.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline Add(IOperation operation, int priority, string? group = null)
        {
            _operations.Add(new PrioritizedOperation<IOperation>(operation, priority, group));
            _needsSorting = true;
            return this;
        }

        /// <summary>
        /// Adds a synchronous operation with a priority level.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <param name="level">The priority level.</param>
        /// <param name="group">Optional group name.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline Add(IOperation operation, PriorityLevel level, string? group = null)
        {
            return Add(operation, (int)level, group);
        }

        /// <summary>
        /// Adds an async operation with auto-detected priority.
        /// </summary>
        /// <param name="operation">The async operation to add.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline AddAsync(IOperationAsync operation)
        {
            var priority = OperationPriorityHelper.GetPriority(operation);
            var group = OperationPriorityHelper.GetGroup(operation);
            _asyncOperations.Add(new PrioritizedOperation<IOperationAsync>(operation, priority, group));
            _needsSorting = true;
            return this;
        }

        /// <summary>
        /// Adds an async operation with explicit priority.
        /// </summary>
        /// <param name="operation">The async operation to add.</param>
        /// <param name="priority">The priority value.</param>
        /// <param name="group">Optional group name.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline AddAsync(IOperationAsync operation, int priority, string? group = null)
        {
            _asyncOperations.Add(new PrioritizedOperation<IOperationAsync>(operation, priority, group));
            _needsSorting = true;
            return this;
        }

        /// <summary>
        /// Adds an async operation with a priority level.
        /// </summary>
        /// <param name="operation">The async operation to add.</param>
        /// <param name="level">The priority level.</param>
        /// <param name="group">Optional group name.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline AddAsync(IOperationAsync operation, PriorityLevel level, string? group = null)
        {
            return AddAsync(operation, (int)level, group);
        }

        /// <summary>
        /// Executes the pipeline with the given input message.
        /// </summary>
        /// <param name="input">The pipeline message to process.</param>
        /// <returns>The processed message.</returns>
        public IPipelineMessage Execute(IPipelineMessage input)
        {
            EnsureSorted();

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-execute-start", $"operations:{_operations.Count}");

            foreach (var prioritized in _operations)
            {
                try
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-operation-start",
                        $"operation:{prioritized.Operation.GetType().Name},priority:{prioritized.Priority}");

                    prioritized.Operation.Execute(input);

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-operation-end",
                        $"operation:{prioritized.Operation.GetType().Name}");

                    if (IsBreakOnFail && input.IsFaulty)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Warning, "pipe-priority-break-on-fail",
                            $"operation:{prioritized.Operation.GetType().Name}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-priority-operation-error", ex);
                    input.SetFailure();

                    if (AllowPropagateException)
                    {
                        throw;
                    }

                    if (IsBreakOnFail)
                    {
                        break;
                    }
                }
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-execute-end");
            return input;
        }

        /// <summary>
        /// Executes the pipeline asynchronously.
        /// </summary>
        /// <param name="input">The pipeline message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed message.</returns>
        public async Task<IPipelineMessage> ExecuteAsync(IPipelineMessage input, CancellationToken cancellationToken = default)
        {
            EnsureSorted();

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-async-execute-start",
                $"syncOps:{_operations.Count},asyncOps:{_asyncOperations.Count}");

            // Combine sync and async operations, sorted by priority
            var allOperations = _operations
                .Select(p => (p.Priority, p.Group, SyncOp: (IOperation?)p.Operation, AsyncOp: (IOperationAsync?)null))
                .Concat(_asyncOperations
                    .Select(p => (p.Priority, p.Group, SyncOp: (IOperation?)null, AsyncOp: (IOperationAsync?)p.Operation)))
                .OrderByDescending(x => x.Priority)
                .ToList();

            foreach (var op in allOperations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var opName = op.SyncOp?.GetType().Name ?? op.AsyncOp?.GetType().Name ?? "unknown";
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-async-operation-start",
                        $"operation:{opName},priority:{op.Priority}");

                    if (op.AsyncOp != null)
                    {
                        await op.AsyncOp.ExecuteAsync(input);
                    }
                    else if (op.SyncOp != null)
                    {
                        op.SyncOp.Execute(input);
                    }

                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-async-operation-end",
                        $"operation:{opName}");

                    if (IsBreakOnFail && input.IsFaulty)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Warning, "pipe-priority-async-break-on-fail",
                            $"operation:{opName}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-priority-async-operation-error", ex);
                    input.SetFailure();

                    if (AllowPropagateException)
                    {
                        throw;
                    }

                    if (IsBreakOnFail)
                    {
                        break;
                    }
                }
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-priority-async-execute-end");
            return input;
        }

        /// <summary>
        /// Gets the operations in priority order.
        /// </summary>
        /// <returns>Operations sorted by priority.</returns>
        public IReadOnlyList<(IOperation Operation, int Priority, string? Group)> GetOperationsInOrder()
        {
            EnsureSorted();
            return _operations
                .Select(p => (p.Operation, p.Priority, p.Group))
                .ToList();
        }

        /// <summary>
        /// Gets the async operations in priority order.
        /// </summary>
        /// <returns>Async operations sorted by priority.</returns>
        public IReadOnlyList<(IOperationAsync Operation, int Priority, string? Group)> GetAsyncOperationsInOrder()
        {
            EnsureSorted();
            return _asyncOperations
                .Select(p => (p.Operation, p.Priority, p.Group))
                .ToList();
        }

        private void EnsureSorted()
        {
            if (_needsSorting)
            {
                _operations.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _asyncOperations.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _needsSorting = false;
            }
        }

        /// <summary>
        /// Adds an interceptor action with the highest priority.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline AddInterceptor(Action<IPipelineMessage> action)
        {
            Add(new InterceptorOperation(action), PriorityLevel.Highest);
            return this;
        }

        /// <summary>
        /// Adds multiple interceptor actions with the highest priority.
        /// </summary>
        /// <param name="actions">The actions to execute.</param>
        /// <returns>This pipeline for chaining.</returns>
        public PriorityPipeline AddInterceptors(params Action<IPipelineMessage>[] actions)
        {
            foreach (var action in actions)
            {
                Add(new InterceptorOperation(action), PriorityLevel.Highest);
            }
            return this;
        }
    }

    /// <summary>
    /// Simple interceptor operation wrapper.
    /// </summary>
    internal sealed class InterceptorOperation : IOperation
    {
        private readonly Action<IPipelineMessage> _action;

        public InterceptorOperation(Action<IPipelineMessage> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public bool IsRequired => false;

        public void Execute(IPipelineMessage input) => _action(input);

        public void Rollback(IPipelineMessage input) { }
    }
}

