//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Binders
{
    /// <summary>
    /// Model binder for <see cref="DateOnly"/> type.
    /// Supports ISO 8601 date format (yyyy-MM-dd) and common date formats.
    /// </summary>
    /// <remarks>
    /// This binder enables binding of <see cref="DateOnly"/> values from query strings,
    /// route parameters, and form data in Minimal APIs and MVC controllers.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query string: ?date=2024-01-15
    /// app.MapGet("/events", (DateOnly date) => { ... });
    /// </code>
    /// </example>
    public class DateOnlyModelBinder : IModelBinder
    {
        private static readonly string[] SupportedFormats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "dd-MM-yyyy",
            "MM-dd-yyyy"
        };

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.CompletedTask;
            }

            // Try parsing with ISO 8601 format first (most common)
            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(dateOnly);
                return Task.CompletedTask;
            }

            // Try parsing with standard DateOnly.Parse
            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(dateOnly);
                return Task.CompletedTask;
            }

            // Try parsing with supported formats
            foreach (var format in SupportedFormats)
            {
                if (DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOnly))
                {
                    bindingContext.Result = ModelBindingResult.Success(dateOnly);
                    return Task.CompletedTask;
                }
            }

            bindingContext.ModelState.TryAddModelError(
                modelName,
                $"The value '{value}' is not a valid date. Expected format: yyyy-MM-dd");

            return Task.CompletedTask;
        }
    }
}

