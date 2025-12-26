//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.Email.Extensions;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Extensions;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Extensions;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Observability.Extensions;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Security.Extensions;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Sms.Extensions;
using Mvp24Hours.Infrastructure.Sms.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Configuration
{
    /// <summary>
    /// Extension methods for configuring all Infrastructure subsystems in a unified way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This extension provides a centralized way to configure all Infrastructure subsystems
    /// with support for:
    /// - Configuration via IConfiguration (appsettings.json)
    /// - Environment variables
    /// - Fluent API
    /// - Validation on startup
    /// - Lazy initialization for expensive providers
    /// </para>
    /// </remarks>
    public static class InfrastructureServiceExtensions
    {
        /// <summary>
        /// Adds all Infrastructure services to the service collection with unified configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Optional configuration source (IConfiguration).</param>
        /// <param name="configure">Optional configuration action for Infrastructure options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers all Infrastructure subsystems with unified configuration.
        /// Configuration can be provided via:
        /// 1. IConfiguration (appsettings.json) - section "Infrastructure"
        /// 2. Environment variables - prefix "MVP24HOURS_INFRASTRUCTURE_"
        /// 3. Fluent API via configure action
        /// </para>
        /// <para>
        /// Configuration precedence: Fluent API > Environment Variables > IConfiguration
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Using IConfiguration
        /// services.AddMvpInfrastructure(configuration);
        /// 
        /// // Using fluent API
        /// services.AddMvpInfrastructure(builder =>
        /// {
        ///     builder.ConfigureHttp(options => { ... });
        ///     builder.ConfigureEmail(options => { ... });
        /// });
        /// 
        /// // Using both
        /// services.AddMvpInfrastructure(configuration, builder =>
        /// {
        ///     builder.ConfigureHttp(options => { ... }); // Overrides IConfiguration
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvpInfrastructure(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<IInfrastructureBuilder>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = new InfrastructureBuilder(services, configuration);
            var options = new InfrastructureOptions();

            // Load from IConfiguration if provided
            if (configuration != null)
            {
                var infrastructureSection = configuration.GetSection("Infrastructure");
                if (infrastructureSection.Exists())
                {
                    infrastructureSection.Bind(options);
                }

                // Also load from environment variables (with prefix)
                LoadFromEnvironmentVariables(options);
            }

            // Apply fluent API configuration (overrides IConfiguration)
            configure?.Invoke(builder);

            // Merge builder options into main options
            MergeOptions(options, builder.Options);

            // Register options
            services.Configure<InfrastructureOptions>(opts =>
            {
                CopyOptions(opts, options);
            });

            // Register validation if enabled
            if (options.ValidateOnStart)
            {
                services.TryAddSingleton<IValidateOptions<InfrastructureOptions>, InfrastructureOptionsValidator>();
            }

            // Register observability first (other subsystems may depend on it)
            if (options.Observability != null)
            {
                services.AddInfrastructureObservability(opts =>
                {
                    CopyObservabilityOptions(opts, options.Observability);
                });
            }

            // Register resilience options
            if (options.Resilience != null)
            {
                if (options.Resilience.Retry != null)
                {
                    services.Configure<RetryOptions>(opts =>
                    {
                        CopyRetryOptions(opts, options.Resilience.Retry);
                    });
                }

                if (options.Resilience.CircuitBreaker != null)
                {
                    services.Configure<CircuitBreakerOptions>(opts =>
                    {
                        CopyCircuitBreakerOptions(opts, options.Resilience.CircuitBreaker);
                    });
                }
            }

            // Register security/secret providers
            if (options.Security != null)
            {
                RegisterSecurityServices(services, options.Security);
            }

            // Execute builder configurations (for subsystems that use builder pattern)
            builder.ExecuteBuilderConfigurations();

            // Register other subsystems via options (if configured)
            RegisterSubsystemsFromOptions(services, options, builder);

            // Validate configuration on startup if enabled
            if (options.ValidateOnStart)
            {
                services.AddOptions<InfrastructureOptions>()
                    .ValidateOnStart();
            }

            return services;
        }

        /// <summary>
        /// Loads configuration from environment variables.
        /// </summary>
        private static void LoadFromEnvironmentVariables(InfrastructureOptions options)
        {
            // Environment variable prefix
            const string prefix = "MVP24HOURS_INFRASTRUCTURE_";

            // Load basic options
            var enableLazyInit = Environment.GetEnvironmentVariable($"{prefix}ENABLE_LAZY_INITIALIZATION");
            if (bool.TryParse(enableLazyInit, out var lazyInit))
            {
                options.EnableLazyInitialization = lazyInit;
            }

            var validateOnStart = Environment.GetEnvironmentVariable($"{prefix}VALIDATE_ON_START");
            if (bool.TryParse(validateOnStart, out var validate))
            {
                options.ValidateOnStart = validate;
            }

            // Note: Individual subsystem configuration via environment variables
            // should be handled by each subsystem's configuration loader
        }

        /// <summary>
        /// Merges builder options into main options.
        /// </summary>
        private static void MergeOptions(InfrastructureOptions target, InfrastructureOptions source)
        {
            if (source.Http != null) target.Http = source.Http;
            if (source.Email != null) target.Email = source.Email;
            if (source.Sms != null) target.Sms = source.Sms;
            if (source.FileStorage != null) target.FileStorage = source.FileStorage;
            if (source.DistributedLocking != null) target.DistributedLocking = source.DistributedLocking;
            if (source.BackgroundJobs != null) target.BackgroundJobs = source.BackgroundJobs;
            if (source.Observability != null) target.Observability = source.Observability;
            if (source.Resilience != null) target.Resilience = source.Resilience;
            if (source.Security != null) target.Security = source.Security;
            target.EnableLazyInitialization = source.EnableLazyInitialization;
            target.ValidateOnStart = source.ValidateOnStart;
        }

        /// <summary>
        /// Copies options from source to target.
        /// </summary>
        private static void CopyOptions(InfrastructureOptions target, InfrastructureOptions source)
        {
            target.Http = source.Http;
            target.Email = source.Email;
            target.Sms = source.Sms;
            target.FileStorage = source.FileStorage;
            target.DistributedLocking = source.DistributedLocking;
            target.BackgroundJobs = source.BackgroundJobs;
            target.Observability = source.Observability;
            target.Resilience = source.Resilience;
            target.Security = source.Security;
            target.EnableLazyInitialization = source.EnableLazyInitialization;
            target.ValidateOnStart = source.ValidateOnStart;
        }

        /// <summary>
        /// Copies observability options.
        /// </summary>
        private static void CopyObservabilityOptions(ObservabilityOptions target, ObservabilityOptions source)
        {
            target.EnableDetailedLogging = source.EnableDetailedLogging;
            target.EnableCorrelationIdPropagation = source.EnableCorrelationIdPropagation;
            target.EnableMetrics = source.EnableMetrics;
            target.EnableTracing = source.EnableTracing;
        }

        /// <summary>
        /// Copies retry options.
        /// </summary>
        private static void CopyRetryOptions(RetryOptions target, RetryOptions source)
        {
            target.MaxRetries = source.MaxRetries;
            target.Delay = source.Delay;
            target.MaxDelay = source.MaxDelay;
            target.BackoffMultiplier = source.BackoffMultiplier;
            target.UseJitter = source.UseJitter;
        }

        /// <summary>
        /// Copies circuit breaker options.
        /// </summary>
        private static void CopyCircuitBreakerOptions(CircuitBreakerOptions target, CircuitBreakerOptions source)
        {
            target.FailureThreshold = source.FailureThreshold;
            target.SamplingDuration = source.SamplingDuration;
            target.MinimumThroughput = source.MinimumThroughput;
            target.DurationOfBreak = source.DurationOfBreak;
        }

        /// <summary>
        /// Registers security services based on configuration.
        /// </summary>
        private static void RegisterSecurityServices(IServiceCollection services, SecurityOptions securityOptions)
        {
            // Register secret provider based on configuration
            if (securityOptions.SecretProvider != null)
            {
                // Determine which provider to use based on configuration
                if (securityOptions.AzureKeyVault != null)
                {
                    services.AddAzureKeyVaultSecretProvider(opts =>
                    {
                        CopyAzureKeyVaultOptions(opts, securityOptions.AzureKeyVault);
                    });
                }
                else if (securityOptions.AwsSecretsManager != null)
                {
                    services.AddAwsSecretsManagerProvider(opts =>
                    {
                        CopyAwsSecretsManagerOptions(opts, securityOptions.AwsSecretsManager);
                    });
                }
                else if (securityOptions.EnvironmentVariable != null)
                {
                    services.AddEnvironmentVariableSecretProvider(opts =>
                    {
                        CopyEnvironmentVariableOptions(opts, securityOptions.EnvironmentVariable);
                    });
                }
            }
        }

        /// <summary>
        /// Copies Azure Key Vault options.
        /// </summary>
        private static void CopyAzureKeyVaultOptions(AzureKeyVaultOptions target, AzureKeyVaultOptions source)
        {
            target.VaultUri = source.VaultUri;
            target.UseManagedIdentity = source.UseManagedIdentity;
            target.ClientId = source.ClientId;
            target.ClientSecret = source.ClientSecret;
            target.TenantId = source.TenantId;
        }

        /// <summary>
        /// Copies AWS Secrets Manager options.
        /// </summary>
        private static void CopyAwsSecretsManagerOptions(AwsSecretsManagerOptions target, AwsSecretsManagerOptions source)
        {
            target.Region = source.Region;
            target.AccessKeyId = source.AccessKeyId;
            target.SecretAccessKey = source.SecretAccessKey;
        }

        /// <summary>
        /// Copies environment variable options.
        /// </summary>
        private static void CopyEnvironmentVariableOptions(EnvironmentVariableOptions target, EnvironmentVariableOptions source)
        {
            target.VariableNamePrefix = source.VariableNamePrefix;
            target.CaseSensitive = source.CaseSensitive;
        }

        /// <summary>
        /// Registers subsystems from options (for subsystems that don't use builder pattern).
        /// </summary>
        private static void RegisterSubsystemsFromOptions(
            IServiceCollection services,
            InfrastructureOptions options,
            InfrastructureBuilder builder)
        {
            // Register HTTP clients if configured
            if (options.Http != null)
            {
                // HTTP clients are typically registered per-API, not globally
                // This is a placeholder - actual registration should be done per API
            }

            // Register email service if configured
            if (options.Email != null)
            {
                var emailOptions = options.Email;
                services.AddEmailService(opts =>
                {
                    CopyEmailOptions(opts, emailOptions);
                });
            }

            // Register SMS service if configured
            if (options.Sms != null)
            {
                var smsOptions = options.Sms;
                services.AddSmsService(opts =>
                {
                    CopySmsOptions(opts, smsOptions);
                });
            }

            // Register file storage if configured
            if (options.FileStorage != null)
            {
                var fileStorageOptions = options.FileStorage;
                services.AddFileStorage(opts =>
                {
                    CopyFileStorageOptions(opts, fileStorageOptions);
                });
            }
        }

        /// <summary>
        /// Copies email options (simplified - just use source directly in configuration).
        /// </summary>
        private static void CopyEmailOptions(EmailOptions target, EmailOptions source)
        {
            // Copy key properties - in practice, the options object is used directly
            if (!string.IsNullOrWhiteSpace(source.DefaultFrom))
                target.DefaultFrom = source.DefaultFrom;
            if (!string.IsNullOrWhiteSpace(source.DefaultReplyTo))
                target.DefaultReplyTo = source.DefaultReplyTo;
            if (source.MaxAttachmentSize > 0)
                target.MaxAttachmentSize = source.MaxAttachmentSize;
        }

        /// <summary>
        /// Copies SMS options (simplified - just use source directly in configuration).
        /// </summary>
        private static void CopySmsOptions(SmsOptions target, SmsOptions source)
        {
            // Copy key properties - in practice, the options object is used directly
            if (!string.IsNullOrWhiteSpace(source.DefaultSender))
                target.DefaultSender = source.DefaultSender;
            if (!string.IsNullOrWhiteSpace(source.DefaultCountryCode))
                target.DefaultCountryCode = source.DefaultCountryCode;
        }

        /// <summary>
        /// Copies file storage options (simplified - just use source directly in configuration).
        /// </summary>
        private static void CopyFileStorageOptions(FileStorageOptions target, FileStorageOptions source)
        {
            // Copy key properties - in practice, the options object is used directly
            if (!string.IsNullOrWhiteSpace(source.BasePath))
                target.BasePath = source.BasePath;
            if (source.MaxFileSize > 0)
                target.MaxFileSize = source.MaxFileSize;
            if (source.AllowedExtensions != null && source.AllowedExtensions.Any())
                target.AllowedExtensions = source.AllowedExtensions.ToList();
        }
    }
}

