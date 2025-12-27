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

namespace Mvp24Hours.Infrastructure.Pipe.Operations.Branch
{
    /// <summary>
    /// Represents a conditional branch case.
    /// </summary>
    public record BranchCase(string Key, Func<IPipelineMessage, bool> Condition);

    /// <summary>
    /// Synchronous conditional branching operation (switch/case pattern).
    /// </summary>
    public class ConditionalBranchOperation : IConditionalBranch, IOperation
    {
        private readonly Dictionary<string, List<IOperation>> _branches = new();
        private readonly List<BranchCase> _cases = new();
        private readonly ILogger<ConditionalBranchOperation>? _logger;
        private List<IOperation>? _defaultBranch;
        private List<IOperation>? _executedBranch;

        /// <summary>
        /// Creates a new conditional branch operation.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public ConditionalBranchOperation(ILogger<ConditionalBranchOperation>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a case branch with a condition.
        /// </summary>
        /// <param name="key">Unique key for this branch.</param>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="operations">Operations to execute if condition is true.</param>
        /// <returns>This operation for chaining.</returns>
        public ConditionalBranchOperation AddCase(string key, Func<IPipelineMessage, bool> condition, params IOperation[] operations)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (operations == null || operations.Length == 0)
                throw new ArgumentException("Operations cannot be null or empty", nameof(operations));

            _cases.Add(new BranchCase(key, condition));
            _branches[key] = operations.ToList();
            return this;
        }

        /// <summary>
        /// Sets the default branch when no conditions match.
        /// </summary>
        /// <param name="operations">Operations for the default branch.</param>
        /// <returns>This operation for chaining.</returns>
        public ConditionalBranchOperation SetDefault(params IOperation[] operations)
        {
            _defaultBranch = operations?.ToList();
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyList<IOperation>> Branches =>
            _branches.ToDictionary(k => k.Key, v => (IReadOnlyList<IOperation>)v.Value.AsReadOnly());

        /// <inheritdoc />
        public IReadOnlyList<IOperation>? DefaultBranch => _defaultBranch?.AsReadOnly();

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <inheritdoc />
        public string EvaluateBranch(IPipelineMessage message)
        {
            foreach (var branchCase in _cases)
            {
                if (branchCase.Condition(message))
                {
                    return branchCase.Key;
                }
            }
            return null!;
        }

        /// <inheritdoc />
        public void Execute(IPipelineMessage input)
        {
            _logger?.LogDebug("ConditionalBranchOperation: Execute started with {BranchCount} branches", _branches.Count);

            var branchKey = EvaluateBranch(input);
            
            List<IOperation>? branchToExecute = null;
            if (branchKey != null && _branches.TryGetValue(branchKey, out var branch))
            {
                branchToExecute = branch;
                _logger?.LogDebug("ConditionalBranchOperation: Branch '{BranchKey}' matched", branchKey);
            }
            else if (_defaultBranch != null)
            {
                branchToExecute = _defaultBranch;
                _logger?.LogDebug("ConditionalBranchOperation: Using default branch");
            }
            else
            {
                _logger?.LogDebug("ConditionalBranchOperation: No branch matched");
                return;
            }

            _executedBranch = branchToExecute;

            foreach (var operation in branchToExecute)
            {
                if (input.IsLocked && !operation.IsRequired)
                    continue;

                _logger?.LogDebug("ConditionalBranchOperation: Operation '{OperationName}' started", operation.GetType().Name);
                try
                {
                    operation.Execute(input);
                }
                finally
                {
                    _logger?.LogDebug("ConditionalBranchOperation: Operation '{OperationName}' finished", operation.GetType().Name);
                }
            }

            _logger?.LogDebug("ConditionalBranchOperation: Execute finished");
        }

        /// <inheritdoc />
        public void Rollback(IPipelineMessage input)
        {
            if (_executedBranch == null) return;

            foreach (var operation in _executedBranch.Reverse<IOperation>())
            {
                try
                {
                    operation.Rollback(input);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ConditionalBranchOperation: Rollback failed");
                }
            }
        }
    }

