//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Interface for pipelines that can be composed with other pipelines.
    /// Allows building complex pipelines from smaller, reusable pipeline components.
    /// </summary>
    public interface IComposablePipeline : IPipeline
    {
        /// <summary>
        /// Adds another pipeline's operations to this pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to compose.</param>
        /// <returns>This pipeline for chaining.</returns>
        IPipeline AddPipeline(IPipeline pipeline);

        /// <summary>
        /// Creates a sub-pipeline scope that groups operations together.
        /// </summary>
        /// <param name="name">Optional name for the sub-pipeline scope.</param>
        /// <returns>A sub-pipeline builder.</returns>
        ISubPipelineBuilder BeginScope(string? name = null);
    }

    /// <summary>
    /// Async version of composable pipeline.
    /// </summary>
    public interface IComposablePipelineAsync : IPipelineAsync
    {
        /// <summary>
        /// Adds another async pipeline's operations to this pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to compose.</param>
        /// <returns>This pipeline for chaining.</returns>
        IPipelineAsync AddPipeline(IPipelineAsync pipeline);

        /// <summary>
        /// Creates a sub-pipeline scope that groups operations together.
        /// </summary>
        /// <param name="name">Optional name for the sub-pipeline scope.</param>
        /// <returns>A sub-pipeline builder.</returns>
        ISubPipelineBuilderAsync BeginScope(string? name = null);
    }

    /// <summary>
    /// Builder for creating a sub-pipeline scope.
    /// </summary>
    public interface ISubPipelineBuilder
    {
        /// <summary>
        /// Gets the name of this scope.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Adds an operation to this scope.
        /// </summary>
        ISubPipelineBuilder Add<T>() where T : class, IOperation;

        /// <summary>
        /// Adds an operation to this scope.
        /// </summary>
        ISubPipelineBuilder Add(IOperation operation);

        /// <summary>
        /// Ends the scope and returns to the main pipeline.
        /// </summary>
        IPipeline EndScope();
    }

    /// <summary>
    /// Async builder for creating a sub-pipeline scope.
    /// </summary>
    public interface ISubPipelineBuilderAsync
    {
        /// <summary>
        /// Gets the name of this scope.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Adds an operation to this scope.
        /// </summary>
        ISubPipelineBuilderAsync Add<T>() where T : class, IOperationAsync;

        /// <summary>
        /// Adds an operation to this scope.
        /// </summary>
        ISubPipelineBuilderAsync Add(IOperationAsync operation);

        /// <summary>
        /// Ends the scope and returns to the main pipeline.
        /// </summary>
        IPipelineAsync EndScope();
    }
}

