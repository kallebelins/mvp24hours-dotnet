//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.DependencyGraph
{
    /// <summary>
    /// Executes a dependency graph respecting node dependencies and priorities.
    /// Uses topological sorting to determine execution order and parallel execution
    /// for independent nodes.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    /// <example>
    /// <code>
    /// var graph = new DependencyGraph&lt;OrderContext&gt;();
    /// 
    /// graph.AddNode(new ValidateOrderNode("validate"));
    /// graph.AddNode(new CheckInventoryNode("inventory").DependsOn("validate"));
    /// graph.AddNode(new CalculateTaxNode("tax").DependsOn("validate"));
    /// graph.AddNode(new ProcessPaymentNode("payment").DependsOn("inventory", "tax"));
    /// graph.AddNode(new FulfillOrderNode("fulfill").DependsOn("payment"));
    /// 
    /// var executor = new DependencyGraphExecutor&lt;OrderContext&gt;(graph);
    /// var result = await executor.ExecuteAsync(new OrderContext { OrderId = "123" });
    /// </code>
    /// </example>
    public class DependencyGraphExecutor<TContext> : IDependencyGraphExecutor<TContext>
    {
        private readonly DependencyGraph<TContext> _graph;
        private readonly DependencyGraphOptions _options;

        /// <summary>
        /// Creates a new dependency graph executor.
        /// </summary>
        /// <param name="graph">The dependency graph to execute.</param>
        /// <param name="options">Execution options.</param>
        public DependencyGraphExecutor(DependencyGraph<TContext> graph, DependencyGraphOptions? options = null)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _options = options ?? new DependencyGraphOptions();

            if (_options.ValidateNoCycles && HasCycles())
            {
                throw new InvalidOperationException("The dependency graph contains circular dependencies.");
            }
        }

        /// <inheritdoc/>
        public DependencyGraphResult<TContext> Execute(TContext context)
        {
            return ExecuteAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<DependencyGraphResult<TContext>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-dependency-graph-execute-start", $"nodes:{_graph.NodeCount}");

            var nodeResults = new ConcurrentDictionary<string, object?>();
            var executionResults = new ConcurrentDictionary<string, NodeExecutionResult>();
            var executionOrder = new ConcurrentQueue<string>();
            var completedNodes = new ConcurrentBag<string>();
            var failedNodes = new ConcurrentBag<string>();
            var skippedNodes = new ConcurrentBag<string>();

            try
            {
                using var timeoutCts = _options.ExecutionTimeout.HasValue
                    ? new CancellationTokenSource(_options.ExecutionTimeout.Value)
                    : new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var semaphore = _options.MaxDegreeOfParallelism.HasValue
                    ? new SemaphoreSlim(_options.MaxDegreeOfParallelism.Value)
                    : null;

                // Track pending and completed nodes
                var pendingNodes = new ConcurrentDictionary<string, IDependencyGraphNode<TContext>>(
                    _graph.Nodes.ToDictionary(n => n.Id, n => n));
                var completedNodeIds = new ConcurrentDictionary<string, bool>();
                var failedNodeIds = new ConcurrentDictionary<string, bool>();

                while (pendingNodes.Count > 0)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    // Find all nodes whose dependencies are satisfied
                    var readyNodes = pendingNodes.Values
                        .Where(node => node.Dependencies.All(d => completedNodeIds.ContainsKey(d)))
                        .Where(node => !node.Dependencies.Any(d => failedNodeIds.ContainsKey(d) && _options.StopOnFirstFailure))
                        .OrderByDescending(node => node.Priority)
                        .ToList();

                    if (!readyNodes.Any())
                    {
                        // Check if we're stuck due to failed dependencies
                        var stuckNodes = pendingNodes.Values
                            .Where(node => node.Dependencies.Any(d => failedNodeIds.ContainsKey(d)))
                            .ToList();

                        if (stuckNodes.Any())
                        {
                            foreach (var stuckNode in stuckNodes)
                            {
                                pendingNodes.TryRemove(stuckNode.Id, out _);
                                skippedNodes.Add(stuckNode.Id);
                                executionResults[stuckNode.Id] = new NodeExecutionResult
                                {
                                    IsSuccess = false,
                                    WasSkipped = true,
                                    ErrorMessage = "Skipped due to failed dependencies"
                                };
                            }
                            continue;
                        }

                        // No ready nodes and no stuck nodes - shouldn't happen if graph is valid
                        break;
                    }

                    // Execute ready nodes in parallel
                    var tasks = readyNodes.Select(async node =>
                    {
                        if (semaphore != null)
                        {
                            await semaphore.WaitAsync(linkedCts.Token);
                        }

                        try
                        {
                            var nodeStopwatch = Stopwatch.StartNew();
                            var startedAt = DateTime.UtcNow;

                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-dependency-graph-node-start", $"node:{node.Id}");

                            try
                            {
                                var dependencyResults = node.Dependencies
                                    .ToDictionary(d => d, d => nodeResults.GetValueOrDefault(d));

                                var result = _options.NodeTimeout.HasValue
                                    ? await ExecuteNodeWithTimeout(node, context, dependencyResults, _options.NodeTimeout.Value, linkedCts.Token)
                                    : await node.ExecuteAsync(context, dependencyResults, linkedCts.Token);

                                nodeStopwatch.Stop();
                                executionOrder.Enqueue(node.Id);

                                if (result.IsSuccess)
                                {
                                    nodeResults[node.Id] = result.Value;
                                    completedNodeIds[node.Id] = true;
                                    completedNodes.Add(node.Id);
                                    executionResults[node.Id] = new NodeExecutionResult
                                    {
                                        IsSuccess = true,
                                        Value = result.Value,
                                        Duration = nodeStopwatch.Elapsed,
                                        StartedAt = startedAt,
                                        CompletedAt = DateTime.UtcNow
                                    };
                                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-dependency-graph-node-success", $"node:{node.Id},duration:{nodeStopwatch.ElapsedMilliseconds}ms");
                                }
                                else
                                {
                                    failedNodeIds[node.Id] = true;
                                    failedNodes.Add(node.Id);
                                    executionResults[node.Id] = new NodeExecutionResult
                                    {
                                        IsSuccess = false,
                                        ErrorMessage = result.ErrorMessage,
                                        Duration = nodeStopwatch.Elapsed,
                                        StartedAt = startedAt,
                                        CompletedAt = DateTime.UtcNow
                                    };
                                    TelemetryHelper.Execute(TelemetryLevels.Warning, "pipe-dependency-graph-node-failed", $"node:{node.Id},error:{result.ErrorMessage}");
                                }
                            }
                            catch (Exception ex)
                            {
                                nodeStopwatch.Stop();
                                failedNodeIds[node.Id] = true;
                                failedNodes.Add(node.Id);
                                executionResults[node.Id] = new NodeExecutionResult
                                {
                                    IsSuccess = false,
                                    ErrorMessage = ex.Message,
                                    Duration = nodeStopwatch.Elapsed,
                                    StartedAt = startedAt,
                                    CompletedAt = DateTime.UtcNow
                                };
                                TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-dependency-graph-node-error", ex);
                            }

                            pendingNodes.TryRemove(node.Id, out _);
                        }
                        finally
                        {
                            semaphore?.Release();
                        }
                    });

                    await Task.WhenAll(tasks);

                    // Check if we should stop due to failures
                    if (_options.StopOnFirstFailure && failedNodeIds.Count > 0)
                    {
                        // Skip remaining pending nodes
                        foreach (var node in pendingNodes.Values)
                        {
                            skippedNodes.Add(node.Id);
                            executionResults[node.Id] = new NodeExecutionResult
                            {
                                IsSuccess = false,
                                WasSkipped = true,
                                ErrorMessage = "Skipped due to StopOnFirstFailure"
                            };
                        }
                        break;
                    }
                }

                stopwatch.Stop();
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "pipe-dependency-graph-execute-end", $"duration:{stopwatch.ElapsedMilliseconds}ms,completed:{completedNodes.Count},failed:{failedNodes.Count},skipped:{skippedNodes.Count}");

                return new DependencyGraphResult<TContext>
                {
                    IsSuccess = failedNodes.Count == 0,
                    Context = context,
                    NodeResults = executionResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CompletedNodes = [.. completedNodes],
                    FailedNodes = [.. failedNodes],
                    SkippedNodes = [.. skippedNodes],
                    ExecutionOrder = [.. executionOrder],
                    TotalDuration = stopwatch.Elapsed,
                    ErrorMessage = failedNodes.Count > 0
                        ? $"Graph execution failed. Failed nodes: {string.Join(", ", failedNodes)}"
                        : null
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new DependencyGraphResult<TContext>
                {
                    IsSuccess = false,
                    Context = context,
                    NodeResults = executionResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CompletedNodes = [.. completedNodes],
                    FailedNodes = [.. failedNodes],
                    SkippedNodes = [.. skippedNodes],
                    ExecutionOrder = [.. executionOrder],
                    TotalDuration = stopwatch.Elapsed,
                    ErrorMessage = "Graph execution was cancelled or timed out"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                TelemetryHelper.Execute(TelemetryLevels.Error, "pipe-dependency-graph-execute-error", ex);
                return new DependencyGraphResult<TContext>
                {
                    IsSuccess = false,
                    Context = context,
                    NodeResults = executionResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    CompletedNodes = [.. completedNodes],
                    FailedNodes = [.. failedNodes],
                    SkippedNodes = [.. skippedNodes],
                    ExecutionOrder = [.. executionOrder],
                    TotalDuration = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<Core.Contract.Infrastructure.Pipe.IOperationResult<object>> ExecuteNodeWithTimeout(
            IDependencyGraphNode<TContext> node,
            TContext context,
            IReadOnlyDictionary<string, object?> dependencyResults,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await node.ExecuteAsync(context, dependencyResults, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return Typed.OperationResult<object>.Failure($"Node '{node.Id}' timed out after {timeout.TotalSeconds}s");
            }
        }

        private bool HasCycles()
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in _graph.Nodes)
            {
                if (HasCyclesDfs(node.Id, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCyclesDfs(string nodeId, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(nodeId))
            {
                return true; // Cycle detected
            }

            if (visited.Contains(nodeId))
            {
                return false; // Already processed
            }

            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var node = _graph.GetNode(nodeId);
            if (node != null)
            {
                foreach (var dependency in node.Dependencies)
                {
                    if (HasCyclesDfs(dependency, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }
    }

    /// <summary>
    /// Represents a collection of dependency graph nodes.
    /// </summary>
    /// <typeparam name="TContext">The shared context type.</typeparam>
    public class DependencyGraph<TContext>
    {
        private readonly Dictionary<string, IDependencyGraphNode<TContext>> _nodes = [];

        /// <summary>
        /// Gets all nodes in the graph.
        /// </summary>
        public IEnumerable<IDependencyGraphNode<TContext>> Nodes => _nodes.Values;

        /// <summary>
        /// Gets the number of nodes in the graph.
        /// </summary>
        public int NodeCount => _nodes.Count;

        /// <summary>
        /// Adds a node to the graph.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <returns>This graph for chaining.</returns>
        public DependencyGraph<TContext> AddNode(IDependencyGraphNode<TContext> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (_nodes.ContainsKey(node.Id))
                throw new ArgumentException($"A node with ID '{node.Id}' already exists in the graph.", nameof(node));

            _nodes[node.Id] = node;
            return this;
        }

        /// <summary>
        /// Gets a node by ID.
        /// </summary>
        /// <param name="nodeId">The node ID.</param>
        /// <returns>The node, or null if not found.</returns>
        public IDependencyGraphNode<TContext>? GetNode(string nodeId)
        {
            return _nodes.GetValueOrDefault(nodeId);
        }

        /// <summary>
        /// Removes a node from the graph.
        /// </summary>
        /// <param name="nodeId">The ID of the node to remove.</param>
        /// <returns>True if the node was removed; otherwise, false.</returns>
        public bool RemoveNode(string nodeId)
        {
            return _nodes.Remove(nodeId);
        }

        /// <summary>
        /// Gets the topological order of nodes (respecting dependencies).
        /// </summary>
        /// <returns>Nodes in topological order.</returns>
        public IEnumerable<IDependencyGraphNode<TContext>> GetTopologicalOrder()
        {
            var result = new List<IDependencyGraphNode<TContext>>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            void Visit(string nodeId)
            {
                if (visited.Contains(nodeId))
                    return;

                if (recursionStack.Contains(nodeId))
                    throw new InvalidOperationException($"Circular dependency detected involving node '{nodeId}'");

                recursionStack.Add(nodeId);

                var node = _nodes.GetValueOrDefault(nodeId);
                if (node != null)
                {
                    foreach (var dep in node.Dependencies)
                    {
                        Visit(dep);
                    }

                    visited.Add(nodeId);
                    result.Add(node);
                }

                recursionStack.Remove(nodeId);
            }

            foreach (var node in _nodes.Values.OrderByDescending(n => n.Priority))
            {
                Visit(node.Id);
            }

            return result;
        }
    }
}

