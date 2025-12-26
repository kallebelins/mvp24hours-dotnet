//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Sms.Options
{
    /// <summary>
    /// Configuration options for Azure Communication Services SMS provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the Azure Communication Services SMS client, including
    /// connection string, endpoint, and Azure-specific settings.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the Azure Communication Services SMS NuGet package. Install it via:
    /// <c>dotnet add package Azure.Communication.Sms</c>
    /// </para>
    /// </remarks>
    public class AzureCommunicationSmsOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommunicationSmsOptions"/> class.
        /// </summary>
        public AzureCommunicationSmsOptions()
        {
        }

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
        /// Gets or sets the Azure Communication Services access key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the access key for your Azure Communication Services resource.
        /// If not specified, it will be extracted from the connection string.
        /// </para>
        /// <para>
        /// <strong>Security:</strong>
        /// Store this value securely. Never commit access keys to source control.
        /// </para>
        /// </remarks>
        public string? AccessKey { get; set; }

        /// <summary>
        /// Gets or sets whether to enable delivery reports.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, the provider will request delivery reports from Azure Communication Services.
        /// Delivery reports are received via webhooks configured in Azure Portal.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool EnableDeliveryReports { get; set; } = false;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                errors.Add("Azure Communication Services connection string is required.");
            }

            return errors;
        }
    }
}

