//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.ForkJoin
{
    /// <summary>
    /// Implements the Fork/Join pattern for parallel processing with result aggregation.
    /// </summary>
    /// <typeparam name="TInput">The type of input to fork.</typeparam>
    /// <typeparam name="TBranchInput">The type of input for each branch.</typeparam>
    /// <typeparam name="TBranchOutput">The type of output from each branch.</typeparam>
    /// <typeparam name="TOutput">The type of combined output.</typeparam>
    /// <example>
    /// <code>
    /// // Example: Processing orders in parallel
    /// var forkJoin = new ForkJoinOperation&lt;OrderBatch, Order, ProcessedOrder, OrderBatchResult&gt;(
    ///     fork: batch => batch.Orders,
    ///     branch: order => orderProcessor.Process(order),
    ///     join: results => new OrderBatchResult { Processed = results.Where(r => r.IsSuccess).Select(r => r.Value).ToList() }
    /// );
    /// 
    /// var result = await forkJoin.ExecuteAsync(orderBatch);
    /// </code>
    /// </example>
    public class ForkJoinOperation<TInput, TBranchInput, TBranchOutput, TOutput>
        : IForkJoinOperation<TInput, TBranchInput, TBranchOutput, TOutput>,
          IForkJoinOperationAsync<TInput, TBranchInput, TBranchOutput, TOutput>
    {
        private readonly Func<TInput, IEnumerable<TBranchInput>> _fork;
        private readonly Func<TBranchInput, IOperationResult<TBranchOutput>> _branchSync;
        private readonly Func<TBranchInput, CancellationToken, Task<IOperationResult<TBranchOutput>>>? _branchAsync;
        private readonly Func<IReadOnlyList<IOperationResult<TBranchOutput>>, IOperationResult<TOutput>> _join;
        private readonly ForkJoinOptions _options;
        private readonly ILogger? _logger;

        /// <summary>
        /// Creates a new fork-join operation with synchronous branch processing.
        /// </summary>
        public ForkJoinOperation(
            Func<TInput, IEnumerable<TBranchInput>> fork,
            Func<TBranchInput, IOperationResult<TBranchOutput>> branch,
            Func<IReadOnlyList<IOperationResult<TBranchOutput>>, IOperationResult<TOutput>> join,
            ForkJoinOptions? options = null,
            ILogger? logger = null)
        {
            _fork = fork ?? throw new ArgumentNullException(nameof(fork));
            _branchSync = branch ?? throw new ArgumentNullException(nameof(branch));
            _join = join ?? throw new ArgumentNullException(nameof(join));
            _options = options ?? new ForkJoinOptions();
            _logger = logger;
        }

        /// <summary>
        /// Creates a new fork-join operation with async branch processing.
        /// </summary>
        public ForkJoinOperation(
            Func<TInput, IEnumerable<TBranchInput>> fork,
            Func<TBranchInput, CancellationToken, Task<IOperationResult<TBranchOutput>>> branchAsync,
            Func<IReadOnlyList<IOperationResult<TBranchOutput>>, IOperationResult<TOutput>> join,
            ForkJoinOptions? options = null,
            ILogger? logger = null)
        {
            _fork = fork ?? throw new ArgumentNullException(nameof(fork));
            _branchAsync = branchAsync ?? throw new ArgumentNullException(nameof(branchAsync));
            _branchSync = (input) => branchAsync(input, CancellationToken.None).GetAwaiter().GetResult();
            _join = join ?? throw new ArgumentNullException(nameof(join));
            _options = options ?? new ForkJoinOptions();
            _logger = logger;
        }

        /// <inheritdoc/>
        public IOperationResult<TOutput> Execute(TInput input)
        {
            _logger?.LogDebug("ForkJoinOperation: Execute started");

            try
            {
                // Fork phase
                var branchInputs = _fork(input).ToList();
                _logger?.LogDebug("ForkJoinOperation: Fork phase complete. Branches: {BranchCount}", branchInputs.Count);

                // Branch processing phase (parallel)
                var branchResults = new ConcurrentBag<(int Index, IOperationResult<TBranchOutput> Result)>();
                var exceptions = new ConcurrentBag<Exception>();

                var parallelOptions = _options.MaxDegreeOfParallelism.HasValue
                    ? new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism.Value }
                    : new ParallelOptions();

                try
                {
                    Parallel.ForEach(
                        branchInputs.Select((input, index) => (input, index)),
                        parallelOptions,
                        item =>
                        {
                            try
                            {
                                _logger?.LogDebug("ForkJoinOperation: Branch {BranchIndex} started", item.index);
                                var result = _branchSync(item.input);
                                branchResults.Add((item.index, result));
                                _logger?.LogDebug("ForkJoinOperation: Branch {BranchIndex} finished. Success: {IsSuccess}", item.index, result.IsSuccess);

                                if (result.IsFailure && _options.RequireAllSuccess)
                                {
                                    throw new OperationCanceledException("Branch failed and RequireAllSuccess is true");
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                exceptions.Add(ex);
                                branchResults.Add((item.index, OperationResult<TBranchOutput>.Failure(ex)));
                                _logger?.LogError(ex, "ForkJoinOperation: Branch {BranchIndex} failed", item.index);
                            }
                        });
                }
                catch (AggregateException)
                {
                    // Expected when CancelOnFirstFailure triggers
                }

                // Order results if needed
                var orderedResults = _options.PreserveOrder
                    ? branchResults.OrderBy(r => r.Index).Select(r => r.Result).ToList()
                    : branchResults.Select(r => r.Result).ToList();

                // Join phase
                _logger?.LogDebug("ForkJoinOperation: Join phase started");
                var joinResult = _join(orderedResults);
                _logger?.LogDebug("ForkJoinOperation: Join phase finished. Success: {IsSuccess}", joinResult.IsSuccess);

                return joinResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ForkJoinOperation: Execute failed");
                return OperationResult<TOutput>.Failure(ex);
            }
            finally
            {
                _logger?.LogDebug("ForkJoinOperation: Execute finished");
            }
        }

        /// <inheritdoc/>
        public async Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("ForkJoinOperation: ExecuteAsync started");

            try
            {
                // Fork phase
                var branchInputs = _fork(input).ToList();
                _logger?.LogDebug("ForkJoinOperation: Fork async phase complete. Branches: {BranchCount}", branchInputs.Count);

                // Create linked cancellation token for cancel-on-failure
                using var linkedCts = _options.CancelOnFirstFailure
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                var effectiveToken = linkedCts?.Token ?? cancellationToken;

                // Branch processing phase (parallel)
                var branchResults = new ConcurrentDictionary<int, IOperationResult<TBranchOutput>>();
                var semaphore = _options.MaxDegreeOfParallelism.HasValue
                    ? new SemaphoreSlim(_options.MaxDegreeOfParallelism.Value)
                    : null;

                var tasks = branchInputs.Select(async (branchInput, index) =>
                {
                    if (semaphore != null)
                    {
                        await semaphore.WaitAsync(effectiveToken);
                    }

                    try
                    {
                        effectiveToken.ThrowIfCancellationRequested();

                        _logger?.LogDebug("ForkJoinOperation: Async branch {BranchIndex} started", index);

                        IOperationResult<TBranchOutput> result;

                        if (_options.BranchTimeout.HasValue)
                        {
                            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(effectiveToken);
                            timeoutCts.CancelAfter(_options.BranchTimeout.Value);

                            try
                            {
                                result = _branchAsync != null
                                    ? await _branchAsync(branchInput, timeoutCts.Token)
                                    : _branchSync(branchInput);
                            }
                            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                            {
                                result = OperationResult<TBranchOutput>.Failure($"Branch {index} timed out after {_options.BranchTimeout.Value.TotalSeconds}s");
                            }
                        }
                        else
                        {
                            result = _branchAsync != null
                                ? await _branchAsync(branchInput, effectiveToken)
                                : _branchSync(branchInput);
                        }

                        branchResults[index] = result;
                        _logger?.LogDebug("ForkJoinOperation: Async branch {BranchIndex} finished. Success: {IsSuccess}", index, result.IsSuccess);

                        if (result.IsFailure && _options.RequireAllSuccess && _options.CancelOnFirstFailure)
                        {
                            linkedCts?.Cancel();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        branchResults[index] = OperationResult<TBranchOutput>.Failure("Operation was cancelled");
                    }
                    catch (Exception ex)
                    {
                        branchResults[index] = OperationResult<TBranchOutput>.Failure(ex);
                        _logger?.LogError(ex, "ForkJoinOperation: Async branch {BranchIndex} failed", index);

                        if (_options.RequireAllSuccess && _options.CancelOnFirstFailure)
                        {
                            linkedCts?.Cancel();
                        }
                    }
                    finally
                    {
                        semaphore?.Release();
                    }
                });

                await Task.WhenAll(tasks);

                // Order results
                var orderedResults = _options.PreserveOrder
                    ? Enumerable.Range(0, branchInputs.Count)
                        .Select(i => branchResults.TryGetValue(i, out var r) ? r : OperationResult<TBranchOutput>.Failure("Branch did not complete"))
                        .ToList()
                    : branchResults.Values.ToList();

                // Join phase
                _logger?.LogDebug("ForkJoinOperation: Join async phase started");
                var joinResult = _join(orderedResults);
                _logger?.LogDebug("ForkJoinOperation: Join async phase finished. Success: {IsSuccess}", joinResult.IsSuccess);

                return joinResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ForkJoinOperation: ExecuteAsync failed");
                return OperationResult<TOutput>.Failure(ex);
            }
            finally
            {
                _logger?.LogDebug("ForkJoinOperation: ExecuteAsync finished");
            }
        }
    }

    /// <summary>
    /// Simplified fork-join where branch input and output are the same type as combined.
    /// </summary>
    /// <typeparam name="T">The common type for all operations.</typeparam>
    public class ForkJoinOperation<T> : ForkJoinOperation<IEnumerable<T>, T, T, IReadOnlyList<T>>
    {
        /// <summary>
        /// Creates a fork-join operation for processing a collection in parallel.
        /// </summary>
        public ForkJoinOperation(
            Func<T, IOperationResult<T>> branch,
            ForkJoinOptions? options = null)
            : base(
                fork: inputs => inputs,
                branch: branch,
                join: results => results.All(r => r.IsSuccess)
                    ? OperationResult<IReadOnlyList<T>>.Success(results.Where(r => r.IsSuccess).Select(r => r.Value!).ToList())
                    : OperationResult<IReadOnlyList<T>>.Failure(string.Join("; ", results.Where(r => r.IsFailure).Select(r => r.ErrorMessage))),
                options: options)
        {
        }

        /// <summary>
        /// Creates an async fork-join operation for processing a collection in parallel.
        /// </summary>
        public ForkJoinOperation(
            Func<T, CancellationToken, Task<IOperationResult<T>>> branchAsync,
            ForkJoinOptions? options = null)
            : base(
                fork: inputs => inputs,
                branchAsync: branchAsync,
                join: results => results.All(r => r.IsSuccess)
                    ? OperationResult<IReadOnlyList<T>>.Success(results.Where(r => r.IsSuccess).Select(r => r.Value!).ToList())
                    : OperationResult<IReadOnlyList<T>>.Failure(string.Join("; ", results.Where(r => r.IsFailure).Select(r => r.ErrorMessage))),
                options: options)
        {
        }
    }
}

