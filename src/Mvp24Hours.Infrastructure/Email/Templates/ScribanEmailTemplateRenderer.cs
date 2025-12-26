//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Templates
{
    /// <summary>
    /// Email template renderer using Scriban (Liquid-like syntax).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scriban is a fast, lightweight, and secure template engine for .NET. It uses a
    /// Liquid-like syntax that is easy to learn and safe for non-developers to use.
    /// </para>
    /// <para>
    /// <strong>Template Syntax:</strong>
    /// <list type="bullet">
    /// <item>
    /// <description>Variables: <c>{{ name }}</c> or <c>{{ user.name }}</c></description>
    /// </item>
    /// <item>
    /// <description>Conditionals: <c>{% if condition %}...{% end %}</c></description>
    /// </item>
    /// <item>
    /// <description>Loops: <c>{% for item in items %}...{% end %}</c></description>
    /// </item>
    /// <item>
    /// <description>Filters: <c>{{ name | upcase }}</c> or <c>{{ price | string.format "C" }}</c></description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Security:</strong>
    /// Scriban templates are safe by default - they cannot execute arbitrary code or access
    /// system resources. Only the data provided in the model is accessible.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple template
    /// var template = "Hello {{ name }}, welcome to {{ company }}!";
    /// var data = new { name = "John", company = "Acme Corp" };
    /// var renderer = new ScribanEmailTemplateRenderer();
    /// var rendered = await renderer.RenderAsync(template, data);
    /// // Result: "Hello John, welcome to Acme Corp!"
    /// 
    /// // Template with conditionals
    /// var template2 = @"{% if isPremium %}
    ///   Thank you for being a premium member!
    /// {% else %}
    ///   Upgrade to premium for exclusive benefits.
    /// {% end %}";
    /// var data2 = new { isPremium = true };
    /// var rendered2 = await renderer.RenderAsync(template2, data2);
    /// </code>
    /// </example>
    public class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly TemplateOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScribanEmailTemplateRenderer"/> class.
        /// </summary>
        /// <param name="options">Optional template rendering options.</param>
        public ScribanEmailTemplateRenderer(TemplateOptions? options = null)
        {
            _options = options ?? new TemplateOptions();
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
                var scribanTemplate = Template.Parse(template);
                if (scribanTemplate.HasErrors)
                {
                    var errors = scribanTemplate.Messages.Select(m => m.ToString()).ToList();
                    throw new TemplateRenderException($"Template parsing errors: {string.Join("; ", errors)}", errors);
                }

                var scriptObject = CreateScriptObject(model);
                var context = new TemplateContext
                {
                    MemberRenamer = member => member.Name,
                    MemberFilter = member => true
                };
                context.PushGlobal(scriptObject);

                var result = scribanTemplate.Render(context);
                return Task.FromResult(result);
            }
            catch (Exception ex) when (!(ex is TemplateRenderException))
            {
                throw new TemplateRenderException($"Error rendering template: {ex.Message}", ex);
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
                var scribanTemplate = Template.Parse(template);
                if (scribanTemplate.HasErrors)
                {
                    var errors = scribanTemplate.Messages.Select(m => m.ToString()).ToList();
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
        /// Creates a Scriban script object from a model.
        /// </summary>
        private ScriptObject CreateScriptObject(object? model)
        {
            var scriptObject = new ScriptObject();

            if (model == null)
            {
                return scriptObject;
            }

            // Handle dictionary
            if (model is IDictionary<string, object?> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    scriptObject[kvp.Key] = kvp.Value;
                }
                return scriptObject;
            }

            // Handle object properties
            var type = model.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    var value = property.GetValue(model);
                    scriptObject[property.Name] = value;
                }
            }

            // Handle fields
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(model);
                scriptObject[field.Name] = value;
            }

            return scriptObject;
        }
    }

    /// <summary>
    /// Options for template rendering.
    /// </summary>
    public class TemplateOptions
    {
        /// <summary>
        /// Gets or sets whether to enable strict mode (throw on missing variables).
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the default value for missing variables.
        /// </summary>
        public string? DefaultValueForMissingVariables { get; set; }
    }

    /// <summary>
    /// Exception thrown when template rendering fails.
    /// </summary>
    public class TemplateRenderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateRenderException"/> class.
        /// </summary>
        public TemplateRenderException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateRenderException"/> class.
        /// </summary>
        public TemplateRenderException(string message, IList<string> errors) : base(message)
        {
            Errors = errors ?? new List<string> { message };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateRenderException"/> class.
        /// </summary>
        public TemplateRenderException(string message, Exception innerException) : base(message, innerException)
        {
            Errors = new List<string> { message };
        }

        /// <summary>
        /// Gets the list of template errors.
        /// </summary>
        public IList<string> Errors { get; }
    }
}

