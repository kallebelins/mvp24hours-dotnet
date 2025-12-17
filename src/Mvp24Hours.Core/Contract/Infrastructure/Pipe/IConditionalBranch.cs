//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines a conditional branching operation that routes pipeline execution
    /// based on evaluated conditions (switch/case pattern).
    /// </summary>
    public interface IConditionalBranch
    {
        /// <summary>
        /// Evaluates conditions and returns the key of the branch to execute.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <returns>The key identifying which branch to execute, or null for default.</returns>
        string EvaluateBranch(IPipelineMessage message);

        /// <summary>
        /// Gets the available branches mapped by their keys.
        /// </summary>
        IReadOnlyDictionary<string, IReadOnlyList<IOperation>> Branches { get; }

        /// <summary>
        /// Gets the default branch to execute when no condition matches.
        /// </summary>
        IReadOnlyList<IOperation>? DefaultBranch { get; }
    }

    /// <summary>
    /// Async version of conditional branching.
    /// </summary>
    public interface IConditionalBranchAsync
    {
        /// <summary>
        /// Evaluates conditions and returns the key of the branch to execute.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The key identifying which branch to execute, or null for default.</returns>
        Task<string?> EvaluateBranchAsync(IPipelineMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the available branches mapped by their keys.
        /// </summary>
        IReadOnlyDictionary<string, IReadOnlyList<IOperationAsync>> Branches { get; }

        /// <summary>
        /// Gets the default branch to execute when no condition matches.
        /// </summary>
        IReadOnlyList<IOperationAsync>? DefaultBranch { get; }
    }

    /// <summary>
    /// Builder interface for creating conditional branches fluently.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline type for fluent chaining.</typeparam>
    public interface IConditionalBranchBuilder<TPipeline>
    {
        /// <summary>
        /// Adds a case branch with a condition.
        /// </summary>
        /// <param name="key">Unique key for this branch.</param>
        /// <param name="condition">Condition to evaluate.</param>
        /// <param name="configure">Action to configure operations for this branch.</param>
        /// <returns>The builder for chaining.</returns>
        IConditionalBranchBuilder<TPipeline> Case(string key, Func<IPipelineMessage, bool> condition, Action<TPipeline> configure);

        /// <summary>
        /// Sets the default branch when no conditions match.
        /// </summary>
        /// <param name="configure">Action to configure operations for default branch.</param>
        /// <returns>The builder for chaining.</returns>
        IConditionalBranchBuilder<TPipeline> Default(Action<TPipeline> configure);

        /// <summary>
        /// Builds and returns to the main pipeline.
        /// </summary>
        /// <returns>The main pipeline.</returns>
        TPipeline EndSwitch();
    }
}

