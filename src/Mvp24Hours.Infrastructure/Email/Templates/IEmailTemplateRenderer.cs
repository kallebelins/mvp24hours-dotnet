//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Templates
{
    /// <summary>
    /// Interface for rendering email templates with data binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a unified way to render email templates using different
    /// template engines (Razor, Scriban/Liquid, etc.). Templates can include placeholders
    /// that are replaced with actual data at runtime.
    /// </para>
    /// <para>
    /// <strong>Template Engines Supported:</strong>
    /// - Scriban (Liquid-like syntax) - Recommended for simple templates
    /// - Razor (C# syntax) - For complex templates with C# logic
    /// </para>
    /// <para>
    /// <strong>Template Syntax Examples:</strong>
    /// <list type="bullet">
    /// <item>
    /// <description>Scriban: <c>Hello {{ name }}, welcome to {{ company }}!</c></description>
    /// </item>
    /// <item>
    /// <description>Razor: <c>Hello @Model.Name, welcome to @Model.Company!</c></description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Render a Scriban template
    /// var template = "Hello {{ name }}, your order #{{ orderId }} is ready!";
    /// var data = new { name = "John", orderId = 12345 };
    /// var rendered = await renderer.RenderAsync(template, data, cancellationToken);
    /// 
    /// // Use in email
    /// message.HtmlBody = rendered;
    /// </code>
    /// </example>
    public interface IEmailTemplateRenderer
    {
        /// <summary>
        /// Renders a template string with the provided data model.
        /// </summary>
        /// <param name="template">The template string with placeholders.</param>
        /// <param name="model">The data model to bind to the template.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The rendered template string.</returns>
        /// <remarks>
        /// <para>
        /// This method processes the template and replaces placeholders with values from
        /// the model. The model can be an object, dictionary, or anonymous type.
        /// </para>
        /// <para>
        /// <strong>Model Binding:</strong>
        /// The renderer will attempt to access properties/keys from the model. For example:
        /// - Object properties: <c>model.Name</c> → <c>{{ name }}</c> or <c>@Model.Name</c>
        /// - Dictionary keys: <c>model["key"]</c> → <c>{{ key }}</c> or <c>@Model["key"]</c>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
        Task<string> RenderAsync(
            string template,
            object? model = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders a template string with the provided dictionary of variables.
        /// </summary>
        /// <param name="template">The template string with placeholders.</param>
        /// <param name="variables">Dictionary of variable names and values.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The rendered template string.</returns>
        /// <remarks>
        /// <para>
        /// This overload is useful when you have a dictionary of variables rather than
        /// a strongly-typed model object.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
        Task<string> RenderAsync(
            string template,
            IDictionary<string, object?> variables,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders a template from a file path with the provided data model.
        /// </summary>
        /// <param name="templatePath">Path to the template file.</param>
        /// <param name="model">The data model to bind to the template.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The rendered template string.</returns>
        /// <remarks>
        /// <para>
        /// This method loads the template from a file and renders it with the provided model.
        /// Useful for storing templates in separate files for better organization.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when templatePath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when template file is not found.</exception>
        Task<string> RenderFromFileAsync(
            string templatePath,
            object? model = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a template string for syntax errors.
        /// </summary>
        /// <param name="template">The template string to validate.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Validation result with any errors found.</returns>
        /// <remarks>
        /// <para>
        /// This method checks the template syntax without rendering it. Useful for validating
        /// templates before storing them or before attempting to render with data.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
        Task<TemplateValidationResult> ValidateAsync(
            string template,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of template validation.
    /// </summary>
    public class TemplateValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Whether the template is valid.</param>
        /// <param name="errors">List of validation errors, if any.</param>
        public TemplateValidationResult(bool isValid, IList<string>? errors = null)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }

        /// <summary>
        /// Gets whether the template is valid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the list of validation errors, if any.
        /// </summary>
        public IList<string> Errors { get; }

        /// <summary>
        /// Creates a valid template validation result.
        /// </summary>
        public static TemplateValidationResult Valid() => new(true);

        /// <summary>
        /// Creates an invalid template validation result with errors.
        /// </summary>
        public static TemplateValidationResult Invalid(params string[] errors) => 
            new(false, errors);
    }
}

