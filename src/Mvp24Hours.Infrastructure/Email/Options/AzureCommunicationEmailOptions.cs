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
    /// Configuration options for Azure Communication Services email provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the Azure Communication Services Email client, including
    /// connection string, endpoint, and Azure-specific settings.
    /// </para>
    /// </remarks>
    public class AzureCommunicationEmailOptions
    {
        /// <summary>
        /// Gets or sets the Azure Communication Services connection string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the connection string obtained from your Azure Communication Services resource.
        /// You can find it in the Azure Portal under your Communication Services resource > Keys.
        /// </para>
        /// <para>
        /// The connection string format is: "endpoint=https://...;accesskey=..."
        /// </para>
        /// <para>
        /// <strong>Security:</strong>
        /// Store this value securely (e.g., in Azure Key Vault, AWS Secrets Manager, or
        /// environment variables). Never commit connection strings to source control.
        /// </para>
        /// <para>
        /// This property is required.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");
        /// </code>
        /// </example>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Azure Communication Services endpoint URL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the endpoint URL for your Azure Communication Services resource.
        /// It's typically in the format: "https://{resource-name}.communication.azure.com"
        /// </para>
        /// <para>
        /// If not specified, it will be extracted from the connection string.
        /// </para>
        /// </remarks>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the default sender email address.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the default sender email address used when not specified in the email message.
        /// The email address must be verified in your Azure Communication Services resource.
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
        /// Gets or sets whether to enable user engagement tracking.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, Azure Communication Services will track email opens and clicks.
        /// This can be overridden per email message.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool EnableUserEngagementTracking { get; set; } = true;

        /// <summary>
        /// Validates the Azure Communication Services configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                errors.Add("Azure Communication Services Connection String is required.");
            }
            else
            {
                // Validate connection string format
                if (!ConnectionString.Contains("endpoint=") || !ConnectionString.Contains("accesskey="))
                {
                    errors.Add("Azure Communication Services Connection String must contain 'endpoint' and 'accesskey'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(Endpoint) && !Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            {
                errors.Add($"Azure Communication Services Endpoint is not a valid URI: {Endpoint}");
            }

            return errors;
        }
    }
}

