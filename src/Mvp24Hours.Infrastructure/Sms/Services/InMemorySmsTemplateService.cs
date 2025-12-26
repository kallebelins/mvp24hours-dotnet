//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Services
{
    /// <summary>
    /// In-memory implementation of SMS template service for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores templates in memory and uses simple placeholder replacement
    /// for rendering. For production use, consider implementing a persistent storage solution
    /// (e.g., database-backed template service).
    /// </para>
    /// </remarks>
    public class InMemorySmsTemplateService : ISmsTemplateService
    {
        private readonly Dictionary<string, SmsTemplate> _templates = new();

        /// <summary>
        /// Gets a template by ID.
        /// </summary>
        public Task<SmsTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException("Template ID cannot be null or empty.", nameof(templateId));
            }

            _templates.TryGetValue(templateId, out var template);
            return Task.FromResult<SmsTemplate?>(template);
        }

        /// <summary>
        /// Saves or updates a template.
        /// </summary>
        public Task SaveTemplateAsync(SmsTemplate template, CancellationToken cancellationToken = default)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var validationErrors = template.Validate();
            if (validationErrors.Count > 0)
            {
                throw new ArgumentException($"Template validation failed: {string.Join(", ", validationErrors)}");
            }

            if (string.IsNullOrWhiteSpace(template.Id))
            {
                throw new ArgumentException("Template ID is required.", nameof(template));
            }

            template.UpdatedAt = DateTimeOffset.UtcNow;
            if (!template.CreatedAt.HasValue || template.CreatedAt.Value == default(DateTimeOffset))
            {
                template.CreatedAt = DateTimeOffset.UtcNow;
            }

            _templates[template.Id] = template;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes a template.
        /// </summary>
        public Task DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException("Template ID cannot be null or empty.", nameof(templateId));
            }

            _templates.Remove(templateId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Lists all templates.
        /// </summary>
        public Task<IList<SmsTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IList<SmsTemplate>>(_templates.Values.ToList());
        }

        /// <summary>
        /// Renders a template with the provided values.
        /// </summary>
        public Task<string> RenderAsync(
            SmsTemplate template,
            IDictionary<string, object> values,
            CancellationToken cancellationToken = default)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (string.IsNullOrWhiteSpace(template.Body))
            {
                throw new ArgumentException("Template body is required.", nameof(template));
            }

            if (values == null)
            {
                values = new Dictionary<string, object>();
            }

            var engine = template.TemplateEngine ?? "Simple";
            var rendered = engine switch
            {
                "Simple" => RenderSimple(template.Body, values),
                _ => throw new NotSupportedException($"Template engine '{engine}' is not supported.")
            };

            return Task.FromResult(rendered);
        }

        /// <summary>
        /// Renders a template by ID with the provided values.
        /// </summary>
        public async Task<string> RenderByIdAsync(
            string templateId,
            IDictionary<string, object> values,
            CancellationToken cancellationToken = default)
        {
            var template = await GetTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new KeyNotFoundException($"Template '{templateId}' not found.");
            }

            return await RenderAsync(template, values, cancellationToken);
        }

        /// <summary>
        /// Renders a template using simple placeholder replacement.
        /// </summary>
        /// <param name="templateBody">The template body with placeholders.</param>
        /// <param name="values">The values to replace placeholders.</param>
        /// <returns>The rendered message.</returns>
        /// <remarks>
        /// Simple placeholder syntax: {Name}, {Code}, etc.
        /// Placeholders are replaced with values from the dictionary (case-insensitive).
        /// </remarks>
        private static string RenderSimple(string templateBody, IDictionary<string, object> values)
        {
            var result = new StringBuilder(templateBody);
            var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in values)
            {
                var placeholder = $"{{{kvp.Key}}}";
                var value = kvp.Value?.ToString() ?? string.Empty;

                // Replace all occurrences of the placeholder
                result.Replace(placeholder, value);
                processedKeys.Add(kvp.Key);
            }

            return result.ToString();
        }
    }
}

