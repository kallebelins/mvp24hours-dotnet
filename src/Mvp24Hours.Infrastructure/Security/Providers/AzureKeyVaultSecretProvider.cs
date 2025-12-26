//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Security.Providers
{
    /// <summary>
    /// Azure Key Vault secret provider implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider retrieves secrets from Azure Key Vault. It supports:
    /// - Managed Identity authentication (recommended for Azure-hosted apps)
    /// - Client secret authentication
    /// - Secret versioning
    /// - Batch retrieval
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// Requires the Azure.Security.KeyVault.Secrets NuGet package.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Azure Key Vault provider with Managed Identity
    /// services.AddAzureKeyVaultSecretProvider(options =>
    /// {
    ///     options.VaultUri = new Uri("https://myvault.vault.azure.net/");
    ///     options.UseManagedIdentity = true;
    /// });
    /// 
    /// // Use in code
    /// var secretProvider = serviceProvider.GetRequiredService&lt;ISecretProvider&gt;();
    /// var apiKey = await secretProvider.GetSecretAsync("ApiKey", cancellationToken);
    /// </code>
    /// </example>
    public class AzureKeyVaultSecretProvider : ISecretProvider
    {
        private readonly AzureKeyVaultOptions _options;
        private readonly ILogger<AzureKeyVaultSecretProvider>? _logger;
        private readonly Lazy<Azure.Security.KeyVault.Secrets.SecretClient> _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultSecretProvider"/> class.
        /// </summary>
        /// <param name="options">The Azure Key Vault options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public AzureKeyVaultSecretProvider(
            IOptions<AzureKeyVaultOptions> options,
            ILogger<AzureKeyVaultSecretProvider>? logger = null)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultSecretProvider"/> class.
        /// </summary>
        /// <param name="options">The Azure Key Vault options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public AzureKeyVaultSecretProvider(
            AzureKeyVaultOptions options,
            ILogger<AzureKeyVaultSecretProvider>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            if (_options.VaultUri == null)
            {
                throw new InvalidOperationException("Azure Key Vault URI must be configured.");
            }

            _client = new Lazy<Azure.Security.KeyVault.Secrets.SecretClient>(CreateClient);
        }

        /// <inheritdoc />
        public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            try
            {
                var response = await _client.Value.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                return response.Value.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger?.LogDebug("Secret '{SecretName}' not found in Azure Key Vault.", secretName);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving secret '{SecretName}' from Azure Key Vault.", secretName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, string?>> GetSecretsAsync(
            IEnumerable<string> secretNames,
            CancellationToken cancellationToken = default)
        {
            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var tasks = secretNames.Select(async name =>
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return new KeyValuePair<string, string?>(name, null);
                }

                var value = await GetSecretAsync(name, cancellationToken);
                return new KeyValuePair<string, string?>(name, value);
            });

            var results = await Task.WhenAll(tasks);
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<string?> GetSecretVersionAsync(
            string secretName,
            string version,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Version cannot be null or empty.", nameof(version));
            }

            try
            {
                var response = await _client.Value.GetSecretAsync(secretName, version, cancellationToken);
                return response.Value.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger?.LogDebug("Secret '{SecretName}' version '{Version}' not found in Azure Key Vault.", secretName, version);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving secret '{SecretName}' version '{Version}' from Azure Key Vault.", secretName, version);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            var value = await GetSecretAsync(secretName, cancellationToken);
            return value != null;
        }

        /// <summary>
        /// Creates the Azure Key Vault Secret Client.
        /// </summary>
        /// <returns>The Secret Client instance.</returns>
        private Azure.Security.KeyVault.Secrets.SecretClient CreateClient()
        {
            Azure.Core.TokenCredential credential;

            if (_options.UseManagedIdentity)
            {
                // Use Managed Identity
                credential = new Azure.Identity.DefaultAzureCredential(
                    new Azure.Identity.DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = _options.ManagedIdentityClientId
                    });
            }
            else
            {
                // Use client secret authentication
                if (string.IsNullOrWhiteSpace(_options.TenantId) ||
                    string.IsNullOrWhiteSpace(_options.ClientId) ||
                    string.IsNullOrWhiteSpace(_options.ClientSecret))
                {
                    throw new InvalidOperationException(
                        "Azure Key Vault authentication requires either Managed Identity or Client Secret credentials.");
                }

                credential = new Azure.Identity.ClientSecretCredential(
                    _options.TenantId,
                    _options.ClientId,
                    _options.ClientSecret);
            }

            return new Azure.Security.KeyVault.Secrets.SecretClient(_options.VaultUri, credential);
        }
    }
}

