//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// Model binder wrapper that provides binding with integrated validation support.
    /// Supports both FluentValidation and DataAnnotations validation.
    /// </summary>
    /// <typeparam name="T">The type to bind and validate.</typeparam>
    /// <remarks>
    /// <para>
    /// This binder automatically validates the bound model using:
    /// 1. FluentValidation (if a validator is registered in DI)
    /// 2. DataAnnotations (as fallback)
    /// </para>
    /// <para>
    /// Validation errors are captured in the <see cref="ValidationErrors"/> property
    /// without throwing exceptions, allowing the endpoint to handle them gracefully.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("/customers", async (ModelBinder&lt;CustomerFilter&gt; filter) =>
    /// {
    ///     if (filter.HasErrors)
    ///         return Results.ValidationProblem(filter.ValidationErrors);
    ///     
    ///     // Use filter.Data
    /// });
    /// </code>
    /// </example>
    public class ModelBinder<T> : IExtensionBinder<ModelBinder<T>>
        where T : class, new()
    {
        /// <summary>
        /// Gets the bound data model.
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Gets the binding exception, if any occurred during binding.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Gets the validation errors, if validation failed.
        /// </summary>
        public Dictionary<string, string[]> ValidationErrors { get; private set; }

        /// <summary>
        /// Gets a value indicating whether binding or validation failed.
        /// </summary>
        public bool HasErrors => Error != null || (ValidationErrors != null && ValidationErrors.Count > 0);

        /// <summary>
        /// Gets a value indicating whether validation passed.
        /// </summary>
        public bool IsValid => !HasErrors;

        /// <summary>
        /// Binds the model from the HTTP request query string and validates it.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The model binder instance with bound data and validation results.</returns>
        public static ValueTask<ModelBinder<T>> BindAsync(HttpContext context)
        {
            Exception exception = null;
            T data = null;
            Dictionary<string, string[]> validationErrors = null;

            try
            {
                // Bind from query string
                data = context.Request.GetFromQueryString<T>() ?? new T();

                // Validate using FluentValidation (if available) or DataAnnotations
                var serviceProvider = context.RequestServices;
                var fluentValidator = serviceProvider?.GetService<IValidator<T>>();

                if (fluentValidator != null)
                {
                    // Use FluentValidation
                    var validationResult = fluentValidator.Validate(data);
                    if (!validationResult.IsValid)
                    {
                        validationErrors = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            );
                    }
                }
                else
                {
                    // Fallback to DataAnnotations
                    var validationResults = new List<ValidationResult>();
                    var validationContext = new ValidationContext(data, null, null);
                    if (!Validator.TryValidateObject(data, validationContext, validationResults, true))
                    {
                        validationErrors = validationResults
                            .GroupBy(v => string.Join(",", v.MemberNames))
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(v => v.ErrorMessage ?? "Validation error").ToArray()
                            );
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            ModelBinder<T> model = new()
            {
                Data = data ?? new T(),
                Error = exception,
                ValidationErrors = validationErrors
            };

            return ValueTask.FromResult(model);
        }
    }
}
