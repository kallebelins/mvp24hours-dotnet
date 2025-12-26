//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Contract
{
    /// <summary>
    /// Service interface for managing and rendering SMS templates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SMS templates allow you to define reusable message formats with placeholders that can be
    /// filled with dynamic values. This service provides functionality to store, retrieve, and
    /// render templates.
    /// </para>
    /// </remarks>
    public interface ISmsTemplateService
    {
        /// <summary>
        /// Gets a template by ID.
        /// </summary>
        /// <param name="templateId">The template ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The template, or null if not found.</returns>
        Task<SmsTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves or updates a template.
        /// </summary>
        /// <param name="template">The template to save.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveTemplateAsync(SmsTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a template.
        /// </summary>
        /// <param name="templateId">The template ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all templates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A collection of templates.</returns>
        Task<IList<SmsTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders a template with the provided values.
        /// </summary>
        /// <param name="template">The template to render.</param>
        /// <param name="values">The values to fill in the placeholders.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The rendered message body.</returns>
        /// <remarks>
        /// <para>
        /// This method replaces placeholders in the template with the provided values.
        /// The exact syntax depends on the template engine specified in the template.
        /// </para>
        /// <para>
        /// Example:
        /// Template: "Welcome {Name}! Your code is {Code}."
        /// Values: { Name = "John", Code = "123456" }
        /// Result: "Welcome John! Your code is 123456."
        /// </para>
        /// </remarks>
        Task<string> RenderAsync(
            SmsTemplate template,
            IDictionary<string, object> values,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Renders a template by ID with the provided values.
        /// </summary>
        /// <param name="templateId">The template ID.</param>
        /// <param name="values">The values to fill in the placeholders.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The rendered message body.</returns>
        Task<string> RenderByIdAsync(
            string templateId,
            IDictionary<string, object> values,
            CancellationToken cancellationToken = default);
    }
}