    /// <summary>
    /// Async version of conditional branching operation.
    /// </summary>
    public class ConditionalBranchOperationAsync : IConditionalBranchAsync, IOperationAsync
    {
        private readonly Dictionary<string, List<IOperationAsync>> _branches = new();
        private readonly List<BranchCase> _cases = new();
        private readonly ILogger<ConditionalBranchOperationAsync>? _logger;
        private List<IOperationAsync>? _defaultBranch;
        private List<IOperationAsync>? _executedBranch;

        /// <summary>
        /// Creates a new async conditional branch operation.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public ConditionalBranchOperationAsync(ILogger<ConditionalBranchOperationAsync>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds a case branch with a condition.
        /// </summary>
        public ConditionalBranchOperationAsync AddCase(string key, Func<IPipelineMessage, bool> condition, params IOperationAsync[] operations)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (operations == null || operations.Length == 0)
                throw new ArgumentException("Operations cannot be null or empty", nameof(operations));

            _cases.Add(new BranchCase(key, condition));
            _branches[key] = operations.ToList();
            return this;
        }

        /// <summary>
        /// Sets the default branch when no conditions match.
        /// </summary>
        public ConditionalBranchOperationAsync SetDefault(params IOperationAsync[] operations)
        {
            _defaultBranch = operations?.ToList();
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyList<IOperationAsync>> Branches =>
            _branches.ToDictionary(k => k.Key, v => (IReadOnlyList<IOperationAsync>)v.Value.AsReadOnly());

        /// <inheritdoc />
        public IReadOnlyList<IOperationAsync>? DefaultBranch => _defaultBranch?.AsReadOnly();

        /// <inheritdoc />
        public bool IsRequired => false;

        /// <inheritdoc />
        public Task<string?> EvaluateBranchAsync(IPipelineMessage message, CancellationToken cancellationToken = default)
        {
            foreach (var branchCase in _cases)
            {
                if (branchCase.Condition(message))
                {
                    return Task.FromResult<string?>(branchCase.Key);
                }
            }
            return Task.FromResult<string?>(null);
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IPipelineMessage input)
        {
            await ExecuteAsync(input, CancellationToken.None);
        }

        /// <summary>
        /// Executes the conditional branch with cancellation support.
        /// </summary>
        public async Task ExecuteAsync(IPipelineMessage input, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("ConditionalBranchOperationAsync: ExecuteAsync started with {BranchCount} branches", _branches.Count);

            var branchKey = await EvaluateBranchAsync(input, cancellationToken);

            List<IOperationAsync>? branchToExecute = null;
            if (branchKey != null && _branches.TryGetValue(branchKey, out var branch))
            {
                branchToExecute = branch;
                _logger?.LogDebug("ConditionalBranchOperationAsync: Branch '{BranchKey}' matched", branchKey);
            }
            else if (_defaultBranch != null)
            {
                branchToExecute = _defaultBranch;
                _logger?.LogDebug("ConditionalBranchOperationAsync: Using default branch");
            }
            else
            {
                _logger?.LogDebug("ConditionalBranchOperationAsync: No branch matched");
                return;
            }

            _executedBranch = branchToExecute;

            foreach (var operation in branchToExecute)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (input.IsLocked && !operation.IsRequired)
                    continue;

                _logger?.LogDebug("ConditionalBranchOperationAsync: Operation '{OperationName}' started", operation.GetType().Name);
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
                }
                finally
                {
                    _logger?.LogDebug("ConditionalBranchOperationAsync: Operation '{OperationName}' finished", operation.GetType().Name);
                }
            }

            _logger?.LogDebug("ConditionalBranchOperationAsync: ExecuteAsync finished");
        }

        /// <inheritdoc />
        public async Task RollbackAsync(IPipelineMessage input)
        {
            if (_executedBranch == null) return;

            foreach (var operation in _executedBranch.Reverse<IOperationAsync>())
            {
                try
                {
                    await operation.RollbackAsync(input);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ConditionalBranchOperationAsync: RollbackAsync failed");
                }
            }
        }
    }
}

