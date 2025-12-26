//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Contract;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Security.Helpers
{
    /// <summary>
    /// Default implementation of secret rotation helper.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper provides basic secret rotation functionality. Provider-specific implementations
    /// may provide more advanced features like automatic rotation scheduling.
    /// </para>
    /// </remarks>
    public class SecretRotationHelper : ISecretRotationHelper
    {
        private readonly ISecretProvider _secretProvider;
        private readonly ILogger<SecretRotationHelper>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretRotationHelper"/> class.
        /// </summary>
        /// <param name="secretProvider">The secret provider.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when secretProvider is null.</exception>
        public SecretRotationHelper(
            ISecretProvider secretProvider,
            ILogger<SecretRotationHelper>? logger = null)
        {
            _secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> NeedsRotationAsync(
            string secretName,
            TimeSpan maxAge,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            var lastRotation = await GetLastRotationDateAsync(secretName, cancellationToken);
            if (lastRotation == null)
            {
                // If we can't determine the last rotation date, assume it needs rotation
                return true;
            }

            var age = DateTime.UtcNow - lastRotation.Value;
            return age > maxAge;
        }

        /// <inheritdoc />
        public async Task<string> RotateSecretAsync(
            string secretName,
            Func<Task<string>> generateNewSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            if (generateNewSecret == null)
            {
                throw new ArgumentNullException(nameof(generateNewSecret));
            }

            _logger?.LogInformation("Rotating secret '{SecretName}'.", secretName);

            var newSecret = await generateNewSecret();
            if (string.IsNullOrWhiteSpace(newSecret))
            {
                throw new InvalidOperationException("Generated secret cannot be null or empty.");
            }

            // Note: This is a basic implementation. Provider-specific implementations should
            // handle the actual secret update. This helper is mainly for coordination.
            _logger?.LogInformation("Secret '{SecretName}' rotation completed.", secretName);

            return newSecret;
        }

        /// <inheritdoc />
        public Task<DateTime?> GetSecretCreationDateAsync(
            string secretName,
            CancellationToken cancellationToken = default)
        {
            // Default implementation doesn't support creation date retrieval
            // Provider-specific implementations should override this
            return Task.FromResult<DateTime?>(null);
        }

        /// <inheritdoc />
        public Task<DateTime?> GetLastRotationDateAsync(
            string secretName,
            CancellationToken cancellationToken = default)
        {
            // Default implementation doesn't support rotation date retrieval
            // Provider-specific implementations should override this
            return Task.FromResult<DateTime?>(null);
        }
    }
}

