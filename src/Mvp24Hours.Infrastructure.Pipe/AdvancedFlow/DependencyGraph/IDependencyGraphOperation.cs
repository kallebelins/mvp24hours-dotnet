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

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.DependencyGraph
{
    /// <summary>
    /// Represents an operation node in a dependency graph.
    /// </summary>
    public interface IDependencyGraphNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Optional name for display purposes.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// IDs of nodes that must complete before this node can execute.
        /// </summary>
        IReadOnlyCollection<string> Dependencies { get; }

        /// <summary>
        /// Priority for execution ordering when multiple nodes are ready.
        /// Higher values execute first.
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// Represents a typed operation node in a dependency graph.
    /// </summary>
    /// <typeparam name="TContext">The shared context type passed between operations.</typeparam>
    public interface IDependencyGraphNode<TContext> : IDependencyGraphNode
    {
        /// <summary>
        /// Executes the operation synchronously.
        /// </summary>
        /// <param name="context">The shared execution context.</param>
        /// <param name="nodeResults">Results from completed dependency nodes.</param>
        /// <returns>The operation result.</returns>
        IOperationResult<object> Execute(TContext context, IReadOnlyDictionary<string, object?> nodeResults);

        /// <summary>
        /// Executes the operation asynchronously.
        /// </summary>
        /// <param name="context">The shared execution context.</param>
        /// <param name="nodeResults">Results from completed dependency nodes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The operation result.</returns>
        Task<IOperationResult<object>> ExecuteAsync(TContext context, IReadOnlyDictionary<string, object?> nodeResults, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for configuring dependency graph execution.
    /// </summary>
    public sealed class DependencyGraphOptions
    {
        /// <summary>
        /// Maximum degree of parallelism for executing independent nodes.
        /// </summary>
        public int? MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Whether to stop execution on first failure.
        /// </summary>
        public bool StopOnFirstFailure { get; set; } = true;

        /// <summary>
        /// Timeout for the entire graph execution.
        /// </summary>
        public TimeSpan? ExecutionTimeout { get; set; }

        /// <summary>
        /// Timeout for individual node execution.
        /// </summary>
        public TimeSpan? NodeTimeout { get; set; }

        /// <summary>
        /// Whether to detect and prevent circular dependencies.
        /// </summary>
        public bool ValidateNoCycles { get; set; } = true;
    }

    /// <summary>
    /// Result of a dependency graph execution.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public sealed class DependencyGraphResult<TContext>
    {
        /// <summary>
        /// Whether the overall execution succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// The context after execution.
        /// </summary>
        public TContext? Context { get; init; }

        /// <summary>
        /// Results from each node, keyed by node ID.
        /// </summary>
        public IReadOnlyDictionary<string, NodeExecutionResult> NodeResults { get; init; } = new Dictionary<string, NodeExecutionResult>();

        /// <summary>
        /// Nodes that completed successfully.
        /// </summary>
        public IReadOnlyCollection<string> CompletedNodes { get; init; } = [];

        /// <summary>
        /// Nodes that failed.
        /// </summary>
        public IReadOnlyCollection<string> FailedNodes { get; init; } = [];

        /// <summary>
        /// Nodes that were skipped due to dependency failures.
        /// </summary>
        public IReadOnlyCollection<string> SkippedNodes { get; init; } = [];

        /// <summary>
        /// Order in which nodes were executed.
        /// </summary>
        public IReadOnlyList<string> ExecutionOrder { get; init; } = [];

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan TotalDuration { get; init; }

        /// <summary>
        /// Overall error message if execution failed.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Result of executing a single node.
    /// </summary>
    public sealed class NodeExecutionResult
    {
        /// <summary>
        /// Whether the node execution succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// The result value from the node.
        /// </summary>
        public object? Value { get; init; }

        /// <summary>
        /// Error message if the node failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Duration of this node's execution.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// When the node started executing.
        /// </summary>
        public DateTime StartedAt { get; init; }

        /// <summary>
        /// When the node finished executing.
        /// </summary>
        public DateTime CompletedAt { get; init; }

        /// <summary>
        /// Whether the node was skipped due to dependency failures.
        /// </summary>
        public bool WasSkipped { get; init; }
    }

    /// <summary>
    /// Interface for executing a dependency graph.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public interface IDependencyGraphExecutor<TContext>
    {
        /// <summary>
        /// Executes the dependency graph synchronously.
        /// </summary>
        /// <param name="context">The initial context.</param>
        /// <returns>The execution result.</returns>
        DependencyGraphResult<TContext> Execute(TContext context);

        /// <summary>
        /// Executes the dependency graph asynchronously.
        /// </summary>
        /// <param name="context">The initial context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The execution result.</returns>
        Task<DependencyGraphResult<TContext>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
    }
}

