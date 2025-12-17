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
    /// Model binder for <see cref="DateTimeOffset"/> type with enhanced format support.
    /// Supports ISO 8601 format and common date-time formats with timezone information.
    /// </summary>
    /// <remarks>
    /// This binder enables binding of <see cref="DateTimeOffset"/> values from query strings,
    /// route parameters, and form data in Minimal APIs and MVC controllers.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query string: ?timestamp=2024-01-15T14:30:00Z
    /// app.MapGet("/events", (DateTimeOffset timestamp) => { ... });
    /// </code>
    /// </example>
    public class DateTimeOffsetModelBinder : IModelBinder
    {
        private static readonly string[] SupportedFormats = new[]
        {
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mmZ",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss"
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
            if (DateTimeOffset.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTimeOffset))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTimeOffset);
                return Task.CompletedTask;
            }

            if (DateTimeOffset.TryParseExact(value, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTimeOffset))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTimeOffset);
                return Task.CompletedTask;
            }

            // Try parsing with standard DateTimeOffset.Parse (handles most formats)
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTimeOffset))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTimeOffset);
                return Task.CompletedTask;
            }

            // Try parsing with supported formats
            foreach (var format in SupportedFormats)
            {
                if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTimeOffset))
                {
                    bindingContext.Result = ModelBindingResult.Success(dateTimeOffset);
                    return Task.CompletedTask;
                }
            }

            bindingContext.ModelState.TryAddModelError(
                modelName,
                $"The value '{value}' is not a valid date-time. Expected format: yyyy-MM-ddTHH:mm:ssZ (ISO 8601)");

            return Task.CompletedTask;
        }
    }
}

