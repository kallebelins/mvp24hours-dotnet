//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.DependencyGraph
{
    /// <summary>
    /// Base class for dependency graph nodes with synchronous execution.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public abstract class DependencyGraphNodeBase<TContext> : IDependencyGraphNode<TContext>
    {
        private readonly HashSet<string> _dependencies = [];

        /// <summary>
        /// Creates a new dependency graph node.
        /// </summary>
        /// <param name="id">Unique identifier for this node.</param>
        /// <param name="name">Optional display name.</param>
        /// <param name="priority">Execution priority (higher executes first).</param>
        protected DependencyGraphNodeBase(string id, string? name = null, int priority = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            Priority = priority;
        }

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string? Name { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> Dependencies => _dependencies;

        /// <inheritdoc/>
        public int Priority { get; }

        /// <summary>
        /// Adds a dependency on another node.
        /// </summary>
        /// <param name="nodeId">The ID of the node to depend on.</param>
        /// <returns>This node for chaining.</returns>
        public DependencyGraphNodeBase<TContext> DependsOn(string nodeId)
        {
            _dependencies.Add(nodeId);
            return this;
        }

        /// <summary>
        /// Adds dependencies on multiple nodes.
        /// </summary>
        /// <param name="nodeIds">The IDs of nodes to depend on.</param>
        /// <returns>This node for chaining.</returns>
        public DependencyGraphNodeBase<TContext> DependsOn(params string[] nodeIds)
        {
            foreach (var nodeId in nodeIds)
            {
                _dependencies.Add(nodeId);
            }
            return this;
        }

        /// <inheritdoc/>
        public abstract IOperationResult<object> Execute(TContext context, IReadOnlyDictionary<string, object?> nodeResults);

        /// <inheritdoc/>
        public virtual Task<IOperationResult<object>> ExecuteAsync(TContext context, IReadOnlyDictionary<string, object?> nodeResults, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Execute(context, nodeResults));
        }

        /// <summary>
        /// Creates a successful result with the given value.
        /// </summary>
        protected static OperationResult<object> Success(object? value = null) => OperationResult<object>.Success(value ?? new object());

        /// <summary>
        /// Creates a failed result with the given error message.
        /// </summary>
        protected static OperationResult<object> Failure(string errorMessage) => OperationResult<object>.Failure(errorMessage);

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        protected static OperationResult<object> Failure(Exception exception) => OperationResult<object>.Failure(exception);

        /// <summary>
        /// Gets the result from a specific dependency.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="nodeResults">The dictionary of node results.</param>
        /// <param name="nodeId">The ID of the dependency node.</param>
        /// <returns>The result value, or default if not found.</returns>
        protected static T? GetDependencyResult<T>(IReadOnlyDictionary<string, object?> nodeResults, string nodeId)
        {
            if (nodeResults.TryGetValue(nodeId, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }
    }

    /// <summary>
    /// Async dependency graph node base class.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public abstract class DependencyGraphNodeAsyncBase<TContext> : DependencyGraphNodeBase<TContext>
    {
        /// <summary>
        /// Creates a new async dependency graph node.
        /// </summary>
        protected DependencyGraphNodeAsyncBase(string id, string? name = null, int priority = 0)
            : base(id, name, priority)
        {
        }

        /// <inheritdoc/>
        public override IOperationResult<object> Execute(TContext context, IReadOnlyDictionary<string, object?> nodeResults)
        {
            return ExecuteAsync(context, nodeResults, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public abstract override Task<IOperationResult<object>> ExecuteAsync(TContext context, IReadOnlyDictionary<string, object?> nodeResults, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Lambda-based dependency graph node for quick node creation.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public sealed class LambdaDependencyGraphNode<TContext> : DependencyGraphNodeBase<TContext>
    {
        private readonly Func<TContext, IReadOnlyDictionary<string, object?>, IOperationResult<object>> _execute;
        private readonly Func<TContext, IReadOnlyDictionary<string, object?>, CancellationToken, Task<IOperationResult<object>>>? _executeAsync;

        /// <summary>
        /// Creates a synchronous lambda-based node.
        /// </summary>
        public LambdaDependencyGraphNode(
            string id,
            Func<TContext, IReadOnlyDictionary<string, object?>, IOperationResult<object>> execute,
            string? name = null,
            int priority = 0)
            : base(id, name, priority)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        /// <summary>
        /// Creates an async lambda-based node.
        /// </summary>
        public LambdaDependencyGraphNode(
            string id,
            Func<TContext, IReadOnlyDictionary<string, object?>, CancellationToken, Task<IOperationResult<object>>> executeAsync,
            string? name = null,
            int priority = 0)
            : base(id, name, priority)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _execute = (ctx, results) => executeAsync(ctx, results, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override IOperationResult<object> Execute(TContext context, IReadOnlyDictionary<string, object?> nodeResults)
        {
            return _execute(context, nodeResults);
        }

        /// <inheritdoc/>
        public override Task<IOperationResult<object>> ExecuteAsync(TContext context, IReadOnlyDictionary<string, object?> nodeResults, CancellationToken cancellationToken = default)
        {
            if (_executeAsync != null)
            {
                return _executeAsync(context, nodeResults, cancellationToken);
            }
            return Task.FromResult(_execute(context, nodeResults));
        }
    }
}

