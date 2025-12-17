//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Builders;
using Mvp24Hours.Infrastructure.Pipe.Operations.Parallel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Extensions
{
    /// <summary>
    /// Fluent extensions for Pipeline to support advanced patterns.
    /// </summary>
    public static class PipelineFluentExtensions
    {
        #region [ Parallel Operations - Sync ]

        /// <summary>
        /// Begins a parallel operations group.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>A parallel operation builder.</returns>
        public static IParallelOperationBuilder<IPipeline> BeginParallel(this Pipeline pipeline)
        {
            return new ParallelOperationBuilder(pipeline, null);
        }

        /// <summary>
        /// Adds operations to execute in parallel.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipeline AddParallel(
            this Pipeline pipeline,
            IEnumerable<IOperation> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true)
        {
            var parallelGroup = new ParallelOperationGroup(operations, maxDegreeOfParallelism, requireAllSuccess);
            pipeline.Add(parallelGroup);
            return pipeline;
        }

        /// <summary>
        /// Adds operations to execute in parallel using a configuration action.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configure">Action to configure parallel operations.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipeline AddParallel(this Pipeline pipeline, Action<IParallelOperationBuilder<IPipeline>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new ParallelOperationBuilder(pipeline, null);
            configure(builder);
            return builder.EndParallel();
        }

        #endregion

        #region [ Parallel Operations - Async ]

        /// <summary>
        /// Begins a parallel operations group.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <returns>A parallel operation builder.</returns>
        public static IParallelOperationBuilder<IPipelineAsync> BeginParallel(this PipelineAsync pipeline)
        {
            return new ParallelOperationBuilderAsync(pipeline, null);
        }

        /// <summary>
        /// Adds operations to execute in parallel.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="operations">Operations to execute in parallel.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel operations.</param>
        /// <param name="requireAllSuccess">Whether all operations must succeed.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipelineAsync AddParallel(
            this PipelineAsync pipeline,
            IEnumerable<IOperationAsync> operations,
            int? maxDegreeOfParallelism = null,
            bool requireAllSuccess = true)
        {
            var parallelGroup = new ParallelOperationGroupAsync(operations, maxDegreeOfParallelism, requireAllSuccess);
            pipeline.Add(parallelGroup);
            return pipeline;
        }

        /// <summary>
        /// Adds operations to execute in parallel using a configuration action.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="configure">Action to configure parallel operations.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipelineAsync AddParallel(this PipelineAsync pipeline, Action<IParallelOperationBuilder<IPipelineAsync>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new ParallelOperationBuilderAsync(pipeline, null);
            configure(builder);
            return builder.EndParallel();
        }

        #endregion

        #region [ Conditional Branch - Sync ]

        /// <summary>
        /// Begins a conditional branch (switch/case pattern).
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <returns>A conditional branch builder.</returns>
        public static IConditionalBranchBuilder<IPipeline> BeginSwitch(this Pipeline pipeline)
        {
            return new ConditionalBranchBuilder(pipeline, null);
        }

        /// <summary>
        /// Adds a conditional branch using a configuration action.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configure">Action to configure branches.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipeline AddSwitch(this Pipeline pipeline, Action<IConditionalBranchBuilder<IPipeline>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new ConditionalBranchBuilder(pipeline, null);
            configure(builder);
            return builder.EndSwitch();
        }

        #endregion

        #region [ Conditional Branch - Async ]

        /// <summary>
        /// Begins a conditional branch (switch/case pattern).
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <returns>A conditional branch builder.</returns>
        public static IConditionalBranchBuilder<IPipelineAsync> BeginSwitch(this PipelineAsync pipeline)
        {
            return new ConditionalBranchBuilderAsync(pipeline, null);
        }

        /// <summary>
        /// Adds a conditional branch using a configuration action.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="configure">Action to configure branches.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipelineAsync AddSwitch(this PipelineAsync pipeline, Action<IConditionalBranchBuilder<IPipelineAsync>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new ConditionalBranchBuilderAsync(pipeline, null);
            configure(builder);
            return builder.EndSwitch();
        }

        #endregion

        #region [ Sub-Pipeline / Scope - Sync ]

        /// <summary>
        /// Begins a sub-pipeline scope.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">Optional name for the scope.</param>
        /// <returns>A sub-pipeline builder.</returns>
        public static ISubPipelineBuilder BeginScope(this Pipeline pipeline, string? name = null)
        {
            return new SubPipelineBuilder(pipeline, null, name);
        }

        /// <summary>
        /// Adds a sub-pipeline scope using a configuration action.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">Optional name for the scope.</param>
        /// <param name="configure">Action to configure the scope.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipeline AddScope(this Pipeline pipeline, string? name, Action<ISubPipelineBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new SubPipelineBuilder(pipeline, null, name);
            configure(builder);
            return builder.EndScope();
        }

        /// <summary>
        /// Adds a sub-pipeline scope using a configuration action.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configure">Action to configure the scope.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipeline AddScope(this Pipeline pipeline, Action<ISubPipelineBuilder> configure)
        {
            return AddScope(pipeline, null, configure);
        }

        #endregion

        #region [ Sub-Pipeline / Scope - Async ]

        /// <summary>
        /// Begins a sub-pipeline scope.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="name">Optional name for the scope.</param>
        /// <returns>A sub-pipeline builder.</returns>
        public static ISubPipelineBuilderAsync BeginScope(this PipelineAsync pipeline, string? name = null)
        {
            return new SubPipelineBuilderAsync(pipeline, null, name);
        }

        /// <summary>
        /// Adds a sub-pipeline scope using a configuration action.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="name">Optional name for the scope.</param>
        /// <param name="configure">Action to configure the scope.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipelineAsync AddScope(this PipelineAsync pipeline, string? name, Action<ISubPipelineBuilderAsync> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new SubPipelineBuilderAsync(pipeline, null, name);
            configure(builder);
            return builder.EndScope();
        }

        /// <summary>
        /// Adds a sub-pipeline scope using a configuration action.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="configure">Action to configure the scope.</param>
        /// <returns>The pipeline for chaining.</returns>
        public static IPipelineAsync AddScope(this PipelineAsync pipeline, Action<ISubPipelineBuilderAsync> configure)
        {
            return AddScope(pipeline, null, configure);
        }

        #endregion

        #region [ Pipeline Composition - Sync ]

        /// <summary>
        /// Composes another pipeline's operations into this pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="otherPipeline">The pipeline to compose.</param>
        /// <returns>This pipeline for chaining.</returns>
        public static IPipeline AddPipeline(this Pipeline pipeline, Pipeline otherPipeline)
        {
            if (otherPipeline == null)
                throw new ArgumentNullException(nameof(otherPipeline));

            foreach (var operation in otherPipeline.GetOperations())
            {
                pipeline.Add(operation);
            }

            return pipeline;
        }

        #endregion

        #region [ Pipeline Composition - Async ]

        /// <summary>
        /// Composes another pipeline's operations into this pipeline.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="otherPipeline">The pipeline to compose.</param>
        /// <returns>This pipeline for chaining.</returns>
        public static IPipelineAsync AddPipeline(this PipelineAsync pipeline, PipelineAsync otherPipeline)
        {
            if (otherPipeline == null)
                throw new ArgumentNullException(nameof(otherPipeline));

            foreach (var operation in otherPipeline.GetOperations())
            {
                pipeline.Add(operation);
            }

            return pipeline;
        }

        #endregion

        #region [ Validation ]

        /// <summary>
        /// Validates the pipeline structure before execution.
        /// </summary>
        /// <param name="pipeline">The pipeline to validate.</param>
        /// <param name="validator">The validator to use. If null, uses default validator.</param>
        /// <returns>The validation result.</returns>
        public static PipelineValidationResult Validate(this Pipeline pipeline, IPipelineValidator? validator = null)
        {
            validator ??= new Validation.DefaultPipelineValidator();
            return validator.Validate(pipeline.GetOperations());
        }

        /// <summary>
        /// Validates the pipeline structure before execution.
        /// </summary>
        /// <param name="pipeline">The async pipeline to validate.</param>
        /// <param name="validator">The validator to use. If null, uses default validator.</param>
        /// <returns>The validation result.</returns>
        public static PipelineValidationResult Validate(this PipelineAsync pipeline, IPipelineValidator? validator = null)
        {
            validator ??= new Validation.DefaultPipelineValidator();
            return validator.Validate(pipeline.GetOperations());
        }

        /// <summary>
        /// Executes the pipeline after validating. Throws if validation fails.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="input">The input message.</param>
        /// <param name="validator">The validator to use.</param>
        public static void ExecuteValidated(this Pipeline pipeline, IPipelineMessage? input = null, IPipelineValidator? validator = null)
        {
            var result = pipeline.Validate(validator);
            result.ThrowIfInvalid();
            pipeline.Execute(input);
        }

        /// <summary>
        /// Executes the pipeline after validating. Throws if validation fails.
        /// </summary>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="input">The input message.</param>
        /// <param name="validator">The validator to use.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task ExecuteValidatedAsync(
            this PipelineAsync pipeline,
            IPipelineMessage? input = null,
            IPipelineValidator? validator = null,
            CancellationToken cancellationToken = default)
        {
            var result = pipeline.Validate(validator);
            result.ThrowIfInvalid();
            await pipeline.ExecuteAsync(input);
        }

        #endregion
    }
}

