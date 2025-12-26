//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Security.Providers
{
    /// <summary>
    /// Environment Variable secret provider implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider retrieves secrets from environment variables. It is useful for:
    /// - Development and testing (simple configuration)
    /// - Containerized applications (Docker, Kubernetes)
    /// - CI/CD pipelines (secrets injected as environment variables)
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// - No versioning support (environment variables don't have versions)
    /// - Secrets are visible in process memory and environment listings
    /// - Not suitable for high-security scenarios (use Azure Key Vault or AWS Secrets Manager)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register environment variable provider
    /// services.AddEnvironmentVariableSecretProvider(options =>
    /// {
    ///     options.VariableNamePrefix = "MYAPP_";
    ///     options.CaseSensitive = false;
    /// });
    /// 
    /// // Use in code
    /// var secretProvider = serviceProvider.GetRequiredService&lt;ISecretProvider&gt;();
    /// var apiKey = await secretProvider.GetSecretAsync("ApiKey", cancellationToken);
    /// // Reads from environment variable "MYAPP_ApiKey"
    /// </code>
    /// </example>
    public class EnvironmentVariableSecretProvider : ISecretProvider
    {
        private readonly EnvironmentVariableOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableSecretProvider"/> class.
        /// </summary>
        /// <param name="options">The environment variable options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public EnvironmentVariableSecretProvider(IOptions<EnvironmentVariableOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableSecretProvider"/> class.
        /// </summary>
        /// <param name="options">The environment variable options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public EnvironmentVariableSecretProvider(EnvironmentVariableOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            var variableName = GetVariableName(secretName);
            var value = Environment.GetEnvironmentVariable(variableName, _options.Target);

            return Task.FromResult<string?>(value);
        }

        /// <inheritdoc />
        public Task<IDictionary<string, string?>> GetSecretsAsync(
            IEnumerable<string> secretNames,
            CancellationToken cancellationToken = default)
        {
            if (secretNames == null)
            {
                throw new ArgumentNullException(nameof(secretNames));
            }

            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var secretName in secretNames)
            {
                if (string.IsNullOrWhiteSpace(secretName))
                {
                    continue;
                }

                var variableName = GetVariableName(secretName);
                var value = Environment.GetEnvironmentVariable(variableName, _options.Target);
                result[secretName] = value;
            }

            return Task.FromResult<IDictionary<string, string?>>(result);
        }

        /// <inheritdoc />
        public Task<string?> GetSecretVersionAsync(
            string secretName,
            string version,
            CancellationToken cancellationToken = default)
        {
            // Environment variables don't support versioning
            // Return the current value (same as GetSecretAsync)
            return GetSecretAsync(secretName, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            var variableName = GetVariableName(secretName);
            var value = Environment.GetEnvironmentVariable(variableName, _options.Target);

            return Task.FromResult(value != null);
        }

        /// <summary>
        /// Gets the environment variable name from the secret name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The environment variable name.</returns>
        private string GetVariableName(string secretName)
        {
            var variableName = secretName;

            // Apply prefix if specified
            if (!string.IsNullOrWhiteSpace(_options.VariableNamePrefix))
            {
                variableName = _options.VariableNamePrefix + variableName;
            }

            // Handle case sensitivity
            if (!_options.CaseSensitive && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // On Windows, environment variables are case-insensitive
                // Find the actual variable name by checking all environment variables
                var envVars = Environment.GetEnvironmentVariables(_options.Target);
                foreach (string key in envVars.Keys)
                {
                    if (string.Equals(key, variableName, StringComparison.OrdinalIgnoreCase))
                    {
                        return key; // Return the actual case from environment
                    }
                }
            }

            return variableName;
        }
    }
}

