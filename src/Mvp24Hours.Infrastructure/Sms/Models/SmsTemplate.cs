//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Sms.Models
{
    /// <summary>
    /// Represents an SMS template for reusable message formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SMS templates allow you to define reusable message formats with placeholders that can be
    /// filled with dynamic values. This is useful for standardizing messages, ensuring compliance,
    /// and simplifying message creation.
    /// </para>
    /// <para>
    /// <strong>Template Syntax:</strong>
    /// Templates use placeholders (e.g., {Name}, {Code}, {Date}) that are replaced with actual
    /// values when rendering. The exact syntax depends on the template engine used.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a template
    /// var template = new SmsTemplate
    /// {
    ///     Id = "welcome-message",
    ///     Name = "Welcome Message",
    ///     Body = "Welcome {Name}! Your verification code is {Code}. Valid until {ExpiryDate}."
    /// };
    /// 
    /// // Render with values
    /// var message = templateService.Render(template, new Dictionary&lt;string, object&gt;
    /// {
    ///     { "Name", "John" },
    ///     { "Code", "123456" },
    ///     { "ExpiryDate", DateTime.Now.AddMinutes(10) }
    /// });
    /// </code>
    /// </example>
    public class SmsTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsTemplate"/> class.
        /// </summary>
        public SmsTemplate()
        {
        }

        /// <summary>
        /// Gets or sets the unique identifier of the template.
        /// </summary>
        /// <remarks>
        /// This ID is used to retrieve and reference the template.
        /// </remarks>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        /// <remarks>
        /// A human-readable name for the template (e.g., "Welcome Message", "Password Reset").
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the template body with placeholders.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The template body contains the message text with placeholders that will be replaced
        /// with actual values during rendering. Placeholder syntax depends on the template engine:
        /// - Simple: {Name}, {Code}
        /// - Scriban/Liquid: {{ name }}, {{ code }}
        /// - Razor: @Model.Name, @Model.Code
        /// </para>
        /// <para>
        /// Example: "Welcome {Name}! Your code is {Code}."
        /// </para>
        /// </remarks>
        public string? Body { get; set; }

        /// <summary>
        /// Gets or sets the default sender (From) for messages using this template.
        /// </summary>
        /// <remarks>
        /// If specified, this sender will be used when sending messages with this template,
        /// unless overridden in the message.
        /// </remarks>
        public string? DefaultFrom { get; set; }

        /// <summary>
        /// Gets or sets the template engine to use for rendering.
        /// </summary>
        /// <remarks>
        /// Specifies which template engine should be used to render this template:
        /// - "Simple": Basic placeholder replacement ({Name} → value)
        /// - "Scriban": Scriban/Liquid template engine
        /// - "Razor": Razor template engine
        /// </remarks>
        public string? TemplateEngine { get; set; } = "Simple";

        /// <summary>
        /// Gets or sets metadata associated with the template.
        /// </summary>
        /// <remarks>
        /// Can be used to store additional information about the template (e.g., category,
        /// tags, version, author).
        /// </remarks>
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets when the template was created.
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the template was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        /// <summary>
        /// Validates the template.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
            {
                errors.Add("Template ID is required.");
            }

            if (string.IsNullOrWhiteSpace(Body))
            {
                errors.Add("Template body is required.");
            }

            return errors;
        }
    }
}

