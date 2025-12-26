//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Security.Options
{
    /// <summary>
    /// Configuration options for secret provider operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control various aspects of secret retrieval behavior, including caching,
    /// retry policies, and provider-specific settings.
    /// </para>
    /// </remarks>
    public class SecretProviderOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretProviderOptions"/> class.
        /// </summary>
        public SecretProviderOptions()
        {
        }

        /// <summary>
        /// Gets or sets whether to cache secret values in memory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, secret values are cached in memory after first retrieval to reduce
        /// calls to the secret provider. This improves performance but means secrets won't reflect
        /// changes until the cache expires or is cleared.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (no caching) for security reasons. Enable caching only if
        /// you understand the security implications.
        /// </para>
        /// </remarks>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the cache expiration time for cached secrets.
        /// </summary>
        /// <remarks>
        /// This value is only used when <see cref="EnableCaching"/> is <c>true</c>.
        /// Default is 5 minutes.
        /// </remarks>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default timeout for secret retrieval operations.
        /// </summary>
        /// <remarks>
        /// If a secret retrieval operation takes longer than this timeout, it will be cancelled.
        /// Default is 30 seconds.
        /// </remarks>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to throw exceptions when secrets are not found.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (default), <see cref="Contract.ISecretProvider.GetSecretAsync"/> returns
        /// <c>null</c> when a secret is not found. When <c>true</c>, it throws an exception instead.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool ThrowOnNotFound { get; set; } = false;
    }
}

