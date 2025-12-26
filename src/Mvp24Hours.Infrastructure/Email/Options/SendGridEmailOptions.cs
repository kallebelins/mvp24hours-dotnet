//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Email.Options
{
    /// <summary>
    /// Configuration options for SendGrid email provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the SendGrid API client, including API key, default sender,
    /// and SendGrid-specific features like categories and tracking.
    /// </para>
    /// </remarks>
    public class SendGridEmailOptions
    {
        /// <summary>
        /// Gets or sets the SendGrid API key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the API key obtained from your SendGrid account. You can create API keys
        /// in the SendGrid dashboard under Settings > API Keys.
        /// </para>
        /// <para>
        /// <strong>Security:</strong>
        /// Store this value securely (e.g., in Azure Key Vault, AWS Secrets Manager, or
        /// environment variables). Never commit API keys to source control.
        /// </para>
        /// <para>
        /// This property is required.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        /// </code>
        /// </example>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default sender email address.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the default sender email address used when not specified in the email message.
        /// The email address must be verified in your SendGrid account.
        /// </para>
        /// <para>
        /// Can be in format "user@example.com" or "Display Name &lt;user@example.com&gt;".
        /// </para>
        /// </remarks>
        public string? DefaultFrom { get; set; }

        /// <summary>
        /// Gets or sets the default sender name.
        /// </summary>
        /// <remarks>
        /// This is the display name used when only an email address is provided for the sender.
        /// </remarks>
        public string? DefaultFromName { get; set; }

        /// <summary>
        /// Gets or sets the default categories for tracking emails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Categories help you track email performance in SendGrid. You can use categories
        /// to group emails by type (e.g., "welcome", "password-reset", "newsletter").
        /// </para>
        /// <para>
        /// Categories specified here are added to all emails sent through this provider,
        /// unless overridden in the individual email message.
        /// </para>
        /// </remarks>
        public IList<string> DefaultCategories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to enable click tracking by default.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, SendGrid will replace links in emails with tracking links to monitor
        /// click-through rates. This can be overridden per email message.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool EnableClickTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable open tracking by default.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, SendGrid will add a tracking pixel to emails to monitor open rates.
        /// This can be overridden per email message.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool EnableOpenTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets the SendGrid API base URL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the base URL for the SendGrid API. The default is "https://api.sendgrid.com/v3".
        /// You typically don't need to change this unless you're using a custom SendGrid endpoint.
        /// </para>
        /// </remarks>
        public string ApiBaseUrl { get; set; } = "https://api.sendgrid.com/v3";

        /// <summary>
        /// Validates the SendGrid configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                errors.Add("SendGrid API Key is required.");
            }

            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                errors.Add("SendGrid API Base URL cannot be empty.");
            }

            if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
            {
                errors.Add($"SendGrid API Base URL is not a valid URI: {ApiBaseUrl}");
            }

            return errors;
        }
    }
}

