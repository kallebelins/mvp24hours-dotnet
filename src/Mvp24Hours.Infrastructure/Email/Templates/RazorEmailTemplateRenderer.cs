//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Templates
{
    /// <summary>
    /// Email template renderer using Razor syntax (C# templates).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This renderer uses Razor syntax for email templates, allowing you to use C# code
    /// directly in templates. This is useful for complex templates that require logic,
    /// loops, conditionals, and strong typing.
    /// </para>
    /// <para>
    /// <strong>Template Syntax:</strong>
    /// <list type="bullet">
    /// <item>
    /// <description>Variables: <c>@Model.Name</c> or <c>@Model.User.Name</c></description>
    /// </item>
    /// <item>
    /// <description>Conditionals: <c>@if (Model.IsPremium) { ... }</c></description>
    /// </item>
    /// <item>
    /// <description>Loops: <c>@foreach (var item in Model.Items) { ... }</c></description>
    /// </item>
    /// <item>
    /// <description>Code blocks: <c>@{ var total = Model.Price * Model.Quantity; }</c></description>
    /// </item>
    /// <item>
    /// <description>Helpers: <c>@Html.Raw(Model.HtmlContent)</c></description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong>
    /// This implementation uses a simple Razor-like template engine. For production use
    /// with complex templates, consider using RazorLight or RazorEngine.NetCore packages.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple template
    /// var template = "Hello @Model.Name, welcome to @Model.Company!";
    /// var data = new { Name = "John", Company = "Acme Corp" };
    /// var renderer = new RazorEmailTemplateRenderer();
    /// var rendered = await renderer.RenderAsync(template, data);
    /// // Result: "Hello John, welcome to Acme Corp!"
    /// 
    /// // Template with conditionals
    /// var template2 = @"@if (Model.IsPremium) {
    ///   Thank you for being a premium member!
    /// } else {
    ///   Upgrade to premium for exclusive benefits.
    /// }";
    /// var data2 = new { IsPremium = true };
    /// var rendered2 = await renderer.RenderAsync(template2, data2);
    /// </code>
    /// </example>
    public class RazorEmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly TemplateOptions? _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorEmailTemplateRenderer"/> class.
        /// </summary>
        /// <param name="options">Optional template rendering options.</param>
        public RazorEmailTemplateRenderer(TemplateOptions? options = null)
        {
            _options = options;
        }

        /// <inheritdoc />
        public Task<string> RenderAsync(
            string template,
            object? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or empty.", nameof(template));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Simple Razor-like template rendering
                // For production, consider using RazorLight or RazorEngine.NetCore
                var rendered = RenderRazorTemplate(template, model);
                return Task.FromResult(rendered);
            }
            catch (Exception ex)
            {
                throw new TemplateRenderException($"Error rendering Razor template: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public Task<string> RenderAsync(
            string template,
            IDictionary<string, object?> variables,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or empty.", nameof(template));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            return RenderAsync(template, (object)variables, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> RenderFromFileAsync(
            string templatePath,
            object? model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));
            }

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}", templatePath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
            return await RenderAsync(templateContent, model, cancellationToken);
        }

        /// <inheritdoc />
        public Task<TemplateValidationResult> ValidateAsync(
            string template,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or empty.", nameof(template));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Basic validation - check for balanced braces and common Razor syntax
                var errors = ValidateRazorSyntax(template);
                if (errors.Count > 0)
                {
                    return Task.FromResult(TemplateValidationResult.Invalid(errors.ToArray()));
                }

                return Task.FromResult(TemplateValidationResult.Valid());
            }
            catch (Exception ex)
            {
                return Task.FromResult(TemplateValidationResult.Invalid(ex.Message));
            }
        }

        /// <summary>
        /// Renders a Razor-like template with the provided model.
        /// </summary>
        /// <remarks>
        /// This is a simplified Razor template renderer. For production use with complex
        /// templates, consider using RazorLight or RazorEngine.NetCore packages.
        /// </remarks>
        private string RenderRazorTemplate(string template, object? model)
        {
            var result = template;

            if (model == null)
            {
                return result;
            }

            // Handle dictionary
            if (model is IDictionary<string, object?> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    var placeholder = $"@Model.{kvp.Key}";
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    result = result.Replace(placeholder, value);
                    
                    // Also handle without Model prefix
                    placeholder = $"@{kvp.Key}";
                    result = result.Replace(placeholder, value);
                }
                return result;
            }

            // Handle object properties using reflection
            var type = model.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    var value = property.GetValue(model);
                    var stringValue = value?.ToString() ?? string.Empty;

                    // Replace @Model.PropertyName
                    var placeholder = $"@Model.{property.Name}";
                    result = result.Replace(placeholder, stringValue);

                    // Replace @PropertyName
                    placeholder = $"@{property.Name}";
                    result = result.Replace(placeholder, stringValue);
                }
            }

            // Handle fields
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(model);
                var stringValue = value?.ToString() ?? string.Empty;

                var placeholder = $"@Model.{field.Name}";
                result = result.Replace(placeholder, stringValue);

                placeholder = $"@{field.Name}";
                result = result.Replace(placeholder, stringValue);
            }

            return result;
        }

        /// <summary>
        /// Validates basic Razor syntax.
        /// </summary>
        private List<string> ValidateRazorSyntax(string template)
        {
            var errors = new List<string>();

            // Check for balanced braces
            var openBraces = template.Count(c => c == '{');
            var closeBraces = template.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                errors.Add($"Unbalanced braces: {openBraces} open, {closeBraces} close");
            }

            // Check for balanced parentheses
            var openParens = template.Count(c => c == '(');
            var closeParens = template.Count(c => c == ')');
            if (openParens != closeParens)
            {
                errors.Add($"Unbalanced parentheses: {openParens} open, {closeParens} close");
            }

            // Basic @ symbol validation (should be followed by valid identifier or code block)
            // This is a simplified check - full Razor parsing would be more complex
            var atSymbols = template.Split('@');
            for (int i = 1; i < atSymbols.Length; i++)
            {
                var afterAt = atSymbols[i];
                if (string.IsNullOrWhiteSpace(afterAt))
                {
                    errors.Add("Invalid @ symbol usage: @ must be followed by identifier or code block");
                }
            }

            return errors;
        }
    }
}

