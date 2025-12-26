//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Security.Options
{
    /// <summary>
    /// Configuration options for Azure Key Vault secret provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the connection and behavior when using Azure Key Vault
    /// as the secret provider.
    /// </para>
    /// </remarks>
    public class AzureKeyVaultOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultOptions"/> class.
        /// </summary>
        public AzureKeyVaultOptions()
        {
        }

        /// <summary>
        /// Gets or sets the Azure Key Vault URI.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The URI of your Azure Key Vault. Format: https://{vault-name}.vault.azure.net/
        /// </para>
        /// <para>
        /// This is required for Azure Key Vault provider.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.VaultUri = new Uri("https://myvault.vault.azure.net/");
        /// </code>
        /// </example>
        public Uri? VaultUri { get; set; }

        /// <summary>
        /// Gets or sets the Azure Key Vault URI as a string.
        /// </summary>
        /// <remarks>
        /// Convenience property for setting the vault URI from configuration strings.
        /// </remarks>
        public string? VaultUriString
        {
            get => VaultUri?.ToString();
            set => VaultUri = string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
        }

        /// <summary>
        /// Gets or sets the Azure AD tenant ID for authentication.
        /// </summary>
        /// <remarks>
        /// Required when using client secret or certificate authentication.
        /// Not required when using Managed Identity.
        /// </remarks>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Azure AD client ID for authentication.
        /// </summary>
        /// <remarks>
        /// Required when using client secret or certificate authentication.
        /// Not required when using Managed Identity.
        /// </remarks>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the Azure AD client secret for authentication.
        /// </summary>
        /// <remarks>
        /// Required when using client secret authentication.
        /// Not required when using Managed Identity or certificate authentication.
        /// </remarks>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets whether to use Managed Identity for authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the provider will use Managed Identity (system-assigned or user-assigned)
        /// for authentication. This is the recommended approach for Azure-hosted applications.
        /// </para>
        /// <para>
        /// Default is <c>false</c>. Set to <c>true</c> when running on Azure App Service, Azure Functions,
        /// Azure VM, or other Azure services that support Managed Identity.
        /// </para>
        /// </remarks>
        public bool UseManagedIdentity { get; set; } = false;

        /// <summary>
        /// Gets or sets the user-assigned managed identity client ID.
        /// </summary>
        /// <remarks>
        /// Only used when <see cref="UseManagedIdentity"/> is <c>true</c> and using a user-assigned
        /// managed identity. Leave <c>null</c> for system-assigned managed identity.
        /// </remarks>
        public string? ManagedIdentityClientId { get; set; }
    }
}

