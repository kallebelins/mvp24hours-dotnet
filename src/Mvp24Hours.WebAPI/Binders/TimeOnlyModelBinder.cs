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
    /// Model binder for <see cref="TimeOnly"/> type.
    /// Supports ISO 8601 time format (HH:mm:ss) and common time formats.
    /// </summary>
    /// <remarks>
    /// This binder enables binding of <see cref="TimeOnly"/> values from query strings,
    /// route parameters, and form data in Minimal APIs and MVC controllers.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query string: ?time=14:30:00
    /// app.MapGet("/appointments", (TimeOnly time) => { ... });
    /// </code>
    /// </example>
    public class TimeOnlyModelBinder : IModelBinder
    {
        private static readonly string[] SupportedFormats = new[]
        {
            "HH:mm:ss",
            "HH:mm",
            "h:mm tt",
            "h:mm:ss tt",
            "HH:mm:ss.fff"
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

            // Try parsing with ISO 8601 format first (HH:mm:ss)
            if (TimeOnly.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(timeOnly);
                return Task.CompletedTask;
            }

            // Try parsing with HH:mm format
            if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(timeOnly);
                return Task.CompletedTask;
            }

            // Try parsing with standard TimeOnly.Parse
            if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out timeOnly))
            {
                bindingContext.Result = ModelBindingResult.Success(timeOnly);
                return Task.CompletedTask;
            }

            // Try parsing with supported formats
            foreach (var format in SupportedFormats)
            {
                if (TimeOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out timeOnly))
                {
                    bindingContext.Result = ModelBindingResult.Success(timeOnly);
                    return Task.CompletedTask;
                }
            }

            bindingContext.ModelState.TryAddModelError(
                modelName,
                $"The value '{value}' is not a valid time. Expected format: HH:mm:ss or HH:mm");

            return Task.CompletedTask;
        }
    }
}

