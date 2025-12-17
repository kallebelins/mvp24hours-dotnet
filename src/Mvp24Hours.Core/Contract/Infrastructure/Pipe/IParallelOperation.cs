//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Represents a group of operations that can be executed in parallel.
    /// </summary>
    public interface IParallelOperationGroup
    {
        /// <summary>
        /// Gets the operations to execute in parallel.
        /// </summary>
        IReadOnlyList<IOperation> Operations { get; }

        /// <summary>
        /// Gets the maximum degree of parallelism. 
        /// null = unlimited, matching Task.WhenAll behavior.
        /// </summary>
        int? MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Indicates whether all operations must succeed for the group to succeed.
        /// If false, failures are collected but don't stop other parallel operations.
        /// </summary>
        bool RequireAllSuccess { get; }
    }

    /// <summary>
    /// Async version of parallel operation group.
    /// </summary>
    public interface IParallelOperationGroupAsync
    {
        /// <summary>
        /// Gets the operations to execute in parallel.
        /// </summary>
        IReadOnlyList<IOperationAsync> Operations { get; }

        /// <summary>
        /// Gets the maximum degree of parallelism.
        /// </summary>
        int? MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Indicates whether all operations must succeed.
        /// </summary>
        bool RequireAllSuccess { get; }

        /// <summary>
        /// Executes all operations in parallel.
        /// </summary>
        /// <param name="message">The pipeline message.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the parallel execution.</returns>
        Task ExecuteAsync(IPipelineMessage message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Builder interface for creating parallel operation groups fluently.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline type for fluent chaining.</typeparam>
    public interface IParallelOperationBuilder<TPipeline>
    {
        /// <summary>
        /// Adds an operation to the parallel group.
        /// </summary>
        /// <typeparam name="T">Type of operation.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IParallelOperationBuilder<TPipeline> Add<T>() where T : class;

        /// <summary>
        /// Adds an operation instance to the parallel group.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        /// <returns>The builder for chaining.</returns>
        IParallelOperationBuilder<TPipeline> Add(object operation);

        /// <summary>
        /// Sets the maximum degree of parallelism.
        /// </summary>
        /// <param name="maxDegree">Maximum parallel operations.</param>
        /// <returns>The builder for chaining.</returns>
        IParallelOperationBuilder<TPipeline> WithMaxDegreeOfParallelism(int maxDegree);

        /// <summary>
        /// Configures whether all operations must succeed.
        /// </summary>
        /// <param name="require">True to require all success.</param>
        /// <returns>The builder for chaining.</returns>
        IParallelOperationBuilder<TPipeline> RequireAllSuccess(bool require = true);

        /// <summary>
        /// Ends the parallel group and returns to the main pipeline.
        /// </summary>
        /// <returns>The main pipeline.</returns>
        TPipeline EndParallel();
    }
}

