//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Validation
{
    /// <summary>
    /// Interface for customizable validation pipeline that allows multiple validation steps.
    /// Validation steps are executed in order, allowing composition of different validation strategies.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <remarks>
    /// <para>
    /// The validation pipeline allows chaining multiple validators in a specific order:
    /// <list type="number">
    /// <item>Pre-validation (e.g., null checks, format validation)</item>
    /// <item>FluentValidation validators</item>
    /// <item>DataAnnotation validation</item>
    /// <item>Custom business rules</item>
    /// <item>Cross-property validation</item>
    /// <item>Cascade/nested validation</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// services.AddValidationPipeline&lt;OrderDto&gt;()
    ///     .AddStep&lt;NullCheckValidationStep&lt;OrderDto&gt;&gt;()
    ///     .AddStep&lt;FluentValidationStep&lt;OrderDto&gt;&gt;()
    ///     .AddStep&lt;DataAnnotationValidationStep&lt;OrderDto&gt;&gt;()
    ///     .AddStep&lt;CascadeValidationStep&lt;OrderDto&gt;&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public interface IValidationPipeline<T> where T : class
    {
        /// <summary>
        /// Gets the collection of validation steps in the pipeline.
        /// </summary>
        IReadOnlyList<IValidationStep<T>> Steps { get; }

        /// <summary>
        /// Executes all validation steps in the pipeline synchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <returns>Combined validation result from all steps.</returns>
        ValidationServiceResult Execute(T instance);

        /// <summary>
        /// Executes all validation steps in the pipeline synchronously with options.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="options">Validation options.</param>
        /// <returns>Combined validation result from all steps.</returns>
        ValidationServiceResult Execute(T instance, ValidationOptions options);

        /// <summary>
        /// Executes all validation steps in the pipeline asynchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Combined validation result from all steps.</returns>
        Task<ValidationServiceResult> ExecuteAsync(T instance, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes all validation steps in the pipeline asynchronously with options.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="options">Validation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Combined validation result from all steps.</returns>
        Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for a single validation step in the validation pipeline.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public interface IValidationStep<T> where T : class
    {
        /// <summary>
        /// Gets the order of this step in the pipeline.
        /// Lower values execute first.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets the name of this validation step for logging/debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether this step is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Executes this validation step synchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="context">The validation context with options and state.</param>
        /// <returns>Validation result from this step.</returns>
        ValidationServiceResult Execute(T instance, ValidationStepContext context);

        /// <summary>
        /// Executes this validation step asynchronously.
        /// </summary>
        /// <param name="instance">The instance to validate.</param>
        /// <param name="context">The validation context with options and state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result from this step.</returns>
        Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether this step should execute based on the current context.
        /// </summary>
        /// <param name="instance">The instance being validated.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>True if this step should execute; otherwise, false.</returns>
        bool ShouldExecute(T instance, ValidationStepContext context);
    }

    /// <summary>
    /// Context passed to validation steps containing options and state.
    /// </summary>
    public class ValidationStepContext
    {
        /// <summary>
        /// Gets the validation options.
        /// </summary>
        public ValidationOptions Options { get; }

        /// <summary>
        /// Gets the current depth in nested validation.
        /// </summary>
        public int CurrentDepth { get; set; }

        /// <summary>
        /// Gets the property path for nested validation.
        /// </summary>
        public string PropertyPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets state data shared between validation steps.
        /// </summary>
        public IDictionary<string, object> State { get; }

        /// <summary>
        /// Gets the accumulated errors from previous steps.
        /// </summary>
        public IList<IMessageResult> AccumulatedErrors { get; }

        /// <summary>
        /// Gets the service provider for resolving dependencies.
        /// </summary>
        public System.IServiceProvider? ServiceProvider { get; }

        /// <summary>
        /// Creates a new validation step context.
        /// </summary>
        /// <param name="options">Validation options.</param>
        /// <param name="serviceProvider">Optional service provider.</param>
        public ValidationStepContext(ValidationOptions options, System.IServiceProvider? serviceProvider = null)
        {
            Options = options ?? ValidationOptions.Default;
            State = new Dictionary<string, object>();
            AccumulatedErrors = new List<IMessageResult>();
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a child context for nested validation.
        /// </summary>
        /// <param name="propertyName">The property name being validated.</param>
        /// <returns>A new context for nested validation.</returns>
        public ValidationStepContext CreateChildContext(string propertyName)
        {
            var childPath = string.IsNullOrEmpty(PropertyPath)
                ? propertyName
                : $"{PropertyPath}.{propertyName}";

            return new ValidationStepContext(Options, ServiceProvider)
            {
                CurrentDepth = CurrentDepth + 1,
                PropertyPath = childPath
            };
        }

        /// <summary>
        /// Creates a child context for collection item validation.
        /// </summary>
        /// <param name="propertyName">The property name of the collection.</param>
        /// <param name="index">The index of the item in the collection.</param>
        /// <returns>A new context for the collection item.</returns>
        public ValidationStepContext CreateChildContext(string propertyName, int index)
        {
            var childPath = string.IsNullOrEmpty(PropertyPath)
                ? $"{propertyName}[{index}]"
                : $"{PropertyPath}.{propertyName}[{index}]";

            return new ValidationStepContext(Options, ServiceProvider)
            {
                CurrentDepth = CurrentDepth + 1,
                PropertyPath = childPath
            };
        }
    }

    /// <summary>
    /// Builder interface for configuring validation pipeline.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public interface IValidationPipelineBuilder<T> where T : class
    {
        /// <summary>
        /// Adds a validation step to the pipeline.
        /// </summary>
        /// <typeparam name="TStep">The validation step type.</typeparam>
        /// <returns>The builder for chaining.</returns>
        IValidationPipelineBuilder<T> AddStep<TStep>() where TStep : class, IValidationStep<T>;

        /// <summary>
        /// Adds a validation step instance to the pipeline.
        /// </summary>
        /// <param name="step">The validation step instance.</param>
        /// <returns>The builder for chaining.</returns>
        IValidationPipelineBuilder<T> AddStep(IValidationStep<T> step);

        /// <summary>
        /// Adds FluentValidation support to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        IValidationPipelineBuilder<T> UseFluentValidation();

        /// <summary>
        /// Adds DataAnnotation validation support to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        IValidationPipelineBuilder<T> UseDataAnnotations();

        /// <summary>
        /// Adds cascade validation for nested objects.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        IValidationPipelineBuilder<T> UseCascadeValidation();

        /// <summary>
        /// Builds the validation pipeline.
        /// </summary>
        /// <returns>The configured validation pipeline.</returns>
        IValidationPipeline<T> Build();
    }
}

