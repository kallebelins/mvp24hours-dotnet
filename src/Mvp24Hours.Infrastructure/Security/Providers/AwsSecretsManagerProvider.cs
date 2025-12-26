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
    /// AWS Secrets Manager secret provider implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider retrieves secrets from AWS Secrets Manager. It supports:
    /// - IAM role authentication (recommended for AWS-hosted apps)
    /// - Access key authentication
    /// - Secret versioning
    /// - Batch retrieval
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// Requires the AWSSDK.SecretsManager NuGet package.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register AWS Secrets Manager provider with IAM role
    /// services.AddAwsSecretsManagerProvider(options =>
    /// {
    ///     options.Region = "us-east-1";
    ///     // IAM role will be used automatically if running on EC2/ECS/Lambda
    /// });
    /// 
    /// // Use in code
    /// var secretProvider = serviceProvider.GetRequiredService&lt;ISecretProvider&gt;();
    /// var apiKey = await secretProvider.GetSecretAsync("myapp/ApiKey", cancellationToken);
    /// </code>
    /// </example>
    public class AwsSecretsManagerProvider : ISecretProvider
    {
        private readonly AwsSecretsManagerOptions _options;
        private readonly ILogger<AwsSecretsManagerProvider>? _logger;
        private readonly Lazy<Amazon.SecretsManager.AmazonSecretsManagerClient> _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwsSecretsManagerProvider"/> class.
        /// </summary>
        /// <param name="options">The AWS Secrets Manager options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public AwsSecretsManagerProvider(
            IOptions<AwsSecretsManagerOptions> options,
            ILogger<AwsSecretsManagerProvider>? logger = null)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)), logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AwsSecretsManagerProvider"/> class.
        /// </summary>
        /// <param name="options">The AWS Secrets Manager options.</param>
        /// <param name="logger">Optional logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public AwsSecretsManagerProvider(
            AwsSecretsManagerOptions options,
            ILogger<AwsSecretsManagerProvider>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _client = new Lazy<Amazon.SecretsManager.AmazonSecretsManagerClient>(CreateClient);
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
                var fullSecretName = GetFullSecretName(secretName);
                var request = new Amazon.SecretsManager.Model.GetSecretValueRequest
                {
                    SecretId = fullSecretName
                };

                var response = await _client.Value.GetSecretValueAsync(request, cancellationToken);
                return response.SecretString;
            }
            catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
            {
                _logger?.LogDebug("Secret '{SecretName}' not found in AWS Secrets Manager.", secretName);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving secret '{SecretName}' from AWS Secrets Manager.", secretName);
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
                var fullSecretName = GetFullSecretName(secretName);
                var request = new Amazon.SecretsManager.Model.GetSecretValueRequest
                {
                    SecretId = fullSecretName,
                    VersionId = version
                };

                var response = await _client.Value.GetSecretValueAsync(request, cancellationToken);
                return response.SecretString;
            }
            catch (Amazon.SecretsManager.Model.ResourceNotFoundException)
            {
                _logger?.LogDebug("Secret '{SecretName}' version '{Version}' not found in AWS Secrets Manager.", secretName, version);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving secret '{SecretName}' version '{Version}' from AWS Secrets Manager.", secretName, version);
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
        /// Gets the full secret name with prefix applied.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The full secret name.</returns>
        private string GetFullSecretName(string secretName)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretNamePrefix))
            {
                return secretName;
            }

            return _options.SecretNamePrefix.TrimEnd('/') + "/" + secretName;
        }

        /// <summary>
        /// Creates the AWS Secrets Manager client.
        /// </summary>
        /// <returns>The Secrets Manager client instance.</returns>
        private Amazon.SecretsManager.AmazonSecretsManagerClient CreateClient()
        {
            var config = new Amazon.SecretsManager.AmazonSecretsManagerConfig();

            if (!string.IsNullOrWhiteSpace(_options.Region))
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);
            }

            Amazon.Runtime.AWSCredentials credentials;

            if (!string.IsNullOrWhiteSpace(_options.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(_options.SecretAccessKey))
            {
                // Use access key authentication
                if (!string.IsNullOrWhiteSpace(_options.SessionToken))
                {
                    credentials = new Amazon.Runtime.SessionAWSCredentials(
                        _options.AccessKeyId,
                        _options.SecretAccessKey,
                        _options.SessionToken);
                }
                else
                {
                    credentials = new Amazon.Runtime.BasicAWSCredentials(
                        _options.AccessKeyId,
                        _options.SecretAccessKey);
                }
            }
            else
            {
                // Use default credential chain (IAM role, environment variables, etc.)
                credentials = new Amazon.Runtime.DefaultInstanceProfileAWSCredentials();
            }

            return new Amazon.SecretsManager.AmazonSecretsManagerClient(credentials, config);
        }
    }
}

