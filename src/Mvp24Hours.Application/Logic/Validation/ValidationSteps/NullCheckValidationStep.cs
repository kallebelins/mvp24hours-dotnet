//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Logic.Validation;

/// <summary>
/// Validation step that checks for null values in required properties.
/// This is a fast pre-validation step that runs before other validators.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public class NullCheckValidationStep<T> : IValidationStep<T> where T : class
{
    /// <inheritdoc/>
    public int Order => 10;

    /// <inheritdoc/>
    public string Name => "NullCheck";

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public ValidationServiceResult Execute(T instance, ValidationStepContext context)
    {
        var errors = new List<IMessageResult>();
        Type type = typeof(T);
        IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (PropertyInfo? property in properties)
        {
            // Check for [Required] attribute
            RequiredAttribute? requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttr == null)
            {
                continue;
            }

            object? value = property.GetValue(instance);

            if (value == null)
            {
                string propertyPath = context.Options.IncludePropertyPath &&
                                  !string.IsNullOrEmpty(context.PropertyPath)
                    ? $"{context.PropertyPath}.{property.Name}"
                    : property.Name;

                string errorMessage = requiredAttr.ErrorMessage
                    ?? $"The {property.Name} field is required.";

                errors.Add(new MessageResult(
                    propertyPath,
                    errorMessage,
                    Core.Enums.MessageType.Error));

                if (context.Options.StopOnFirstError)
                {
                    return ValidationServiceResult.Failure(errors);
                }
            }
            else if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                // Also check for empty/whitespace strings if AllowEmptyStrings is false
                if (!requiredAttr.AllowEmptyStrings)
                {
                    string propertyPath = context.Options.IncludePropertyPath &&
                                      !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{property.Name}"
                        : property.Name;

                    string errorMessage = requiredAttr.ErrorMessage
                        ?? $"The {property.Name} field is required.";

                    errors.Add(new MessageResult(
                        propertyPath,
                        errorMessage,
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
    public Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Execute(instance, context));
    }

    /// <inheritdoc/>
    public bool ShouldExecute(T instance, ValidationStepContext context)
    {
        return true;
    }
}

