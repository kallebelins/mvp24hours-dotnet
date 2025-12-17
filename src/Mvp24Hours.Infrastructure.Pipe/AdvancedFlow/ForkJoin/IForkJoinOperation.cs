//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.ForkJoin
{
    /// <summary>
    /// Represents a fork operation that splits input into multiple parallel branches.
    /// </summary>
    /// <typeparam name="TInput">The type of input to fork.</typeparam>
    /// <typeparam name="TBranchInput">The type of input for each branch.</typeparam>
    public interface IForkOperation<TInput, TBranchInput>
    {
        /// <summary>
        /// Splits the input into multiple branch inputs.
        /// </summary>
        /// <param name="input">The input to split.</param>
        /// <returns>A collection of branch inputs to process in parallel.</returns>
        IEnumerable<TBranchInput> Fork(TInput input);
    }

    /// <summary>
    /// Represents a join operation that combines results from parallel branches.
    /// </summary>
    /// <typeparam name="TBranchOutput">The type of output from each branch.</typeparam>
    /// <typeparam name="TOutput">The type of combined output.</typeparam>
    public interface IJoinOperation<TBranchOutput, TOutput>
    {
        /// <summary>
        /// Combines the results from all branches into a single output.
        /// </summary>
        /// <param name="branchResults">The results from all branches.</param>
        /// <returns>The combined result.</returns>
        IOperationResult<TOutput> Join(IReadOnlyList<IOperationResult<TBranchOutput>> branchResults);
    }

    /// <summary>
    /// Represents a complete fork-join operation pattern.
    /// </summary>
    /// <typeparam name="TInput">The type of input to fork.</typeparam>
    /// <typeparam name="TBranchInput">The type of input for each branch.</typeparam>
    /// <typeparam name="TBranchOutput">The type of output from each branch.</typeparam>
    /// <typeparam name="TOutput">The type of combined output.</typeparam>
    public interface IForkJoinOperation<TInput, TBranchInput, TBranchOutput, TOutput>
    {
        /// <summary>
        /// Executes the fork-join operation synchronously.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <returns>The combined result.</returns>
        IOperationResult<TOutput> Execute(TInput input);
    }

    /// <summary>
    /// Represents an async fork-join operation pattern.
    /// </summary>
    /// <typeparam name="TInput">The type of input to fork.</typeparam>
    /// <typeparam name="TBranchInput">The type of input for each branch.</typeparam>
    /// <typeparam name="TBranchOutput">The type of output from each branch.</typeparam>
    /// <typeparam name="TOutput">The type of combined output.</typeparam>
    public interface IForkJoinOperationAsync<TInput, TBranchInput, TBranchOutput, TOutput>
    {
        /// <summary>
        /// Executes the fork-join operation asynchronously.
        /// </summary>
        /// <param name="input">The input to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The combined result.</returns>
        Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for configuring fork-join behavior.
    /// </summary>
    public sealed class ForkJoinOptions
    {
        /// <summary>
        /// Maximum degree of parallelism for branch execution. Null means unlimited.
        /// </summary>
        public int? MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Whether all branches must succeed for the operation to succeed.
        /// </summary>
        public bool RequireAllSuccess { get; set; } = true;

        /// <summary>
        /// Whether to cancel remaining branches when one fails (only applies when RequireAllSuccess is true).
        /// </summary>
        public bool CancelOnFirstFailure { get; set; } = false;

        /// <summary>
        /// Timeout for each branch execution. Null means no timeout.
        /// </summary>
        public TimeSpan? BranchTimeout { get; set; }

        /// <summary>
        /// Whether to preserve the order of results matching the order of fork inputs.
        /// </summary>
        public bool PreserveOrder { get; set; } = true;
    }

    /// <summary>
    /// Result of a fork-join operation including individual branch results.
    /// </summary>
    /// <typeparam name="TBranchOutput">The type of output from each branch.</typeparam>
    /// <typeparam name="TOutput">The type of combined output.</typeparam>
    public sealed class ForkJoinResult<TBranchOutput, TOutput>
    {
        /// <summary>
        /// The combined output from the join operation.
        /// </summary>
        public TOutput? CombinedOutput { get; init; }

        /// <summary>
        /// Individual results from each branch.
        /// </summary>
        public IReadOnlyList<IOperationResult<TBranchOutput>> BranchResults { get; init; } = [];

        /// <summary>
        /// Whether the overall operation succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Error messages if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Number of successful branches.
        /// </summary>
        public int SuccessCount => BranchResults?.Count(r => r.IsSuccess) ?? 0;

        /// <summary>
        /// Number of failed branches.
        /// </summary>
        public int FailureCount => BranchResults?.Count(r => r.IsFailure) ?? 0;

        /// <summary>
        /// Total number of branches.
        /// </summary>
        public int TotalBranches => BranchResults?.Count ?? 0;
    }
}

