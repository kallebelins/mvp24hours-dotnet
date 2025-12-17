//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Validation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic.Validation
{
    /// <summary>
    /// Validation step that uses DataAnnotation attributes.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    public class DataAnnotationValidationStep<T> : IValidationStep<T> where T : class
    {
        /// <inheritdoc/>
        public int Order => 200;

        /// <inheritdoc/>
        public string Name => "DataAnnotations";

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <inheritdoc/>
        public ValidationServiceResult Execute(T instance, ValidationStepContext context)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(instance, null, null);

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                var errors = new List<IMessageResult>();

                foreach (var result in validationResults)
                {
                    var propertyName = result.MemberNames.Any()
                        ? string.Join(", ", result.MemberNames)
                        : "Unknown";

                    var fullPath = context.Options.IncludePropertyPath &&
                                  !string.IsNullOrEmpty(context.PropertyPath)
                        ? $"{context.PropertyPath}.{propertyName}"
                        : propertyName;

                    errors.Add(new MessageResult(
                        fullPath,
                        result.ErrorMessage ?? "Validation failed",
                        Core.Enums.MessageType.Error));

                    if (context.Options.StopOnFirstError)
                    {
                        return ValidationServiceResult.Failure(errors);
                    }
                }

                return ValidationServiceResult.Failure(errors);
            }

            return ValidationServiceResult.Success();
        }

        /// <inheritdoc/>
        public Task<ValidationServiceResult> ExecuteAsync(T instance, ValidationStepContext context, CancellationToken cancellationToken = default)
        {
            // DataAnnotations doesn't have async support, so run synchronously
            return Task.FromResult(Execute(instance, context));
        }

        /// <inheritdoc/>
        public bool ShouldExecute(T instance, ValidationStepContext context)
        {
            return true;
        }
    }
}

