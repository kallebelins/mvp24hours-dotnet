//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic.Validation;

/// <summary>
/// Default implementation of customizable validation pipeline.
/// Executes validation steps in order of priority.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
/// <remarks>
/// Creates a new validation pipeline with the specified steps.
/// </remarks>
/// <param name="steps">Collection of validation steps.</param>
/// <param name="serviceProvider">Service provider for resolving dependencies.</param>
/// <param name="logger">Logger instance.</param>
public class ValidationPipeline<T>(
    IEnumerable<IValidationStep<T>>? steps = null,
    IServiceProvider? serviceProvider = null,
    ILogger<ValidationPipeline<T>>? logger = null) : IValidationPipeline<T> where T : class
{
    private readonly List<IValidationStep<T>> _steps = (steps?.OrderBy(s => s.Order).ToList()) ?? [];
    private readonly IServiceProvider? _serviceProvider = serviceProvider;
    private readonly ILogger<ValidationPipeline<T>>? _logger = logger;

    /// <inheritdoc/>
    public IReadOnlyList<IValidationStep<T>> Steps => _steps.AsReadOnly();

    /// <inheritdoc/>
    public ValidationServiceResult Execute(T instance)
    {
        return Execute(instance, ValidationOptions.Default);
    }

    /// <inheritdoc/>
    public ValidationServiceResult Execute(T instance, ValidationOptions options)
    {
        _logger?.LogDebug("application-validationpipeline-execute");

        if (instance == null)
        {
            return ValidationServiceResult.Failure("instance", "Instance cannot be null.");
        }

        var context = new ValidationStepContext(options, _serviceProvider);
        var allErrors = new List<IMessageResult>();

        foreach (IValidationStep<T>? step in _steps.Where(s => s.IsEnabled))
        {
            if (!step.ShouldExecute(instance, context))
            {
                _logger?.LogDebug("Skipping validation step {StepName} for type {TypeName}",
                    step.Name, typeof(T).Name);
                continue;
            }

            _logger?.LogDebug("Executing validation step {StepName} for type {TypeName}",
                step.Name, typeof(T).Name);

            ValidationServiceResult result = step.Execute(instance, context);

            if (!result.IsValid)
            {
                allErrors.AddRange(result.Errors);
                context.AccumulatedErrors.Clear();
                foreach (IMessageResult error in allErrors)
                {
                    context.AccumulatedErrors.Add(error);
                }

                if (options.StopOnFirstError)
                {
                    _logger?.LogDebug("Stopping validation pipeline after step {StepName} due to errors",
                        step.Name);
                    break;
                }
            }
        }

        if (allErrors.Any())
        {
            _logger?.LogDebug("Validation pipeline completed with {ErrorCount} error(s) for type {TypeName}",
                allErrors.Count, typeof(T).Name);

            if (options.ThrowOnValidationFailure)
            {
                throw new Core.Exceptions.ValidationException(
                    $"Validation failed for {typeof(T).Name}",
                    "VALIDATION_PIPELINE_ERROR",
                    allErrors);
            }

            return ValidationServiceResult.Failure(allErrors);
        }

        return ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ExecuteAsync(T instance, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(instance, ValidationOptions.Default, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationOptions options, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("application-validationpipeline-executeasync");

        if (instance == null)
        {
            return ValidationServiceResult.Failure("instance", "Instance cannot be null.");
        }

        var context = new ValidationStepContext(options, _serviceProvider);
        var allErrors = new List<IMessageResult>();

        foreach (IValidationStep<T>? step in _steps.Where(s => s.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!step.ShouldExecute(instance, context))
            {
                _logger?.LogDebug("Skipping validation step {StepName} for type {TypeName}",
                    step.Name, typeof(T).Name);
                continue;
            }

            _logger?.LogDebug("Executing validation step {StepName} for type {TypeName}",
                step.Name, typeof(T).Name);

            ValidationServiceResult result = await step.ExecuteAsync(instance, context, cancellationToken);

            if (!result.IsValid)
            {
                allErrors.AddRange(result.Errors);
                context.AccumulatedErrors.Clear();
                foreach (IMessageResult error in allErrors)
                {
                    context.AccumulatedErrors.Add(error);
                }

                if (options.StopOnFirstError)
                {
                    _logger?.LogDebug("Stopping validation pipeline after step {StepName} due to errors",
                        step.Name);
                    break;
                }
            }
        }

        if (allErrors.Any())
        {
            _logger?.LogDebug("Validation pipeline completed with {ErrorCount} error(s) for type {TypeName}",
                allErrors.Count, typeof(T).Name);

            if (options.ThrowOnValidationFailure)
            {
                throw new Core.Exceptions.ValidationException(
                    $"Validation failed for {typeof(T).Name}",
                    "VALIDATION_PIPELINE_ERROR",
                    allErrors);
            }

            return ValidationServiceResult.Failure(allErrors);
        }

        return ValidationServiceResult.Success();
    }

    /// <summary>
    /// Adds a validation step to the pipeline.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(IValidationStep<T> step)
    {
        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _steps.Add(step);
        _steps.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>
    /// Removes a validation step from the pipeline.
    /// </summary>
    /// <param name="step">The step to remove.</param>
    /// <returns>True if the step was removed; otherwise, false.</returns>
    public bool RemoveStep(IValidationStep<T> step)
    {
        return _steps.Remove(step);
    }
}

/// <summary>
/// Builder for constructing validation pipelines with fluent API.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
/// <remarks>
/// Creates a new validation pipeline builder.
/// </remarks>
/// <param name="serviceProvider">Service provider for resolving dependencies.</param>
public class ValidationPipelineBuilder<T>(IServiceProvider? serviceProvider = null) : IValidationPipelineBuilder<T> where T : class
{
    private readonly List<IValidationStep<T>> _steps = [];
    private readonly IServiceProvider? _serviceProvider = serviceProvider;

    /// <inheritdoc/>
    public IValidationPipelineBuilder<T> AddStep<TStep>() where TStep : class, IValidationStep<T>
    {
        IValidationStep<T>? step = null;

        if (_serviceProvider != null)
        {
            step = _serviceProvider.GetService(typeof(TStep)) as TStep;
        }

        step ??= Activator.CreateInstance<TStep>();
        _steps.Add(step);

        return this;
    }

    /// <inheritdoc/>
    public IValidationPipelineBuilder<T> AddStep(IValidationStep<T> step)
    {
        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    public IValidationPipelineBuilder<T> UseFluentValidation()
    {
        FluentValidationStep<T>? step = _serviceProvider != null
            ? _serviceProvider.GetService(typeof(FluentValidationStep<T>)) as FluentValidationStep<T>
            : null;

        step ??= new FluentValidationStep<T>(_serviceProvider);
        _steps.Add(step);

        return this;
    }

    /// <inheritdoc/>
    public IValidationPipelineBuilder<T> UseDataAnnotations()
    {
        var step = new DataAnnotationValidationStep<T>();
        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    public IValidationPipelineBuilder<T> UseCascadeValidation()
    {
        var step = new CascadeValidationStep<T>(_serviceProvider);
        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    public IValidationPipeline<T> Build()
    {
        var logger = _serviceProvider?.GetService(typeof(ILogger<ValidationPipeline<T>>))
            as ILogger<ValidationPipeline<T>>;

        return new ValidationPipeline<T>(_steps, _serviceProvider, logger);
    }
}

