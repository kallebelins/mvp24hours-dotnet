//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Typed
{
    /// <summary>
    /// Fluent API extensions for creating and working with typed pipelines.
    /// </summary>
    public static class TypedPipelineFluentExtensions
    {
        #region [ Starting a Chain ]

        /// <summary>
        /// Creates a new typed pipeline starting with the given input type.
        /// Usage: Pipe.Create&lt;Input, Output&gt;()
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <returns>A new typed pipeline.</returns>
        public static TypedPipeline<TInput, TOutput> Create<TInput, TOutput>()
        {
            return new TypedPipeline<TInput, TOutput>();
        }

        /// <summary>
        /// Creates a new async typed pipeline starting with the given input type.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <returns>A new async typed pipeline.</returns>
        public static TypedPipelineAsync<TInput, TOutput> CreateAsync<TInput, TOutput>()
        {
            return new TypedPipelineAsync<TInput, TOutput>();
        }

        #endregion

        #region [ Chaining Operations ]

        /// <summary>
        /// Chains a transformation operation to the pipeline.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The current output type.</typeparam>
        /// <typeparam name="TNext">The new output type after transformation.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="transform">The transformation function.</param>
        /// <returns>A new pipeline with the transformed output type.</returns>
        public static TypedPipeline<TInput, TNext> Then<TInput, TOutput, TNext>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Func<TOutput, TNext> transform)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var newPipeline = new TypedPipeline<TInput, TNext>
            {
                IsBreakOnFail = pipeline.IsBreakOnFail,
                ForceRollbackOnFailure = pipeline.ForceRollbackOnFailure,
                AllowPropagateException = pipeline.AllowPropagateException
            };

            // Add transformation that wraps the original pipeline
            newPipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsFailure)
                {
                    return OperationResult<TNext>.Failure(result.Messages);
                }

                try
                {
                    var transformed = transform(result.Value!);
                    return OperationResult<TNext>.Success(transformed, result.Messages);
                }
                catch (Exception ex)
                {
                    return OperationResult<TNext>.Failure(ex);
                }
            });

            return newPipeline;
        }

        /// <summary>
        /// Chains an async transformation operation to the pipeline.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The current output type.</typeparam>
        /// <typeparam name="TNext">The new output type after transformation.</typeparam>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="transform">The async transformation function.</param>
        /// <returns>A new async pipeline with the transformed output type.</returns>
        public static TypedPipelineAsync<TInput, TNext> ThenAsync<TInput, TOutput, TNext>(
            this TypedPipelineAsync<TInput, TOutput> pipeline,
            Func<TOutput, CancellationToken, Task<TNext>> transform)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var newPipeline = new TypedPipelineAsync<TInput, TNext>
            {
                IsBreakOnFail = pipeline.IsBreakOnFail,
                ForceRollbackOnFailure = pipeline.ForceRollbackOnFailure,
                AllowPropagateException = pipeline.AllowPropagateException
            };

            newPipeline.Add(async (input, ct) =>
            {
                var result = await pipeline.ExecuteAsync(input, ct);
                if (result.IsFailure)
                {
                    return OperationResult<TNext>.Failure(result.Messages);
                }

                try
                {
                    var transformed = await transform(result.Value!, ct);
                    return OperationResult<TNext>.Success(transformed, result.Messages);
                }
                catch (Exception ex)
                {
                    return OperationResult<TNext>.Failure(ex);
                }
            });

            return newPipeline;
        }

        #endregion

        #region [ Side Effects ]

        /// <summary>
        /// Adds a side-effect action that doesn't change the pipeline output type.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="action">The side-effect action.</param>
        /// <returns>The same pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> Tap<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Action<TOutput> action)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            pipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsSuccess)
                {
                    action(result.Value!);
                }
                return result;
            });

            return pipeline;
        }

        /// <summary>
        /// Adds an async side-effect action that doesn't change the pipeline output type.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="action">The async side-effect action.</param>
        /// <returns>The same pipeline for continued chaining.</returns>
        public static TypedPipelineAsync<TInput, TOutput> TapAsync<TInput, TOutput>(
            this TypedPipelineAsync<TInput, TOutput> pipeline,
            Func<TOutput, CancellationToken, Task> action)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            pipeline.Add(async (input, ct) =>
            {
                var result = await pipeline.ExecuteAsync(input, ct);
                if (result.IsSuccess)
                {
                    await action(result.Value!, ct);
                }
                return result;
            });

            return pipeline;
        }

        #endregion

        #region [ Conditional Branching ]

        /// <summary>
        /// Adds a conditional operation that only executes if the condition is met.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="operation">The operation to execute if condition is true.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> When<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Func<TOutput, bool> condition,
            Func<TOutput, IOperationResult<TOutput>> operation)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            pipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsFailure)
                {
                    return result;
                }

                if (condition(result.Value!))
                {
                    return operation(result.Value!);
                }

                return result;
            });

            return pipeline;
        }

        /// <summary>
        /// Adds an if-else conditional branch.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="thenOperation">The operation to execute if condition is true.</param>
        /// <param name="elseOperation">The operation to execute if condition is false.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> Branch<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Func<TOutput, bool> condition,
            Func<TOutput, IOperationResult<TOutput>> thenOperation,
            Func<TOutput, IOperationResult<TOutput>> elseOperation)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (thenOperation == null)
                throw new ArgumentNullException(nameof(thenOperation));
            if (elseOperation == null)
                throw new ArgumentNullException(nameof(elseOperation));

            pipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsFailure)
                {
                    return result;
                }

                return condition(result.Value!)
                    ? thenOperation(result.Value!)
                    : elseOperation(result.Value!);
            });

            return pipeline;
        }

        #endregion

        #region [ Error Handling ]

        /// <summary>
        /// Adds error handling that provides a fallback value on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="fallback">The fallback value to use on failure.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> OnError<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            TOutput fallback)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            var newPipeline = new TypedPipeline<TInput, TOutput>
            {
                IsBreakOnFail = pipeline.IsBreakOnFail,
                ForceRollbackOnFailure = pipeline.ForceRollbackOnFailure,
                AllowPropagateException = pipeline.AllowPropagateException
            };

            newPipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                return result.IsFailure
                    ? OperationResult<TOutput>.Success(fallback, result.Messages)
                    : result;
            });

            return newPipeline;
        }

        /// <summary>
        /// Adds error handling that provides a fallback function on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="fallbackFactory">The fallback factory function.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> OnError<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Func<TInput, TOutput> fallbackFactory)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (fallbackFactory == null)
                throw new ArgumentNullException(nameof(fallbackFactory));

            var newPipeline = new TypedPipeline<TInput, TOutput>
            {
                IsBreakOnFail = pipeline.IsBreakOnFail,
                ForceRollbackOnFailure = pipeline.ForceRollbackOnFailure,
                AllowPropagateException = pipeline.AllowPropagateException
            };

            newPipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsFailure)
                {
                    try
                    {
                        var fallback = fallbackFactory(input);
                        return OperationResult<TOutput>.Success(fallback, result.Messages);
                    }
                    catch (Exception ex)
                    {
                        return OperationResult<TOutput>.Failure(ex);
                    }
                }
                return result;
            });

            return newPipeline;
        }

        #endregion

        #region [ Validation ]

        /// <summary>
        /// Adds validation that must pass before proceeding.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="predicate">The validation predicate.</param>
        /// <param name="errorMessage">The error message if validation fails.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> Ensure<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            Func<TOutput, bool> predicate,
            string errorMessage)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            pipeline.Add(input =>
            {
                var result = pipeline.Execute(input);
                if (result.IsFailure)
                {
                    return result;
                }

                return predicate(result.Value!)
                    ? result
                    : OperationResult<TOutput>.Failure(errorMessage ?? "Validation failed");
            });

            return pipeline;
        }

        #endregion

        #region [ Configuration ]

        /// <summary>
        /// Configures the pipeline to break on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="breakOnFail">Whether to break on failure.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> WithBreakOnFail<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            bool breakOnFail = true)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            pipeline.IsBreakOnFail = breakOnFail;
            return pipeline;
        }

        /// <summary>
        /// Configures the pipeline to rollback on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="forceRollback">Whether to force rollback on failure.</param>
        /// <returns>The pipeline for continued chaining.</returns>
        public static TypedPipeline<TInput, TOutput> WithRollbackOnFailure<TInput, TOutput>(
            this TypedPipeline<TInput, TOutput> pipeline,
            bool forceRollback = true)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            pipeline.ForceRollbackOnFailure = forceRollback;
            return pipeline;
        }

        /// <summary>
        /// Configures the async pipeline to break on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="breakOnFail">Whether to break on failure.</param>
        /// <returns>The async pipeline for continued chaining.</returns>
        public static TypedPipelineAsync<TInput, TOutput> WithBreakOnFail<TInput, TOutput>(
            this TypedPipelineAsync<TInput, TOutput> pipeline,
            bool breakOnFail = true)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            pipeline.IsBreakOnFail = breakOnFail;
            return pipeline;
        }

        /// <summary>
        /// Configures the async pipeline to rollback on failure.
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="pipeline">The async pipeline.</param>
        /// <param name="forceRollback">Whether to force rollback on failure.</param>
        /// <returns>The async pipeline for continued chaining.</returns>
        public static TypedPipelineAsync<TInput, TOutput> WithRollbackOnFailure<TInput, TOutput>(
            this TypedPipelineAsync<TInput, TOutput> pipeline,
            bool forceRollback = true)
        {
            if (pipeline == null)
                throw new ArgumentNullException(nameof(pipeline));

            pipeline.ForceRollbackOnFailure = forceRollback;
            return pipeline;
        }

        #endregion
    }

    /// <summary>
    /// Static factory for creating typed pipelines with a fluent API.
    /// </summary>
    public static class Pipe
    {
        /// <summary>
        /// Creates a new typed pipeline.
        /// Usage: Pipe.Create&lt;Input, Output&gt;().Add(...).Execute(input)
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <returns>A new typed pipeline.</returns>
        public static TypedPipeline<TInput, TOutput> Create<TInput, TOutput>()
        {
            return new TypedPipeline<TInput, TOutput>();
        }

        /// <summary>
        /// Creates a new async typed pipeline.
        /// Usage: Pipe.CreateAsync&lt;Input, Output&gt;().Add(...).ExecuteAsync(input)
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <returns>A new async typed pipeline.</returns>
        public static TypedPipelineAsync<TInput, TOutput> CreateAsync<TInput, TOutput>()
        {
            return new TypedPipelineAsync<TInput, TOutput>();
        }

        /// <summary>
        /// Creates an operation chain starting with a transformation.
        /// Usage: Pipe.From&lt;Input&gt;().Then(x => ...).Finally(input)
        /// </summary>
        /// <typeparam name="T">The input and initial output type.</typeparam>
        /// <returns>A new operation chain.</returns>
        public static OperationChain<T, T> From<T>()
        {
            return OperationChain.Start<T>();
        }

        /// <summary>
        /// Creates an operation chain with an initial transformation.
        /// Usage: Pipe.From&lt;Input, Output&gt;(x => transform).Then(...).Finally(input)
        /// </summary>
        /// <typeparam name="TInput">The input type.</typeparam>
        /// <typeparam name="TOutput">The output type.</typeparam>
        /// <param name="initialTransform">The initial transformation.</param>
        /// <returns>A new operation chain.</returns>
        public static OperationChain<TInput, TOutput> From<TInput, TOutput>(Func<TInput, TOutput> initialTransform)
        {
            return OperationChain.Pipe<TInput, TOutput>(initialTransform);
        }
    }
}

