//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Security.Contract
{
    /// <summary>
    /// Unified interface for retrieving secrets from different providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a consistent API for accessing secrets regardless of the
    /// underlying secret provider (Azure Key Vault, AWS Secrets Manager, Environment Variables, etc.).
    /// All operations are asynchronous and support cancellation tokens for proper resource management.
    /// </para>
    /// <para>
    /// <strong>Secret Naming:</strong>
    /// Secret names are provider-specific. For example:
    /// - Azure Key Vault: "MySecret" or "https://myvault.vault.azure.net/secrets/MySecret"
    /// - AWS Secrets Manager: "my/secret/name" or ARN
    /// - Environment Variables: "MY_SECRET" (case-sensitive on some platforms)
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// If a secret is not found, the method returns <c>null</c>. For provider errors or
    /// authentication failures, exceptions are thrown. Check the specific provider documentation
    /// for exception types.
    /// </para>
    /// <para>
    /// <strong>Secret Rotation:</strong>
    /// Some providers support automatic secret rotation. Use <see cref="ISecretRotationHelper"/>
    /// for managing secret rotation and versioning.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get a secret value
    /// var secretProvider = serviceProvider.GetRequiredService&lt;ISecretProvider&gt;();
    /// var apiKey = await secretProvider.GetSecretAsync("ApiKey", cancellationToken);
    /// if (apiKey != null)
    /// {
    ///     Console.WriteLine($"API Key retrieved: {apiKey.Substring(0, 4)}...");
    /// }
    /// 
    /// // Get multiple secrets
    /// var secrets = await secretProvider.GetSecretsAsync(new[] { "ApiKey", "DatabasePassword" }, cancellationToken);
    /// foreach (var secret in secrets)
    /// {
    ///     Console.WriteLine($"{secret.Key}: {secret.Value?.Substring(0, 4)}...");
    /// }
    /// </code>
    /// </example>
    public interface ISecretProvider
    {
        /// <summary>
        /// Gets a secret value by name.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The secret value, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves a single secret value. The secret name format depends on the provider.
        /// </para>
        /// <para>
        /// <strong>Return Value:</strong>
        /// Returns the secret value as a string. Returns <c>null</c> if the secret doesn't exist.
        /// </para>
        /// <para>
        /// <strong>Exceptions:</strong>
        /// Throws provider-specific exceptions for authentication failures, network errors,
        /// or other provider errors. Check the specific provider documentation for exception types.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when secretName is null or empty.</exception>
        Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple secret values by name.
        /// </summary>
        /// <param name="secretNames">The names of the secrets to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary mapping secret names to their values. Secrets not found will have <c>null</c> values.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves multiple secrets in a single operation. The implementation may
        /// optimize batch retrieval depending on the provider (e.g., using batch APIs when available).
        /// </para>
        /// <para>
        /// <strong>Partial Success:</strong>
        /// If some secrets are found and others are not, the dictionary will contain entries for all
        /// requested secrets, with <c>null</c> values for secrets that don't exist.
        /// </para>
        /// <para>
        /// <strong>Error Handling:</strong>
        /// If a provider error occurs during batch retrieval, the behavior depends on the provider.
        /// Some providers may return partial results, while others may throw exceptions.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when secretNames is null.</exception>
        Task<IDictionary<string, string?>> GetSecretsAsync(
            IEnumerable<string> secretNames,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a secret value with a specific version.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <param name="version">The version identifier of the secret.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The secret value for the specified version, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves a specific version of a secret. Version support depends on the provider.
        /// Some providers (like Azure Key Vault) support versioning, while others may not.
        /// </para>
        /// <para>
        /// <strong>Version Format:</strong>
        /// The version format is provider-specific:
        /// - Azure Key Vault: GUID or version string
        /// - AWS Secrets Manager: Version ID or staging label
        /// - Environment Variables: Not supported (returns latest)
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when secretName is null or empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when the provider doesn't support versioning.</exception>
        Task<string?> GetSecretVersionAsync(
            string secretName,
            string version,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a secret exists.
        /// </summary>
        /// <param name="secretName">The name of the secret to check.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the secret exists, <c>false</c> otherwise.</returns>
        /// <remarks>
        /// This method checks for secret existence without retrieving the value, which can be
        /// more efficient than calling <see cref="GetSecretAsync"/> and checking for null.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when secretName is null or empty.</exception>
        Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);
    }
}

