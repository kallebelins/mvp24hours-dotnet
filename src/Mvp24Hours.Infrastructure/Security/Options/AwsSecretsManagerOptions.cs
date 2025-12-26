//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Security.Options
{
    /// <summary>
    /// Configuration options for AWS Secrets Manager secret provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the connection and behavior when using AWS Secrets Manager
    /// as the secret provider.
    /// </para>
    /// </remarks>
    public class AwsSecretsManagerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AwsSecretsManagerOptions"/> class.
        /// </summary>
        public AwsSecretsManagerOptions()
        {
        }

        /// <summary>
        /// Gets or sets the AWS region where the secrets are stored.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The AWS region name (e.g., "us-east-1", "eu-west-1"). This is required for
        /// AWS Secrets Manager operations.
        /// </para>
        /// <para>
        /// If not specified, the provider will attempt to use the default region from
        /// AWS SDK configuration (environment variables, credentials file, etc.).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.Region = "us-east-1";
        /// </code>
        /// </example>
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets the AWS access key ID for authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Required when using access key authentication. Not required when using IAM roles,
        /// instance profiles, or other AWS credential providers.
        /// </para>
        /// <para>
        /// For production, prefer using IAM roles or instance profiles instead of hardcoding
        /// access keys.
        /// </para>
        /// </remarks>
        public string? AccessKeyId { get; set; }

        /// <summary>
        /// Gets or sets the AWS secret access key for authentication.
        /// </summary>
        /// <remarks>
        /// Required when using access key authentication. Must be used together with
        /// <see cref="AccessKeyId"/>.
        /// </remarks>
        public string? SecretAccessKey { get; set; }

        /// <summary>
        /// Gets or sets the AWS session token for temporary credentials.
        /// </summary>
        /// <remarks>
        /// Used when authenticating with temporary credentials (e.g., STS assume role).
        /// Not required for permanent access keys or IAM roles.
        /// </remarks>
        public string? SessionToken { get; set; }

        /// <summary>
        /// Gets or sets the default secret name prefix.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When specified, this prefix is automatically prepended to all secret names
        /// when retrieving secrets. Useful for organizing secrets by environment or application.
        /// </para>
        /// <para>
        /// Example: If prefix is "prod/myapp/", secret name "ApiKey" becomes "prod/myapp/ApiKey".
        /// </para>
        /// </remarks>
        public string? SecretNamePrefix { get; set; }
    }
}

