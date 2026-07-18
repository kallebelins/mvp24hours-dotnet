//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic.Validation;

/// <summary>
/// Validation step that uses FluentValidation validators.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public class FluentValidationStep<T> : IValidationStep<T> where T : class
{
    private readonly IEnumerable<IValidator<T>> _validators;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Creates a new FluentValidation step.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving validators.</param>
    public FluentValidationStep(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        _validators = serviceProvider?.GetServices<IValidator<T>>()
            ?? [];
    }

    /// <summary>
    /// Creates a new FluentValidation step with explicit validators.
    /// </summary>
    /// <param name="validators">Collection of validators to use.</param>
    public FluentValidationStep(IEnumerable<IValidator<T>> validators)
    {
        _validators = validators ?? [];
    }

    /// <inheritdoc/>
    public int Order => 100;

    /// <inheritdoc/>
    public string Name => "FluentValidation";

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public ValidationServiceResult Execute(T instance, ValidationStepContext context)
    {
        if (!_validators.Any())
        {
            return ValidationServiceResult.Success();
        }

        var errors = new List<IMessageResult>();
        var validationContext = new ValidationContext<T>(instance);

        // Handle RuleSets if specified
        if (context.Options.RuleSets?.Any() == true)
        {
            validationContext.SetRuleSetsExecuted(context.Options.RuleSets);
        }

        foreach (IValidator<T> validator in _validators)
        {
            ValidationResult result = validator.Validate(validationContext);
            if (!result.IsValid)
            {
                foreach (ValidationFailure? failure in result.Errors)
                {
                    string propertyPath = context.Options.IncludePropertyPath &&
                                      !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{failure.PropertyName}"
                        : failure.PropertyName;

                    errors.Add(new MessageResult(
                        propertyPath ?? failure.ErrorCode,
                        failure.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return ValidationServiceResult.Failure(errors);
                    }
                }
            }
        }

        return errors.Any()
            ? ValidationServiceResult.Failure(errors)
            : ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public async Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
    {
        if (!_validators.Any())
        {
            return ValidationServiceResult.Success();
        }

        var errors = new List<IMessageResult>();
        var validationContext = new ValidationContext<T>(instance);

        // Handle RuleSets if specified
        if (context.Options.RuleSets?.Any() == true)
        {
            validationContext.SetRuleSetsExecuted(context.Options.RuleSets);
        }

        foreach (IValidator<T> validator in _validators)
        {
            ValidationResult result = await validator.ValidateAsync(validationContext, cancellationToken);
            if (!result.IsValid)
            {
                foreach (ValidationFailure? failure in result.Errors)
                {
                    string propertyPath = context.Options.IncludePropertyPath &&
                                      !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{failure.PropertyName}"
                        : failure.PropertyName;

                    errors.Add(new MessageResult(
                        propertyPath ?? failure.ErrorCode,
                        failure.ErrorMessage,
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return ValidationServiceResult.Failure(errors);
                    }
                }
            }
        }

        return errors.Any()
            ? ValidationServiceResult.Failure(errors)
            : ValidationServiceResult.Success();
    }

    /// <inheritdoc/>
    public bool ShouldExecute(T instance, ValidationStepContext context)
    {
        return _validators.Any();
    }
}

internal static class ValidationContextExtensions
{
    public static void SetRuleSetsExecuted<T>(this ValidationContext<T> context, string[] ruleSets)
    {
        // FluentValidation handles RuleSets through selector
        // This is a placeholder - actual implementation requires ValidatorOptions
    }
}

