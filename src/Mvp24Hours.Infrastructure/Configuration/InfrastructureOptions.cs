//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Observability.Extensions;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Sms.Options;
using System;

namespace Mvp24Hours.Infrastructure.Configuration
{
    /// <summary>
    /// Unified configuration options for all Infrastructure subsystems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class aggregates configuration options for all Infrastructure subsystems,
    /// allowing centralized configuration via IConfiguration or fluent API.
    /// </para>
    /// <para>
    /// Configuration can be loaded from appsettings.json:
    /// <code>
    /// {
    ///   "Infrastructure": {
    ///     "Http": { ... },
    ///     "Email": { ... },
    ///     "Sms": { ... },
    ///     "FileStorage": { ... },
    ///     "DistributedLocking": { ... },
    ///     "BackgroundJobs": { ... },
    ///     "Observability": { ... },
    ///     "Resilience": { ... },
    ///     "Security": { ... }
    ///   }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class InfrastructureOptions
    {
        /// <summary>
        /// Gets or sets HTTP client configuration options.
        /// </summary>
        public Http.Options.HttpClientOptions? Http { get; set; }

        /// <summary>
        /// Gets or sets email service configuration options.
        /// </summary>
        public Email.Options.EmailOptions? Email { get; set; }

        /// <summary>
        /// Gets or sets SMS service configuration options.
        /// </summary>
        public Sms.Options.SmsOptions? Sms { get; set; }

        /// <summary>
        /// Gets or sets file storage configuration options.
        /// </summary>
        public FileStorage.Options.FileStorageOptions? FileStorage { get; set; }

        /// <summary>
        /// Gets or sets distributed locking configuration options.
        /// </summary>
        public DistributedLockOptions? DistributedLocking { get; set; }

        /// <summary>
        /// Gets or sets background jobs configuration options.
        /// </summary>
        public JobOptions? BackgroundJobs { get; set; }

        /// <summary>
        /// Gets or sets observability configuration options.
        /// </summary>
        public ObservabilityOptions? Observability { get; set; }

        /// <summary>
        /// Gets or sets resilience configuration options.
        /// </summary>
        public ResilienceOptions? Resilience { get; set; }

        /// <summary>
        /// Gets or sets security/secret provider configuration options.
        /// </summary>
        public SecurityOptions? Security { get; set; }

        /// <summary>
        /// Gets or sets whether to enable lazy initialization for expensive providers.
        /// </summary>
        /// <remarks>
        /// When enabled, providers that require external connections (e.g., Redis, Azure)
        /// will be initialized on first use rather than at startup.
        /// </remarks>
        public bool EnableLazyInitialization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate configuration on startup.
        /// </summary>
        /// <remarks>
        /// When enabled, configuration validation will occur during service registration,
        /// causing startup to fail early if configuration is invalid.
        /// </remarks>
        public bool ValidateOnStart { get; set; } = true;
    }

    /// <summary>
    /// Resilience configuration options.
    /// </summary>
    public class ResilienceOptions
    {
        /// <summary>
        /// Gets or sets retry policy options.
        /// </summary>
        public RetryOptions? Retry { get; set; }

        /// <summary>
        /// Gets or sets circuit breaker options.
        /// </summary>
        public CircuitBreakerOptions? CircuitBreaker { get; set; }
    }

    /// <summary>
    /// Security configuration options.
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// Gets or sets secret provider options.
        /// </summary>
        public SecretProviderOptions? SecretProvider { get; set; }

        /// <summary>
        /// Gets or sets Azure Key Vault options.
        /// </summary>
        public AzureKeyVaultOptions? AzureKeyVault { get; set; }

        /// <summary>
        /// Gets or sets AWS Secrets Manager options.
        /// </summary>
        public AwsSecretsManagerOptions? AwsSecretsManager { get; set; }

        /// <summary>
        /// Gets or sets environment variable options.
        /// </summary>
        public EnvironmentVariableOptions? EnvironmentVariable { get; set; }
    }
}

